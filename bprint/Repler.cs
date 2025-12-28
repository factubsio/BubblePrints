using BlueprintExplorer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bprint;

internal static class Repler
{
    [Cmdlet("repl")]
    public static async Task Repl(string[] args)
    {
        string game = args[1];

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

        while (true)
        {
            Console.Write("> ");
            var line = Console.ReadLine()?.Trim();
            if (line == null)
                break;

            if (line.Length == 0)
                continue;

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
