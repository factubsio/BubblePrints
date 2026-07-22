using System.Text.Json;

namespace BubbleprintEngine;

public sealed class StreamingTreeQuery
{
    private readonly CompiledMatcher[] matchers;
    private readonly CompiledKey[] keys;
    private readonly int rootMatcher;

    private StreamingTreeQuery(
        CompiledMatcher[] matchers,
        CompiledKey[] keys,
        int rootMatcher)
    {
        this.matchers = matchers;
        this.keys = keys;
        this.rootMatcher = rootMatcher;
    }

    public static bool TryCompile(Matcher matcher, out StreamingTreeQuery query)
    {
        var builder = new Builder();
        if (!builder.TryAdd(matcher, out int rootMatcher))
        {
            query = null;
            return false;
        }

        query = new StreamingTreeQuery(
            builder.Matchers.ToArray(),
            builder.Keys.Select(key => key.Compile()).ToArray(),
            rootMatcher);
        return true;
    }

    public bool CheckMatch(ReadOnlySpan<byte> rawUtf8)
    {
        Span<Frame> stack = stackalloc Frame[130];
        int stackCount = 0;
        var reader = new Utf8JsonReader(
            rawUtf8,
            new JsonReaderOptions { MaxDepth = 128 });

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.StartObject:
                case JsonTokenType.StartArray:
                {
                    int parentKey = -1;
                    if (stackCount > 0 && stack[stackCount - 1].Kind == JsonTokenType.StartObject)
                    {
                        ref var parent = ref stack[stackCount - 1];
                        parentKey = parent.PendingKey;
                        parent.PendingKey = -1;
                        BeginPropertyValue(ref parent, parentKey);
                    }

                    if (stackCount == stack.Length)
                        throw new JsonException("JSON depth exceeds the streaming matcher capacity.");

                    stack[stackCount++] = new Frame
                    {
                        Kind = reader.TokenType,
                        ParentKey = parentKey,
                        PendingKey = -1,
                    };
                    break;
                }
                case JsonTokenType.EndObject:
                case JsonTokenType.EndArray:
                {
                    ref var completed = ref stack[stackCount - 1];
                    CompleteFrame(ref completed);
                    bool rootMatches =
                        (completed.ResultBits & Bit(rootMatcher)) != 0;

                    stackCount--;
                    if (stackCount == 0)
                        return rootMatches;

                    PropagateChild(ref stack[stackCount - 1], in completed);
                    break;
                }
                case JsonTokenType.PropertyName:
                    stack[stackCount - 1].PendingKey = FindKey(ref reader);
                    break;
                default:
                    if (stackCount > 0 && stack[stackCount - 1].Kind == JsonTokenType.StartObject)
                    {
                        ref var parent = ref stack[stackCount - 1];
                        int key = parent.PendingKey;
                        parent.PendingKey = -1;
                        ApplyScalarProperty(ref parent, key, ref reader);
                    }
                    break;
            }
        }

        return false;
    }

    private int FindKey(ref Utf8JsonReader reader)
    {
        for (int i = 0; i < keys.Length; i++)
        {
            if (reader.ValueTextEquals(keys[i].Name))
                return i;
        }
        return -1;
    }

    private void BeginPropertyValue(ref Frame parent, int keyIndex)
    {
        if (keyIndex < 0)
            return;

        var key = keys[keyIndex];
        parent.ResultBits &= ~key.ResetMask;
        parent.ResultBits |= key.ExistencePredicateMask;
    }

    private void ApplyScalarProperty(
        ref Frame parent,
        int keyIndex,
        ref Utf8JsonReader reader)
    {
        if (keyIndex < 0)
            return;

        var key = keys[keyIndex];
        parent.ResultBits &= ~key.ResetMask;
        parent.ResultBits |= key.ExistencePredicateMask;

        foreach (int matcherIndex in key.ValuePredicates)
        {
            if (MatchesScalar(matchers[matcherIndex], ref reader))
                parent.ResultBits |= Bit(matcherIndex);
        }
    }

    private void CompleteFrame(ref Frame frame)
    {
        for (int i = 0; i < matchers.Length; i++)
        {
            var matcher = matchers[i];
            bool matches = matcher.Kind switch
            {
                CompiledMatcherKind.And =>
                    (frame.ResultBits & matcher.RequiredBits) == matcher.RequiredBits,
                CompiledMatcherKind.Relation =>
                    (frame.ResultBits & Bit(matcher.Anchor)) != 0
                    && RelationTargetMatches(matcher, in frame),
                _ => false,
            };

            if (matches)
                frame.ResultBits |= Bit(i);
        }

        frame.DescendantBits = frame.ResultBits | frame.ChildDescendantBits;
    }

    private static bool RelationTargetMatches(CompiledMatcher matcher, in Frame frame)
    {
        ulong target = Bit(matcher.Target);
        return matcher.Combinator switch
        {
            NfaCombinator.Child => (frame.ChildResultBits & target) != 0,
            NfaCombinator.Descendant => (frame.ChildDescendantBits & target) != 0,
            _ => false,
        };
    }

    private void PropagateChild(ref Frame parent, in Frame child)
    {
        parent.ChildResultBits |= child.ResultBits;
        parent.ChildDescendantBits |= child.DescendantBits;

        if (parent.Kind != JsonTokenType.StartObject || child.ParentKey < 0)
            return;

        foreach (int matcherIndex in keys[child.ParentKey].PathMatchers)
        {
            var matcher = matchers[matcherIndex];
            ulong next = Bit(matcher.Next);
            bool matches = matcher.Combinator switch
            {
                NfaCombinator.Child => (child.ResultBits & next) != 0,
                NfaCombinator.Descendant => (child.DescendantBits & next) != 0,
                NfaCombinator.ArrayItem when child.Kind == JsonTokenType.StartArray =>
                    (child.ChildResultBits & next) != 0,
                _ => false,
            };

            if (matches)
                parent.ResultBits |= Bit(matcherIndex);
        }
    }

    private static bool MatchesScalar(
        CompiledMatcher matcher,
        ref Utf8JsonReader reader)
    {
        return reader.TokenType switch
        {
            JsonTokenType.True => matcher.IsBoolean && matcher.BooleanValue,
            JsonTokenType.False => matcher.IsBoolean && !matcher.BooleanValue,
            JsonTokenType.Number => matcher.IsNumber
                && reader.TryGetInt64(out long value)
                && value == matcher.NumberValue,
            JsonTokenType.String => StringMatches(
                reader.GetString()!,
                matcher.Value!,
                matcher.Fuzzy),
            JsonTokenType.Null => StringMatches("null", matcher.Value!, matcher.Fuzzy),
            _ => false,
        };
    }

    private static bool StringMatches(string actual, string expected, bool fuzzy) =>
        fuzzy
            ? actual.Contains(expected, StringComparison.InvariantCultureIgnoreCase)
            : actual.Equals(expected, StringComparison.InvariantCultureIgnoreCase);

    private static ulong Bit(int index) => 1UL << index;

    private enum CompiledMatcherKind
    {
        Predicate,
        Path,
        And,
        Relation,
    }

    private sealed class CompiledMatcher
    {
        public CompiledMatcherKind Kind { get; init; }
        public string Value { get; init; }
        public bool Fuzzy { get; init; }
        public bool IsNumber { get; init; }
        public long NumberValue { get; init; }
        public bool IsBoolean { get; init; }
        public bool BooleanValue { get; init; }
        public NfaCombinator Combinator { get; init; }
        public int Next { get; init; }
        public ulong RequiredBits { get; init; }
        public int Anchor { get; init; }
        public int Target { get; init; }
    }

    private sealed class CompiledKey
    {
        public required string Name { get; init; }
        public required int[] ValuePredicates { get; init; }
        public required int[] PathMatchers { get; init; }
        public ulong ExistencePredicateMask { get; init; }
        public ulong ResetMask { get; init; }
    }

    private sealed class KeyBuilder(string name)
    {
        public string Name { get; } = name;
        public List<int> ValuePredicates { get; } = [];
        public List<int> PathMatchers { get; } = [];
        public ulong ExistencePredicateMask { get; set; }
        public ulong ResetMask { get; set; }

        public CompiledKey Compile() => new()
        {
            Name = Name,
            ValuePredicates = ValuePredicates.ToArray(),
            PathMatchers = PathMatchers.ToArray(),
            ExistencePredicateMask = ExistencePredicateMask,
            ResetMask = ResetMask,
        };
    }

    private sealed class Builder
    {
        private readonly Dictionary<string, int> keyIndexes =
            new(StringComparer.Ordinal);

        public List<CompiledMatcher> Matchers { get; } = [];
        public List<KeyBuilder> Keys { get; } = [];

        public bool TryAdd(Matcher matcher, out int matcherIndex)
        {
            matcherIndex = -1;
            if (Matchers.Count >= 64)
                return false;

            switch (matcher)
            {
                case PredicateMatcher { Key: not null } predicate:
                {
                    int keyIndex = GetOrAddKey(predicate.Key);
                    matcherIndex = Add(new CompiledMatcher
                    {
                        Kind = CompiledMatcherKind.Predicate,
                        Value = predicate.Value,
                        Fuzzy = predicate.Fuzzy,
                        IsNumber = long.TryParse(predicate.Value, out long numberValue),
                        NumberValue = numberValue,
                        IsBoolean = bool.TryParse(predicate.Value, out bool booleanValue),
                        BooleanValue = booleanValue,
                    });
                    ulong bit = Bit(matcherIndex);
                    Keys[keyIndex].ResetMask |= bit;
                    if (predicate.Value is null)
                        Keys[keyIndex].ExistencePredicateMask |= bit;
                    else
                        Keys[keyIndex].ValuePredicates.Add(matcherIndex);
                    return true;
                }
                case PathMatcher path:
                {
                    if (!TryAdd(path.NextRequirement, out int next))
                        return false;
                    if (Matchers.Count >= 64)
                        return false;

                    int keyIndex = GetOrAddKey(path.KeyRequirement);
                    matcherIndex = Add(new CompiledMatcher
                    {
                        Kind = CompiledMatcherKind.Path,
                        Combinator = path.Combinator,
                        Next = next,
                    });
                    ulong bit = Bit(matcherIndex);
                    Keys[keyIndex].ResetMask |= bit;
                    Keys[keyIndex].PathMatchers.Add(matcherIndex);
                    return true;
                }
                case AndMatcher and:
                {
                    ulong requiredBits = 0;
                    foreach (var child in and.Matchers)
                    {
                        if (!TryAdd(child, out int childIndex))
                            return false;
                        requiredBits |= Bit(childIndex);
                    }
                    if (Matchers.Count >= 64)
                        return false;

                    matcherIndex = Add(new CompiledMatcher
                    {
                        Kind = CompiledMatcherKind.And,
                        RequiredBits = requiredBits,
                    });
                    return true;
                }
                case RelationMatcher relation:
                {
                    if (!TryAdd(relation.Anchor, out int anchor)
                        || !TryAdd(relation.Target, out int target))
                    {
                        return false;
                    }
                    if (Matchers.Count >= 64)
                        return false;

                    matcherIndex = Add(new CompiledMatcher
                    {
                        Kind = CompiledMatcherKind.Relation,
                        Combinator = relation.Combinator,
                        Anchor = anchor,
                        Target = target,
                    });
                    return true;
                }
                default:
                    return false;
            }
        }

        private int Add(CompiledMatcher matcher)
        {
            if (Matchers.Count >= 64)
                throw new InvalidOperationException("Streaming matcher capacity exceeded.");

            int index = Matchers.Count;
            Matchers.Add(matcher);
            return index;
        }

        private int GetOrAddKey(string key)
        {
            if (keyIndexes.TryGetValue(key, out int index))
                return index;

            index = Keys.Count;
            Keys.Add(new KeyBuilder(key));
            keyIndexes.Add(key, index);
            return index;
        }
    }

    private struct Frame
    {
        public JsonTokenType Kind;
        public int ParentKey;
        public int PendingKey;
        public ulong ResultBits;
        public ulong ChildResultBits;
        public ulong ChildDescendantBits;
        public ulong DescendantBits;
    }
}
