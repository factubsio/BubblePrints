using BlueprintExplorer;
using System.Formats.Tar;
using System.IO.Compression;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json;

namespace BinzFactory;

public interface IGameDefinitions
{
    public Assembly Wrath { get; }
    public IEnumerable<Assembly> Assemblies { get; }
    public IEnumerable<Type?> Types { get; }
    bool HasTypeSupport { get; }
    void Import(BlueprintDB db, JsonSerializerOptions writeOptions, HashSet<string> referencedTypes, ConnectionProgress progress);

    byte[] LoadDictionary();
    void FindReferences(BlueprintDB db, JsonElement node, HashSet<string> types);
    void AddComponentType(Type? type, BlueprintDB db);
}

public abstract class OwlcatGame : IGameDefinitions
{
    protected OwlcatGame(string gamePath, string gameData, string assemblyName, string typeIdTypeName)
    {
        var managedPath = Path.Combine(gamePath, gameData, "Managed");
        this.gamePath = gamePath;
        this.gameData = gameData;

        var resolver = new PathAssemblyResolver(Directory.EnumerateFiles(managedPath, "*.dll"));
        var _mlc = new MetadataLoadContext(resolver);

        Wrath = _mlc.LoadFromAssemblyPath(Path.Combine(managedPath, assemblyName));

        Assemblies = Directory
            .EnumerateFiles(Path.GetDirectoryName(Wrath.Location) ?? throw new NotSupportedException(), "*.dll")
            .SelectMany(assFile =>
            {
                try
                {
                    return new[] { _mlc.LoadFromAssemblyPath(assFile) };
                }
                catch
                {
                    return [];
                }
            });

        Types = Assemblies.SelectMany(ass =>
        {
            try
            {
                return ass.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null);
            }
        });
        typeIdType = Assemblies
            .Select(ass => ass.GetType(typeIdTypeName))
            .FirstOrDefault(t => t is not null) ?? throw new NotSupportedException("cannot find TypeId Type");
    }

    public abstract void Import(BlueprintDB db, JsonSerializerOptions writeOptions, HashSet<string> referencedTypes, ConnectionProgress progress);

    protected readonly Type typeIdType;
    private readonly string gamePath;
    private readonly string gameData;

    public bool HasTypeSupport => true;

    public IEnumerable<Assembly> Assemblies { get; }
    public IEnumerable<Type?> Types { get; }

    public Assembly Wrath { get; }

    protected string GetGamePath(params string[] path)
    {
        return Path.Combine([gamePath, .. path]);
    }

    public byte[] LoadDictionary() => File.ReadAllBytes(GetGamePath(gameData, @"StreamingAssets\Localization\enGB.json"));
    public void FindReferences(BlueprintDB db, JsonElement node, HashSet<string> types)
    {
        if (node.ValueKind == JsonValueKind.Array)
        {
            foreach (var elem in node.EnumerateArray())
                FindReferences(db, elem, types);
        }
        else if (node.ValueKind == JsonValueKind.Object)
        {
            if (node.TryGetProperty("$type", out var raw))
            {
                types.Add(ParseJsonType(db, raw));
            }
            foreach (var elem in node.EnumerateObject())
                FindReferences(db, elem.Value, types);
        }

    }

    public virtual void AddComponentType(Type? type, BlueprintDB db)
    {
        if (type?.FullName == null) return;

        foreach (var data in type.GetCustomAttributesData())
        {
            try
            {
                if (data.AttributeType.Name == typeIdType.Name)
                {
                    if (data.ConstructorArguments[0].Value is string guid)
                    {
                        if (db.GuidToFullTypeName.TryAdd(guid, type.FullName))
                            db.TypeGuidsInOrder.Add(guid);
                    }
                }
            }
            catch // Pretty sure this is some cursed attribute and not the TypeId we want
            { }
        }
    }

    protected abstract string ParseJsonType(BlueprintDB db, JsonElement raw);
}


