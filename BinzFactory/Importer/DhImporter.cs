using BlueprintExplorer;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BinzFactory.Importer;

public class DhImporter(string gamePath, string dataFolder) : OwlcatGame("DH", gamePath, dataFolder, "Code.dll", "Owlcat.Runtime.Core.Utility.TypeIdAttribute") 
{
    protected override string ParseJsonType(BlueprintDB db, JsonElement raw) => raw.NewTypeStr(db).Guid;

    public override void Import(BlueprintDB db, JsonSerializerOptions writeOptions, HashSet<string> referencedTypes, ConnectionProgress progress)
    {
        var zipPath = GetGamePath("blueprints.zip");
        if (!File.Exists(zipPath))
            throw new NotSupportedException($"Dark Heresy does not currently support automatic import without manually creating a blueprint dump and placing it at: {zipPath}");
        using var bpDump = ZipFile.OpenRead(zipPath);

        progress.EstimatedTotal = bpDump.Entries.Count(e => e.Name.EndsWith(".jbp"));
        foreach (var entry in bpDump.Entries)
        {
            if (!entry.Name.EndsWith(".jbp")) continue;
            if (entry.Name.StartsWith("Appsflyer")) continue;
            try
            {
                using Stream? stream = entry.GetType().GetMethod("OpenInReadMode", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(entry, [false]) as Stream;
                BinzImportExport.ReadDumpFromStream(db, stream ?? throw new FileNotFoundException(entry.FullName), writeOptions, entry.Name, referencedTypes, progress, this);
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
