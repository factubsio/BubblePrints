using System.Text.Json;
using System.Text.Json.Nodes;
using BlueprintExplorer;

namespace BubbleprintMcpWorker;

internal static class ToolHelpers
{
    public static BlueprintHandle ResolveBlueprint(BlueprintDB db, JsonObject args)
    {
        var guidText = args["guid"]?.GetValue<string>();
        if (Guid.TryParse(guidText, out var guid) && db.Blueprints.TryGetValue(guid, out var byGuid))
            return byGuid;

        var name = args["name"]?.GetValue<string>();
        if (!string.IsNullOrWhiteSpace(name))
        {
            var exact = db.BlueprintsInOrder.FirstOrDefault(
                bp => bp.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (exact is not null)
                return exact;

            var matches = db.BlueprintsInOrder
                .Where(bp => bp.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
                .Take(2)
                .ToArray();
            if (matches.Length == 1)
                return matches[0];
            if (matches.Length > 1)
                throw new ArgumentException($"Blueprint name '{name}' is ambiguous; use a GUID.");
        }

        throw new ArgumentException("Provide a valid 'guid' or an unambiguous 'name'.");
    }

    public static JsonObject Summary(BlueprintHandle blueprint)
    {
        return new JsonObject
        {
            ["guid"] = blueprint.GuidText,
            ["name"] = blueprint.Name,
            ["type"] = blueprint.Type,
            ["namespace"] = blueprint.Namespace,
            ["rawBytes"] = blueprint.RawByteLength,
        };
    }

    public static JsonElement Navigate(JsonElement node, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return node;

        foreach (var segment in path.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            if (node.ValueKind == JsonValueKind.Object)
            {
                if (!node.TryGetProperty(segment, out node))
                    throw new ArgumentException($"Property '{segment}' was not found.");
            }
            else if (node.ValueKind == JsonValueKind.Array && int.TryParse(segment, out var index))
            {
                if (index < 0 || index >= node.GetArrayLength())
                    throw new ArgumentException($"Array index {index} is out of range.");
                node = node[index];
            }
            else
            {
                throw new ArgumentException($"Cannot navigate segment '{segment}' through {node.ValueKind}.");
            }
        }

        return node;
    }
}
