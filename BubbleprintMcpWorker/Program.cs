using BubbleprintMcpCore;
using BubbleprintMcpWorker;

if (args.Length < 1)
{
    Console.Error.WriteLine("Usage: BubbleprintMcpWorker <binz-path>");
    return 1;
}

var protocolOut = Console.Out;
Console.SetOut(Console.Error);

try
{
    var session = new BinzSession();
    session.Open(args[0]);
    var path = session.Path!;

    Console.Error.WriteLine($"bubbleprint-worker: loaded {path}");
    Console.Error.WriteLine($"bubbleprint-worker: {session.BlueprintCount} blueprints");

    var worker = new JsonlWorker(session, path, protocolOut);
    worker.Run();
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex);
    return 1;
}
