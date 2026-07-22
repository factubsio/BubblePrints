using System.Text.Json.Nodes;
using BlueprintExplorer;

namespace BubbleprintMcpCore.Tools;

public sealed class BlueprintTool : IMcpTool
{
    public string Name => "blueprint";
    public string Description =>
        "Get one blueprint by guid or unambiguous name. Args: guid or name; includeRaw (default false).";
    public int MaxResultBytes => 256 * 1024;

    public object Handle(BlueprintDB db, JsonObject args)
    {
        var blueprint = ToolHelpers.ResolveBlueprint(db, args);
        var result = ToolHelpers.Summary(blueprint);
        if (args["includeRaw"]?.GetValue<bool>() == true)
            result["raw"] = blueprint.Raw;
        return result;
    }
}
