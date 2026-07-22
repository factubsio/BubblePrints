using System.Diagnostics;
using System.Text;
using System.Text.Json;
using BubbleprintEngine;

namespace BlueprintExplorer;

public partial class BlueprintDB
{
    private const int ScalarValueDistinctLimit = 256;
    private const int ScalarValueStringByteLimit = 64;

    private byte[] scalarValuePostingData = [];
    private Dictionary<string, ScalarKeyIndex> scalarValueKeys =
        new(StringComparer.Ordinal);
    private bool scalarValuePostingIndexBuilt;

    public int ScalarValuePostingCount { get; private set; }
    public int ScalarValueIndexedKeyCount { get; private set; }
    public int ScalarValueDisqualifiedKeyCount { get; private set; }
    public long ScalarValuePostingIndexBytes => scalarValuePostingData.LongLength;
    public bool HasScalarValuePostingIndex => scalarValuePostingIndexBuilt;
    public double ScalarValuePostingBuildElapsedMs { get; private set; }

    public void BuildScalarValuePostingIndex(
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var builders = new Dictionary<string, ScalarKeyBuilder>(
            StringComparer.Ordinal);

        for (int blueprintIndex = 0; blueprintIndex < cache.Count; blueprintIndex++)
        {
            ScalarKeyBuilder pendingKey = null;
            var reader = new Utf8JsonReader(
                cache[blueprintIndex].RawUtf8,
                new JsonReaderOptions { MaxDepth = 128 });

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string key = reader.GetString()!;
                    if (!builders.TryGetValue(key, out pendingKey))
                    {
                        pendingKey = new ScalarKeyBuilder();
                        builders.Add(key, pendingKey);
                    }
                    continue;
                }

                if (pendingKey is null)
                    continue;

                if (reader.TokenType is JsonTokenType.StartObject
                    or JsonTokenType.StartArray)
                {
                    pendingKey = null;
                    continue;
                }

                if (!pendingKey.Disqualified
                    && TryReadIndexedScalar(ref reader, out var value))
                {
                    pendingKey.Add(value, blueprintIndex);
                }
                pendingKey = null;
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        int compressedBytes = builders.Values
            .Where(builder => !builder.Disqualified)
            .SelectMany(builder => builder.Postings.Values)
            .Sum(EncodedPostingSize);
        var data = new byte[compressedBytes];
        var keys = new Dictionary<string, ScalarKeyIndex>(
            builders.Count,
            StringComparer.Ordinal);
        int offset = 0;
        int postingCount = 0;
        int indexedKeyCount = 0;
        int disqualifiedKeyCount = 0;

        foreach (var (key, builder) in builders.OrderBy(
                     item => item.Key,
                     StringComparer.Ordinal))
        {
            if (builder.Disqualified)
            {
                keys.Add(key, ScalarKeyIndex.Disqualified);
                disqualifiedKeyCount++;
                continue;
            }

            var postings = new Dictionary<ScalarValue, ScalarPosting>(
                builder.Postings.Count,
                ScalarValueComparer.Instance);
            foreach (var (value, blueprintIndexes) in builder.Postings)
            {
                int start = offset;
                WritePosting(data, ref offset, blueprintIndexes);
                postings.Add(
                    value,
                    new ScalarPosting(
                        start,
                        offset - start,
                        blueprintIndexes.Count));
            }

            keys.Add(key, new ScalarKeyIndex(postings, isDisqualified: false));
            if (postings.Count > 0)
                indexedKeyCount++;
            postingCount += postings.Count;
        }

        scalarValuePostingData = data;
        scalarValueKeys = keys;
        ScalarValuePostingCount = postingCount;
        ScalarValueIndexedKeyCount = indexedKeyCount;
        ScalarValueDisqualifiedKeyCount = disqualifiedKeyCount;
        scalarValuePostingIndexBuilt = true;
        ScalarValuePostingBuildElapsedMs = stopwatch.Elapsed.TotalMilliseconds;
    }

    private void InvalidateScalarValuePostingIndex()
    {
        if (!scalarValuePostingIndexBuilt)
            return;

        scalarValuePostingData = [];
        scalarValueKeys = new(StringComparer.Ordinal);
        ScalarValuePostingCount = 0;
        ScalarValueIndexedKeyCount = 0;
        ScalarValueDisqualifiedKeyCount = 0;
        scalarValuePostingIndexBuilt = false;
        ScalarValuePostingBuildElapsedMs = 0;
    }

