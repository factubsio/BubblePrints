using BlueprintExplorer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.ObjectPool;
using Microsoft.OpenApi.Extensions;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;

namespace bprintweb.Controllers;

[ApiController]
[Route("bp")]
public class BlueprintDbController : ControllerBase
{
    private static readonly ConcurrentDictionary<string, bool> _active = new();

    private readonly ILogger<BlueprintDbController> _logger;

    public BlueprintDbController(ILogger<BlueprintDbController> logger)
    {
        _logger = logger;
    }

    private IActionResult DoFind(BlueprintDB db, ScoreBuffer resultBuffer, string query)
    {
        if (db == null)
        {
            return StatusCode(StatusCodes.Status404NotFound);
        }

        var results = db.SearchBlueprints(query, resultBuffer, CancellationToken.None);

        return Ok(results.Take(20).Select(x => new
        {
            x.Name,
            x.Namespace,
            x.GuidText,
            x.TypeName,
        }));

    }

    private static string? GetString(BlueprintDB db, IDisplayableElement el)
    {
        if (el.levelDelta < 0) return null;
        return el.Node.ParseAsString(db);
    }

    private static string GetLinkTarget(BlueprintDB db, IDisplayableElement el)
    {
        if (string.IsNullOrEmpty(el.link)) return "";
        if (db.Blueprints.TryGetValue(Guid.Parse(el.link), out var target))
        {
            return target.Name;
        }
        else
        {
            return "stale";
        }
    }

    private static readonly ConcurrentQueue<BlueprintHandle> _lruQueue = new();
    private const int MaxResidentBlueprints = 1000;
    private const int MaxEvictionAttempts = 10;
    private static int _residentCount = 0;

    /// <summary>
    /// Atomically makes a blueprint resident, returning the raw json value and
    /// the root of the parsed tree
    /// </summary>
    /// <param name="bp"></param>
    /// <returns></returns>
    private static (string Raw, JsonElement root) MakeBlueprintResident(GameData gameData, BlueprintHandle bp)
    {
        string raw;
        JsonElement root;

        lock (bp.UserData)
        {
            if (bp.Raw == null)
            {
                var meta = (bp.UserData as StringMetadata)!;
                byte[] tmp = ArrayPool<byte>.Shared.Rent(meta.Length);
                try
                {
                    gameData.Data.ReadArray<byte>(meta.Offset, tmp, 0, meta.Length);
                    bp.Raw = Encoding.UTF8.GetString(tmp, 0, meta.Length);
                    bp.EnsureParsed();

                    _lruQueue.Enqueue(bp);
                    Interlocked.Increment(ref _residentCount);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(tmp);
                }
            }
            raw = bp.Raw;
            root = bp.obj;
        }

        int evictionAttempt = 0;

        // Eviction logic
        while (_residentCount > MaxResidentBlueprints && evictionAttempt++ < MaxEvictionAttempts)
        {
            if (_lruQueue.TryDequeue(out var toEvict))
            {
                // Use TryEnter to avoid deadlock if another thread is currently making this specific BP resident
                if (Monitor.TryEnter(toEvict.UserData))
                {
                    try
                    {
                        if (toEvict.Raw != null)
                        {
                            toEvict.Raw = null;
                            toEvict.obj = default;
                            toEvict.Parsed = false;
                            Interlocked.Decrement(ref _residentCount);
                        }
                    }
                    finally
                    {
                        Monitor.Exit(toEvict.UserData);
                    }
                }
                else
                {
                    // If we couldn't get the lock, put it back to try later or let another call handle it
                    _lruQueue.Enqueue(toEvict);
                    break;
                }
            }
            else
            {
                break;
            }
        }

        return (raw, root);

    }

