using System.Text.Json;
using System.Text.Json.Nodes;
using BlueprintExplorer;

namespace BubbleprintMcpWorker.Tools;

public sealed class ShapeTool : IMcpTool
{
    public string Name => "shape";
    public string Description =>
        "Inspect JSON structure for a blueprint. Args: guid or name; path (dot-separated, optional); limit (default 50).";

    public object Handle(BlueprintDB db, JsonObject args)
    {
        var blueprint = ToolHelpers.ResolveBlueprint(db, args);
        var path = args["path"]?.GetValue<string>();
        var limit = Math.Clamp(args["limit"]?.GetValue<int>() ?? 50, 1, 200);
        var node = ToolHelpers.Navigate(blueprint.EnsureObj, path);

        var result = ToolHelpers.Summary(blueprint);
        result["path"] = path ?? "";
        result["kind"] = node.ValueKind.ToString();
        result["shape"] = Describe(node, limit);
        return result;
    }

    private static JsonNode Describe(JsonElement node, int limit)
    {
        if (node.ValueKind == JsonValueKind.Object)
        {
            return new JsonArray(node.EnumerateObject().Take(limit).Select(property =>
                (JsonNode)new JsonObject
                {
                    ["name"] = property.Name,
                    ["kind"] = property.Value.ValueKind.ToString(),
                    ["count"] = ContainerCount(property.Value),
                }).ToArray());
        }

        if (node.ValueKind == JsonValueKind.Array)
        {
            return new JsonObject
            {
                ["length"] = node.GetArrayLength(),
                ["items"] = new JsonArray(node.EnumerateArray().Take(limit).Select((item, index) =>
                    (JsonNode)new JsonObject
                    {
                        ["index"] = index,
                        ["kind"] = item.ValueKind.ToString(),
                        ["type"] = item.ValueKind == JsonValueKind.Object ? item.Str("$type") : null,
                        ["count"] = ContainerCount(item),
                    }).ToArray()),
            };
        }

        var raw = node.GetRawText();
        return JsonValue.Create(raw.Length <= 500 ? raw : raw[..500] + "...")!;
    }

    private static int? ContainerCount(JsonElement node)
    {
        return node.ValueKind switch
        {
            JsonValueKind.Array => node.GetArrayLength(),
            JsonValueKind.Object => node.EnumerateObject().Count(),
            _ => null,
        };
    }
}
