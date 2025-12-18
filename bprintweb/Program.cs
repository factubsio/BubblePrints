
using BlueprintExplorer;
using bprintweb.Controllers;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.ObjectPool;
using System.Data.Common;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Text.Json;

namespace bprintweb;

public record class GameData(BlueprintDB DB)
{
    public MemoryMappedViewAccessor Data { get; set; } = null!;
    public ObjectPool<MatchResultBuffer> ResultBuffer { get; set; } = null!;
}

public static class Program
{
    public static readonly Dictionary<string, GameData> games = new()
    {
        ["rt"] = new(new()),
        ["wrath"] = new(new()),
        ["km"] = new(new()),
    };

    public async static Task Main(string[] args)
    {
        BubblePrints.Install();

        var bins = new BinzManager();

        foreach (var (gameName, gameData) in games)
        {
            Console.WriteLine($"Loading DB: ${gameName}");
            var binz = bins.Local.First(b => b.Version.Game.Equals(gameName, StringComparison.InvariantCultureIgnoreCase)) ?? throw new Exception("no rt binz available");

            var loadProgress = new ConnectionProgress();

            var initialize = Task.Run(() => gameData.DB.TryConnect(loadProgress, binz.Path));
            var idle = Task.Run(() =>
            {
                while (!initialize.IsCompleted)
                {
                    Thread.Sleep(200);
                    Console.Write(".");
                }
            });
            await initialize;
            await idle;
            Console.WriteLine();

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);

            Console.WriteLine("Caching raw json to disk");
            var tmr = Stopwatch.StartNew();
            var handle = await FlushAllRaw(gameData.DB);
            gameData.ResultBuffer = new DefaultObjectPool<MatchResultBuffer>(new MatchResultBufferPolicy(gameData.DB), 8);

            Console.WriteLine($"Caching complete (in {tmr.Elapsed.TotalSeconds:F1}s)");
            gameData.Data = handle.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);
        }

        Console.WriteLine("About to launch");

        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();


        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();


        builder.Services.AddHttpLogging(logging =>
        {
            logging.LoggingFields = HttpLoggingFields.RequestPropertiesAndHeaders;
            logging.RequestHeaders.Add("User-Agent");
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        HashSet<string> Passthru = [
            "/",
            "/dialog.html",
            "/index.html",
            "/help.html",
            "/theme.css",
            "/style.css",
            "/help.css",
            "/js/app.js",
            "/js/theme.js",
        ];

        app.Use(async (context, next) =>
        {
            if (Passthru.Contains(context.Request.Path.Value!))
            {
                await next(context);
                return;
            }

            string? game = null;
            const string baseDomain = "bubbleprints.dev";
            var request = context.Request;

            var host = request.Host.Host;

            // Check for subdomain
            if (host.Length > baseDomain.Length + 1 &&
                host[host.Length - baseDomain.Length - 1] == '.' &&
                host.EndsWith(baseDomain, StringComparison.OrdinalIgnoreCase))
            {
                var gamePart = host.AsSpan(0, host.Length - (baseDomain.Length + 1));
                if (!gamePart.IsEmpty && gamePart.IndexOf('.') == -1)
                {
                    game = gamePart.ToString();
                }
            }

            // If not found, check path
            if (game == null && request.Path.HasValue && request.Path.Value.Length > 1)
            {
                var pathSpan = request.Path.Value.AsSpan(1); // Skip leading '/'
                var slashIndex = pathSpan.IndexOf('/');

                var gamePart = slashIndex == -1 ? pathSpan : pathSpan[..slashIndex];
                context.Request.PathBase = new($"/{gamePart}");

                if (!gamePart.IsEmpty)
                {
                    game = gamePart.ToString();
                }

                if (slashIndex == -1)
                {
                    context.Request.Path = new("/");
                }
                else
                {
                    context.Request.Path = new($"{pathSpan[slashIndex..]}");
                }
            }

            if (game == null || !games.TryGetValue(game, out var gameData))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }
            context.Items["game"] = gameData;

            await next(context);
        });

        app.Use(async (context, next) =>
        {
            if (context.Request.Headers.UserAgent.Any(x => x?.Contains("Discordbot/2.0") == true))
            {
                var ok = await BlueprintDbController.SendEmbedTags(context);
                if (!ok)
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                }
                return;
            }

            await next(context);
        });

        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();

        //app.UseHttpLogging();

        app.MapControllers();
        app.MapFallbackToFile("index.html");

        app.Run();
    }

    private static async Task<MemoryMappedFile> FlushAllRaw(BlueprintDB db)
    {
        string tempFilePath = Path.GetTempFileName();
        await using (FileStream fs = new(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            foreach (var bp in db.BlueprintsInOrder)
            {
                if (bp.Raw == null) continue;

                byte[] encoded = Encoding.UTF8.GetBytes(bp.Raw);
                long offset = fs.Position;
                await fs.WriteAsync(encoded);

                bp.Type = TryIntern(bp.Type);
                bp.TypeName = TryIntern(bp.TypeName);
                bp.TypeNameLower = TryIntern(bp.TypeNameLower);
                bp.Namespace = TryIntern(bp.Namespace);
                bp.NamespaceLower = TryIntern(bp.NamespaceLower);

                bp.UserData = new StringMetadata(offset, encoded.Length);
                bp.Raw = null;
                bp.obj = default;
                bp.Parsed = false;
            }

            Console.WriteLine($"raw cache size: {fs.Position}");
        }

        FileStream closer = new(tempFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.DeleteOnClose);
        closeOnDelete.Add(closer);
        return MemoryMappedFile.CreateFromFile(closer, null, 0, MemoryMappedFileAccess.Read, HandleInheritability.None, false);
    }

    private static string? TryIntern(string? str)
    {
        return str == null ? null : string.Intern(str);
    }

    private static readonly List<FileStream> closeOnDelete = [];
}

//public class GameDbBinder : IModelBinder
//{
//    public Task BindModelAsync(ModelBindingContext context)
//    {
//        var game = context.ActionContext.RouteData.Values["game"]?.ToString();

//        // Your existing switch logic
//        var db = game switch
//        {
//            "rt" => Program.DbRt,
//            _ => null
//        };

//        context.Result = ModelBindingResult.Success(db);
//        return Task.CompletedTask;
//    }
//}

//public class GameDbProvider : IModelBinderProvider
//{
//    public IModelBinder? GetBinder(ModelBinderProviderContext context)
//    {
//        return context.Metadata.ModelType == typeof(BlueprintDB)
//            ? new GameDbBinder()
//            : null;
//    }
//}
public record class StringMetadata(long Offset, int Length);

