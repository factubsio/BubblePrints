using System.Text.Json.Nodes;
using Xunit;

namespace BubbleprintsMcpServer.Tests;

public sealed class McpServerTests
{
    [Fact]
    public void HandlesInitializationNotificationAndPing()
    {
        var responses = Run(
            """{"jsonrpc":"2.0","id":"init","method":"initialize","params":{"protocolVersion":"2025-06-18"}}""",
            """{"jsonrpc":"2.0","method":"notifications/initialized"}""",
            """{"jsonrpc":"2.0","id":2,"method":"ping"}""");

        Assert.Equal(2, responses.Length);
        Assert.Equal("init", responses[0]["id"]!.GetValue<string>());
        Assert.Equal(
            "2025-06-18",
            responses[0]["result"]!["protocolVersion"]!.GetValue<string>());
        Assert.Equal(2, responses[1]["id"]!.GetValue<int>());
    }

    [Fact]
    public void ListsTheEnvelopeTools()
    {
        var response = Assert.Single(Run(
            """{"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}"""));
        var tools = response["result"]!["tools"]!.AsArray();

        Assert.Equal(
            [
                "open_binz",
                "list_binz",
                "download_binz",
                "close_binz",
                "list_tools",
                "run",
                "status",
            ],
            tools.Select(tool => tool!["name"]!.GetValue<string>()).ToArray());
        Assert.Null(tools[0]!["inputSchema"]!["required"]);
        Assert.Equal(
            "game",
            tools[2]!["inputSchema"]!["required"]![0]!.GetValue<string>());
    }

    [Fact]
    public void ReportsUnloadedStatusAndToolErrorsAsMcpResults()
    {
        var responses = Run(
            """{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"status","arguments":{}}}""",
            """{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"run","arguments":{"tool":"info"}}}""");

        Assert.Equal(
            "No binz loaded.",
            responses[0]["result"]!["content"]![0]!["text"]!.GetValue<string>());
        Assert.True(responses[1]["result"]!["isError"]!.GetValue<bool>());
        Assert.Contains(
            "Call open_binz first",
            responses[1]["result"]!["content"]![0]!["text"]!.GetValue<string>());
    }

    [Fact]
    public void ReturnsStandardErrorsForInvalidRequests()
    {
        var responses = Run(
            """{"jsonrpc":"2.0","id":1,"method":"missing"}""",
            """{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"status","arguments":[]}}""",
            """{"jsonrpc":2,"id":3,"method":"ping"}""",
            """{"jsonrpc":"2.0","id":true,"method":"ping"}""",
            """[]""",
            """{bad json""");

        Assert.Equal([-32601, -32602, -32600, -32600, -32600, -32700], responses
            .Select(response => response["error"]!["code"]!.GetValue<int>())
            .ToArray());
    }

    private static JsonObject[] Run(params string[] messages)
    {
        var input = new StringReader(string.Join(Environment.NewLine, messages));
        var output = new StringWriter();
        var diagnostics = new StringWriter();
        var server = new McpServer(
            input,
            output,
            diagnostics);

        server.Run();

        return output.ToString()
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => JsonNode.Parse(line)!.AsObject())
            .ToArray();
    }
}
