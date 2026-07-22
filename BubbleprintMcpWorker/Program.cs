using BlueprintExplorer;
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
    var path = Path.GetFullPath(args[0]);
    var version = BinzManager.VersionFromFile(path);
    var db = new BlueprintDB(version.Game);
    db.TryConnect(new ConnectionProgress(), path, saveSettings: false);
    db.PreferStreamingTreeQueries = true;

    Console.Error.WriteLine($"bubbleprint-worker: loaded {path}");
    Console.Error.WriteLine($"bubbleprint-worker: {db.BlueprintsInOrder.Count} blueprints");

    var worker = new JsonlWorker(db, path, protocolOut);
    worker.Run();
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex);
    return 1;
}
