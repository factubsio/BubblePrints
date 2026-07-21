using System.Diagnostics;
using System.Text.Json.Nodes;
using BlueprintExplorer;

namespace BubbleprintMcpWorker.Tools;

public sealed class ProfileTool : IMcpTool
{
    public string Name => "profile";
    public string Description =>
        "Run a query repeatedly. Args: query (required), iterations (default 5, max 20), warmup (default true), timeoutMs (optional).";

    public object Handle(BlueprintDB db, JsonObject args)
    {
        var query = args["query"]?.GetValue<string>()
            ?? throw new ArgumentException("'query' is required.");
        var iterations = Math.Clamp(args["iterations"]?.GetValue<int>() ?? 5, 1, 20);
        var warmup = args["warmup"]?.GetValue<bool>() ?? true;
        var oldTimeout = BlueprintDB.MAX_TREE_QUERY_RUNTIME_MS;

        if (args["timeoutMs"] is JsonValue timeoutNode)
            BlueprintDB.MAX_TREE_QUERY_RUNTIME_MS = Math.Max(1, timeoutNode.GetValue<int>());

        try
        {
            if (warmup)
                db.SearchBlueprints(query, new ScoreBuffer(), CancellationToken.None);

            var elapsed = new double[iterations];
            var matches = new int[iterations];
            var rootTypeCandidates = new int[iterations];
            var rootTypePrefilterElapsed = new double[iterations];
            var typePresenceCandidates = new int[iterations];
            var typePresencePrefilterElapsed = new double[iterations];
            var propertyKeyCandidates = new int[iterations];
            var propertyKeyPrefilterElapsed = new double[iterations];
            var metadataPrefilterElapsed = new double[iterations];
            var textFilterElapsed = new double[iterations];
            var treeCandidates = new int[iterations];
            var treeCandidatesEvaluated = new int[iterations];
            var treeElapsed = new double[iterations];
            var truncated = new bool[iterations];
            for (int i = 0; i < iterations; i++)
            {
                var diagnostics = new BlueprintDB.SearchDiagnostics();
                var stopwatch = Stopwatch.StartNew();
                matches[i] = db.SearchBlueprints(
                    query,
                    new ScoreBuffer(),
                    CancellationToken.None,
                    diagnostics).Count;
                stopwatch.Stop();
                elapsed[i] = stopwatch.Elapsed.TotalMilliseconds;
                rootTypeCandidates[i] = diagnostics.RootTypeCandidates;
                rootTypePrefilterElapsed[i] = diagnostics.RootTypePrefilterElapsedMs;
                typePresenceCandidates[i] = diagnostics.TypePresenceCandidates;
                typePresencePrefilterElapsed[i] = diagnostics.TypePresencePrefilterElapsedMs;
                propertyKeyCandidates[i] = diagnostics.PropertyKeyCandidates;
                propertyKeyPrefilterElapsed[i] = diagnostics.PropertyKeyPrefilterElapsedMs;
                metadataPrefilterElapsed[i] = diagnostics.MetadataPrefilterElapsedMs;
                textFilterElapsed[i] = diagnostics.TextFilterElapsedMs;
                treeCandidates[i] = diagnostics.TreeCandidates;
                treeCandidatesEvaluated[i] = diagnostics.TreeCandidatesEvaluated;
                treeElapsed[i] = diagnostics.TreeElapsedMs;
                truncated[i] = diagnostics.TreeTruncated;
            }

            var sorted = elapsed.Order().ToArray();
            return new JsonObject
            {
                ["query"] = query,
                ["iterations"] = iterations,
                ["warmup"] = warmup,
                ["timeoutMs"] = BlueprintDB.MAX_TREE_QUERY_RUNTIME_MS,
                ["stableMatches"] = matches.Distinct().Count() == 1,
                ["matches"] = new JsonArray(matches.Select(value => (JsonNode)JsonValue.Create(value)!).ToArray()),
                ["elapsedMs"] = new JsonArray(elapsed.Select(value =>
                    (JsonNode)JsonValue.Create(Math.Round(value, 3))!).ToArray()),
                ["rootTypeCandidates"] = new JsonArray(rootTypeCandidates.Select(value =>
                    (JsonNode)JsonValue.Create(value)!).ToArray()),
                ["rootTypePrefilterMs"] = new JsonArray(rootTypePrefilterElapsed.Select(value =>
                    (JsonNode)JsonValue.Create(Math.Round(value, 3))!).ToArray()),
                ["typePresenceCandidates"] = new JsonArray(typePresenceCandidates.Select(value =>
                    (JsonNode)JsonValue.Create(value)!).ToArray()),
                ["typePresencePrefilterMs"] = new JsonArray(typePresencePrefilterElapsed.Select(value =>
                    (JsonNode)JsonValue.Create(Math.Round(value, 3))!).ToArray()),
                ["propertyKeyCandidates"] = new JsonArray(propertyKeyCandidates.Select(value =>
                    (JsonNode)JsonValue.Create(value)!).ToArray()),
                ["propertyKeyPrefilterMs"] = new JsonArray(propertyKeyPrefilterElapsed.Select(value =>
                    (JsonNode)JsonValue.Create(Math.Round(value, 3))!).ToArray()),
                ["metadataPrefilterMs"] = new JsonArray(metadataPrefilterElapsed.Select(value =>
                    (JsonNode)JsonValue.Create(Math.Round(value, 3))!).ToArray()),
                ["textFilterMs"] = new JsonArray(textFilterElapsed.Select(value =>
                    (JsonNode)JsonValue.Create(Math.Round(value, 3))!).ToArray()),
                ["treeCandidates"] = new JsonArray(treeCandidates.Select(value =>
                    (JsonNode)JsonValue.Create(value)!).ToArray()),
                ["treeCandidatesEvaluated"] = new JsonArray(treeCandidatesEvaluated.Select(value =>
                    (JsonNode)JsonValue.Create(value)!).ToArray()),
                ["treeElapsedMs"] = new JsonArray(treeElapsed.Select(value =>
                    (JsonNode)JsonValue.Create(Math.Round(value, 3))!).ToArray()),
                ["truncated"] = new JsonArray(truncated.Select(value =>
                    (JsonNode)JsonValue.Create(value)!).ToArray()),
                ["minMs"] = sorted[0],
                ["medianMs"] = sorted[sorted.Length / 2],
                ["maxMs"] = sorted[^1],
                ["averageMs"] = elapsed.Average(),
            };
        }
        finally
        {
            BlueprintDB.MAX_TREE_QUERY_RUNTIME_MS = oldTimeout;
        }
    }
}
