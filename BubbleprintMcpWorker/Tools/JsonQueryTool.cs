using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using BlueprintExplorer;

namespace BubbleprintMcpWorker.Tools;

public sealed class JsonQueryTool : IMcpTool
{
    public string Name => "json_query";
    public string Description =>
        "Extract bounded JSON from one blueprint. Args: guid or name; path (default $, supports .property, [index], [*], .*); recursive; whereKey with whereEquals or whereContains; select (property-name array); limit (default 50, max 200).";
    public int MaxResultBytes => 256 * 1024;

    public object Handle(BlueprintDB db, JsonObject args)
    {
        var blueprint = ToolHelpers.ResolveBlueprint(db, args);
        var pathText = args["path"]?.GetValue<string>() ?? "$";
        var recursive = args["recursive"]?.GetValue<bool>() ?? false;
        var whereKey = args["whereKey"]?.GetValue<string>();
        var whereEquals = args["whereEquals"];
        var whereContains = args["whereContains"]?.GetValue<string>();
        var select = ReadSelection(args["select"]);
        var limit = Math.Clamp(args["limit"]?.GetValue<int>() ?? 50, 1, 200);

        if (whereEquals is not null && whereContains is not null)
            throw new ArgumentException("Use either 'whereEquals' or 'whereContains', not both.");
        if ((whereEquals is not null || whereContains is not null) && string.IsNullOrWhiteSpace(whereKey))
            throw new ArgumentException("'whereKey' is required when filtering by value.");

        var segments = JsonSelector.Parse(pathText);
        var stopwatch = Stopwatch.StartNew();
        using var document = JsonDocument.Parse(blueprint.RawUtf8);

        IEnumerable<JsonMatch> matches =
            JsonSelector.Select(document.RootElement, segments);
        if (recursive)
            matches = matches.SelectMany(JsonSelector.DescendantsAndSelf);
        if (!string.IsNullOrWhiteSpace(whereKey))
            matches = matches.Where(match =>
                MatchesFilter(match.Value, whereKey, whereEquals, whereContains));

        var selected = matches.Take(limit + 1).ToArray();
        var truncated = selected.Length > limit;
        var returned = Math.Min(selected.Length, limit);
        var results = new JsonArray();

        foreach (var match in selected.Take(limit))
        {
            results.Add(new JsonObject
            {
                ["path"] = match.Path,
                ["value"] = Project(match.Value, select),
            });
        }
        stopwatch.Stop();

        return new JsonObject
        {
            ["blueprint"] = ToolHelpers.Summary(blueprint),
            ["path"] = pathText,
            ["recursive"] = recursive,
            ["returned"] = returned,
            ["truncated"] = truncated,
            ["elapsedMs"] = stopwatch.Elapsed.TotalMilliseconds,
            ["results"] = results,
        };
    }

    private static string[] ReadSelection(JsonNode? node)
    {
        if (node is null)
            return [];
        if (node is not JsonArray array)
            throw new ArgumentException("'select' must be an array of property names.");

        var selection = array
            .Select(item => item?.GetValue<string>()
                ?? throw new ArgumentException("'select' cannot contain null."))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (selection.Length > 32)
            throw new ArgumentException("'select' supports at most 32 properties.");
        return selection;
    }

    private static bool MatchesFilter(
        JsonElement node,
        string key,
        JsonNode? whereEquals,
        string? whereContains)
    {
        if (node.ValueKind != JsonValueKind.Object
            || !node.TryGetProperty(key, out var value))
            return false;

        if (whereEquals is null && whereContains is null)
            return true;

        if (whereContains is not null)
        {
            var text = value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : value.GetRawText();
            return text?.Contains(whereContains, StringComparison.OrdinalIgnoreCase) == true;
        }

        if (whereEquals is JsonValue jsonValue
            && jsonValue.TryGetValue<string>(out var expectedString))
        {
            return value.ValueKind == JsonValueKind.String
                && value.GetString()?.Equals(expectedString, StringComparison.OrdinalIgnoreCase) == true;
        }

        return value.GetRawText() == whereEquals!.ToJsonString();
    }

    private static JsonNode? Project(JsonElement node, string[] select)
    {
        if (select.Length == 0 || node.ValueKind != JsonValueKind.Object)
            return JsonNode.Parse(node.GetRawText());

        var result = new JsonObject();
        foreach (var propertyName in select)
        {
            result[propertyName] = node.TryGetProperty(propertyName, out var value)
                ? JsonNode.Parse(value.GetRawText())
                : null;
        }
        return result;
    }
}

internal readonly record struct JsonMatch(JsonElement Value, string Path);

internal abstract record JsonPathSegment
{
    public sealed record Property(string Name) : JsonPathSegment;
    public sealed record Index(int Value) : JsonPathSegment;
    public sealed record Wildcard : JsonPathSegment;
}

