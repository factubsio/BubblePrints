using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using BlueprintExplorer;

namespace BubbleprintMcpWorker.Tools;

public sealed class ScalarIndexProfileTool : IMcpTool
{
    private const ulong FnvOffset = 14695981039346656037;
    private const ulong FnvPrime = 1099511628211;
    private const ulong SecondaryOffset = 7809847782465536322;
    private const ulong SecondaryPrime = 0x9e3779b185ebca87;

    public string Name => "scalar-index-profile";
    public string Description =>
        "Profile exact scalar key/value postings. Args: maxDistinctValuesPerKey (default 256), maxStringBytes (default 64), top (default 10).";

    public object Handle(BlueprintDB db, JsonObject args)
    {
        int maxDistinct = Math.Clamp(
            args["maxDistinctValuesPerKey"]?.GetValue<int>() ?? 256,
            2,
            4096);
        int maxStringBytes = Math.Clamp(
            args["maxStringBytes"]?.GetValue<int>() ?? 64,
            0,
            1024);
        int top = Math.Clamp(args["top"]?.GetValue<int>() ?? 10, 1, 50);
        var keys = new Dictionary<ValueHash, KeyStats>();
        long rawBytes = 0;
        long scalarProperties = 0;
        long indexedScalarProperties = 0;

        var stopwatch = Stopwatch.StartNew();
        for (int blueprintIndex = 0; blueprintIndex < db.BlueprintsInOrder.Count; blueprintIndex++)
        {
            byte[] raw = db.BlueprintsInOrder[blueprintIndex].RawUtf8;
            rawBytes += raw.Length;
            KeyStats? pendingKey = null;
            var reader = new Utf8JsonReader(
                raw,
                new JsonReaderOptions { MaxDepth = 128 });

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    ValueHash keyHash = HashJsonString(ref reader, foldAsciiCase: false);
                    if (!keys.TryGetValue(keyHash, out var key))
                    {
                        key = new KeyStats(reader.GetString()!);
                        keys.Add(keyHash, key);
                    }
                    pendingKey = key;
                    pendingKey.AddDocument(blueprintIndex);
                    continue;
                }

                if (pendingKey is null)
                    continue;

                if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
                {
                    pendingKey = null;
                    continue;
                }

