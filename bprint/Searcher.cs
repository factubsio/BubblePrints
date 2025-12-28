using BlueprintExplorer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bprint;

internal static class Searcher
{
    [Cmdlet("search")]
    internal static async Task Search(string[] args)
    {
        string game = args[1];

        BlueprintDB db = await Program.LoadDB(game);

        ScoreBuffer resultsBuffer = new();

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
}
