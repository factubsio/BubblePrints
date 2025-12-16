using BlueprintExplorer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.ObjectPool;
using System.Collections.Concurrent;

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

    private IActionResult DoFind(BlueprintDB db, MatchResultBuffer resultBuffer, string query)
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
            x.TypeForResults,
        }));

    }

    private static string? GetString(BlueprintDB db, IDisplayableElement el)
    {
        if (el.levelDelta < 0) return null;
        return el.Node.ParseAsString(null, db);
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

    [HttpGet("view/{game}/{guid}", Name = "ViewBlueprint")]
    [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Client)]

    public IActionResult GetBlueprintView(BlueprintDB db, string guid)
    {
        if (db == null || !db.Blueprints.TryGetValue(Guid.Parse(guid), out var bp))
            return NotFound();

        return Ok(bp.DisplayableElements.Select(e => new
        {
            e.key,
            e.value,
            e.levelDelta,
            e.link,
            e.isObj,
            String = GetString(db, e),
            Target = GetLinkTarget(db, e),
            typeName = e.MaybeType.Name // Assumes MaybeType name is available here, or pass e.MaybeType?.Name
        }));
    }

    [HttpGet("get/{game}/{guid}", Name = "GetBlueprint")]
    [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Client)]
    public IActionResult GetBlueprint(BlueprintDB db, string guid)
    {
        if (db == null || !db.Blueprints.TryGetValue(Guid.Parse(guid), out var blueprint))
        {
            return NotFound();
        }

        return Ok(blueprint.Raw);
    }

    [HttpGet("find/{game}", Name = "FindBlueprint")]
    public IActionResult FindBlueprint(BlueprintDB db, string game, string query)
    {
        if (query.Length > 512) return NotFound();

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        if (!_active.TryAdd(ip, true))
            return StatusCode(429, "Too many concurrent queries");

        ObjectPool<MatchResultBuffer> pool = game switch
        {
            "rt" => MatchResultsPool.RT,
            _ => throw new NotSupportedException()
        };

        var resultsBuffer = pool.Get();

        try
        {
            return DoFind(db, resultsBuffer, query);
        }
        finally
        {
            pool.Return(resultsBuffer);
            _active.TryRemove(ip, out _);
        }
    }
}

public class MatchResultBufferPolicy(BlueprintDB db) : IPooledObjectPolicy<MatchResultBuffer>
{
    public MatchResultBuffer Create()
    {
        MatchResultBuffer resultBuffer = new();
        resultBuffer.Init(db.Blueprints.Values, BlueprintHandle.MatchKeys);
        return resultBuffer;
    }

    public bool Return(MatchResultBuffer obj) { return true; }
}

public static class MatchResultsPool
{
    public static void Init()
    {
        RT = new DefaultObjectPool<MatchResultBuffer>(new MatchResultBufferPolicy(Program.DbRt), 8);
    }

    public static ObjectPool<MatchResultBuffer> RT { get; private set; } = null!;
}
