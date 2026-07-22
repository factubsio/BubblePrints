using System.Diagnostics;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using BlueprintExplorer;

namespace BubbleprintMcpWorker.Tools;

public sealed class BloomProfileTool : IMcpTool
{
    private const ulong FnvOffset = 14695981039346656037;
    private const ulong FnvPrime = 1099511628211;
    private const ulong HashStep = 0x9e3779b97f4a7c15;

    public string Name => "bloom-profile";
    public string Description =>
        "Stream all raw JSON and profile property-key Bloom filters. Args: bits (default [128,256,512]), hashes (default 2), salted (default false), keys (optional).";

    public object Handle(BlueprintDB db, JsonObject args)
    {
        int[] sizes = ReadSizes(args);
        int hashCount = Math.Clamp(args["hashes"]?.GetValue<int>() ?? 2, 1, 8);
        bool salted = args["salted"]?.GetValue<bool>() ?? false;
        int blueprintCount = db.BlueprintsInOrder.Count;
        int largestBits = sizes[^1];
        int largestWords = largestBits / 64;
        var filters = new Dictionary<int, ulong[]>
        {
            [largestBits] = new ulong[checked(blueprintCount * largestWords)],
        };
        var keyStats = new Dictionary<ulong, KeyStats>();
        var documentKeys = new HashSet<ulong>();
        long rawBytes = 0;
        long propertyOccurrences = 0;
        long distinctDocumentKeys = 0;

        var stopwatch = Stopwatch.StartNew();
        for (int blueprintIndex = 0; blueprintIndex < blueprintCount; blueprintIndex++)
        {
            var raw = db.BlueprintsInOrder[blueprintIndex].RawUtf8;
            rawBytes += raw.Length;
            documentKeys.Clear();

            var reader = new Utf8JsonReader(raw, new JsonReaderOptions { MaxDepth = 128 });
            while (reader.Read())
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                    continue;

                propertyOccurrences++;
                ulong hash = HashPropertyName(ref reader);
                SetBits(
                    filters[largestBits],
                    blueprintIndex,
                    largestWords,
                    largestBits,
                    hash,
                    hashCount,
                    salted);

                if (documentKeys.Add(hash) && !keyStats.ContainsKey(hash))
                    keyStats.Add(hash, new KeyStats(reader.GetString()!));
            }

            distinctDocumentKeys += documentKeys.Count;
            foreach (ulong hash in documentKeys)
                keyStats[hash].AddDocument(blueprintIndex);
        }

        double scanAndBuildMs = stopwatch.Elapsed.TotalMilliseconds;

        stopwatch.Restart();
        for (int sizeIndex = sizes.Length - 2; sizeIndex >= 0; sizeIndex--)
        {
            int bits = sizes[sizeIndex];
            int words = bits / 64;
            int sourceBits = sizes[sizeIndex + 1];
            int sourceWords = sourceBits / 64;
            var source = filters[sourceBits];
            var destination = new ulong[checked(blueprintCount * words)];

            for (int blueprintIndex = 0; blueprintIndex < blueprintCount; blueprintIndex++)
            {
                int sourceOffset = blueprintIndex * sourceWords;
                int destinationOffset = blueprintIndex * words;
                for (int sourceWord = 0; sourceWord < sourceWords; sourceWord++)
                    destination[destinationOffset + (sourceWord % words)] |= source[sourceOffset + sourceWord];
            }

            filters.Add(bits, destination);
        }
        double deriveMs = stopwatch.Elapsed.TotalMilliseconds;

        var selectedKeys = SelectKeys(args, keyStats, blueprintCount);
        var keyResults = new List<object>();
        foreach (string key in selectedKeys)
        {
            ulong hash = Hash(Encoding.UTF8.GetBytes(key));
            int exact = keyStats.TryGetValue(hash, out var stats) && stats.Name == key
                ? stats.Documents
                : 0;
            var filterResults = new List<object>();

            foreach (int bits in sizes)
            {
                int positives = CountPositives(
                    filters[bits],
                    blueprintCount,
                    bits,
                    hash,
                    hashCount,
                    salted);
                int falsePositives = positives - exact;
                int absent = blueprintCount - exact;
                filterResults.Add(new
                {
                    bits,
                    positives,
                    falsePositives,
                    falsePositiveRate = absent == 0 ? 0 : (double)falsePositives / absent,
                    rejectedFraction = 1.0 - (double)positives / blueprintCount,
                });
            }

            keyResults.Add(new
            {
                key,
                exactDocuments = exact,
                documentFraction = (double)exact / blueprintCount,
                filters = filterResults,
            });
        }

        var configurations = sizes.Select(bits =>
        {
            var filter = filters[bits];
            long setBits = 0;
            foreach (ulong word in filter)
                setBits += BitOperations.PopCount(word);

            return new
            {
                bits,
                bytes = filter.LongLength * sizeof(ulong),
                averageSaturation = (double)setBits / (blueprintCount * bits),
            };
        }).ToArray();

        var postingEstimates = new[] { 0.001, 0.01, 0.05, 0.1, 0.25, 0.5, 1.0 }
            .Select(maxDocumentFraction =>
            {
                int maxDocuments = Math.Max(
                    1,
                    (int)(blueprintCount * maxDocumentFraction));
                var included = keyStats.Values
                    .Where(stats => stats.Documents <= maxDocuments)
                    .ToArray();
                long entries = included.Sum(stats => (long)stats.Documents);
                return new
                {
                    maxDocumentFraction,
                    keys = included.Length,
                    entries,
                    int32Bytes = entries * sizeof(int),
                    deltaVarintBytes = included.Sum(stats => stats.DeltaVarintBytes),
                };
            })
            .ToArray();

