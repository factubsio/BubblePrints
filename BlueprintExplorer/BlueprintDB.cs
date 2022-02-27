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

        public static readonly string ImportFolderBase = @"D:\WOTR-1.2-DEBUG";

        #region DEV
        bool generateOutput = false;
        bool importNew = false;
        bool forceLastKnown = true;
        #endregion

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

        private readonly GameVersion LastKnown = new(1, 2, 0, 'f', 1);

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

        private static Dictionary<string, Dictionary<string, string>> defaults = new();
        public static string DefaultForField(string typename, string field)
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
        public async Task<bool> TryConnect(ConnectionProgress progress)
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

                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine(e.Message);
                        Console.Error.WriteLine(e.StackTrace);
                        throw;
                    }   
                }

                Console.WriteLine("COMPLETE, press a key");
                Console.ReadKey();
                Console.WriteLine("Continuing");
            }
            else
            {
                List<Task<List<BlueprintHandle>>> tasks = new();

                Stopwatch watch = new();
                watch.Start();

                string fileToOpen = null;

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
                        await client.DownloadFileTaskAsync(latestVersionUrl, fileToOpen);
                        break;
                    case GoingToLoad.FromCache:
                        fileToOpen = Path.Combine(CacheDir, FileName);
                        break;
                    case GoingToLoad.FromLocalFile:
                        Console.WriteLine("reading from local dev...");
                        fileToOpen = FileName;
                        break;
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

                        for (int i = 0; i < res.Count; i++)
                        {
                            int refCount = refs.ReadInt32();
                            for (int r = 0; r < refCount; r++)
                            {
                                refs.Read(guid_cache);
                                res[i].BackReferences.Add(new Guid(guid_cache));
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
                });

                float loadTime = watch.ElapsedMilliseconds;
                Console.WriteLine($"Loaded {cache.Count} blueprints in {watch.ElapsedMilliseconds}ms");

                var addWatch = new Stopwatch();
                addWatch.Start();
                foreach (var bp in tasks.SelectMany(t => t.Result))
                {
                    AddBlueprint(bp);
                }
                loadMeta.Wait();


                BubblePrints.Settings.LastLoaded = Path.GetFileName(fileToOpen);
                BubblePrints.SaveSettings();
                ctx.Dispose();

            }


            if (generateOutput)
            {
#pragma warning disable CS0162 // Unreachable code detected
                try
                {
                    Console.WriteLine("Generating");
                    File.WriteAllLines(@"D:\bp_types.txt", GuidToFullTypeName.Select(kv => $"{kv.Key} {kv.Value}"));
                    WriteBlueprints();
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

        private void WriteBlueprints()
        {
            int biggestString = 0;
            int biggestStringZ = 0;
            int biggestRefList = 0;
            string mostReferred = "";

            Dictionary<Guid, List<Guid>> References = new();

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
                }

                current?.Dispose();



                var rawLangDict = File.ReadAllBytes(Path.Combine(ImportFolderBase, @"Wrath_Data\StreamingAssets\Localization\enGB.json"));
                file.Write((ushort)ChunkTypes.Strings, 0, rawLangDict);

                using (var types = file.Begin((ushort)ChunkTypes.TypeNames))
                {
                    types.Stream.Write(GuidToFullTypeName.Count);
                    foreach (var k in TypeGuidsInOrder)
                    {
                        types.Stream.Write(k);
                        types.Stream.Write(GuidToFullTypeName[k]);
                    }
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

        public List<BlueprintHandle> SearchBlueprints(string searchText, int matchBuffer, CancellationToken cancellationToken)
        {
            if (searchText?.Length == 0)
                return cache;

            List<BlueprintHandle> toSearch = cache;

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
                        string[] path = special.Split('/', StringSplitOptions.RemoveEmptyEntries);
                        toSearch = toSearch.Where(b => EntryIsNotNull(b, path)).ToList();
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

            var results = new List<BlueprintHandle>() { Capacity = cache.Count };
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
