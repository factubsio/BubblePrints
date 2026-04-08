using BlueprintExplorer;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BinzFactory.Importer;

public class KmImporter(string gamePath, string dataFolder) : OwlcatGame(gamePath, dataFolder, "Assembly-CSharp.dll", "Kingmaker.Blueprints.DirectSerialization.TypeIdAttribute")
{
    protected override string ParseJsonType(BlueprintDB db, JsonElement raw) => raw.NewTypeStr(db).FullName;

    public override void Import(BlueprintDB db, JsonSerializerOptions writeOptions, HashSet<string> referencedTypes, ConnectionProgress progress)
    {
        using var bpDump = ZipFile.OpenRead(GetGamePath("blueprints.zip"));

        progress.EstimatedTotal = bpDump.Entries.Count(e => e.Name.EndsWith(".jbp"));
        BinzImportExport.LoadFromBubbleMine(db, progress, bpDump, this);
    }

    public override void AddComponentType(Type? type, BlueprintDB db)
    {
        if (type?.FullName == null) return;
        if (!m_InheritsCache.ContainsKey(type.FullName))
        {
            var baseType = type;
            while (baseType != null)
            {
                if (baseType.FullName is "Kingmaker.Blueprints.BlueprintComponent" or "Kingmaker.Blueprints.BlueprintScriptableObject")
                {
                    m_InheritsCache[type.FullName] = true;
                    if (db.GuidToFullTypeName.TryAdd(type.FullName, type.FullName))
                        db.TypeGuidsInOrder.Add(type.FullName);
                }
                try
                {
                    baseType = baseType.BaseType;
                }
                catch
                {
                    // This is one of those i18n types
                    break;
                }
            }
            m_InheritsCache[type.FullName] = false;
        }
    }

    private readonly Dictionary<string, bool> m_InheritsCache = [];
}
