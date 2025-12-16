using BlueprintExplorer;
using System.Formats.Tar;
using System.IO.Compression;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json;

namespace BinzFactory;

internal class FromZipImporter : IGameImporter
{
    public BlueprintDB ExtractFromGame(ConnectionProgress progress, string wrathPath, string outputFile, BlueprintExplorer.BlueprintDB.GameVersion version)
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

        var gamePath = Path.Combine(wrathPath, BinzImporter.Game_Data, "Managed");
        var resolver = new PathAssemblyResolver(Directory.EnumerateFiles(gamePath, "*.dll"));
        var _mlc = new MetadataLoadContext(resolver);
        if (BinzImporter.Game_Data == "WH40KRT_Data")
            BinzImporter.Wrath = _mlc.LoadFromAssemblyPath(Path.Combine(gamePath, "Code.dll"));
        else
            BinzImporter.Wrath = _mlc.LoadFromAssemblyPath(Path.Combine(gamePath, "Assembly-CSharp.dll"));

        var writeOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
        };

        if (BinzImporter.Game_Data is "Wrath_Data" or "WH40KRT_Data" or "Kingmaker_Data")
        {
            var assemblies = Directory
                .EnumerateFiles(Path.GetDirectoryName(BinzImporter.Wrath.Location) ?? throw new NotSupportedException(), "*.dll")
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

            var types = assemblies.SelectMany(ass =>
            {
                try
                {
                    return ass.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    return ex.Types.Where(t => t != null).ToArray();
                }
            });

            var typeIdType = assemblies
                .Select(FindTypeId)
                .FirstOrDefault(t => t is not null) ?? throw new NotSupportedException("cannot find TypeId Type");

            foreach (var type in types)
            {
                if (type?.FullName == null) continue;

                if (BinzImporter.CurrentGame == "KM" && KMInheritsFromBPComponent(type))
                {
                    if (db.GuidToFullTypeName.TryAdd(type.FullName, type.FullName))
                        db.TypeGuidsInOrder.Add(type.FullName);
                }
                else
                {
                    foreach (var data in type.GetCustomAttributesData())
                    {
                        try
                        {
                            if (data.AttributeType.Name == typeIdType.Name)
                            {
                                if (data.ConstructorArguments[0].Value is string guid)
                                {
                                    if (db.GuidToFullTypeName.TryAdd(guid, type.FullName))
                                    {
                                        db.TypeGuidsInOrder.Add(guid);
                                    }
                                }
                            }
                        }
                        catch // Pretty sure this is some cursed attribute and not the TypeId we want
                        { }
                    }
                }
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
        int index = 0;

        if (BinzImporter.Game_Data == "WH40KRT_Data")
        {
            const string bpPath = "WhRtModificationTemplate/Blueprints/";
            var tarPath = Path.Combine(wrathPath, "Modding", "WhRtModificationTemplate.tar");
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
                    ReadDumpFromStream(db, stream, writeOptions, tarEntry.Name.Split('/').Last(), ref referencedTypes, ref progress, ref index, bp => bp.FullPath = tarEntry.Name);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e.Message);
                    Console.Error.WriteLine(e.StackTrace);
                    throw;
                }
            }
        }
        else
        {

            using var bpDump = ZipFile.OpenRead(BinzImporter.GetBlueprintSource(wrathPath));

            progress.EstimatedTotal = bpDump.Entries.Count(e => e.Name.EndsWith(".jbp"));
            if (BinzImporter.Game_Data == "Kingmaker_Data")
            {
                LoadFromBubbleMine(db, progress, bpDump);
            }
            else if (BinzImporter.Game_Data is "Wrath_Data" or "WH40KRT_Data")
            {
                foreach (var entry in bpDump.Entries)
                {
                    if (!entry.Name.EndsWith(".jbp")) continue;
                    if (entry.Name.StartsWith("Appsflyer")) continue;
                    try
                    {
                        using Stream? stream = entry.GetType().GetMethod("OpenInReadMode", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(entry, [false]) as Stream;
                        ReadDumpFromStream(db, stream ?? throw new FileNotFoundException(entry.FullName), writeOptions, entry.Name, ref referencedTypes, ref progress, ref index);
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

        progress.Phase = "Writing";
        progress.Current = 0;

        WriteBlueprints(db, progress, wrathPath, outputFile, version);

        return db;

    }
    private static Type? FindTypeId(Assembly ass)
    {
        string typeIdTypeName = BinzImporter.CurrentGame switch
        {
            "RT" => "Kingmaker.Blueprints.JsonSystem.Helpers.TypeIdAttribute",
            "KM" => "Kingmaker.Blueprints.DirectSerialization.TypeIdAttribute",
            _ => "Kingmaker.Blueprints.JsonSystem.TypeIdAttribute",

        };
        return ass.GetType(typeIdTypeName);
    }

    private static readonly ushort[] NoRefs = [];

    private static void ReadDumpFromStream(BlueprintDB db, Stream stream, JsonSerializerOptions writeOptions, string name, ref HashSet<string> referencedTypes, ref ConnectionProgress progress, ref int index, Action<BlueprintHandle>? finalizeImport = null)
    {
        var reader = new StreamReader(stream);
        var contents = reader.ReadToEnd();
        var json = JsonSerializer.Deserialize<JsonElement>(contents);
        var type = json.GetProperty("Data").NewTypeStr();

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
        ReferenceExtractor.VisitObjects(handle.EnsureObj, referencedTypes);
        handle.ComponentIndex = [.. referencedTypes.SelectMany(typeId => db.GuidToFlatIndex.TryGetValue(typeId, out ushort value) ? [value] : NoRefs)];

        finalizeImport?.Invoke(handle);

        db.AddBlueprint(handle);

        progress.Current++;
        index++;
    }

    private static bool KMInheritsFromBPComponent(Type type)
    {
        if (type?.FullName == null) return false;
        if (m_InheritsCache.TryGetValue(type.FullName, out bool result))
        {
            return result;
        }
        else
        {
            var baseType = type;
            while (baseType != null)
            {
                if (baseType.FullName is "Kingmaker.Blueprints.BlueprintComponent" or "Kingmaker.Blueprints.BlueprintScriptableObject")
                {
                    m_InheritsCache[type.FullName] = true;
                    return true;
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
            return false;
        }
    }
    //private static void LoadFromKingmakerZip(BlueprintDB db, ConnectionProgress progress, string path)
    //{
    //    var writeOptions = new JsonSerializerOptions
    //    {
    //        WriteIndented = true,
    //    };

    //    using var stream = File.OpenRead(@"D:\rt_bp.json");
    //    using var doc = JsonDocument.Parse(stream);

    //    JsonElement all = doc.RootElement;

    //    var iterator = all.EnumerateArray();

    //    HashSet<string> referencedTypes = new();

    //    while (iterator.MoveNext())
    //    {
    //        try
    //        {
    //            string name = iterator.Current.GetString() ?? throw new NotSupportedException();
    //            if (!iterator.MoveNext()) throw new Exception();

    //            string guid = iterator.Current.GetString() ?? throw new NotSupportedException();
    //            if (!iterator.MoveNext()) throw new Exception();

    //            string fullTypeSanity = iterator.Current.GetString() ?? throw new NotSupportedException();
    //            if (!iterator.MoveNext()) throw new Exception();

    //            JsonElement json = iterator.Current;

    //            if (json.ValueKind != JsonValueKind.Object)
    //            {
    //                Console.Error.WriteLine("Non object type: " + name + ", " + fullTypeSanity);
    //                continue;
    //            }

    //            var type = json.TypeString();

    //            if (type == null)
    //            {
    //                Console.WriteLine("BadBadBad");
    //            }

    //            var handle = new BlueprintHandle
    //            {
    //                GuidText = guid,
    //                Name = name,
    //                Type = type,
    //                Raw = json.GetRawText(),
    //            };
    //            var components = handle.Type?.Split('.');
    //            if (components == null || components.Length <= 1)
    //            {
    //                handle.TypeName = handle.Type;
    //            }
    //            else
    //            {
    //                handle.TypeName = components[^1];
    //                handle.Namespace = string.Join('.', components.Take(components.Length - 1));
    //            }

    //            handle.EnsureParsed();
    //            foreach (var _ in handle.GetDirectReferences()) { }

    //            referencedTypes.Clear();
    //            ReferenceExtractor.VisitObjects(handle.EnsureObj, referencedTypes);
    //            handle.ComponentIndex = referencedTypes.Select(typeId => db.GuidToFlatIndex[typeId]).ToArray();

    //            db.AddBlueprint(handle);



    //            progress.Current++;
    //        }
    //        catch (Exception e)
    //        {
    //            Console.Error.WriteLine(e.Message);
    //            Console.Error.WriteLine(e.StackTrace);
    //            throw;
    //        }

    //    }

    //    Console.WriteLine("here");
    //}

    private static void LoadFromBubbleMine(BlueprintDB db, ConnectionProgress progress, ZipArchive bpDump)
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
                ReferenceExtractor.VisitObjects(handle.EnsureObj, referencedTypes);
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
    public static void WriteBlueprints(BlueprintDB db, ConnectionProgress progress, string wrathPath, string outPath, BlueprintExplorer.BlueprintDB.GameVersion version)
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



            var rawLangDict = File.ReadAllBytes(Path.Combine(wrathPath, BinzImporter.Game_Data, @"StreamingAssets\Localization\enGB.json"));
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

    private static readonly Dictionary<string, bool> m_InheritsCache = [];
}
