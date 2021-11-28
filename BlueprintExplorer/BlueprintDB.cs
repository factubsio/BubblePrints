using K4os.Compression.LZ4;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BlueprintExplorer {
    public partial class BlueprintDB
    {
        private static BlueprintDB _Instance;
        public static BlueprintDB Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = new();
                return _Instance;
            }
        }

        public readonly Dictionary<Guid, BlueprintHandle> Blueprints = new();
        private List<BlueprintHandle> cache = new();
        public readonly HashSet<string> types = new();

        public struct FileSection
        {
            public int Offset;
            public int Legnth;
        }

        public enum GoingToLoad
        {
            FromLocalFile,
            FromSettingsFile,
            FromWeb,
        }

        private readonly string filenameRoot = "blueprints_raw";
        private readonly string version = "1.1_bbpe2";
        private readonly string extension = "binz";

        string FileName => $"{filenameRoot}_{version}.{extension}";

        public GoingToLoad GetLoadType()
        {
            if (File.Exists(FileName))
                return GoingToLoad.FromLocalFile;
            else if (File.Exists(Properties.Settings.Default.BlueprintDBPath))
                return GoingToLoad.FromSettingsFile;
            else
                return GoingToLoad.FromWeb;
        }

        private const int LatestVersion = 2;

        public string[] ComponentTypeLookup;

        public async Task<bool> TryConnect()
        {

            List<Task<List<BlueprintHandle>>> tasks = new();

            Stopwatch watch = new();
            watch.Start();

            const bool generateOutput = false;
            string fileToOpen = null;

            switch (GetLoadType())
            {
                case GoingToLoad.FromWeb:
                    Console.WriteLine("Settings file does not exist, downloading");
                    var host = "https://github.com/factubsio/BubblePrintsData/releases/download";
                    var latestVersionUrl = new Uri($"{host}/{version}/{filenameRoot}_{version}.{extension}");

                    var client = new WebClient();
                    var userLocalFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BubblePrints");
                    if (!Directory.Exists(userLocalFolder))
                        Directory.CreateDirectory(userLocalFolder);

                    fileToOpen = Path.Combine(userLocalFolder, FileName);
                    await client.DownloadFileTaskAsync(latestVersionUrl, fileToOpen);
                    Properties.Settings.Default.BlueprintDBPath = fileToOpen;
                    Properties.Settings.Default.Save();
                    break;
                case GoingToLoad.FromSettingsFile:
                    fileToOpen = Properties.Settings.Default.BlueprintDBPath;
                    break;
                case GoingToLoad.FromLocalFile:
                    fileToOpen = FileName;
                    break;
            }

            FileStream OpenFile()
            {
                return File.Open(fileToOpen, FileMode.Open, FileAccess.Read, FileShare.Read);
            }

            var file = OpenFile();
            if (file == null)
                return false;
            var binary = new BinaryReader(file);

            Console.WriteLine($"Reading binz from {fileToOpen}");

            int count = binary.ReadInt32();
            int bbpeVersion;
            if (count == 0)
            {
                var magic = binary.ReadChars(4);
                Console.WriteLine(string.Join("", magic));
                bbpeVersion = binary.ReadInt32();
                binary.ReadUInt32(); //skip stuff?
                count = binary.ReadInt32();
            }
            else
            {
                bbpeVersion = 0;
            }
            Console.WriteLine($"BBPE: {bbpeVersion}");

            int batchSize = binary.ReadInt32();
            cache.Capacity = count;
            int sectionCount = binary.ReadInt32();
            FileSection[] sections = new FileSection[sectionCount];
            for (int i = 0; i < sections.Length; i++)
            {
                int offset = binary.ReadInt32();
                sections[i].Offset = offset;

                if (i > 0)
                {
                    int actualOffset = (offset > 0) ? offset : (int)file.Length;
                    sections[i - 1].Legnth = actualOffset - sections[i - 1].Offset;
                }
            }

            for (int i = 0; i < count; i += batchSize)
            {
                int batchItems = Math.Min(count - i, batchSize);
                int start = i;
                int end = start + batchItems;
                FileSection section = sections[i / batchSize];

                var task = Task.Run<List<BlueprintHandle>>(() =>
                {
                    byte[] scratchpad = new byte[10_0000_000];
                    byte[] guid_cache = new byte[16];
                    using var batchFile = OpenFile();
                    using var batchReader = new BinaryReader(batchFile);
                    batchReader.BaseStream.Seek(section.Offset, SeekOrigin.Begin);

                    List<BlueprintHandle> res = new();
                    res.Capacity = batchItems;
                    for (int x = start; x < end; x++)
                    {
                        BlueprintHandle bp = new();
                        bp.GuidText = batchReader.ReadString();
                        bp.Name = batchReader.ReadString();
                        bp.Type = batchReader.ReadString();
                        var components = bp.Type.Split('.');
                        if (components.Length <= 1)
                            bp.TypeName = bp.Type;
                        else
                        {
                            bp.TypeName = components.Last();
                            bp.Namespace = string.Join('.', components.Take(components.Length - 1));
                        }
                        bool rawCompressed = batchReader.ReadBoolean();
                        if (rawCompressed)
                        {
                            int outLen = batchReader.ReadInt32();
                            int inLen = batchReader.ReadInt32();
                            int xLen = LZ4Codec.Decode(batchReader.ReadBytes(inLen), scratchpad);
                            if (xLen != outLen)
                            {
                                Console.WriteLine("decompression failure");
                            }
                            bp.Raw = Encoding.ASCII.GetString(scratchpad, 0, xLen);
                        }
                        else
                        {
                            bp.Raw = batchReader.ReadString();
                        }

                        bp.EnsureParsed();

                        if (bbpeVersion >= 1)
                        {
                            int refCount = batchReader.ReadInt32();
                            for (int i = 0; i < refCount; i++)
                            {
                                batchReader.Read(guid_cache, 0, 16);
                                bp.BackReferences.Add(new Guid(guid_cache));
                            }
                        }
                        if (bbpeVersion >= 2)
                        {
                            int componentIndexCount = batchReader.ReadInt32();
                            bp.ComponentIndex = new UInt16[componentIndexCount];
                            for (int i = 0; i < componentIndexCount; i++)
                            {
                                bp.ComponentIndex[i] = batchReader.ReadUInt16();
                            }
                        }

                        res.Add(bp);
                    }

                    return res;
                });
                tasks.Add(task);
            }

            var loadComponentDict = Task.Run<string[]>(() =>
            {
                string[] componentDict;
                using var batchFile = OpenFile();
                using var batchReader = new BinaryReader(batchFile);
                int offset = sections[^1].Offset;
                Console.WriteLine($"dict offset: {offset}");
                batchReader.BaseStream.Seek(offset, SeekOrigin.Begin);

                componentDict = new string[batchReader.ReadInt32()];

                for (int i =0; i < componentDict.Length; i++)
                {
                    string val = batchReader.ReadString();
                    UInt16 index = batchReader.ReadUInt16();
                    componentDict[index] = val;
                }

                return componentDict;
            });


            float loadTime = watch.ElapsedMilliseconds;
            Console.WriteLine($"Loaded {cache.Count} blueprints in {watch.ElapsedMilliseconds}ms");


            binary.Dispose();
            file.Dispose();

            var addWatch = new Stopwatch();
            addWatch.Start();
            foreach (var bp in tasks.SelectMany(t => t.Result))
            {
                AddBlueprint(bp);
            }

            ComponentTypeLookup = loadComponentDict.Result;
            Console.WriteLine($"loaded {ComponentTypeLookup.Length} component types");


            if (generateOutput)
            {
#pragma warning disable CS0162 // Unreachable code detected
                WriteBlueprints();
#pragma warning restore CS0162 // Unreachable code detected
            }
            Console.WriteLine($"added {cache.Count} blueprints in {addWatch.ElapsedMilliseconds}ms");

            //static int GetCR(JsonElement obj)
            //{
            //    var components = obj.GetProperty("Components");
            //    var experience = components.EnumerateArray().FirstOrDefault(c => c.TypeString() == "Kingmaker.Blueprints.Classes.Experience.Experience");
            //    if (experience.ValueKind == JsonValueKind.Undefined)
            //        return 0;

            //    int cr = experience.Int("CR");
            //    float modifier = experience.Float("Modifier");
            //    if (cr == 1 && Math.Abs(modifier - 0.5f) < 0.001f)
            //        return 0;

            //    return cr;

            //}

            //var units = cache.Where(bp => bp.EnsureObj.TypeString() == "Kingmaker.Blueprints.BlueprintUnit")
            //                 .Select(bp => (bp, cr: GetCR(bp.obj)))
            //                 .Where(i => i.cr > 0)
            //                 .OrderBy(i => i.cr);

            //File.WriteAllLines($"D:/units_by_cr.txt", units.Select(i => $"{i.cr,-3}    {i.bp.GuidText}    {i.bp.Name}"));

            var byType = cache.ToLookup(bp => bp.EnsureObj.TypeString());
            Dictionary<string, WeaponStuff> weaponStuffs = new();


            foreach (var (k, v) in byType["Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon"].Select(bp => (bp.GuidText, GetStuff(bp))))
                weaponStuffs[k] = v;

            if (Directory.Exists("vendor_tables"))
                Directory.Delete("vendor_tables", true);
            Directory.CreateDirectory("vendor_tables");

            GatherItems("Kingmaker.Blueprints.Items.Armors.BlueprintItemArmor", "Armor");
            GatherItems("Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon", "Weapon", IsWeaponGood);
            GatherItems("Kingmaker.Blueprints.Items.Shields.BlueprintItemShield", "Shield");
            GatherItems("Kingmaker.Blueprints.Items.Equipment.BlueprintItemEquipmentBelt", "Belt");
            GatherItems("Kingmaker.Blueprints.Items.Equipment.BlueprintItemEquipmentFeet", "Feet");
            GatherItems("Kingmaker.Blueprints.Items.Equipment.BlueprintItemEquipmentGlasses", "Glasses");
            GatherItems("Kingmaker.Blueprints.Items.Equipment.BlueprintItemEquipmentGloves", "Gloves");
            GatherItems("Kingmaker.Blueprints.Items.Equipment.BlueprintItemEquipmentHand", "Hand");
            GatherItems("Kingmaker.Blueprints.Items.Equipment.BlueprintItemEquipmentHandSimple", "HandSimple");
            GatherItems("Kingmaker.Blueprints.Items.Equipment.BlueprintItemEquipmentHead", "Head");
            GatherItems("Kingmaker.Blueprints.Items.Equipment.BlueprintItemEquipmentNeck", "Neck");
            GatherItems("Kingmaker.Blueprints.Items.Equipment.BlueprintItemEquipmentRing", "Ring");
            GatherItems("Kingmaker.Blueprints.Items.Equipment.BlueprintItemEquipmentShirt", "Shirt");
            GatherItems("Kingmaker.Blueprints.Items.Equipment.BlueprintItemEquipmentShoulders", "Shoulders");
            GatherItems("Kingmaker.Blueprints.Items.Equipment.BlueprintItemEquipmentSimple", "Simple");
            GatherItems("Kingmaker.Blueprints.Items.Equipment.BlueprintItemEquipmentUsable", "Usable");
            GatherItems("Kingmaker.Blueprints.Items.Equipment.BlueprintItemEquipmentWrist", "Wrist");


            void GatherItems(string type, string file, params Func<BlueprintHandle, bool> []filters)
            {
                IEnumerable<BlueprintHandle> items = byType[type];
                foreach (var filter in filters)
                    items = items.Where(filter);

                var byCost = items
                    .Select(bp => (bp, cost: bp.EnsureObj.Int("m_Cost")))
                    .Where(bp_cost => bp_cost.cost > 100)
                    .OrderByDescending(bp_cost => bp_cost.cost);

                var path = $"vendor_tables/vendortable_{file}.txt";

                File.WriteAllLines(path, byCost.Select(i => $"{i.bp.Name}:{i.bp.GuidText}:{i.cost}"));
            }

            WeaponStuff GetStuff(BlueprintHandle bp)
            {
                WeaponStuff stuff = new();
                if (bp.obj.TryGetProperty("m_Type", out var linkText))
                {
                    var (link, target) = BlueprintHandle.ParseReference(linkText.GetString());
                    if (link == null)
                    {
                        Console.WriteLine($"{bp.Name} - NO m_Type on Weapon!!!");
                        System.Windows.Forms.Application.Exit();
                    }

                    if (Blueprints.TryGetValue(link.Guid(), out var type))
                    {
                        if (type.EnsureObj.TryGetProperty("m_FighterGroupFlags", out var fighterGroup))
                        {
                            stuff.IsDouble = fighterGroup.GetString().Contains("Double");
                            stuff.IsSecond = stuff.IsDouble && !bp.obj.True("Double");
                        }
                        else
                        {
                            stuff.IsBad = true;
                        }

                        stuff.IsNatural = type.EnsureObj.True("m_IsNatural");

                    }
                    else
                    {
                        stuff.IsBad = true;
                    }


                }
                return stuff;
            }

            bool IsWeaponGood(BlueprintHandle bp) => weaponStuffs[bp.GuidText].Good;

            //foreach (var armor in .GroupBy(bp => (int)Math.Log(bp.EnsureObj.Int("m_Cost") / 4, 11)).Where(g => g.Key >= 2).OrderByDescending(g => g.Key))
            //{
            //    Console.WriteLine($"Bucket: {armor.Key}");
            //    foreach (var a in armor)
            //    {
            //        Console.WriteLine($"  {a.Name}  -  {a.EnsureObj.Int("m_Cost") / 4}");
            //    }
            //}




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
            byte[] scratchpad = new byte[10_000_000];

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

            Dictionary<string, UInt16> uniqueComponents = new();


            using (var file = File.OpenWrite("blueprints_raw_NEW.binz"))
            {
                using var binary = new BinaryWriter(file);
                binary.Write(0);
                binary.Write("BBPE".AsSpan());
                binary.Write(LatestVersion);
                binary.Write(0);

                binary.Write(cache.Count);
                const int batchSize = 16000;
                binary.Write(batchSize);

                int[] toc = new int[64];
                binary.Write(toc.Length);
                int tocAt = (int)binary.Seek(0, SeekOrigin.Current);
                foreach (var t in toc)
                    binary.Write(t);

                for (int i = 0; i < cache.Count; i++)
                {
                    if (i % batchSize == 0)
                    {
                        toc[i / batchSize] = (int)binary.Seek(0, SeekOrigin.Current);
                    }

                    var c = cache[i];

                    HashSet<string> components = new(c.Objects);

                    binary.Write(c.GuidText);
                    binary.Write(c.Name);
                    binary.Write(c.Type);
                    if (c.Raw.Length > 100)
                    {
                        binary.Write(true);
                        var raw = Encoding.ASCII.GetBytes(c.Raw);
                        int compressed = LZ4Codec.Encode(raw.AsSpan(), scratchpad.AsSpan(), LZ4Level.L10_OPT);
                        binary.Write(c.Raw.Length);
                        binary.Write(compressed);
                        binary.Write(scratchpad, 0, compressed);

                        if (c.Raw.Length > biggestString)
                            biggestString = c.Raw.Length;
                        if (compressed > biggestStringZ)
                            biggestStringZ = compressed;
                    }
                    else
                    {
                        binary.Write(false);
                        binary.Write(c.Raw);
                    }


                    if (References.TryGetValue(Guid.Parse(c.GuidText), out var refList))
                    {
                        binary.Write(refList.Count);
                        foreach (var backRef in refList)
                            binary.Write(backRef.ToByteArray());
                        if (refList.Count > biggestRefList)
                        {
                            biggestRefList = refList.Count;
                            mostReferred = c.Name;
                        }
                    }
                    else
                    {
                        binary.Write(0);
                    }

                    binary.Write(components.Count);
                    foreach (var componentType in components)
                    {
                        if (!uniqueComponents.TryGetValue(componentType, out var index))
                        {
                            index = (UInt16)uniqueComponents.Count;
                            uniqueComponents.Add(componentType, index);
                        }
                        binary.Write(index);
                    }
                }

                toc[^1] = (int)binary.BaseStream.Position;
                binary.Write(uniqueComponents.Count);

                foreach (var kv in uniqueComponents)
                {
                    binary.Write(kv.Key);
                    binary.Write(kv.Value);
                }

                binary.Seek(tocAt, SeekOrigin.Begin);
                foreach (var t in toc)
                    binary.Write(t);

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
}
