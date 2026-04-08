using BlueprintExplorer;
using System;
using System.Collections.Generic;
using System.Formats.Tar;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BinzFactory.Importer;

public class RtImporter(string gamePath, string dataFolder) : OwlcatGame(gamePath, dataFolder, "Code.dll", "Kingmaker.Blueprints.JsonSystem.Helpers.TypeIdAttribute")
{
    protected override string ParseJsonType(BlueprintDB db, JsonElement raw) => raw.NewTypeStr(db).Guid;

    public override void Import(BlueprintDB db, JsonSerializerOptions writeOptions, HashSet<string> referencedTypes, ConnectionProgress progress)
    {
        const string bpPath = "WhRtModificationTemplate/Blueprints/";
        var tarPath = GetGamePath("Modding", "WhRtModificationTemplate.tar");
        using var tarStream = new FileStream(tarPath, FileMode.Open, FileAccess.Read);
        using var reader = new TarReader(tarStream) ?? throw new FileNotFoundException(tarPath);
        List<TarEntry> entries = [];
        TarEntry? entry;
        while ((entry = reader.GetNextEntry()) != null)
        {
            if (entry.EntryType.HasFlag(TarEntryType.RegularFile))
            {
                if (entry.Name.StartsWith(bpPath) && entry.Name.EndsWith(".jbp"))
                {
                    entries.Add(entry);
                }
            }
        }
        progress.EstimatedTotal = entries.Count;
        foreach (var tarEntry in entries)
        {
            try
            {
                using var stream = tarEntry.DataStream ?? throw new FileNotFoundException(tarEntry.Name);
                BinzImportExport.ReadDumpFromStream(db, stream, writeOptions, tarEntry.Name.Split('/').Last(), referencedTypes, progress, this, bp => bp.FullPath = tarEntry.Name);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                Console.Error.WriteLine(e.StackTrace);
                throw;
            }
        }
    }
}
