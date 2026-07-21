using System.Text.Json.Nodes;
using BlueprintExplorer;

namespace BubbleprintMcpWorker.Tools;

public sealed class TypesTool : IMcpTool
{
    public string Name => "types";
    public string Description =>
        "List blueprint types and counts. Args: contains (optional), limit (default 100, max 500).";

    public object Handle(BlueprintDB db, JsonObject args)
    {
        var contains = args["contains"]?.GetValue<string>();
        var limit = Math.Clamp(args["limit"]?.GetValue<int>() ?? 100, 1, 500);

        var groups = db.BlueprintsInOrder
            .GroupBy(bp => bp.Type)
            .Where(group => string.IsNullOrWhiteSpace(contains)
                || group.Key.Contains(contains, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .Take(limit);

        return new JsonArray(groups.Select(group => (JsonNode)new JsonObject
        {
            ["type"] = group.Key,
            ["count"] = group.Count(),
        }).ToArray());
    }
}
