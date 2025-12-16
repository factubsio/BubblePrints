
using BlueprintExplorer;
using bprintweb.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.FileProviders;

namespace bprintweb;

public static class Program
{
    public static BlueprintDB DbRt { get; private set; } = null!;

    public async static Task Main(string[] args)
    {
        BubblePrints.Install();

        var bins = new BinzManager();

        var binz = bins.Available.First(b => b.Local && b.Version.Game == "RT") ?? throw new Exception("no rt binz available");

        var loadProgress = new ConnectionProgress();
        var initialize = Task.Run(() => BlueprintDB.Instance.TryConnect(loadProgress, binz.Path));
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
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);

        DbRt = BlueprintDB.Instance;

        MatchResultsPool.Init();

        //Console.WriteLine($"searching for {args[0]}");
        //var matches = db.SearchBlueprints(args[0], 0, CancellationToken.None);

        //Console.WriteLine("Showing top 10 (max) results");
        //foreach (var match in matches.Take(10))
        //{
        //    Console.WriteLine(match.Name);
        //}


        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddControllers(options =>
        {
            options.ModelBinderProviders.Insert(0, new GameDbProvider());
        });

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseStaticFiles();

        app.UseAuthorization();


        app.MapControllers();
        app.MapFallbackToFile("index.html");

        app.Run();
    }
}

public class GameDbBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext context)
    {
        var game = context.ActionContext.RouteData.Values["game"]?.ToString();

        // Your existing switch logic
        var db = game switch
        {
            "rt" => Program.DbRt,
            _ => null
        };

        context.Result = ModelBindingResult.Success(db);
        return Task.CompletedTask;
    }
}

public class GameDbProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        return context.Metadata.ModelType == typeof(BlueprintDB)
            ? new GameDbBinder()
            : null;
    }
}