    private void ApplyScalarValuePrefilter(
        Matcher matcher,
        ref List<BlueprintHandle> toSearch)
    {
        if (!HasScalarValuePostingIndex || toSearch.Count == 0)
            return;

        var groups = new List<ScalarPosting[]>();
        foreach (var predicate in RequiredExactScalarPredicates(matcher))
        {
            if (!TryGetScalarPostings(predicate, out var postings))
                continue;
            if (postings.Length == 0)
            {
                toSearch = [];
                return;
            }
            groups.Add(postings);
        }

        if (groups.Count == 0)
            return;

        groups.Sort((left, right) =>
            left.Sum(posting => posting.Count)
                .CompareTo(right.Sum(posting => posting.Count)));
        var cursors = groups
            .Select(group => new ScalarPostingGroupCursor(
                scalarValuePostingData,
                group))
            .ToArray();
        var filtered = new List<BlueprintHandle>(
            Math.Min(toSearch.Count, groups[0].Sum(posting => posting.Count)));
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

    private bool IsCoveredByRequiredScalarPredicate(Matcher matcher, string key)
    {
        if (!HasScalarValuePostingIndex)
            return false;

        return RequiredExactScalarPredicates(matcher).Any(predicate =>
            predicate.Key == key && CanUseScalarPredicate(predicate));
    }

    private bool CanUseScalarPredicate(PredicateMatcher predicate)
    {
        return scalarValueKeys.TryGetValue(predicate.Key!, out var key)
               && !key.IsDisqualified
               && Encoding.UTF8.GetByteCount(predicate.Value!)
                   <= ScalarValueStringByteLimit;
    }

    private bool TryGetScalarPostings(
        PredicateMatcher predicate,
        out ScalarPosting[] postings)
    {
        postings = [];
        if (!scalarValueKeys.TryGetValue(predicate.Key!, out var key))
            return true;
        if (key.IsDisqualified)
            return false;

        string queryValue = predicate.Value!;
        if (!CanUseScalarPredicate(predicate))
            return false;

        var matches = new List<ScalarPosting>(4);
        AddPosting(ScalarValue.String(queryValue));
        if (bool.TryParse(queryValue, out bool boolean))
            AddPosting(ScalarValue.Boolean(boolean));
        if (long.TryParse(
                queryValue,
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture,
                out long integer))
        {
            AddPosting(ScalarValue.Integer(integer));
        }
        if (queryValue.Equals("null", StringComparison.InvariantCultureIgnoreCase))
            AddPosting(ScalarValue.Null);

        postings = matches.ToArray();
        return true;

        void AddPosting(ScalarValue value)
        {
            if (key.Postings.TryGetValue(value, out var posting))
                matches.Add(posting);
        }
    }

    private static IEnumerable<PredicateMatcher> RequiredExactScalarPredicates(
        Matcher matcher)
    {
        switch (matcher)
        {
            case PredicateMatcher
            {
                Key: not null and not "$type",
                Value: not null,
                Fuzzy: false,
            } predicate:
                yield return predicate;
                break;
            case PathMatcher path:
                foreach (var predicate in RequiredExactScalarPredicates(
                             path.NextRequirement))
                {
                    yield return predicate;
                }
                break;
            case AndMatcher and:
                foreach (var predicate in and.Matchers.SelectMany(
                             RequiredExactScalarPredicates))
                {
                    yield return predicate;
                }
                break;
            case RelationMatcher relation:
                foreach (var predicate in RequiredExactScalarPredicates(
                             relation.Anchor))
                {
                    yield return predicate;
                }
                foreach (var predicate in RequiredExactScalarPredicates(
                             relation.Target))
                {
                    yield return predicate;
                }
                break;
        }
    }

    private static bool TryReadIndexedScalar(
        ref Utf8JsonReader reader,
        out ScalarValue value)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.True:
                value = ScalarValue.Boolean(true);
                return true;
            case JsonTokenType.False:
                value = ScalarValue.Boolean(false);
                return true;
            case JsonTokenType.Null:
                value = ScalarValue.Null;
                return true;
            case JsonTokenType.Number when reader.TryGetInt64(out long integer):
                value = ScalarValue.Integer(integer);
                return true;
            case JsonTokenType.String:
                string text = reader.GetString()!;
                if (Encoding.UTF8.GetByteCount(text) <= ScalarValueStringByteLimit)
                {
                    value = ScalarValue.String(text);
                    return true;
                }
                break;
        }

