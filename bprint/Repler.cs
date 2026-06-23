using BlueprintExplorer;

namespace bprint;

internal static class Repler
{
    [Cmdlet("repl")]
    public static async Task Repl(string[] args)
    {
        Dictionary<string, BlueprintDB> dbs = [];
        BlueprintDB? db = default;

        async Task MakeCurrent(string game)
        {
            game = game.ToLower();
            if (!dbs.TryGetValue(game, out db))
            {
                db = await Program.LoadDB(game);
                dbs[game] = db;
            }
        }

        ScoreBuffer resultsBuffer = new();
        List<BlueprintHandle> matches = [];
        ReadLine.HistoryEnabled = true;

        string historyPath = BubblePrints.MakeDataPath("history.txt");

        if (File.Exists(historyPath))
        {
            ReadLine.AddHistory([.. File.ReadLines(historyPath)]);
        }

        await using var historyStream = new FileStream(historyPath, FileMode.Append);
        await using var historyWr = new StreamWriter(historyStream);
        Task? flushTask = default;
        

        while (true)
        {
            var line = ReadLine.Read($"{db?.GameName ?? "_"}> ");
            if (line == null)
                break;

            line = line.Trim();

            if (line.Length == 0)
                continue;

            historyWr.WriteLine(line);
            if (flushTask != null) await flushTask;
            flushTask = historyWr.FlushAsync();

            if (line.StartsWith(".db "))
            {
                await MakeCurrent(line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[1]);
            }
            else
            {
                if (db == null)
                {
                    Console.WriteLine("Load a db with `.db game` first");
                }
                else
                {
                    if (line.StartsWith('/'))
                    {
                        matches = db.SearchBlueprints(line[1..], resultsBuffer, CancellationToken.None);

                        Console.WriteLine("Showing top 10 (max) results");
                        BlueprintHandle[] tableRes = [.. matches.Take(10)];
                        int maxNameLen = tableRes.MaxBy(x => x.Name.Length)?.Name.Length ?? 0;
                        foreach (var match in matches.Take(10))
                        {
                            Console.WriteLine($"{match.Name.PadRight(maxNameLen + 4)}{match.GuidText}");
                        }
                    }
                    else if (line.StartsWith('='))
                    {
                        matches = [.. db.BlueprintsInOrder.AsParallel().Where(bp => bp.Raw.Contains(line[1..], StringComparison.InvariantCultureIgnoreCase)).Take(10)];
                        Console.WriteLine("Showing top 10 (max) results");
                        BlueprintHandle[] tableRes = [.. matches.Take(10)];
                        int maxNameLen = tableRes.MaxBy(x => x.Name.Length)?.Name.Length ?? 0;
                        foreach (var match in matches.Take(10))
                        {
                            Console.WriteLine($"{match.Name.PadRight(maxNameLen + 4)}{match.GuidText}");
                        }
                    }
                    else if (line.StartsWith('!'))
                    {
                        if (Guid.TryParse(line[1..], out var guid) && db.Blueprints.TryGetValue(guid, out var bp))
                        {
                            Console.WriteLine(bp.Raw);
                        }
                        else
                        {
                            Console.WriteLine($"cannot find bp: {line[1..]}");
                        }
                    }
                    else if (line.StartsWith('@'))
                    {
                        if (uint.TryParse(line[1..], out var index) && index < matches.Count)
                        {
                            Console.WriteLine(matches[(int)index].Raw);
                        }
                        else
                        {
                            Console.WriteLine($"cannot find bp: {line[1..]}");
                        }
                    }
                }
            }

        }

    }
}
