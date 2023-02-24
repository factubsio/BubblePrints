using K4os.Compression.LZ4;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BlueprintExplorer
{
    public partial class BlueprintDB
    {
        private static void ExtractKeyWords(HashSet<string> result, StringBuilder buffer, string input)
        {
            buffer.Clear();
            foreach (var ch in input)
            {
                if (char.IsLetterOrDigit(ch))
                    buffer.Append(ch);
                else if (ch is '\'' or '-' or '_')
                    continue;
                else
                {
                    if (buffer.Length > 0)
                    {
                        result.Add(buffer.ToString().ToLower());
                        buffer.Clear();
                    }
                }
            }
            if (buffer.Length > 0)
                result.Add(buffer.ToString().ToLower());
        }

        public static readonly string ImportFolderBase = @"D:\steamlib\steamapps\common\Pathfinder Second Adventure";

        #region DEV
        bool generateOutput = false;
        bool importNew = false;
        bool forceLastKnown = false;
        #endregion

        private readonly GameVersion LastKnown = new(2, 0, 7, 'k', 0);

        private Dictionary<string, List<int>> _IndexByWord = new();

        private static BlueprintDB _Instance;
        public static BlueprintDB Instance => _Instance ??= new();
        public readonly Dictionary<string, string> Strings = new();

        public readonly Dictionary<string, string> GuidToFullTypeName = new();
        public string[] FlatIndexToTypeName;

        // Only used during import/export
        public readonly Dictionary<string, ushort> GuidToFlatIndex = new();
        public readonly List<string> TypeGuidsInOrder = new();

        public readonly Dictionary<Guid, BlueprintHandle> Blueprints = new();
        private readonly List<BlueprintHandle> cache = new();
        public readonly HashSet<string> types = new();

        public struct FileSection
        {
            public int Offset;
            public int Legnth;
        }

        public enum GoingToLoad
        {
            FromLocalFile,
            FromCache,
            FromWeb,
            FromNewImport,
        }

        public struct GameVersion : IComparable<GameVersion> {
            public int Major, Minor, Patch;
            public char Suffix;
            public int Bubble;

            public GameVersion(int major, int minor, int patch, char suffix, int bubble)
            {
                Major = major;
                Minor = minor;
                Patch = patch;
                Suffix = suffix;
                Bubble = bubble;
            }

            public int CompareTo(GameVersion other)
            {
                int c = Major.CompareTo(other.Major);
                if (c != 0) return c;

                c = Minor.CompareTo(other.Minor);
                if (c != 0) return c;

                c = Patch.CompareTo(other.Patch);
                if (c != 0) return c;

                c = Suffix.CompareTo(other.Suffix);
                if (c != 0) return c;

                return Bubble.CompareTo(other.Bubble);
            }

            public override bool Equals(object obj) => obj is GameVersion version && Major == version.Major && Minor == version.Minor && Patch == version.Patch && Suffix == version.Suffix && Bubble == version.Bubble;
            public override int GetHashCode() => HashCode.Combine(Major, Minor, Patch, Suffix, Bubble);




            public override string ToString() => $"{Major}.{Minor}.{Patch}{Suffix}_{Bubble}";

        }

        public List<GameVersion> Available = new() { };

        private readonly string filenameRoot = "blueprints_raw";
        private readonly string extension = "binz";

        private string FileNameFor(GameVersion version) => $"{filenameRoot}_{version}.{extension}";
        public string FileName => FileNameFor(Latest);

        public bool InCache => File.Exists(Path.Combine(CacheDir, FileName));

        bool AvailableDetected = false;

        public GameVersion Latest => Available.Last();

        public static string CacheDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BubblePrints");

        public GoingToLoad GetLoadType()
        {
            if (importNew)
                return GoingToLoad.FromNewImport;

            if (!AvailableDetected && forceLastKnown)
            {
                Available.Add(LastKnown);
                AvailableDetected = true;
            }


            if (!AvailableDetected)
            {
                bool fromWeb = false;
                if (BubblePrints.Settings.CheckForNewBP)
                {
                    try
                    {
                        Console.WriteLine("setting available = from web");
                        using var web = new WebClient();

                        var raw = web.DownloadString(@"https://raw.githubusercontent.com/factubsio/BubblePrintsData/main/versions.json");
                        var versions = JsonSerializer.Deserialize<JsonElement>(raw);

                        foreach (var version in versions.EnumerateArray())
                        {
                            GameVersion gv = new()
                            {
                                Major = version[0].GetInt32(),
                                Minor = version[1].GetInt32(),
                                Patch = version[2].GetInt32(),
                                Suffix = version[3].GetString()[0],
                                Bubble = version[4].GetInt32(),
                            };
                            Available.Add(gv);
                        }
                        fromWeb = true;
                    } catch (Exception) { }
                }

                if (!fromWeb)
                {
                    var last = BubblePrints.Settings.LastLoaded;
                    if (!string.IsNullOrWhiteSpace(last))
                    {
                        Console.WriteLine("setting available = last loaded");
                        Regex regex = new(@"blueprints_raw_(\d+)\.(\d+)\.(\d+)(.)_(\d+).binz");
                        var match = regex.Match(last);
                        GameVersion v;
                        v.Major = int.Parse(match.Groups[1].Value);
                        v.Minor = int.Parse(match.Groups[2].Value);
                        v.Patch = int.Parse(match.Groups[3].Value);
                        v.Suffix = match.Groups[4].Value[0];
                        v.Bubble = int.Parse(match.Groups[5].Value);
                        Available.Add(v);
                    }
                    else
                    {
                        Console.WriteLine("setting available = known when built");
                        Available.Add(LastKnown);
                    }
                }
            }


            GoingToLoad mode;
            if (File.Exists(FileName))
                mode = GoingToLoad.FromLocalFile;
            else if (InCache)
                mode = GoingToLoad.FromCache;
            else
                mode = GoingToLoad.FromWeb;

            AvailableDetected = true;

            return mode;

        }

        private Dictionary<string, Dictionary<string, string>> defaults = new();
        private Dictionary<string, Dictionary<string, Type>> fieldTypes = new();
        private Dictionary<string, Type> nullDict = new();

        private IEnumerable<(string name, Type type)> FieldTypes(Type of)
        {
            if (of.BaseType != null)
                foreach (var t in FieldTypes(of.BaseType))
                    yield return t;

            foreach (var f in of.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                yield return (f.Name, f.FieldType);
        }

        public Type TypeForField(string typename, string field)
        {
            if (BubblePrints.Wrath == null || typename == null)
                return null;

            if (fieldTypes.TryGetValue(typename, out var fields))
            {
                if (fields.TryGetValue(field, out var fieldType))
                    return fieldType;
                return null;
            }
            else
            {
                var t = BubblePrints.Wrath.GetType(typename);
                if (t == null)
                {
                    fieldTypes[typename] = nullDict;
                    return null;
                }

                var fieldTypeDict = new Dictionary<string, Type>();

                foreach (var prop in t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                    fieldTypeDict[prop.Name] = prop.PropertyType;

                foreach (var (fname, ftype) in FieldTypes(t))
                    fieldTypeDict[fname] = ftype;

                fieldTypes[typename] = fieldTypeDict;

                if (fieldTypeDict.TryGetValue(field, out var fieldType))
                    return fieldType;

                return null;
            }
        }
        public string DefaultForField(string typename, string field)
        {
            if (typename == null || !defaults.TryGetValue(typename, out var map))
                return null;
            if (!map.TryGetValue(field, out var value))
                value = null;
            return value;
        }

        public class ConnectionProgress
        {
            public int Current;
            public int EstimatedTotal;

            public string Status => EstimatedTotal == 0 ? "??" : $"{Current}/{EstimatedTotal} - {(Current / (double)EstimatedTotal):P1}";
        }
        public async Task<bool> TryConnect(ConnectionProgress progress, string forceFileName = null)
        {
            if (importNew)
            {
                BubblePrints.SetWrathPath();

                if (BubblePrints.TryGetWrathPath(out var wrathPath))
                {
                    BubblePrints.Wrath = Assembly.LoadFrom(Path.Combine(wrathPath, "Wrath_Data", "Managed", "Assembly-CSharp.dll"));
                }

                var writeOptions = new JsonSerializerOptions
                {
                    WriteIndented = true,
                };

                var typeIdType = BubblePrints.Wrath.GetType("Kingmaker.Blueprints.JsonSystem.TypeIdAttribute");
                var typeIdGuid = typeIdType.GetField("GuidString");

                foreach (var type in BubblePrints.Wrath.GetTypes())
                {
                    var typeId = type.GetCustomAttribute(typeIdType);
                    if (typeId != null)
                    {
                        var guid = typeIdGuid.GetValue(typeId) as string;
                        if (GuidToFullTypeName.TryAdd(guid, type.FullName))
                            TypeGuidsInOrder.Add(guid);
                    }
                }

                TypeGuidsInOrder.Sort();
                FlatIndexToTypeName = new string[TypeGuidsInOrder.Count];
                for (int i = 0; i < TypeGuidsInOrder.Count; i++)
                {
                    var guid = TypeGuidsInOrder[i];
                    GuidToFlatIndex[guid] = (ushort)i;
                    FlatIndexToTypeName[i] = GuidToFullTypeName[guid];
                }

                using var bpDump = ZipFile.OpenRead(Path.Combine(ImportFolderBase, "blueprints.zip"));

                if (File.Exists(@"D:\bp_defaults.json"))
                {
                    defaults = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(@"D:\bp_defaults.json"));
                }



                Dictionary<string, string> TypenameToGuid = new();

                progress.EstimatedTotal = bpDump.Entries.Count(e => e.Name.EndsWith(".jbp"));

                HashSet<string> referencedTypes = new();

                int index = 0;

                foreach (var entry in bpDump.Entries)
                {
                    if (!entry.Name.EndsWith(".jbp")) continue;
                    try
                    {
                        var stream = entry.GetType().GetMethod("OpenInReadMode", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(entry, new object[] { false }) as Stream;
                        var reader = new StreamReader(stream);
                        var contents = reader.ReadToEnd();
                        var json = JsonSerializer.Deserialize<JsonElement>(contents);
                        var type = json.GetProperty("Data").NewTypeStr().FullName;

                        var handle = new BlueprintHandle
                        {
                            GuidText = json.Str("AssetId"),
                            Name = entry.Name[0..^4],
                            Type = type,
                            Raw = JsonSerializer.Serialize(json.GetProperty("Data"), writeOptions),
                        };
                        var components = handle.Type.Split('.');
                        if (components.Length <= 1)
                            handle.TypeName = handle.Type;
                        else
                        {
                            handle.TypeName = components.Last();
                            handle.Namespace = string.Join('.', components.Take(components.Length - 1));
                        }

                        handle.EnsureParsed();
                        foreach (var _ in handle.GetDirectReferences()) { }

                        referencedTypes.Clear();
                        BlueprintHandle.VisitObjects(handle.EnsureObj, referencedTypes);
                        handle.ComponentIndex = referencedTypes.Select(typeId => GuidToFlatIndex[typeId]).ToArray();

                        AddBlueprint(handle);



                        progress.Current++;
                        index++;
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine(e.Message);
                        Console.Error.WriteLine(e.StackTrace);
                        throw;
                    }   
                }

                Console.WriteLine("COMPLETE, press a key");
            }
            else
            {
                List<Task<List<BlueprintHandle>>> tasks = new();

                Stopwatch watch = new();
                watch.Start();

                string fileToOpen = null;

                if (forceFileName != null)
                    fileToOpen = forceFileName;
                else
                {
                    switch (GetLoadType())
                    {
                        case GoingToLoad.FromWeb:
                            Console.WriteLine("Settings file does not exist, downloading");
                            var host = "https://github.com/factubsio/BubblePrintsData/releases/download";
                            var latestVersionUrl = new Uri($"{host}/{Latest}/{filenameRoot}_{Latest}.{extension}");

                            var client = new WebClient();
                            if (!Directory.Exists(CacheDir))
                                Directory.CreateDirectory(CacheDir);

                            fileToOpen = Path.Combine(CacheDir, FileName);
                            progress.EstimatedTotal = 100;
                            client.DownloadProgressChanged += (sender, e) =>
                            {
                                progress.Current = e.ProgressPercentage;
                            };
                            string tmp = Path.Combine(CacheDir, "binz_download.tmp");
                            if (File.Exists(tmp))
                                File.Delete(tmp);
                            await client.DownloadFileTaskAsync(latestVersionUrl, tmp);
                            File.Move(tmp, fileToOpen);
                            break;
                        case GoingToLoad.FromCache:
                            fileToOpen = Path.Combine(CacheDir, FileName);
                            break;
                        case GoingToLoad.FromLocalFile:
                            Console.WriteLine("reading from local dev...");
                            fileToOpen = FileName;
                            break;
                    }
                }

                BPFile.Reader reader = new(fileToOpen);

                var ctx = reader.CreateReadContext();

                Console.WriteLine($"Reading binz from {fileToOpen}");

                var headerIn = ctx.Open(reader.Handle.GetChunk((ushort)ChunkTypes.Header).Chunks[0]);

                GameVersion header;
                int count;
                header.Major = headerIn.ReadInt32();
                header.Minor = headerIn.ReadInt32();
                header.Patch = headerIn.ReadInt32();
                header.Suffix = headerIn.ReadChar();
                header.Bubble = 0;
                count = headerIn.ReadInt32();

                Console.WriteLine($"Reading {count} blueprints for Wrath: {header}");

                var blueprintBundles = reader.Handle.GetChunks((ushort)ChunkTypes.Blueprints);

                foreach (var bundle in blueprintBundles)
                {
                    var task = Task.Run<List<BlueprintHandle>>(() =>
                    {
                        using var bundleContext = reader.CreateReadContext();
                        var res = new List<BlueprintHandle>();

                        var main = bundleContext.Open(bundle.Chunks[0]);

                        while (main.BaseStream.Position < main.BaseStream.Length)
                        {
                            var bp = new BlueprintHandle
                            {
                                GuidText = main.ReadString(),
                                Name = main.ReadString(),
                                Type = main.ReadString(),
                                Raw = main.ReadString()
                            };

                            bp.ParseType();

                            res.Add(bp);
                        }

                        byte[] guid_cache = new byte[16];
                        //if (header.Major == 1 && header.Minor == 1 && header.Patch < 6)
                        //    refId = (ushort)ChunkSubTypes.Blueprints.Components;
                        var refs = bundleContext.Open(bundle.ForSubType((ushort)ChunkSubTypes.Blueprints.References));

                        HashSet<Guid> seen = new();

                        for (int i = 0; i < res.Count; i++)
                        {
                            seen.Clear();
                            int refCount = refs.ReadInt32();
                            for (int r = 0; r < refCount; r++)
                            {
                                refs.Read(guid_cache);
                                Guid g = new(guid_cache);
                                if (seen.Add(g))
                                    res[i].BackReferences.Add(g);
                            }

                        }

                        var referencedTypes = bundleContext.Open(bundle.ForSubType((ushort)ChunkSubTypes.Blueprints.Components));
                        if (referencedTypes != null)
                        {
                            for (int i = 0; i < res.Count; i++)
                            {
                                res[i].ComponentIndex = new ushort[referencedTypes.ReadInt32()];
                                for (int r = 0; r < res[i].ComponentIndex.Length; r++)
                                    res[i].ComponentIndex[r] = referencedTypes.ReadUInt16();
                            }
                        }


                        return res;
                    });
                    task.Wait();
                    tasks.Add(task);
                }

                var loadMeta = Task.Run(() =>
                {
                    var types = ctx.Open(reader.Get((ushort)ChunkTypes.TypeNames).Main);
                    int count = types.ReadInt32();
                    FlatIndexToTypeName = new string[count];
                    for (int i = 0; i < count; i++)
                    {
                        string key = types.ReadString();
                        string val = types.ReadString();
                        GuidToFullTypeName[key] = val;
                        FlatIndexToTypeName[i] = val;
                    }


                    var strings = ctx.OpenRaw(reader.Get((ushort)ChunkTypes.Strings).Main);

                    var stringDictRaw = JsonSerializer.Deserialize<JsonElement>(strings.Span).GetProperty("strings");
                    foreach (var kv in stringDictRaw.EnumerateObject())
                    {
                        Strings[kv.Name] = kv.Value.GetString();
                    }

                    var defs = ctx.Open(reader.Get((ushort)ChunkTypes.Defaults)?.Main);
                    if (defs != null)
                    {
                        int defCount = defs.ReadInt32();
                        for (int i = 0; i < defCount; i++)
                        {
                            string key = defs.ReadString();
                            int subCount = defs.ReadInt32();
                            var map = new Dictionary<string, string>();
                            for (int s = 0; s < subCount; s++)
                            {
                                string subKey = defs.ReadString();
                                string subVal = defs.ReadString();
                                map[subKey] = subVal;
                            }
                            defaults[key] = map;
                        }
                    }

                    var searchIndex = ctx.Open(reader.Get((ushort)ChunkTypes.SearchIndex)?.Main);
                    if (searchIndex != null)
                    {
                        int kCount = searchIndex.ReadInt32();
                        for (int i = 0; i < kCount; i++)
                        {
                            string key = searchIndex.ReadString();
                            int refCount = searchIndex.ReadInt32();
                            List<int> references = new();
                            for (int r = 0; r < refCount; r++)
                            {
                                references.Add(searchIndex.Read7BitEncodedInt());
                            }
                            _IndexByWord.Add(key, references);
                        }
                    }
                });

                foreach (var bp in tasks.SelectMany(t => t.Result))
                {
                    AddBlueprint(bp);
                }
                loadMeta.Wait();
                Console.WriteLine($"Loaded {cache.Count} blueprints in {watch.ElapsedMilliseconds}ms");


                BubblePrints.Settings.LastLoaded = Path.GetFileName(fileToOpen);
                BubblePrints.SaveSettings();
                ctx.Dispose();

                //File.WriteAllLines(@"D:\areas.txt", cache.Where(b => b.TypeName == "BlueprintAbilityAreaEffect").SelectMany(bp =>
                //{
                //    return new String[]
                //    {
                //        $"//{bp.Name}",
                //        "{",
                //        $"  \"ability\": \"{bp.GuidText}\"",
                //        "  \"type\": \"bad\"",
                //        "},",

                //    };
                //}));

                //var featureType = BubblePrints.Wrath.GetType("Kingmaker.Blueprints.Classes.BlueprintFeature");
                //int featuresFound = 0;
                //List<(string, string)> features = new();
                //foreach (var bp in cache)
                //{
                //    var type = BubblePrints.Wrath.GetType(bp.Type);
                //    if (type == null) continue;
                //    if (type.IsAssignableTo(featureType))
                //    {
                //        featuresFound++;
                //        features.Add((bp.Name, bp.GuidText));
                //    }
                //}
                //Console.WriteLine("Features found: " + featuresFound);
                //File.WriteAllLines(@"D:\features.txt", features.Select(f => $"{f.Item2} {f.Item1}"));
            }


            if (generateOutput)
            {
                progress.Current = 0;
#pragma warning disable CS0162 // Unreachable code detected
                try
                {
                    Console.WriteLine("Generating");
                    File.WriteAllLines(@"D:\bp_types.txt", GuidToFullTypeName.Select(kv => $"{kv.Key} {kv.Value}"));
                    WriteBlueprints(progress);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex.Message);
                    Console.Error.WriteLine(ex.StackTrace);
                }
#pragma warning restore CS0162 // Unreachable code detected
            }

            return true;
        }
        public struct WeaponStuff
        {
            public bool IsNatural;
            public bool IsBad;
            public bool IsDouble;
            public bool IsSecond;
            public int Cost;
            public bool Good => !(IsBad || IsNatural || IsSecond);
        }

        private void WriteBlueprints(ConnectionProgress progress)
        {

            progress.EstimatedTotal += 3;
            progress.Current = 0;
            int biggestString = 0;
            int biggestStringZ = 0;
            int biggestRefList = 0;
            string mostReferred = "";

            Dictionary<Guid, List<Guid>> References = new();
            HashSet<string> keyWords = new();
            StringBuilder word = new();

            foreach (var bp in cache)
            {
                var from = Guid.Parse(bp.GuidText);
                foreach (var forwardRef in bp.GetDirectReferences())
                {
                    if (!References.TryGetValue(forwardRef, out var refList))
                    {
                        refList = new();
                        References[forwardRef] = refList;
                    }
                    refList.Add(from);
                }
            }

            using (var file = new BPFile.BPWriter("NEW_" + FileNameFor(LastKnown)))
            {
                using (var header = file.Begin((ushort)ChunkTypes.Header))
                {
                    header.Stream.Write(LastKnown.Major);
                    header.Stream.Write(LastKnown.Minor);
                    header.Stream.Write(LastKnown.Patch);
                    header.Stream.Write(LastKnown.Suffix);
                    header.Stream.Write(cache.Count);
                }

                int batchSize = 16000;

                BPFile.ChunkWriter current = null;
                for (int i = 0; i < cache.Count; i++)
                {
                    if (i % batchSize == 0)
                    {
                        current?.Dispose();
                        current = file.Begin((ushort)ChunkTypes.Blueprints);
                    }

                    var c = cache[i];

                    current.Stream.Write(c.GuidText);
                    current.Stream.Write(c.Name);
                    current.Stream.Write(c.Type);
                    current.Stream.Write(c.Raw);

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



                var rawLangDict = File.ReadAllBytes(Path.Combine(ImportFolderBase, @"Wrath_Data\StreamingAssets\Localization\enGB.json"));
                file.Write((ushort)ChunkTypes.Strings, 0, rawLangDict);


                var stringDictRaw = JsonSerializer.Deserialize<JsonElement>(rawLangDict).GetProperty("strings");
                foreach (var kv in stringDictRaw.EnumerateObject())
                {
                    Strings[kv.Name] = kv.Value.GetString();
                }


                int handleIndex = 0;
                foreach (var handle in cache)
                {
                    keyWords.Clear();

                    foreach (var element in handle.Elements)
                    {
                        if (element.key == null || element.levelDelta < 0) continue;

                        ExtractKeyWords(keyWords, word, element.key);

                        string localisedStr = JsonExtensions.ParseAsString(element.Node);
                        if (localisedStr is not null)
                        {
                            if (localisedStr is not "<string-not-present>" and not "<null-string>")
                                ExtractKeyWords(keyWords, word, localisedStr);
                        }
                        else if (element.value != null)
                        {
                            ExtractKeyWords(keyWords, word, element.value);
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
                        if (_IndexByWord.TryGetValue(keyWord, out var list))
                            list.Add(handleIndex);
                        else
                            _IndexByWord[keyWord] = new() { handleIndex };
                    }

                    handleIndex++;
                }

                Console.WriteLine("unique words: " + _IndexByWord.Count);
                ulong sum = 0;
                foreach (var (k, list) in _IndexByWord)
                {
                    sum += (ulong)list.Count;
                }

                Console.WriteLine("total index references: " + sum);

                Console.WriteLine("Continuing");


                using (var types = file.Begin((ushort)ChunkTypes.TypeNames))
                {
                    types.Stream.Write(GuidToFullTypeName.Count);
                    foreach (var k in TypeGuidsInOrder)
                    {
                        types.Stream.Write(k);
                        types.Stream.Write(GuidToFullTypeName[k]);
                    }

                    progress.Current++;
                }

                using (var chunk = file.Begin((ushort)ChunkTypes.Defaults))
                {
                    chunk.Stream.Write(defaults.Count);
                    foreach (var kv in defaults)
                    {
                        chunk.Stream.Write(kv.Key);
                        chunk.Stream.Write(kv.Value.Count);
                        foreach (var sub in kv.Value)
                        {
                            chunk.Stream.Write(sub.Key);
                            chunk.Stream.Write(sub.Value);
                        }
                    }

                    progress.Current++;
                }

                using (var chunk = file.Begin((ushort)ChunkTypes.SearchIndex))
                {
                    chunk.Stream.Write(_IndexByWord.Count);
                    foreach (var (k, list) in _IndexByWord)
                    {
                        chunk.Stream.Write(k);
                        chunk.Stream.Write(list.Count);
                        foreach (var i in list)
                            chunk.Stream.Write7BitEncodedInt(i);
                    }

                    progress.Current++;
                }
            }
            Console.WriteLine($"biggestString: {biggestString}, biggestStringZ: {biggestStringZ}, mostReferred: {mostReferred} ({biggestRefList})");
        }

        private void AddBlueprint(BlueprintHandle bp)
        {
            var guid = Guid.Parse(bp.GuidText);
            bp.NameLower = bp.Name.ToLower();
            bp.TypeNameLower = bp.TypeName.ToLower();
            bp.NamespaceLower = bp.Namespace.ToLower();
            var end = bp.Type.LastIndexOf('.');
            types.Add(bp.Type.Substring(end + 1));
            cache.Add(bp);
            Blueprints[guid] = bp;
            // preheat this
            bp.PrimeMatches(2);
        }

        public class IndexSearchState
        {
            public Dictionary<int, Dictionary<string, float>> results = new();
        };

        public List<BlueprintHandle> SearchBlueprints(string searchText, int matchBuffer, CancellationToken cancellationToken)
        {
            if (searchText?.Length == 0)
                return cache;

            List<BlueprintHandle> toSearch = cache;

            var results = new List<BlueprintHandle>() { Capacity = cache.Count };
            if (searchText[0] == '#')
            {
                StringBuilder buffer = new();
                HashSet<string> searchTerms = new();
                ExtractKeyWords(searchTerms, buffer, searchText);
                int numberOfShortTerms = searchTerms.Count(w => w.Length < 3);
                if (searchTerms.Count - numberOfShortTerms > 1)
                    searchTerms.RemoveWhere(w => w.Length < 3);

                Dictionary<int, Dictionary<string, float>> resultFilter = new();

                foreach (var (indexKey, list) in _IndexByWord)
                {
                    var matchingTerms = searchTerms.Where(t => indexKey.ContainsIgnoreCase(t)).Select(term => (term, term.Length / (float)indexKey.Length));
                    if (matchingTerms.Any())
                    {
                        foreach (var i in list)
                        {
                            if (!resultFilter.TryGetValue(i, out var score))
                            {
                                score = new Dictionary<string, float>();
                                resultFilter[i] = score;
                            }
                            foreach (var t in matchingTerms)
                            {
                                if (score.TryGetValue(t.term, out var currentScore))
                                {
                                    if (t.Item2 > currentScore)
                                        score[t.term] = t.Item2;
                                }
                                else
                                {
                                    score.Add(t.term, t.Item2);
                                }
                            }
                        }
                    }
                }

                var ranked = resultFilter.OrderByDescending(e => e.Value.Sum(kv => kv.Value));

                foreach (var r in ranked.Select(e => e.Key))
                {
                    results.Add(cache[r]);
                }

                return results;
            }

            List<string> passThrough = searchText.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();

            for (int i = 0; i < passThrough.Count; i++)
            {
                var special = passThrough[i][1..];
                bool remove = true;
                switch (passThrough[i][0])
                {
                    case '?':
                        Console.WriteLine($"filtering on has-type, before: {toSearch.Count}");
                        toSearch = toSearch.Where(b => b.ComponentsList.Any(c => c.Contains(special, StringComparison.OrdinalIgnoreCase))).ToList();
                        Console.WriteLine($"                       after: {toSearch.Count}");
                        break;
                    case '!':
                        if (special.StartsWith("bp_"))
                        {

                            toSearch = toSearch.Where(b => b.GuidText.Contains(special.Substring(3), StringComparison.OrdinalIgnoreCase)).ToList();
                        }
                        else
                        {
                            //string[] path = special.Split('/', StringSplitOptions.RemoveEmptyEntries);
                            //toSearch = toSearch.Where(b => EntryIsNotNull(b, path)).ToList();
                        }
                        break;
                    default:
                        remove = false;
                        break;
                }

                if (remove)
                {
                    passThrough[i] = "";
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }

            if (passThrough.All(c => c.Length == 0))
                return toSearch;

            searchText = string.Join(" ", passThrough.Where(c => c.Length > 0)).ToLower();

            MatchQuery query = new(searchText, BlueprintHandle.MatchProvider);
            foreach (var handle in toSearch)
            {
                query.Evaluate(handle, matchBuffer);
                if (handle.HasMatches(matchBuffer))
                    results.Add(handle);
                cancellationToken.ThrowIfCancellationRequested();
            }
            results.Sort((x, y) => y.Score(matchBuffer).CompareTo(x.Score(matchBuffer)));
            return results;
        }

        private static bool EntryIsNotNull(BlueprintHandle b, string[] path)
        {
            var obj = b.EnsureObj;
            foreach (var e in path)
            {
                if (obj.ValueKind == JsonValueKind.Object)
                {
                    if (!obj.TryGetProperty(e, out obj))
                        return false;
                }
                else if (obj.ValueKind == JsonValueKind.Array)
                {
                    if (!int.TryParse(e, out var index))
                        return false;
                    obj = obj[index];
                }
                else
                {
                    return false;
                }
            }

            if (obj.ValueKind == JsonValueKind.True)
            {
                return true;
            }
            else if (obj.ValueKind == JsonValueKind.False)
            {
                return false;
            }
            else
            {
                var raw = obj.GetRawText();
                return raw.Length > 0 && !raw.Contains("NULL", StringComparison.OrdinalIgnoreCase);
            }
        }

        private static bool[] locked = new bool[2];

        private static string Status(bool running)
        {
            return running ? "|" : " ";
        }

        public const bool debugTasks = false;

        private static void LogTask(int bufferIndex, bool starting, string text)
        {
#if DEBUG
            if (!debugTasks)
                return;

#pragma warning disable CS0162 // Unreachable code detected
            if (bufferIndex == 1)
            {
                Console.Write(Status(locked[0]).PadRight(40));
                Console.WriteLine(text);
            }
            else
            {
                Console.Write(text.PadRight(40));
                Console.WriteLine(Status(locked[1]));
            }
#pragma warning restore CS0162 // Unreachable code detected
#endif
        }

        public static void UnlockBuffer(int matchBuffer)
        {
            //Console.WriteLine($"Unlocking: {matchBuffer}");
            locked[matchBuffer] = false;
        }
        public Task<List<BlueprintHandle>> SearchBlueprintsAsync(string searchText, CancellationToken cancellationToken, int matchBuffer = 0)
        {
            //Console.WriteLine($"Locking: {matchBuffer}");
            if (locked[matchBuffer])
            {
                Console.WriteLine(" ************* ERROR: BUFFER IS LOCKED");
            }
            locked[matchBuffer] = true;
            LogTask(matchBuffer, true, $">>> Starting >>>");
            return Task.Run(() =>
            {
                Stopwatch watch = new();
                watch.Start();
                try
                {
                    var result = SearchBlueprints(searchText, matchBuffer, cancellationToken);
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return null;
                    }
                    LogTask(matchBuffer, false, $"<<< Completed <<<");
                    watch.Stop();
                    Console.WriteLine($"Search completed after: {watch.ElapsedMilliseconds}ms");
                    return result;
                }
                catch (OperationCanceledException)
                {
                    LogTask(matchBuffer, false, $"<<< Cancelled <<< ");
                    watch.Stop();
                    Console.WriteLine($"Search canelled after: {watch.ElapsedMilliseconds}ms");
                    return null;
                }
            }, cancellationToken);
        }
    }

    internal struct NameGuid
    {
        public string Name;
        public string Guid;

        public NameGuid(string name, string guid)
        {
            this.Name = name;
            this.Guid = guid;
        }

        public override bool Equals(object obj)
        {
            return obj is NameGuid other &&
                   Name == other.Name &&
                   Guid == other.Guid;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Guid);
        }

        public void Deconstruct(out string name, out string guid)
        {
            name = this.Name;
            guid = this.Guid;
        }

        public static implicit operator (string name, string guid)(NameGuid value)
        {
            return (value.Name, value.Guid);
        }

        public static implicit operator NameGuid((string name, string guid) value)
        {
            return new NameGuid(value.name, value.guid);
        }
    }

    public class Prompt : IDisposable
    {
        public Form RootForm { get; private set; }
        public  bool Result { get; private set; }

        public Prompt(string caption, Action<Panel> builder)
        {
            Result = ShowDialog(caption, builder);
        }
        //use a using statement
        private bool ShowDialog(string caption, Action<Panel> builder)
        {
            RootForm = new Form()
            {
                Width = 800,
                Height = 800,
                FormBorderStyle = FormBorderStyle.SizableToolWindow,
                Text = caption,
                StartPosition = FormStartPosition.CenterScreen,
                TopMost = true
            };

            Button confirmation = new() { Text = "Ok", DialogResult = DialogResult.OK, Height = 40 };
            confirmation.Click += (sender, e) => { RootForm.Close(); };
            confirmation.Dock = DockStyle.Bottom;
            RootForm.Controls.Add(confirmation);
            RootForm.AcceptButton = confirmation;


            var root = new Panel();
            root.Dock = DockStyle.Fill;
            RootForm.Controls.Add(root);
            builder(root);

            return RootForm.ShowDialog() == DialogResult.OK;
        }

        public void Dispose()
        {
            //See Marcus comment
            if (RootForm != null)
            {
                RootForm.Dispose();
                RootForm = null;
            }
        }
    }
}
