using BubbleprintMcpCore;
using BubbleprintsMcpServer;

var protocolOutput = Console.Out;
Console.SetOut(Console.Error);

try
{
    var server = new McpServer(
        new BinzSession(),
        Console.In,
        protocolOutput,
        Console.Error);
    server.Run();
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex);
    return 1;
}
