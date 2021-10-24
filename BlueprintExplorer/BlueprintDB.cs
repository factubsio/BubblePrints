using K4os.Compression.LZ4;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BlueprintExplorer {
    public partial class BlueprintDB
    {
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool AllocConsole();


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

        public Task<bool> TryConnect()
        {

#if DEBUG
            AllocConsole();
#endif
            List<Task<List<BlueprintHandle>>> tasks = new();

            Stopwatch watch = new();
            watch.Start();

            const bool generateOutput = false;

            static FileStream OpenFile()
            {
                var path = Properties.Settings.Default.BlueprintDBPath;
                try {
                    return File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                }
                catch {
                    OpenFileDialog fileDialog = new OpenFileDialog();
                    fileDialog.Title = "Please locate the blueprint database (blueprints_raw.binz)";
                    fileDialog.InitialDirectory = "c:\\";
                    fileDialog.Filter = "Blueprint Database (*.binz)|*.binz";
                    fileDialog.FilterIndex = 2;
                    fileDialog.RestoreDirectory = true;

                    if (fileDialog.ShowDialog() == DialogResult.OK) {
                        path = fileDialog.FileName;
                        if (path?.Length > 0) {
                            Properties.Settings.Default.BlueprintDBPath = path;
                            Properties.Settings.Default.Save();
                            return File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                        }
                    }
                }
                return null;
            }

            var file = OpenFile();
            if (file == null)
                return null;
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
                watch.Restart();
                WriteBlueprints();
                watch.Stop();
                Console.WriteLine($"Wrote blueprints in {watch.ElapsedMilliseconds}ms");
            }

            return Task.Run(() => 
            {
                Console.WriteLine("waiting...");
                Task.WaitAll(tasks.ToArray());
                watch.Stop();
                binary.Dispose();
                file.Dispose();

                foreach (var bp in tasks.SelectMany(t => t.Result))
                {
                    AddBlueprint(bp);
                }

                return true;
            });
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
        }

        public List<BlueprintHandle> SearchBlueprints(string searchText)
        {
            var query = new MatchQuery(searchText);
            query.UpdateSearchResults(cache);
            if (searchText?.Length > 0)
                return cache.Where(h => h.HasMatches()).ToList();
            else
                return cache;

        }
    }
}
