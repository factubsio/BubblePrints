using System.Text.Json.Nodes;
using BlueprintExplorer;

namespace BubbleprintMcpWorker.Tools;

public sealed class ReferencedByTool : IMcpTool
{
    public string Name => "referenced_by";
    public string Description =>
        "List blueprints that reference one blueprint. Args: guid or name; limit (default 50, max 200).";

    public object Handle(BlueprintDB db, JsonObject args)
    {
        var blueprint = ToolHelpers.ResolveBlueprint(db, args);
        var limit = Math.Clamp(args["limit"]?.GetValue<int>() ?? 50, 1, 200);
        var references = blueprint.BackReferences
            .Select(guid => db.Blueprints.GetValueOrDefault(guid))
            .Where(reference => reference is not null)
            .ToArray();

        return new JsonObject
        {
            ["blueprint"] = ToolHelpers.Summary(blueprint),
            ["references"] = blueprint.BackReferences.Count,
            ["returned"] = Math.Min(references.Length, limit),
            ["missing"] = blueprint.BackReferences.Count - references.Length,
            ["results"] = new JsonArray(
                references.Take(limit).Select(reference => ToolHelpers.Summary(reference!)).ToArray()),
        };
    }
}
