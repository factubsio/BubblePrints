using System.Text.Json.Nodes;
using BlueprintExplorer;

namespace BubbleprintMcpCore;

public interface IMcpTool
{
    string Name { get; }
    string Description { get; }
    int MaxResultBytes => 64 * 1024;
    object Handle(BlueprintDB db, JsonObject args);
}
