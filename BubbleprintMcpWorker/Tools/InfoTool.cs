using System.Text.Json.Nodes;
using System.Text.Json;
using BlueprintExplorer;

namespace BubbleprintMcpWorker.Tools;

public sealed class InfoTool : IMcpTool
{
    public string Name => "info";
    public string Description => "Return loaded database metadata and counts. No arguments.";

    public object Handle(BlueprintDB db, JsonObject args)
    {
        return new JsonObject
        {
            ["game"] = db.GameName,
            ["blueprints"] = db.BlueprintsInOrder.Count,
            ["types"] = db.types.Count,
            ["rootTypeIds"] = db.RootTypeCount,
            ["rootTypeIndexBytes"] = db.RootTypeIndexBytes,
            ["propertyKeyBloomBitsPerBlueprint"] = BlueprintDB.PropertyKeyBloomBitsPerBlueprint,
            ["propertyKeyBloomIndexBytes"] = db.PropertyKeyBloomIndexBytes,
            ["propertyKeyBloomBuildMs"] = db.PropertyKeyBloomBuildElapsedMs,
            ["cachedJsonElements"] = db.BlueprintsInOrder.Count(
                blueprint => blueprint._obj.ValueKind != JsonValueKind.Undefined),
            ["strings"] = db.Strings.Count,
            ["indexedWords"] = db.IndexByWord.Count,
        };
    }
}
