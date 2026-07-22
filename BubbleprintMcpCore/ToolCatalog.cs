using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using BlueprintExplorer;

namespace BubbleprintMcpCore;

public sealed class ToolCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private readonly Dictionary<string, IMcpTool> tools;

    public ToolCatalog()
    {
        tools = typeof(ToolCatalog).Assembly
            .GetTypes()
            .Where(type =>
                !type.IsAbstract
                && !type.IsInterface
                && typeof(IMcpTool).IsAssignableFrom(type))
            .Select(type => (IMcpTool)Activator.CreateInstance(type)!)
            .ToDictionary(tool => tool.Name, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<ToolDescription> List()
    {
        return tools.Values
            .OrderBy(tool => tool.Name)
            .Select(tool => new ToolDescription(tool.Name, tool.Description))
            .ToArray();
    }

    public JsonNode? Run(BlueprintDB db, string toolName, JsonObject? args)
    {
        if (!tools.TryGetValue(toolName, out var tool))
            throw new ArgumentException($"Unknown tool '{toolName}'.");

        var result = JsonSerializer.SerializeToNode(
            tool.Handle(db, args ?? new JsonObject()),
            JsonOptions);
        var json = result?.ToJsonString(JsonOptions) ?? "null";
        if (json.Length > tool.MaxResultBytes)
        {
            throw new ToolResultTooLargeException(
                $"Result is {json.Length} bytes; limit is {tool.MaxResultBytes}. Narrow the request.");
        }

        return result;
    }
}

public sealed record ToolDescription(string Name, string Description);

public sealed class ToolResultTooLargeException(string message) : Exception(message);