                scalarProperties++;
                if (!pendingKey.Disqualified
                    && TryGetScalarValue(
                        ref reader,
                        maxStringBytes,
                        out ValueHash valueHash,
                        out string display,
                        out int encodedBytes))
                {
                    if (pendingKey.TryAddValue(
                        valueHash,
                        display,
                        encodedBytes,
                        blueprintIndex,
                        raw.Length,
                        maxDistinct))
                    {
                        indexedScalarProperties++;
                    }
                }
                pendingKey = null;
            }
        }
        stopwatch.Stop();

        var eligibleKeys = keys.Values
            .Where(key => !key.Disqualified && key.Values.Count > 0)
            .ToArray();
        var pairs = eligibleKeys.SelectMany(key => key.Values.Values).ToArray();
        int[] pairCounts = pairs.Select(pair => pair.Documents).Order().ToArray();
        long[] pairRawBytes = pairs.Select(pair => pair.RawBytes).Order().ToArray();
        long postingBytes = pairs.Sum(pair => pair.DeltaVarintBytes);
        long lookupValueBytes = pairs.Sum(pair => (long)pair.EncodedBytes);
        long postingMetadataBytes = pairs.LongLength * 12;
        long keyNameBytes = eligibleKeys.Sum(key =>
            (long)Encoding.UTF8.GetByteCount(key.Name));

        var worstPairs = eligibleKeys
            .SelectMany(key => key.Values.Values.Select(value => new
            {
                key = key.Name,
                value = value.Display,
                documents = value.Documents,
                rawBytes = value.RawBytes,
                documentFraction =
                    (double)value.Documents / db.BlueprintsInOrder.Count,
                keyDocuments = key.Documents,
                reductionWithinKey =
                    1.0 - (double)value.Documents / key.Documents,
            }))
            .OrderByDescending(pair => pair.documents)
            .Take(top)
            .ToArray();

        var worstPairsByRawBytes = eligibleKeys
            .SelectMany(key => key.Values.Values.Select(value => new
            {
                key = key.Name,
                value = value.Display,
                documents = value.Documents,
                rawBytes = value.RawBytes,
                rawByteFraction = (double)value.RawBytes / rawBytes,
            }))
            .OrderByDescending(pair => pair.rawBytes)
            .Take(top)
            .ToArray();

        var keyWorstCases = eligibleKeys
            .Select(key => new
            {
                key = key.Name,
                values = key.Values.Count,
                keyDocuments = key.Documents,
                worstValueDocuments = key.Values.Values.Max(value => value.Documents),
                worstValueFraction = (double)key.Values.Values.Max(value => value.Documents)
                    / db.BlueprintsInOrder.Count,
            })
            .OrderByDescending(key => key.worstValueDocuments)
            .Take(top)
            .ToArray();

        var broadDisqualifiedKeys = keys.Values
            .Where(key => key.Disqualified)
            .OrderByDescending(key => key.Documents)
            .Take(top)
            .Select(key => new
            {
                key = key.Name,
                documents = key.Documents,
                documentFraction =
                    (double)key.Documents / db.BlueprintsInOrder.Count,
            })
            .ToArray();

        return new
        {
            blueprints = db.BlueprintsInOrder.Count,
            rawBytes,
            maxDistinctValuesPerKey = maxDistinct,
            maxStringBytes,
            scanMs = stopwatch.Elapsed.TotalMilliseconds,
            propertyKeys = keys.Count,
            eligibleKeys = eligibleKeys.Length,
            disqualifiedHighCardinalityKeys =
                keys.Values.Count(key => key.Disqualified),
            scalarProperties,
            indexedScalarProperties,
            distinctKeyValuePairs = pairs.Length,
            estimatedBytes = new
            {
                postingBytes,
                postingMetadataBytes,
                keyNameBytes,
                lookupValueBytes,
                lowerBoundTotal =
                    postingBytes + postingMetadataBytes + keyNameBytes + lookupValueBytes,
            },
            candidateCountPercentiles = new
            {
                p50 = Percentile(pairCounts, 0.50),
                p90 = Percentile(pairCounts, 0.90),
                p99 = Percentile(pairCounts, 0.99),
                max = pairCounts.Length == 0 ? 0 : pairCounts[^1],
            },
            candidateRawBytePercentiles = new
            {
                p50 = Percentile(pairRawBytes, 0.50),
                p90 = Percentile(pairRawBytes, 0.90),
                p99 = Percentile(pairRawBytes, 0.99),
                max = pairRawBytes.Length == 0 ? 0 : pairRawBytes[^1],
            },
            worstPairs,
            worstPairsByRawBytes,
            keyWorstCases,
            broadDisqualifiedKeys,
        };
    }

    private static bool TryGetScalarValue(
        ref Utf8JsonReader reader,
        int maxStringBytes,
        out ValueHash hash,
        out string display,
        out int encodedBytes)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.True:
                hash = HashTagged("b:true"u8);
                display = "true";
                encodedBytes = 1;
                return true;
            case JsonTokenType.False:
                hash = HashTagged("b:false"u8);
                display = "false";
                encodedBytes = 1;
                return true;
            case JsonTokenType.Null:
                hash = HashTagged("n:"u8);
                display = "null";
                encodedBytes = 0;
                return true;
            case JsonTokenType.Number when reader.TryGetInt64(out long number):
                display = number.ToString(System.Globalization.CultureInfo.InvariantCulture);
                byte[] numberBytes = Encoding.UTF8.GetBytes($"i:{display}");
                hash = HashTagged(numberBytes);
                encodedBytes = numberBytes.Length - 2;
                return true;
            case JsonTokenType.String:
                string value = reader.GetString()!;
                encodedBytes = Encoding.UTF8.GetByteCount(value);
                if (encodedBytes > maxStringBytes)
                    break;
                string folded = value.ToUpperInvariant();
                byte[] stringBytes = Encoding.UTF8.GetBytes($"s:{folded}");
                hash = HashTagged(stringBytes);
                display = value.Length <= 80 ? value : value[..80];
                return true;
        }

        hash = default;
        display = "";
        encodedBytes = 0;
        return false;
    }

    private static ValueHash HashJsonString(
        ref Utf8JsonReader reader,
        bool foldAsciiCase)
    {
        if (reader.ValueIsEscaped)
        {
            string decoded = reader.GetString()!;
            if (foldAsciiCase)
                decoded = decoded.ToUpperInvariant();
            return HashTagged(Encoding.UTF8.GetBytes(decoded));
        }

        return HashTagged(reader.ValueSpan, foldAsciiCase);
    }

    private static ValueHash HashTagged(
        ReadOnlySpan<byte> value,
        bool foldAsciiCase = false)
    {
        ulong primary = FnvOffset;
        ulong secondary = SecondaryOffset;
        foreach (byte raw in value)
        {
            byte item = foldAsciiCase && raw is >= (byte)'a' and <= (byte)'z'
                ? (byte)(raw - 32)
                : raw;
            primary ^= item;
            primary *= FnvPrime;
            secondary ^= item;
            secondary *= SecondaryPrime;
        }
        return new ValueHash(Mix(primary), Mix(secondary));
    }

    private static int Percentile(int[] sorted, double percentile)
    {
        if (sorted.Length == 0)
            return 0;
        int index = (int)Math.Ceiling(sorted.Length * percentile) - 1;
        return sorted[Math.Clamp(index, 0, sorted.Length - 1)];
    }

    private static long Percentile(long[] sorted, double percentile)
    {
        if (sorted.Length == 0)
            return 0;
        int index = (int)Math.Ceiling(sorted.Length * percentile) - 1;
        return sorted[Math.Clamp(index, 0, sorted.Length - 1)];
    }

    private static ulong Mix(ulong value)
    {
        value ^= value >> 30;
        value *= 0xbf58476d1ce4e5b9;
        value ^= value >> 27;
        value *= 0x94d049bb133111eb;
        return value ^ (value >> 31);
    }

    private readonly record struct ValueHash(ulong Primary, ulong Secondary);

    private sealed class KeyStats(string name)
    {
        public string Name { get; } = name;
        public Dictionary<ValueHash, PairStats> Values { get; } = [];
        public bool Disqualified { get; private set; }
        public int Documents { get; private set; }
        private int lastDocument = -1;

        public void AddDocument(int document)
        {
            if (lastDocument == document)
                return;
            Documents++;
            lastDocument = document;
        }

        public bool TryAddValue(
            ValueHash hash,
            string display,
            int encodedBytes,
            int document,
            int rawBytes,
            int maxDistinct)
        {
            if (!Values.TryGetValue(hash, out var pair))
            {
                if (Values.Count == maxDistinct)
                {
                    Values.Clear();
                    Disqualified = true;
                    return false;
                }

                pair = new PairStats(display, encodedBytes);
                Values.Add(hash, pair);
            }
            return pair.AddDocument(document, rawBytes);
        }
    }

    private sealed class PairStats(string display, int encodedBytes)
    {
        public string Display { get; } = display;
        public int EncodedBytes { get; } = encodedBytes;
        public int Documents { get; private set; }
        public long DeltaVarintBytes { get; private set; }
        public long RawBytes { get; private set; }
        private int lastDocument = -1;

        public bool AddDocument(int document, int rawBytes)
        {
            if (lastDocument == document)
                return false;

            uint delta = (uint)(lastDocument < 0
                ? document
                : document - lastDocument);
            DeltaVarintBytes += VarintSize(delta);
            Documents++;
            RawBytes += rawBytes;
            lastDocument = document;
            return true;
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
