using BlueprintExplorer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
internal static class BubbleModDbGen
{
    [Cmdlet("bubblemod.gen-db")]
    internal static async Task Search(string[] args)
    {
        string game = args[1];

        HashSet<string> IgnoredTypes = [
            "BlueprintCue",
            "BlueprintAnswer",
            "CommandDelay",
            "CommandAction",
            "Gate",
            "CommandUnitPlayCutsceneAnimation",
        ];

        BlueprintDB db = await Program.LoadDB(game);

        var byType = db.BlueprintsInOrder
                .Where(bp => IncludeNamespace(bp.Namespace))
                .ToLookup(x => x.TypeName)
                .ToDictionary(
                    x => x.Key,
                    x => x.ToDictionary(
                           bp => UniqueName(bp.TypeName, bp.Name),
                           bp => bp.GuidText));

        var json = JsonSerializer.Serialize(byType, jsonOpts);
        await using FileStream file = new(@"C:\Users\worce\source\repos\bubblemodding\bubblemod-vscode\src\wrath_db.ts", FileMode.Create);
        await using StreamWriter wr = new(file);

        wr.Write("export const wrathDb: Record<string, Record<string, string>> = ");
        wr.Write(json);
        wr.WriteLine(";");

    }

    private static readonly JsonSerializerOptions jsonOpts = new()
    {
        WriteIndented = true,
    };

    private static readonly HashSet<string> ExcludeNamespaces = [
        "Kingmaker.AreaLogic.Cutscenes",
        "Kingmaker.DialogSystem.Blueprints",
    ];

    private static Dictionary<string, Dictionary<string, int>> uniqueNamesByType = [];

    private static string UniqueName(string type, string name)
    {
        if (!uniqueNamesByType.TryGetValue(type, out var uniq))
        {
            uniq = [];
            uniqueNamesByType[type] = uniq;
        }

        int count = uniq.GetValueOrDefault(name, 1);
        uniq[name] = count + 1;
        if (count == 1) return name;
        return $"{name}/{count}";
    }

    private static bool IncludeNamespace(string ns) => !ExcludeNamespaces.Any(ban => ns.StartsWith(ban));
}
