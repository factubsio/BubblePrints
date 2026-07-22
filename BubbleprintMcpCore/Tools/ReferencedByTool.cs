using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using BlueprintExplorer;

namespace BubbleprintMcpCore.Tools;

public sealed class ReferencedByTool : IMcpTool
{
    public string Name => "referenced_by";
    public string Description =>
        "List blueprints that reference one blueprint. Args: guid or name; limit (default 50, max 200); includeContext (default false).";
    public int MaxResultBytes => 256 * 1024;

    public object Handle(BlueprintDB db, JsonObject args)
    {
        var blueprint = ToolHelpers.ResolveBlueprint(db, args);
        var limit = Math.Clamp(args["limit"]?.GetValue<int>() ?? 50, 1, 200);
        var includeContext = args["includeContext"]?.GetValue<bool>() ?? false;
        var references = blueprint.BackReferences
            .Select(guid => db.Blueprints.GetValueOrDefault(guid))
            .Where(reference => reference is not null)
            .ToArray();
        var contextWatch = Stopwatch.StartNew();
        var results = new JsonArray();
        var occurrences = 0;

        foreach (var reference in references.Take(limit))
        {
            var result = ToolHelpers.Summary(reference!);
            if (includeContext)
            {
                var found = FindOccurrences(reference!, blueprint.Guid);
                occurrences += found.Count;
                result["occurrences"] = found;
            }
            results.Add(result);
        }
        contextWatch.Stop();

        return new JsonObject
        {
            ["blueprint"] = ToolHelpers.Summary(blueprint),
            ["references"] = blueprint.BackReferences.Count,
            ["returned"] = Math.Min(references.Length, limit),
            ["missing"] = blueprint.BackReferences.Count - references.Length,
            ["includeContext"] = includeContext,
            ["occurrences"] = includeContext ? occurrences : null,
            ["contextElapsedMs"] = includeContext ? contextWatch.Elapsed.TotalMilliseconds : null,
            ["results"] = results,
        };
    }

    private static JsonArray FindOccurrences(BlueprintHandle source, Guid target)
    {
        using var document = JsonDocument.Parse(source.RawUtf8);
        var occurrences = new JsonArray();
        var path = new List<PathSegment>();
        FindOccurrences(document.RootElement, target, path, null, occurrences);
        return occurrences;
    }

    private static void FindOccurrences(
        JsonElement node,
        Guid target,
        List<PathSegment> path,
        ObjectContext? enclosingObject,
        JsonArray occurrences)
    {
        switch (node.ValueKind)
        {
            case JsonValueKind.Object:
                var context = new ObjectContext(
                    RenderPath(path),
                    node.Str("$type"),
                    node.Str("name"));
                foreach (var property in node.EnumerateObject())
                {
                    path.Add(new PathSegment(property.Name, null));
                    FindOccurrences(property.Value, target, path, context, occurrences);
                    path.RemoveAt(path.Count - 1);
                }
                break;

            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in node.EnumerateArray())
                {
                    path.Add(new PathSegment(null, index++));
                    FindOccurrences(item, target, path, enclosingObject, occurrences);
                    path.RemoveAt(path.Count - 1);
                }
                break;

            case JsonValueKind.String:
                var parsed = BlueprintHandle.ParseReference(node.GetString()!);
                if (Guid.TryParse(parsed, out var guid) && guid == target)
                {
                    occurrences.Add(new JsonObject
                    {
                        ["path"] = RenderPath(path),
                        ["objectPath"] = enclosingObject?.Path,
                        ["objectType"] = enclosingObject?.Type,
                        ["objectName"] = enclosingObject?.Name,
                    });
                }
                break;
        }
    }

    private static string RenderPath(List<PathSegment> path)
    {
        if (path.Count == 0)
            return "$";

        var result = new System.Text.StringBuilder("$");
        foreach (var segment in path)
        {
            if (segment.Index is { } index)
                result.Append('[').Append(index).Append(']');
            else
                result.Append('.').Append(segment.Property);
        }
        return result.ToString();
    }

    private sealed record ObjectContext(string Path, string? Type, string? Name);
    private readonly record struct PathSegment(string? Property, int? Index);
}
