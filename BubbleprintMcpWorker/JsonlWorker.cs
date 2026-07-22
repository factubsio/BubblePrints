using System.Text.Json.Nodes;
using BubbleprintMcpCore;

namespace BubbleprintMcpWorker;

public sealed class JsonlWorker
{
    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private readonly BinzSession session;
    private readonly string path;
    private readonly TextWriter output;

    public JsonlWorker(BinzSession session, string path, TextWriter output)
    {
        this.session = session;
        this.path = path;
        this.output = output;
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
                            ["game"] = session.Game,
                            ["blueprints"] = session.BlueprintCount,
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

    private JsonArray ListTools()
    {
        var result = new JsonArray();
        foreach (var tool in session.ListTools())
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

        try
        {
            Respond(id, session.Run(toolName, args));
        }
        catch (ToolResultTooLargeException ex)
        {
            Respond(id, error: ex.Message);
        }
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
