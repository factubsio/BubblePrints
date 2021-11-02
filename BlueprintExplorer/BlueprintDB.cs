using K4os.Compression.LZ4;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
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
        private readonly string version = "1.1";
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

        public async Task<bool> TryConnect()
        {
            BubblePrints.SetupLogging();

            List<Task<List<BlueprintHandle>>> tasks = new();

            Stopwatch watch = new();
            watch.Start();

            const bool generateOutput = false;
            string fileToOpen = null;

            switch (GetLoadType())
            {
                case GoingToLoad.FromWeb:
                    Console.WriteLine("Settings file does not exist, downloading");
                    var host = "https://github.com/factubsio/BubblePrints/releases/download/";
                    var latestVersionUrl = new Uri($"{host}/{version}/{filenameRoot}.{extension}");

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

            int count = binary.ReadInt32();
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
                        else {
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

                        res.Add(bp);
                    }

                    return res;
                });
                tasks.Add(task);
            }


            float loadTime = watch.ElapsedMilliseconds;
            Console.WriteLine($"Loaded {cache.Count} blueprints in {watch.ElapsedMilliseconds}ms");

            if (generateOutput)
            {
#pragma warning disable CS0162 // Unreachable code detected
                WriteBlueprints();
#pragma warning restore CS0162 // Unreachable code detected
            }

            binary.Dispose();
            file.Dispose();

            var addWatch = new Stopwatch();
            addWatch.Start();
            foreach (var bp in tasks.SelectMany(t => t.Result))
            {
                AddBlueprint(bp);
            }
            Console.WriteLine($"added {cache.Count} blueprints in {addWatch.ElapsedMilliseconds}ms");
            return true;
        }

        private void WriteBlueprints()
        {
            int biggestString = 0;
            int biggestStringZ = 0;
            byte[] scratchpad = new byte[10_000_000];
            using (var file = File.OpenWrite("blueprints_raw.binz"))
            {
                using var binary = new BinaryWriter(file);
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
                }

                binary.Seek(tocAt, SeekOrigin.Begin);
                foreach (var t in toc)
                    binary.Write(t);

            }
            Console.WriteLine($"biggestString: {biggestString}, biggestStringZ: {biggestStringZ}");
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
            if (searchText?.Length > 0) {
                var results = new List<BlueprintHandle>() { Capacity = cache.Count };
                MatchQuery query = new(searchText, BlueprintHandle.MatchProvider);
                foreach (var handle in cache) {
                    query.Evaluate(handle, matchBuffer);
                    if (handle.HasMatches(matchBuffer))
                        results.Add(handle);
                    cancellationToken.ThrowIfCancellationRequested();
                }
                results.Sort((x, y) => y.Score(matchBuffer).CompareTo(x.Score(matchBuffer)));
                return results;
                //return cache.Select(h => query.Evaluate(h)).OfType<BlueprintHandle>().Where(h => h.HasMatches()).OrderByDescending(h => h.Score()).ToList();
            }
            else
                return cache;

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
            locked[matchBuffer] = true;
            LogTask(matchBuffer, true, $">>> Starting >>>");
            return Task.Run(() =>
            {
                Stopwatch watch = new();
                watch.Start();
                try
                {
                    var result = SearchBlueprints(searchText, matchBuffer, cancellationToken);
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
}
