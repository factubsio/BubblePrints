using System.Diagnostics;
using System.Text;
using System.Text.Json;
using BubbleprintEngine;

namespace BlueprintExplorer;

public partial class BlueprintDB
{
    private const ulong FnvOffset = 14695981039346656037;
    private const ulong FnvPrime = 1099511628211;
    private const ulong SecondaryOffset = 7809847782465536322;
    private const ulong SecondaryPrime = 0x9e3779b185ebca87;

    private byte[] propertyKeyPostingData = [];
    private Dictionary<string, PropertyKeyPosting> propertyKeyPostings =
        new(StringComparer.Ordinal);
    private bool propertyKeyPostingIndexBuilt;

    public int PropertyKeyPostingCount => propertyKeyPostings.Count;
    public long PropertyKeyPostingIndexBytes => propertyKeyPostingData.LongLength;
    public bool HasPropertyKeyPostingIndex => propertyKeyPostingIndexBuilt;
    public double PropertyKeyPostingBuildElapsedMs { get; private set; }

    public void BuildPropertyKeyPostingIndex(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var builders = new Dictionary<PropertyKeyHash, PropertyKeyPostingBuilder>();
        var documentKeys = new HashSet<PropertyKeyHash>();

        for (int blueprintIndex = 0; blueprintIndex < cache.Count; blueprintIndex++)
        {
            documentKeys.Clear();
            var reader = new Utf8JsonReader(
                cache[blueprintIndex].RawUtf8,
                new JsonReaderOptions { MaxDepth = 128 });
            while (reader.Read())
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                    continue;

                PropertyKeyHash hash = HashPropertyKey(ref reader);
                if (!documentKeys.Add(hash))
                    continue;

                if (!builders.TryGetValue(hash, out var builder))
                {
                    builder = new PropertyKeyPostingBuilder(reader.GetString()!);
                    builders.Add(hash, builder);
                }
                builder.BlueprintIndexes.Add(blueprintIndex);
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        int compressedBytes = builders.Values.Sum(builder =>
            EncodedPostingSize(builder.BlueprintIndexes));
        var data = new byte[compressedBytes];
        var postings = new Dictionary<string, PropertyKeyPosting>(
            builders.Count,
            StringComparer.Ordinal);
        int offset = 0;

        foreach (var builder in builders.Values.OrderBy(
                     builder => builder.Name,
                     StringComparer.Ordinal))
        {
            int start = offset;
            int previous = 0;
            for (int i = 0; i < builder.BlueprintIndexes.Count; i++)
            {
                int blueprintIndex = builder.BlueprintIndexes[i];
                uint delta = (uint)(i == 0
                    ? blueprintIndex
                    : blueprintIndex - previous);
                WriteVarint(data, ref offset, delta);
                previous = blueprintIndex;
            }

            postings.Add(
                builder.Name,
                new PropertyKeyPosting(
                    start,
                    offset - start,
                    builder.BlueprintIndexes.Count));
        }

        propertyKeyPostingData = data;
        propertyKeyPostings = postings;
        propertyKeyPostingIndexBuilt = true;
        PropertyKeyPostingBuildElapsedMs = stopwatch.Elapsed.TotalMilliseconds;
    }

    private void InvalidatePropertyKeyPostingIndex()
    {
        if (!propertyKeyPostingIndexBuilt)
            return;

        propertyKeyPostingData = [];
        propertyKeyPostings = new(StringComparer.Ordinal);
        propertyKeyPostingIndexBuilt = false;
        PropertyKeyPostingBuildElapsedMs = 0;
    }

    private void ApplyPropertyKeyPrefilter(Matcher matcher, ref List<BlueprintHandle> toSearch)
    {
        if (!HasPropertyKeyPostingIndex)
            return;

        string[] requiredKeys = RequiredPropertyKeys(matcher)
            .Where(key => key != "$type")
            .Where(key => !IsCoveredByRequiredScalarPredicate(matcher, key))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (requiredKeys.Length == 0)
            return;

        var requiredPostings = new PropertyKeyPosting[requiredKeys.Length];
        for (int i = 0; i < requiredKeys.Length; i++)
        {
            if (!propertyKeyPostings.TryGetValue(requiredKeys[i], out requiredPostings[i]))
            {
                toSearch = [];
                return;
            }
        }
        Array.Sort(
            requiredPostings,
            (left, right) => left.Count.CompareTo(right.Count));

        var cursors = new PropertyKeyPostingCursor[requiredPostings.Length];
        for (int i = 0; i < requiredPostings.Length; i++)
            cursors[i] = new PropertyKeyPostingCursor(
                propertyKeyPostingData,
                requiredPostings[i]);

        var filtered = new List<BlueprintHandle>(
            Math.Min(toSearch.Count, requiredPostings[0].Count));
        bool searchesWholeDatabase = ReferenceEquals(toSearch, cache);
        int candidateIndex = 0;
        while (cursors[0].MoveNext())
        {
            int blueprintIndex = cursors[0].Current;
            BlueprintHandle candidate;
            if (searchesWholeDatabase)
            {
                candidate = cache[blueprintIndex];
            }
            else
            {
                while (candidateIndex < toSearch.Count
                       && toSearch[candidateIndex].DatabaseIndex < blueprintIndex)
                {
                    candidateIndex++;
                }

                if (candidateIndex == toSearch.Count)
                    break;
                if (toSearch[candidateIndex].DatabaseIndex != blueprintIndex)
                    continue;
                candidate = toSearch[candidateIndex];
            }

            bool containsAll = true;
            for (int i = 1; i < cursors.Length; i++)
            {
                if (cursors[i].AdvanceTo(blueprintIndex))
                    continue;

                containsAll = false;
                break;
            }

            if (containsAll)
                filtered.Add(candidate);
        }

        toSearch = filtered;
    }

    private static IEnumerable<string> RequiredPropertyKeys(Matcher matcher)
    {
        switch (matcher)
        {
            case PredicateMatcher { Key: not null } predicate:
                yield return predicate.Key;
                break;
            case PathMatcher path:
                yield return path.KeyRequirement;
                foreach (string key in RequiredPropertyKeys(path.NextRequirement))
                    yield return key;
                break;
            case AndMatcher and:
                foreach (string key in and.Matchers.SelectMany(RequiredPropertyKeys))
                    yield return key;
                break;
            case RelationMatcher relation:
                foreach (string key in RequiredPropertyKeys(relation.Anchor))
                    yield return key;
                foreach (string key in RequiredPropertyKeys(relation.Target))
                    yield return key;
                break;
        }
    }

    private static PropertyKeyHash HashPropertyKey(ref Utf8JsonReader reader)
    {
        if (!reader.ValueIsEscaped)
            return HashPropertyKey(reader.ValueSpan);

        return HashPropertyKey(Encoding.UTF8.GetBytes(reader.GetString()!));
    }

    private static PropertyKeyHash HashPropertyKey(ReadOnlySpan<byte> value)
    {
        ulong primary = FnvOffset;
        ulong secondary = SecondaryOffset;
        foreach (byte item in value)
        {
            primary ^= item;
            primary *= FnvPrime;
            secondary ^= item;
            secondary *= SecondaryPrime;
        }
        return new PropertyKeyHash(Mix(primary), Mix(secondary));
    }

    private static int EncodedPostingSize(List<int> blueprintIndexes)
    {
        int size = 0;
        int previous = 0;
        for (int i = 0; i < blueprintIndexes.Count; i++)
        {
            int blueprintIndex = blueprintIndexes[i];
            uint delta = (uint)(i == 0
                ? blueprintIndex
                : blueprintIndex - previous);
            size += VarintSize(delta);
            previous = blueprintIndex;
        }
        return size;
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

    private static void WriteVarint(byte[] destination, ref int offset, uint value)
    {
        while (value >= 0x80)
        {
            destination[offset++] = (byte)(value | 0x80);
            value >>= 7;
        }
        destination[offset++] = (byte)value;
    }

    private static ulong Mix(ulong value)
    {
        value ^= value >> 30;
        value *= 0xbf58476d1ce4e5b9;
        value ^= value >> 27;
        value *= 0x94d049bb133111eb;
        return value ^ (value >> 31);
    }

    private readonly record struct PropertyKeyHash(ulong Primary, ulong Secondary);
    private readonly record struct PropertyKeyPosting(int Offset, int Length, int Count);

    private sealed class PropertyKeyPostingBuilder(string name)
    {
        public string Name { get; } = name;
        public List<int> BlueprintIndexes { get; } = [];
    }

    private struct PropertyKeyPostingCursor
    {
        private readonly byte[] data;
        private readonly int end;
        private int offset;
        private bool started;

        public int Current { get; private set; }

        public PropertyKeyPostingCursor(byte[] data, PropertyKeyPosting posting)
        {
            this.data = data;
            offset = posting.Offset;
            end = posting.Offset + posting.Length;
            started = false;
            Current = 0;
        }

        public bool MoveNext()
        {
            if (offset >= end)
                return false;

            uint delta = ReadVarint(data, ref offset, end);
            Current = started ? Current + (int)delta : (int)delta;
            started = true;
            return true;
        }

        public bool AdvanceTo(int blueprintIndex)
        {
            while ((!started || Current < blueprintIndex) && MoveNext())
            {
            }
            return started && Current == blueprintIndex;
        }

        private static uint ReadVarint(byte[] source, ref int offset, int end)
        {
            uint value = 0;
            int shift = 0;
            while (offset < end)
            {
                byte current = source[offset++];
                value |= (uint)(current & 0x7f) << shift;
                if ((current & 0x80) == 0)
                    return value;
                shift += 7;
            }
            throw new InvalidDataException("Truncated property-key posting.");
        }
    }
}
