using System.Text.Json.Nodes;
using BlueprintExplorer;

namespace BubbleprintMcpCore;

public sealed class BinzSession
{
    private readonly ToolCatalog catalog;
    private BlueprintDB? db;

    public BinzSession(ToolCatalog? catalog = null)
    {
        this.catalog = catalog ?? new ToolCatalog();
    }

    public bool IsOpen => db is not null;
    public string? Path { get; private set; }
    public string? Game => db?.GameName;
    public int? BlueprintCount => db?.BlueprintsInOrder.Count;

    public JsonObject Open(string? path = null)
    {
        Close();

        var fullPath = BinzCatalog.ResolveOpenPath(path);
        var version = BinzManager.VersionFromFile(fullPath);
        var next = new BlueprintDB(version.Game);
        next.TryConnect(new ConnectionProgress(), fullPath, saveSettings: false);
        next.PreferStreamingTreeQueries = true;

        db = next;
        Path = fullPath;

        return new JsonObject
        {
            ["status"] = "ok",
            ["path"] = Path,
            ["game"] = db.GameName,
            ["blueprints"] = db.BlueprintsInOrder.Count,
        };
    }

    public void Close()
    {
        db = null;
        Path = null;
    }

    public IReadOnlyList<ToolDescription> ListTools()
    {
        RequireDatabase();
        return catalog.List();
    }

    public JsonNode? Run(string toolName, JsonObject? args)
    {
        return catalog.Run(RequireDatabase(), toolName, args);
    }

    private BlueprintDB RequireDatabase()
    {
        return db ?? throw new InvalidOperationException(
            "No binz is open. Call open_binz first.");
    }

}