        value = default;
        return false;
    }

    private static void WritePosting(
        byte[] destination,
        ref int offset,
        List<int> blueprintIndexes)
    {
        int previous = 0;
        for (int i = 0; i < blueprintIndexes.Count; i++)
        {
            int blueprintIndex = blueprintIndexes[i];
            uint delta = (uint)(i == 0
                ? blueprintIndex
                : blueprintIndex - previous);
            WriteVarint(destination, ref offset, delta);
            previous = blueprintIndex;
        }
    }

    private enum ScalarValueKind : byte
    {
        String,
        Boolean,
        Integer,
        Null,
    }

    private readonly record struct ScalarValue(
        ScalarValueKind Kind,
        string Text,
        long Number)
    {
        public static ScalarValue String(string value) =>
            new(ScalarValueKind.String, value, 0);
        public static ScalarValue Boolean(bool value) =>
            new(ScalarValueKind.Boolean, null, value ? 1 : 0);
        public static ScalarValue Integer(long value) =>
            new(ScalarValueKind.Integer, null, value);
        public static ScalarValue Null =>
            new(ScalarValueKind.Null, null, 0);
    }

    private sealed class ScalarValueComparer : IEqualityComparer<ScalarValue>
    {
        public static readonly ScalarValueComparer Instance = new();

        public bool Equals(ScalarValue left, ScalarValue right)
        {
            return left.Kind == right.Kind
                   && left.Number == right.Number
                   && (left.Kind != ScalarValueKind.String
                       || StringComparer.InvariantCultureIgnoreCase.Equals(
                           left.Text,
                           right.Text));
        }

        public int GetHashCode(ScalarValue value)
        {
            return value.Kind == ScalarValueKind.String
                ? HashCode.Combine(
                    value.Kind,
                    StringComparer.InvariantCultureIgnoreCase.GetHashCode(
                        value.Text))
                : HashCode.Combine(value.Kind, value.Number);
        }
    }

    private sealed class ScalarKeyBuilder
    {
        public Dictionary<ScalarValue, List<int>> Postings { get; } =
            new(ScalarValueComparer.Instance);
        public bool Disqualified { get; private set; }

        public void Add(ScalarValue value, int blueprintIndex)
        {
            if (!Postings.TryGetValue(value, out var indexes))
            {
                if (Postings.Count == ScalarValueDistinctLimit)
                {
                    Postings.Clear();
                    Disqualified = true;
                    return;
                }

                indexes = [];
                Postings.Add(value, indexes);
            }

            if (indexes.Count == 0 || indexes[^1] != blueprintIndex)
                indexes.Add(blueprintIndex);
        }
    }

    private sealed class ScalarKeyIndex(
        Dictionary<ScalarValue, ScalarPosting> postings,
        bool isDisqualified)
    {
        public static readonly ScalarKeyIndex Disqualified =
            new(new(ScalarValueComparer.Instance), isDisqualified: true);

        public Dictionary<ScalarValue, ScalarPosting> Postings { get; } = postings;
        public bool IsDisqualified { get; } = isDisqualified;
    }

    private readonly record struct ScalarPosting(int Offset, int Length, int Count);

    private struct ScalarPostingCursor
    {
        private readonly byte[] data;
        private readonly int end;
        private int offset;
        private bool started;

        public int Current { get; private set; }

        public ScalarPostingCursor(byte[] data, ScalarPosting posting)
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
            throw new InvalidDataException("Truncated scalar-value posting.");
        }
    }

    private sealed class ScalarPostingGroupCursor
    {
        private readonly ScalarPostingCursor[] cursors;
        private readonly bool[] active;
        private bool started;

        public int Current { get; private set; }

        public ScalarPostingGroupCursor(
            byte[] data,
            ScalarPosting[] postings)
        {
            cursors = postings
                .Select(posting => new ScalarPostingCursor(data, posting))
                .ToArray();
            active = new bool[cursors.Length];
            for (int i = 0; i < cursors.Length; i++)
                active[i] = cursors[i].MoveNext();
        }

        public bool MoveNext()
        {
            if (started)
            {
                for (int i = 0; i < cursors.Length; i++)
                {
                    while (active[i] && cursors[i].Current <= Current)
                        active[i] = cursors[i].MoveNext();
                }
            }

            int next = int.MaxValue;
            for (int i = 0; i < cursors.Length; i++)
            {
                if (active[i])
                    next = Math.Min(next, cursors[i].Current);
            }

            if (next == int.MaxValue)
                return false;

            Current = next;
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
    }
}
