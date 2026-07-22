using System.Text.Json;
using System.Text.Json.Nodes;
using BubbleprintMcpCore;

namespace BubbleprintsMcpServer;

public sealed class McpServer
{
    private const string LatestProtocolVersion = "2025-11-25";

    private static readonly JsonSerializerOptions CompactJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private static readonly JsonSerializerOptions DisplayJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private static readonly IReadOnlyDictionary<string, EnvelopeTool> Tools =
        CreateTools();

    private readonly BinzSession session;
    private readonly TextReader input;
    private readonly TextWriter output;
    private readonly TextWriter diagnostics;

    public McpServer(
        TextReader input,
        TextWriter output,
        TextWriter diagnostics)
        : this(new BinzSession(), input, output, diagnostics)
    {
    }

    public McpServer(
        BinzSession session,
        TextReader input,
        TextWriter output,
        TextWriter diagnostics)
    {
        this.session = session;
        this.input = input;
        this.output = output;
        this.diagnostics = diagnostics;
    }

    public void Run()
    {
        diagnostics.WriteLine("bubbleprints-mcp: ready");

        while (input.ReadLine() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            HandleLine(line);
        }

        session.Close();
    }

    private void HandleLine(string line)
    {
        JsonNode? id = null;
        bool hasId = false;
        JsonObject request;

        try
        {
            var node = JsonNode.Parse(line);
            if (node is not JsonObject requestObject)
            {
                Error(null, -32600, "Invalid Request: request must be a JSON object.");
                return;
            }
            request = requestObject;
        }
        catch (JsonException ex)
        {
            Error(null, -32700, $"Parse error: {ex.Message}");
            return;
        }

        try
        {
            hasId = request.ContainsKey("id");
            var requestId = request["id"];
            ValidateId(requestId);
            id = requestId?.DeepClone();

            var version = RequestString(request["jsonrpc"], "jsonrpc");
            if (version != "2.0")
                throw new RpcException(-32600, "Invalid Request: jsonrpc must be '2.0'.");

            var method = RequestString(request["method"], "method");
            if (string.IsNullOrWhiteSpace(method))
                throw new RpcException(-32600, "Invalid Request: method is required.");

            if (!hasId)
            {
                HandleNotification(method);
                return;
            }

            var parameters = OptionalObject(request["params"], "params");
            var result = method switch
            {
                "initialize" => Initialize(parameters),
                "ping" => new JsonObject(),
                "tools/list" => ListTools(),
                "tools/call" => CallTool(parameters),
                _ => throw new RpcException(-32601, $"Method not found: {method}"),
            };
            Respond(id, result);
        }
        catch (JsonException ex)
        {
            if (hasId)
                Error(id, -32602, $"Invalid params: {ex.Message}");
        }
        catch (RpcException ex)
        {
            if (hasId || ex.Code is -32700 or -32600)
                Error(id, ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            diagnostics.WriteLine(ex);
            if (hasId)
                Error(id, -32603, "Internal error.");
        }
    }

    private static JsonObject Initialize(JsonObject? parameters)
    {
        var requestedVersion = OptionalString(parameters?["protocolVersion"]);
        return new JsonObject
        {
            ["protocolVersion"] = string.IsNullOrWhiteSpace(requestedVersion)
                ? LatestProtocolVersion
                : requestedVersion,
            ["capabilities"] = new JsonObject
            {
                ["tools"] = new JsonObject
                {
                    ["listChanged"] = false,
                },
            },
            ["serverInfo"] = new JsonObject
            {
                ["name"] = "bubbleprints",
                ["version"] = "0.1.0",
            },
        };
    }

    private static JsonObject ListTools()
    {
        return new JsonObject
        {
            ["tools"] = new JsonArray(
                Tools.Values.Select(tool => (JsonNode)tool.Definition.DeepClone()).ToArray()),
        };
    }

    private JsonObject CallTool(JsonObject? parameters)
    {
        var name = OptionalString(parameters?["name"])
            ?? throw new RpcException(-32602, "Invalid params: tool name is required.");
        var arguments = OptionalObject(parameters?["arguments"], "arguments")
            ?? new JsonObject();

        if (!Tools.TryGetValue(name, out var tool))
            throw new RpcException(-32602, $"Invalid params: unknown tool '{name}'.");

        try
        {
            return ToolResult(tool.Handler(session, arguments));
        }
        catch (Exception ex)
        {
            diagnostics.WriteLine($"bubbleprints-mcp: {name}: {ex}");
            return ToolResult(ex.Message, isError: true);
        }
    }

    private static void HandleNotification(string method)
    {
        if (method is "notifications/initialized" or "notifications/cancelled")
            return;

        // Unknown notifications deliberately receive no JSON-RPC response.
    }

    private static JsonObject ToolResult(object? value, bool isError = false)
    {
        string text = value switch
        {
            null => "null",
            string stringValue => stringValue,
            JsonNode node => node.ToJsonString(DisplayJson),
            _ => JsonSerializer.Serialize(value, DisplayJson),
        };

        var result = new JsonObject
        {
            ["content"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "text",
                    ["text"] = text,
                },
            },
        };
        if (isError)
            result["isError"] = true;
        return result;
    }