public class RtGame(string gamePath, string dataFolder) : OwlcatGame(gamePath, dataFolder, "Code.dll", "Kingmaker.Blueprints.JsonSystem.Helpers.TypeIdAttribute")
{
    protected override string ParseJsonType(BlueprintDB db, JsonElement raw) => raw.NewTypeStr(false, db).Guid;

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
public class WrathGame(string gamePath, string dataFolder) : OwlcatGame(gamePath, dataFolder, "Assembly-CSharp.dll", "Kingmaker.Blueprints.JsonSystem.TypeIdAttribute")
{
    protected override string ParseJsonType(BlueprintDB db, JsonElement raw) => raw.NewTypeStr(false, db).Guid;

    public override void Import(BlueprintDB db, JsonSerializerOptions writeOptions, HashSet<string> referencedTypes, ConnectionProgress progress)
    {
        using var bpDump = ZipFile.OpenRead(GetGamePath("blueprints.zip"));

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

public class KmGame(string gamePath, string dataFolder) : OwlcatGame(gamePath, dataFolder, "Assembly-CSharp.dll", "Kingmaker.Blueprints.DirectSerialization.TypeIdAttribute")
{
    protected override string ParseJsonType(BlueprintDB db, JsonElement raw) => raw.NewTypeStr(false, db).FullName;

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

public static class BinzImportExport
{
    public static BlueprintDB ExtractFromGame(IGameDefinitions game, ConnectionProgress progress)
    {
        BlueprintDB db = new();

        // Should put this somewhere sensible
        AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
        {
            var assName = new AssemblyName(args.Name);

            var dir = Path.GetDirectoryName(args.RequestingAssembly?.Location) ?? throw new NotSupportedException();

            var assFile = Directory.EnumerateFiles(dir, "*.dll")
                .FirstOrDefault(assFile => Path.GetFileNameWithoutExtension(assFile) == assName.Name);

            if (assFile is not null)
                return Assembly.LoadFrom(assFile);

            return null;
        };

        var writeOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
        };

        if (game.HasTypeSupport)
        {
            foreach (var type in game.Types)
            {
                game.AddComponentType(type, db);
            }

            db.TypeGuidsInOrder.Sort();
            db.FlatIndexToTypeName = new string[db.TypeGuidsInOrder.Count];
            for (int i = 0; i < db.TypeGuidsInOrder.Count; i++)
            {
                var guid = db.TypeGuidsInOrder[i];
                db.GuidToFlatIndex[guid] = (ushort)i;
                db.FlatIndexToTypeName[i] = db.GuidToFullTypeName[guid];
            }
        }
        Dictionary<string, string> TypenameToGuid = [];
        progress.Phase = "Extracting";
        HashSet<string> referencedTypes = [];

        game.Import(db, writeOptions, referencedTypes, progress);

        //else
        //{
        //}

        progress.Phase = "Writing";
        progress.Current = 0;

        return db;

    }
    private static readonly ushort[] NoRefs = [];

    internal static void ReadDumpFromStream(BlueprintDB db, Stream stream, JsonSerializerOptions writeOptions, string name, HashSet<string> referencedTypes, ConnectionProgress progress, IGameDefinitions game, Action<BlueprintHandle>? finalizeImport = null)
    {
        var reader = new StreamReader(stream);
        var contents = reader.ReadToEnd();
        var json = JsonSerializer.Deserialize<JsonElement>(contents);
        var type = json.GetProperty("Data").NewTypeStr(false, db);

        var handle = new BlueprintHandle
        {
            GuidText = json.Str("AssetId"),
            Name = name[0..^4],
            Type = type.FullName ?? type.Name,
            Raw = JsonSerializer.Serialize(json.GetProperty("Data"), writeOptions),
        };
        var components = handle.Type.Split('.');
        if (components.Length <= 1)
        {
            handle.TypeName = handle.Type;
        }
        else
        {
            handle.TypeName = components[^1];
            handle.Namespace = string.Join('.', components.Take(components.Length - 1));
        }

        handle.EnsureParsed();
        foreach (var _ in handle.GetDirectReferences()) { }

        referencedTypes.Clear();
        game.FindReferences(db, handle.EnsureObj, referencedTypes);
        handle.ComponentIndex = [.. referencedTypes.SelectMany(typeId => db.GuidToFlatIndex.TryGetValue(typeId, out ushort value) ? [value] : NoRefs)];

        finalizeImport?.Invoke(handle);

        db.AddBlueprint(handle);

        progress.Current++;
    }

    internal static void LoadFromBubbleMine(BlueprintDB db, ConnectionProgress progress, ZipArchive bpDump, IGameDefinitions game)
    {
        var writeOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            MaxDepth = 128
        };

        HashSet<string> referencedTypes = [];

        JsonSerializerOptions options = new()
        {
            MaxDepth = 128,
        };
        foreach (var entry in bpDump.Entries)
        {
            if (!entry.Name.EndsWith(".json")) continue;
            try
            {
                var stream = entry.GetType().GetMethod("OpenInReadMode", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(entry, [false]) as Stream;
                var reader = new StreamReader(stream ?? throw new FileNotFoundException(entry.FullName));
                var contents = reader.ReadToEnd();
                var json = JsonSerializer.Deserialize<JsonElement>(contents, options);
                var type = json.Str("$type").ParseTypeString();


                var lastDot = entry.Name.LastIndexOf('.');
                var secondLastDot = entry.Name.LastIndexOf('.', lastDot - 1);

                if (lastDot == secondLastDot + 1)
                {
                    continue;
                }

                var handle = new BlueprintHandle
                {
                    GuidText = entry.Name[(secondLastDot + 1)..lastDot],
                    Name = entry.Name[0..secondLastDot],
                    Type = type,
                    Raw = JsonSerializer.Serialize(json, writeOptions),
                };
                var components = handle.Type.Split('.');
                if (components.Length <= 1)
                {
                    handle.TypeName = handle.Type;
                }
                else
                {
                    handle.TypeName = components[^1];
                    handle.Namespace = string.Join('.', components.Take(components.Length - 1));
                }

                handle.EnsureParsed();
                foreach (var _ in handle.GetDirectReferences()) { }

                referencedTypes.Clear();
                game.FindReferences(db, handle.EnsureObj, referencedTypes);
                handle.ComponentIndex = [.. referencedTypes.Select<string, ushort?>(typeId =>
                {
                    if (db.GuidToFlatIndex.TryGetValue(typeId, out var val))
                    {
                        return val;
                    }
                    else
                    {
                        return null;
                    }
                }).Where(i => i.HasValue).Select(i => i!.Value)];

                db.AddBlueprint(handle);



                progress.Current++;
                //index++;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                Console.Error.WriteLine(e.StackTrace);
                throw;
            }
        }
    }

    public static void WriteBlueprints(BlueprintDB db, ConnectionProgress progress, IGameDefinitions game, string outPath, BlueprintExplorer.BlueprintDB.GameVersion version)
    {

        progress.EstimatedTotal += 3;
        progress.Current = 0;
        int biggestRefList = 0;
        string mostReferred = "";

        var cache = db.BlueprintsInOrder;

        Dictionary<Guid, List<Guid>> References = [];
        HashSet<string> keyWords = [];
        StringBuilder word = new();

        foreach (var bp in cache)
        {
            var from = Guid.Parse(bp.GuidText);
            foreach (var forwardRef in bp.GetDirectReferences())
            {
                if (!References.TryGetValue(forwardRef, out var refList))
                {
                    refList = [];
                    References[forwardRef] = refList;
                }
                refList.Add(from);
            }
        }

        bool writePaths = cache[0].FullPath != null;

        using (var file = new BPFile.BPWriter(outPath))
        {
            using (var header = file.Begin((ushort)ChunkTypes.Header))
            {
                header.Stream.Write(version.Major);
                header.Stream.Write(version.Minor);
                header.Stream.Write(version.Patch);
                header.Stream.Write(version.Suffix);
                header.Stream.Write(cache.Count);
            }

            const int batchSize = 16000;

            BPFile.ChunkWriter? current = null;
            for (int i = 0; i < cache.Count; i++)
            {
                if (i % batchSize == 0)
                {
                    current?.Dispose();
                    current = file.Begin((ushort)ChunkTypes.Blueprints);
                }

                if (current == null) throw new NotSupportedException();

                var c = cache[i];

                current.Stream.Write(c.GuidText);
                current.Stream.Write(c.Name);
                current.Stream.Write(c.Type);
                current.Stream.Write(c.Raw);

                if (writePaths)
                {
                    var pathChunk = current.GetStream((ushort)ChunkSubTypes.Blueprints.Paths);
                    pathChunk.Write(c.FullPath);
                }

                var refs = current.GetStream((ushort)ChunkSubTypes.Blueprints.References);
                if (References.TryGetValue(Guid.Parse(c.GuidText), out var refList))
                {
                    refs.Write(refList.Count);
                    foreach (var backRef in refList)
                        refs.Write(backRef.ToByteArray());
                    if (refList.Count > biggestRefList)
                    {
                        biggestRefList = refList.Count;
                        mostReferred = c.Name;
                    }
                }
                else
                {
                    refs.Write(0);
                }

                var comps = current.GetStream((ushort)ChunkSubTypes.Blueprints.Components);
                comps.Write(c.ComponentIndex.Length);
                foreach (var index in c.ComponentIndex)
                    comps.Write(index);

                progress.Current++;
            }

            current?.Dispose();

            var rawLangDict = game.LoadDictionary();
            file.Write((ushort)ChunkTypes.Strings, 0, rawLangDict);

            var stringDictRaw = JsonSerializer.Deserialize<JsonElement>(rawLangDict).GetProperty("strings");
            db.SetStrings(BlueprintDB.LoadStringDatabase(stringDictRaw));

            int handleIndex = 0;
            foreach (var handle in cache)
            {
                keyWords.Clear();

                foreach (var element in handle.Elements)
                {
                    if (element.key == null || element.levelDelta < 0) continue;

                    BlueprintDB.ExtractKeyWords(keyWords, word, element.key);

                    string localisedStr = element.Node.ParseAsString(element.key);
                    if (localisedStr is not null)
                    {
                        if (localisedStr is not "<string-not-present>" and not "<null-string>")
                            BlueprintDB.ExtractKeyWords(keyWords, word, localisedStr);
                    }
                    else if (element.value != null)
                    {
                        BlueprintDB.ExtractKeyWords(keyWords, word, element.value);
                    }

                }

                keyWords.Remove("the");
                keyWords.Remove("a");
                keyWords.Remove("i");
                keyWords.Remove("if");
                keyWords.Remove("this");
                keyWords.Remove("that");
                keyWords.Remove("and");

                foreach (var keyWord in keyWords)
                {
                    db.AddWord(keyWord, handleIndex);
                }

                handleIndex++;
            }

            ulong sum = 0;
            foreach (var (k, list) in db.IndexByWord)
            {
                sum += (ulong)list.Count;
            }


            using (var types = file.Begin((ushort)ChunkTypes.TypeNames))
            {
                types.Stream.Write(db.GuidToFullTypeName.Count);
                foreach (var k in db.TypeGuidsInOrder)
                {
                    types.Stream.Write(k);
                    types.Stream.Write(db.GuidToFullTypeName[k]);
                }

                progress.Current++;
            }

            using (var chunk = file.Begin((ushort)ChunkTypes.Defaults))
            {
                chunk.Stream.Write(0);

                //chunk.Stream.Write(defaults.Count);
                //foreach (var kv in defaults)
                //{
                //    chunk.Stream.Write(kv.Key);
                //    chunk.Stream.Write(kv.Value.Count);
                //    foreach (var sub in kv.Value)
                //    {
                //        chunk.Stream.Write(sub.Key);
                //        chunk.Stream.Write(sub.Value);
                //    }
                //}

                progress.Current++;
            }

            using (var chunk = file.Begin((ushort)ChunkTypes.SearchIndex))
            {
                var indexByWord = db.IndexByWord;
                chunk.Stream.Write(indexByWord.Count);
                foreach (var (k, list) in indexByWord)
                {
                    chunk.Stream.Write(k);
                    chunk.Stream.Write(list.Count);
                    foreach (var i in list)
                        chunk.Stream.Write7BitEncodedInt(i);
                }

                progress.Current++;
            }
        }
        Console.WriteLine($"mostReferred: {mostReferred}, biggestRefList: {biggestRefList}");
    }

}
