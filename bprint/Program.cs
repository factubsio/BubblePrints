using BlueprintExplorer;
using System.ComponentModel;

namespace bprint;

internal static class Program
{
    async static Task Main(string[] args)
    {
        BubblePrints.Install();
        BubblePrints.SetWrathPath(false, new TextPromptFolderChooser());
        BubblePrints.LoadAssemblies();

        var bins = new BinzManager();

        var binz = bins.Available.First(b => b.Local && b.Version.Game == "RT") ?? throw new Exception("no rt binz available");

        var loadProgress = new BlueprintDB.ConnectionProgress();
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

        var db = BlueprintDB.Instance;

        Console.WriteLine($"searching for {args[0]}");
        var matches = db.SearchBlueprints(args[0], 0, CancellationToken.None);

        Console.WriteLine("Showing top 10 (max) results");
        foreach (var match in matches.Take(10))
        {
            Console.WriteLine(match.Name);
        }
    }
}