    private void Respond(JsonNode? id, JsonNode result)
    {
        Write(new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id,
            ["result"] = result,
        });
    }

    private void Error(JsonNode? id, int code, string message)
    {
        Write(new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id,
            ["error"] = new JsonObject
            {
                ["code"] = code,
                ["message"] = message,
            },
        });
    }

    private void Write(JsonObject response)
    {
        output.WriteLine(response.ToJsonString(CompactJson));
        output.Flush();
    }

    private static IReadOnlyDictionary<string, EnvelopeTool> CreateTools()
    {
        var tools = new[]
        {
            Define(
                "open_binz",
                "Open a BubblePrints .binz database. Omit path to use the UI's LastLoaded dataset.",
                ObjectSchema(
                    new JsonObject
                    {
                        ["path"] = StringSchema(
                            "Path to the .binz file; omit to use BubblePrints LastLoaded"),
                    }),
                (session, args) =>
                {
                    var path = args["path"]?.GetValue<string>();
                    return session.Open(path);
                }),
            Define(
                "list_binz",
                "List datasets in the shared BubblePrints cache, optionally including remote versions.",
                ObjectSchema(
                    new JsonObject
                    {
                        ["includeRemote"] = BooleanSchema(
                            "Fetch and include versions available for download"),
                    }),
                (_, args) => new BinzCatalog().List(
                    args["includeRemote"]?.GetValue<bool>() ?? false)),
            Define(
                "download_binz",
                "Download a dataset into the shared BubblePrints UI cache without opening it.",
                ObjectSchema(
                    new JsonObject
                    {
                        ["game"] = StringSchema("One of: Wrath, KM, RT, DH"),
                        ["version"] = StringSchema(
                            "Exact version from list_binz; omit for latest"),
                        ["force"] = BooleanSchema(
                            "Redownload even when this version is already cached"),
                    },
                    "game"),
                (_, args) => new BinzCatalog().Download(
                    args["game"]?.GetValue<string>()
                        ?? throw new ArgumentException("'game' is required."),
                    args["version"]?.GetValue<string>(),
                    args["force"]?.GetValue<bool>() ?? false)),
            Define(
                "close_binz",
                "Close the loaded database worker.",
                ObjectSchema(),
                (session, _) =>
                {
                    session.Close();
                    return "Closed.";
                }),
            Define(
                "list_tools",
                "List operations supported by the loaded worker.",
                ObjectSchema(),
                (session, _) => session.ListTools()),
            Define(
                "run",
                "Run an exploration or profiling operation against the loaded database.",
                ObjectSchema(
                    new JsonObject
                    {
                        ["tool"] = StringSchema("Worker tool name"),
                        ["args"] = new JsonObject
                        {
                            ["type"] = "object",
                            ["description"] = "Arguments for the worker tool",
                            ["additionalProperties"] = true,
                        },
                    },
                    "tool"),
                (session, args) =>
                {
                    var tool = args["tool"]?.GetValue<string>()
                        ?? throw new ArgumentException("'tool' is required.");
                    return session.Run(tool, args["args"] as JsonObject);
                }),
            Define(
                "status",
                "Report whether a binz worker is running.",
                ObjectSchema(),
                (session, _) => session.IsOpen
                    ? $"Worker running. Binz: {session.Path}"
                    : "No binz loaded."),
        };

        return tools.ToDictionary(tool => tool.Name, StringComparer.Ordinal);
    }

    private static EnvelopeTool Define(
        string name,
        string description,
        JsonObject inputSchema,
        Func<BinzSession, JsonObject, object?> handler)
    {
        return new EnvelopeTool(
            name,
            new JsonObject
            {
                ["name"] = name,
                ["description"] = description,
                ["inputSchema"] = inputSchema,
            },
            handler);
    }

    private static JsonObject ObjectSchema(
        JsonObject? properties = null,
        params string[] required)
    {
        var schema = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = properties ?? new JsonObject(),
            ["additionalProperties"] = false,
        };
        if (required.Length > 0)
        {
            schema["required"] = new JsonArray(
                required.Select(value => (JsonNode?)JsonValue.Create(value)).ToArray());
        }
        return schema;
    }

    private static JsonObject StringSchema(string description)
    {
        return new JsonObject
        {
            ["type"] = "string",
            ["description"] = description,
        };
    }

    private static JsonObject BooleanSchema(string description)
    {
        return new JsonObject
        {
            ["type"] = "boolean",
            ["description"] = description,
        };
    }

    private static string? OptionalString(JsonNode? node)
    {
        if (node is null)
            return null;
        if (node is JsonValue value && value.TryGetValue<string>(out var result))
            return result;
        throw new RpcException(-32602, "Invalid params: expected a string.");
    }

    private static string? RequestString(JsonNode? node, string name)
    {
        if (node is null)
            return null;
        if (node is JsonValue value && value.TryGetValue<string>(out var result))
            return result;
        throw new RpcException(-32600, $"Invalid Request: '{name}' must be a string.");
    }

    private static void ValidateId(JsonNode? id)
    {
        if (id is null)
            return;

        var kind = JsonSerializer.SerializeToElement(id).ValueKind;
        if (kind is not (JsonValueKind.String or JsonValueKind.Number))
            throw new RpcException(
                -32600,
                "Invalid Request: id must be a string, number, or null.");
    }

    private static JsonObject? OptionalObject(JsonNode? node, string name)
    {
        if (node is null)
            return null;
        return node as JsonObject
            ?? throw new RpcException(-32602, $"Invalid params: '{name}' must be an object.");
    }

    private sealed record EnvelopeTool(
        string Name,
        JsonObject Definition,
        Func<BinzSession, JsonObject, object?> Handler);

    private sealed class RpcException(int code, string message) : Exception(message)
    {
        public int Code { get; } = code;
    }
}