internal static class JsonSelector
{
    public static IReadOnlyList<JsonPathSegment> Parse(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("'path' cannot be empty.");

        var segments = new List<JsonPathSegment>();
        var index = path[0] == '$' ? 1 : 0;

        while (index < path.Length)
        {
            if (path[index] == '.')
            {
                index++;
                if (index >= path.Length)
                    throw InvalidPath(path, index, "Expected a property after '.'.");
                if (path[index] == '*')
                {
                    segments.Add(new JsonPathSegment.Wildcard());
                    index++;
                    continue;
                }

                var start = index;
                while (index < path.Length && path[index] is not ('.' or '['))
                    index++;
                if (start == index)
                    throw InvalidPath(path, index, "Expected a property name.");
                segments.Add(new JsonPathSegment.Property(path[start..index]));
                continue;
            }

            if (path[index] == '[')
            {
                var close = path.IndexOf(']', index + 1);
                if (close < 0)
                    throw InvalidPath(path, index, "Missing ']'.");
                var value = path[(index + 1)..close];
                if (value == "*")
                    segments.Add(new JsonPathSegment.Wildcard());
                else if (int.TryParse(value, out var arrayIndex) && arrayIndex >= 0)
                    segments.Add(new JsonPathSegment.Index(arrayIndex));
                else
                    throw InvalidPath(path, index, "Array selector must be a non-negative index or '*'.");
                index = close + 1;
                continue;
            }

            if (index == 0)
            {
                var start = index;
                while (index < path.Length && path[index] is not ('.' or '['))
                    index++;
                segments.Add(new JsonPathSegment.Property(path[start..index]));
                continue;
            }

            throw InvalidPath(path, index, $"Unexpected character '{path[index]}'.");
        }

        return segments;
    }

    public static IEnumerable<JsonMatch> Select(
        JsonElement root,
        IReadOnlyList<JsonPathSegment> segments)
    {
        IEnumerable<JsonMatch> current = [new JsonMatch(root, "$")];
        foreach (var segment in segments)
            current = Apply(current, segment);
        return current;
    }

    public static IEnumerable<JsonMatch> DescendantsAndSelf(JsonMatch root)
    {
        yield return root;

        if (root.Value.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in root.Value.EnumerateObject())
            {
                foreach (var match in DescendantsAndSelf(
                    new JsonMatch(property.Value, AppendProperty(root.Path, property.Name))))
                    yield return match;
            }
        }
        else if (root.Value.ValueKind == JsonValueKind.Array)
        {
            var index = 0;
            foreach (var item in root.Value.EnumerateArray())
            {
                foreach (var match in DescendantsAndSelf(
                    new JsonMatch(item, $"{root.Path}[{index++}]")))
                    yield return match;
            }
        }
    }

    private static IEnumerable<JsonMatch> Apply(
        IEnumerable<JsonMatch> input,
        JsonPathSegment segment)
    {
        foreach (var match in input)
        {
            switch (segment)
            {
                case JsonPathSegment.Property property
                    when match.Value.ValueKind == JsonValueKind.Object
                         && match.Value.TryGetProperty(property.Name, out var value):
                    yield return new JsonMatch(
                        value,
                        AppendProperty(match.Path, property.Name));
                    break;

                case JsonPathSegment.Index arrayIndex
                    when match.Value.ValueKind == JsonValueKind.Array
                         && arrayIndex.Value < match.Value.GetArrayLength():
                    yield return new JsonMatch(
                        match.Value[arrayIndex.Value],
                        $"{match.Path}[{arrayIndex.Value}]");
                    break;

                case JsonPathSegment.Wildcard
                    when match.Value.ValueKind == JsonValueKind.Array:
                    var index = 0;
                    foreach (var item in match.Value.EnumerateArray())
                        yield return new JsonMatch(item, $"{match.Path}[{index++}]");
                    break;

                case JsonPathSegment.Wildcard
                    when match.Value.ValueKind == JsonValueKind.Object:
                    foreach (var property in match.Value.EnumerateObject())
                    {
                        yield return new JsonMatch(
                            property.Value,
                            AppendProperty(match.Path, property.Name));
                    }
                    break;
            }
        }
    }

    private static string AppendProperty(string path, string property)
    {
        if (property.All(character =>
                char.IsLetterOrDigit(character) || character is '_' or '$'))
            return $"{path}.{property}";

        var escaped = property.Replace("\\", "\\\\").Replace("'", "\\'");
        return $"{path}['{escaped}']";
    }

    private static ArgumentException InvalidPath(string path, int index, string message) =>
        new($"Invalid JSON path '{path}' at offset {index}: {message}");
}