    [HttpGet("view/{guid}", Name = "ViewBlueprint")]
    [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Client)]

    public IActionResult GetBlueprintView(string guid)
    {
        var gameData = (HttpContext.Items["game"] as GameData)!;
        var db = gameData.DB;
        if (!Guid.TryParse(guid, out var guidObj) || !db.Blueprints.TryGetValue(guidObj, out var blueprint))
        {
            return NotFound();
        }

        var (_, root) = MakeBlueprintResident(gameData, blueprint);

        return Ok(
            new
            {
                Blueprint = BlueprintHandle.Visit(db, root, blueprint.Name).Select(e => new
                {
                    e.key,
                    e.value,
                    e.levelDelta,
                    e.link,
                    e.isObj,
                    String = GetString(db, e),
                    Target = GetLinkTarget(db, e),
                    typeName = e.MaybeType.Name
                }),
                References = blueprint.BackReferences.Select(x => new
                {
                    Id = x,
                    Name = db.Blueprints.GetValueOrDefault(x)?.Name ?? "-"
                })
            });
    }

    [HttpGet("get/{guid}", Name = "GetBlueprint")]
    [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Client)]
    public IActionResult GetBlueprint(string guid, bool? strings)
    {
        var gameData = (HttpContext.Items["game"] as GameData)!;
        var db = gameData.DB;
        if (db == null || !Guid.TryParse(guid, out var guidObj) || !db.Blueprints.TryGetValue(guidObj, out var blueprint))
        {
            return NotFound();
        }

        var (raw, root) = MakeBlueprintResident(gameData, blueprint);

        if (strings != true)
        {
            Response.Headers.Append("BP-Name", blueprint.Name);
            return Content(raw, "application/json");
        }
        else
        {
            Dictionary<string, string> strs = [];

            FindStrings(db, root, strs);

            return Ok(new
            {
                root,
                meta = new
                {
                    name = blueprint.Name,
                },
                strs,
            });
        }
    }

    private static void FindStrings(BlueprintDB db, JsonElement root, Dictionary<string, string> strs)
    {
        var (k, v) = root.ParseAsStringWithKey(db);
        if (!string.IsNullOrEmpty(k) && !string.IsNullOrEmpty(v))
        {
            strs[k] = v;
        }
        else if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in root.EnumerateObject())
            {
                FindStrings(db, prop.Value, strs);
            }
        }
        else if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var val in root.EnumerateArray())
            {
                FindStrings(db, val, strs);
            }
        }
    }

    [HttpGet("find", Name = "FindBlueprint")]
    public IActionResult FindBlueprint(string query)
    {
        if (query.Length > 512) return NotFound();
        var gameData = (HttpContext.Items["game"] as GameData)!;
        var db = gameData.DB;

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        if (!_active.TryAdd(ip, true))
            return StatusCode(429, "Too many concurrent queries");

        var resultsBuffer = ResultsPool.Get();

        try
        {
            return DoFind(db, resultsBuffer, query);
        }
        finally
        {
            ResultsPool.Return(resultsBuffer);
            _active.TryRemove(ip, out _);
        }
    }

    private static BlueprintHandle? GetBlueprint(HttpContext context)
    {
        var gameData = (context.Items["game"] as GameData)!;
        var db = gameData.DB;
        ReadOnlySpan<char> path = context.Request.Path.Value.AsSpan();

        if (path.IsEmpty) return null;
        if (path[0] == '/') path = path[1..];

        int slashIndex = path.IndexOf('/');
        if (slashIndex == -1) return null;

        ReadOnlySpan<char> gameName = path[..slashIndex];
        ReadOnlySpan<char> guidSpan = path[(slashIndex + 1)..];

        if (db != null && Guid.TryParse(guidSpan, out Guid guid) && db.Blueprints.TryGetValue(guid, out var blueprint))
        {
            return blueprint;
        }

        return null;
    }

    internal static async ValueTask<bool> SendEmbedTags(HttpContext context)
    {
        return false;

        if (context.Request.Path.Value == null) return false;
        var blueprint = GetBlueprint(context);
        if (blueprint == null) return false;

        context.Response.ContentType = "text/html";

        //Zero alloc path, needs to be tested locally...
        //var writer = context.Response.BodyWriter;
        //var encoding = Encoding.UTF8;

        //void WriteString(string s)
        //{
        //    int byteCount = encoding.GetByteCount(s);
        //    encoding.GetBytes(s, writer.GetSpan(byteCount));
        //    writer.Advance(byteCount);
        //}

        //writer.Write("<html><head><meta property=\"og:title\" content=\""u8);
        //WriteString(blueprint.Name);
        //writer.Write("\" /><meta property=\"og:description\" content=\""u8);
        //WriteString(blueprint.Name);
        //writer.Write(" ("u8);
        //WriteString(blueprint.Type);
        //writer.Write(")\" /><meta property=\"og:type\" content=\"website\" /></head></html>"u8);

        //await writer.FlushAsync();
        //return true;

        var description = $"{blueprint.Name} ({blueprint.TypeName})";

        await context.Response.WriteAsync($"""
            <html><head>
            <meta property="og:title" content="{blueprint.Name}" />
            <meta property="og:description" content="{HttpUtility.HtmlAttributeEncode(description)}" />
            <meta property="og:type" content="website" />
            </head></html>
            """);
        return true;
    }
    private static readonly ObjectPool<ScoreBuffer> ResultsPool = new DefaultObjectPool<ScoreBuffer>(new MatchResultBufferPolicy());

}

public class MatchResultBufferPolicy() : IPooledObjectPolicy<ScoreBuffer>
{
    public ScoreBuffer Create()
    {
        return new();
    }

    public bool Return(ScoreBuffer obj) { return true; }
}


