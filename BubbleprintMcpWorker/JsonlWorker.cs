using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using BlueprintExplorer;

namespace BubbleprintMcpWorker;

public sealed class JsonlWorker
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private readonly BlueprintDB db;
    private readonly string path;
    private readonly TextWriter output;
    private readonly Dictionary<string, IMcpTool> tools = new(StringComparer.OrdinalIgnoreCase);

    public JsonlWorker(BlueprintDB db, string path, TextWriter output)
    {
        this.db = db;
        this.path = path;
        this.output = output;
        DiscoverTools();
    }

    public void Run()
    {
        using var reader = new StreamReader(Console.OpenStandardInput());

        while (reader.ReadLine() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            int id = -1;
            try
            {
                var request = JsonNode.Parse(line)?.AsObject()
                    ?? throw new InvalidDataException("Request must be a JSON object.");
                id = request["id"]?.GetValue<int>() ?? -1;
                var method = request["method"]?.GetValue<string>() ?? "";
                var parameters = request["params"] as JsonObject;

                switch (method)
                {
                    case "ready":
                        Respond(id, new JsonObject
                        {
                            ["status"] = "ok",
                            ["path"] = path,
                            ["game"] = db.GameName,
                            ["blueprints"] = db.BlueprintsInOrder.Count,
                        });
                        break;
                    case "list":
                        Respond(id, ListTools());
                        break;
                    case "run":
                        RunTool(id, parameters);
                        break;
                    case "close":
                        Respond(id, new JsonObject { ["status"] = "closing" });
                        return;
                    default:
                        Respond(id, error: $"Unknown method '{method}'.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Respond(id, error: ex.Message);
            }
        }
    }

    private void DiscoverTools()
    {
        foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
        {
            if (type.IsAbstract || type.IsInterface || !typeof(IMcpTool).IsAssignableFrom(type))
                continue;

            var tool = (IMcpTool)Activator.CreateInstance(type)!;
            tools[tool.Name] = tool;
        }

        Console.Error.WriteLine(
            $"bubbleprint-worker: discovered {tools.Count} tools: {string.Join(", ", tools.Keys.Order())}");
    }

    private JsonArray ListTools()
    {
        var result = new JsonArray();
        foreach (var tool in tools.Values.OrderBy(tool => tool.Name))
        {
            result.Add(new JsonObject
            {
                ["name"] = tool.Name,
                ["description"] = tool.Description,
            });
        }
        return result;
    }

    private void RunTool(int id, JsonObject? parameters)
    {
        var toolName = parameters?["tool"]?.GetValue<string>() ?? "";
        var args = parameters?["args"] as JsonObject ?? new JsonObject();

        if (!tools.TryGetValue(toolName, out var tool))
            throw new ArgumentException($"Unknown tool '{toolName}'.");

        var result = JsonSerializer.SerializeToNode(tool.Handle(db, args), JsonOptions);
        var json = result?.ToJsonString(JsonOptions) ?? "null";
        if (json.Length > tool.MaxResultBytes)
        {
            Respond(id, error: $"Result is {json.Length} bytes; limit is {tool.MaxResultBytes}. Narrow the request.");
            return;
        }

        Respond(id, result);
    }

    private void Respond(int id, JsonNode? result = null, string? error = null)
    {
        var response = new JsonObject { ["id"] = id };
        if (error is null)
            response["result"] = result;
        else
            response["error"] = error;

        output.WriteLine(response.ToJsonString(JsonOptions));
        output.Flush();
    }
}