        return new
        {
            blueprints = blueprintCount,
            rawBytes,
            propertyOccurrences,
            uniquePropertyNames = keyStats.Count,
            averageDistinctKeysPerBlueprint = (double)distinctDocumentKeys / blueprintCount,
            hashes = hashCount,
            salted,
            scanAndBuildMs,
            deriveSmallerFiltersMs = deriveMs,
            configurations,
            exactPostingEstimates = postingEstimates,
            keys = keyResults,
        };
    }

    private static int[] ReadSizes(JsonObject args)
    {
        int[] sizes = args["bits"] is JsonArray requested
            ? requested.Select(node => node?.GetValue<int>() ?? 0).ToArray()
            : [128, 256, 512];

        sizes = sizes.Distinct().Order().ToArray();
        if (sizes.Length == 0 || sizes.Any(bits => bits < 64 || bits > 4096 || !BitOperations.IsPow2(bits)))
            throw new ArgumentException("'bits' entries must be unique powers of two from 64 through 4096.");

        for (int i = 1; i < sizes.Length; i++)
        {
            if (sizes[i] != sizes[i - 1] * 2)
                throw new ArgumentException("'bits' entries must be consecutive powers of two.");
        }

        return sizes;
    }

    private static IReadOnlyList<string> SelectKeys(
        JsonObject args,
        Dictionary<ulong, KeyStats> keyStats,
        int blueprintCount)
    {
        var selected = new HashSet<string>(StringComparer.Ordinal);
        if (args["keys"] is JsonArray requested)
        {
            foreach (var node in requested)
            {
                if (node is not null)
                    selected.Add(node.GetValue<string>());
            }
        }
        else
        {
            selected.UnionWith(["$type", "name", "Comment", "Components", "Actions", "Description"]);
        }

        foreach (double targetFraction in new[] { 0.0001, 0.001, 0.01, 0.1, 0.5 })
        {
            int targetCount = Math.Max(1, (int)(blueprintCount * targetFraction));
            var nearest = keyStats.Values.MinBy(stats => Math.Abs(stats.Documents - targetCount));
            if (nearest is not null)
                selected.Add(nearest.Name);
        }

        return selected.Order(StringComparer.Ordinal).ToArray();
    }

    private static ulong HashPropertyName(ref Utf8JsonReader reader)
    {
        if (!reader.ValueIsEscaped)
            return Hash(reader.ValueSpan);

        return Hash(Encoding.UTF8.GetBytes(reader.GetString()!));
    }

    private static ulong Hash(ReadOnlySpan<byte> value)
    {
        ulong hash = FnvOffset;
        foreach (byte item in value)
        {
            hash ^= item;
            hash *= FnvPrime;
        }
        return Mix(hash);
    }

    private static void SetBits(
        ulong[] filter,
        int blueprintIndex,
        int words,
        int bits,
        ulong hash,
        int hashCount,
        bool salted)
    {
        int offset = blueprintIndex * words;
        if (salted)
            hash = Mix(hash ^ ((ulong)blueprintIndex * HashStep));

        for (int i = 0; i < hashCount; i++)
        {
            int bit = BloomBit(hash, i, bits);
            filter[offset + bit / 64] |= 1UL << (bit % 64);
        }
    }

    private static int CountPositives(
        ulong[] filter,
        int blueprintCount,
        int bits,
        ulong hash,
        int hashCount,
        bool salted)
    {
        int positives = 0;
        int words = bits / 64;
        for (int blueprintIndex = 0; blueprintIndex < blueprintCount; blueprintIndex++)
        {
            int offset = blueprintIndex * words;
            ulong documentHash = salted
                ? Mix(hash ^ ((ulong)blueprintIndex * HashStep))
                : hash;
            bool present = true;
            for (int i = 0; i < hashCount; i++)
            {
                int bit = BloomBit(documentHash, i, bits);
                if ((filter[offset + bit / 64] & (1UL << (bit % 64))) == 0)
                {
                    present = false;
                    break;
                }
            }

            if (present)
                positives++;
        }
        return positives;
    }

    private static int BloomBit(ulong hash, int index, int bits) => index switch
    {
        0 => (int)(hash & (ulong)(bits - 1)),
        1 => (int)((hash >> 32) & (ulong)(bits - 1)),
        _ => (int)(Mix(hash + (ulong)index * HashStep) & (ulong)(bits - 1)),
    };

    private static ulong Mix(ulong value)
    {
        value ^= value >> 30;
        value *= 0xbf58476d1ce4e5b9;
        value ^= value >> 27;
        value *= 0x94d049bb133111eb;
        return value ^ (value >> 31);
    }

    private sealed class KeyStats(string name)
    {
        public string Name { get; } = name;
        public int Documents { get; set; }
        public long DeltaVarintBytes { get; private set; }
        private int lastDocument = -1;

        public void AddDocument(int document)
        {
            uint delta = (uint)(lastDocument < 0
                ? document
                : document - lastDocument);
            DeltaVarintBytes += VarintSize(delta);
            Documents++;
            lastDocument = document;
        }

        private static int VarintSize(uint value)
        {
            int size = 1;
            while (value >= 0x80)
            {
                value >>= 7;
                size++;
            }
            return size;
        }
    }
}
