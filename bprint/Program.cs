using BlueprintExplorer;
using static BlueprintExplorer.BlueprintDB;
using BinzFactory;

namespace bprint;

public static class Program
{
    public static async Task Main(string[] args)
    {
        BubblePrints.Install();

        switch (args[0])
        {
            case "import":
                await Import(args);
                break;
            case "search":
                await Search(args);
                break;
            default:
                Console.Error.WriteLine("Command not found");
                Console.Error.WriteLine("     import gamePath [overwrite|increment]");
                Console.Error.WriteLine("     search game term");
                break;
        }

    }

    public static async Task Search(string[] args)
    {
        var bins = new BinzManager();

        string game = args[1];

        var binz = bins.Available.First(b => b.Local && b.Version.Game == game) ?? throw new Exception($"no {game} binz available");

        BlueprintDB db = new();

        var loadProgress = new ConnectionProgress();
        var initialize = Task.Run(() => db.TryConnect(loadProgress, binz.Path));
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

        MatchResultBuffer resultsBuffer = new();
        resultsBuffer.Init(db.Blueprints.Values, BlueprintHandle.MatchKeys);

        string query = args[2];
        Console.WriteLine($"searching for {game}:{query}");
        var matches = db.SearchBlueprints(query, resultsBuffer, CancellationToken.None);

        Console.WriteLine("Showing top 10 (max) results");
        foreach (var match in matches.Take(10))
        {
            Console.WriteLine(match.Name);
            Console.WriteLine($"ns: {match.Namespace}");
        }
    }


    public static async Task Import(string[] args)
    {
        string gamePath = args[1];

        var version = BinzImporter.GetGameVersion(gamePath);
        var gameName = BinzImporter.GetGameName(gamePath);
        var filename = BlueprintDB.FileNameFor(version, gameName);
        while (File.Exists(Path.Join(BubblePrints.DataPath, filename)))
        {
            version.Bubble++;
            filename = BlueprintDB.FileNameFor(version, gameName);
        }

        Console.WriteLine(version);

        if (version.Bubble > 0)
        {

            string? handleOverwrite = args.Length > 2 ? args[2] : null;
            string? promptResult = handleOverwrite switch
            {
                "overwrite" => "0",
                "increment" => "1",
                _ => "2",
            };

            if (promptResult == null)
            {

                Console.WriteLine("Do you want to overwrite this file?\n" +
                    $"    0: Overwrite                         - {version with { Bubble = version.Bubble - 1 }}\n" +
                    $"    1: Extract but increment the version - {version}\n" +
                    $"    2: Do nothing");

                promptResult = Console.ReadLine();
            }

            if (promptResult == null) return;

            if (promptResult == "0")
            {
                version.Bubble--;
                filename = BlueprintDB.FileNameFor(version, gameName);
            }
            else if (promptResult != "1")
            {
                return;
            }
        }

        try
        {
            ConnectionProgress progress = new();
            BPFile.BPWriter.Verbose = false;
            var path = Path.Join(BubblePrints.DataPath, filename);
            Console.WriteLine($"Writing binz to: {path}");
            var extract = Task.Run(() => BinzImporter.Import(progress, gamePath, path, version));

            var (left, top) = Console.GetCursorPosition();
            const int barWidth = 60;
            Console.WriteLine();

            var idle = Task.Run(() =>
            {
                while (!extract.IsCompleted)
                {
                    Thread.Sleep(500);
                    var (saveLeft, saveTop) = Console.GetCursorPosition();
                    Console.SetCursorPosition(left, top);
                    Console.Write("\x1b[2K[");
                    if (progress.EstimatedTotal == 0)
                    {
                        for (int i = 0; i < barWidth; i++)
                            Console.Write(" ");
                    }
                    else
                    {
                        int activeBar = (int)(barWidth * ((float)progress.Current / progress.EstimatedTotal));
                        for (int i = 0; i < barWidth; i++)
                            Console.Write(i < activeBar ? "#" : " ");
                    }
                    Console.WriteLine($"] {progress.Phase ?? "-"}");
                    Console.SetCursorPosition(saveLeft, saveTop);
                }
            });
            await extract;
            await idle;
            Binz binz = new()
            {
                Local = true,
                Version = new()
                {
                    Version = version,
                    Game = gameName,
                },
                Path = path,
                Source = "local",
            };
            //binMan.Available.Add(binz);
            //binMan.ByVersion[binz.Version] = binz;
            //ShowMain(binz);
        }
        catch (Exception)
        {
            //MessageBox.Show("Error extracting blueprints", ex.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);
            throw;
        }


    }
}
