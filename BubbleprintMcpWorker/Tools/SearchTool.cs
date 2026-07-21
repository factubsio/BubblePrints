using System.Diagnostics;
using System.Text.Json.Nodes;
using BlueprintExplorer;

namespace BubbleprintMcpWorker.Tools;

public sealed class SearchTool : IMcpTool
{
    public string Name => "search";
    public string Description =>
        "Search blueprints. Args: query (required), limit (default 20, max 100). Returns elapsed time and result summaries.";

    public object Handle(BlueprintDB db, JsonObject args)
    {
        var query = args["query"]?.GetValue<string>()
            ?? throw new ArgumentException("'query' is required.");
        var limit = Math.Clamp(args["limit"]?.GetValue<int>() ?? 20, 1, 100);

        var stopwatch = Stopwatch.StartNew();
        var diagnostics = new BlueprintDB.SearchDiagnostics();
        var matches = db.SearchBlueprints(
            query,
            new ScoreBuffer(),
            CancellationToken.None,
            diagnostics);
        stopwatch.Stop();

        return new JsonObject
        {
            ["query"] = query,
            ["elapsedMs"] = stopwatch.Elapsed.TotalMilliseconds,
            ["matches"] = matches.Count,
            ["returned"] = Math.Min(matches.Count, limit),
            ["rootTypeCandidates"] = diagnostics.RootTypeCandidates,
            ["rootTypePrefilterMs"] = diagnostics.RootTypePrefilterElapsedMs,
            ["typePresenceCandidates"] = diagnostics.TypePresenceCandidates,
            ["typePresencePrefilterMs"] = diagnostics.TypePresencePrefilterElapsedMs,
            ["propertyKeyCandidates"] = diagnostics.PropertyKeyCandidates,
            ["propertyKeyPrefilterMs"] = diagnostics.PropertyKeyPrefilterElapsedMs,
            ["metadataPrefilterMs"] = diagnostics.MetadataPrefilterElapsedMs,
            ["textFilterMs"] = diagnostics.TextFilterElapsedMs,
            ["treeCandidates"] = diagnostics.TreeCandidates,
            ["treeCandidatesEvaluated"] = diagnostics.TreeCandidatesEvaluated,
            ["treeElapsedMs"] = diagnostics.TreeElapsedMs,
            ["truncated"] = diagnostics.TreeTruncated,
            ["results"] = new JsonArray(matches.Take(limit).Select(ToolHelpers.Summary).ToArray()),
        };
    }
}
