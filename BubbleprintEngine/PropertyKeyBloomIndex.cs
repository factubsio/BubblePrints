using System.Diagnostics;
using System.Text;
using System.Text.Json;
using BubbleprintEngine;

namespace BlueprintExplorer;

public partial class BlueprintDB
{
    public const int PropertyKeyBloomBitsPerBlueprint = 512;
    private const int PropertyKeyBloomWordsPerBlueprint =
        PropertyKeyBloomBitsPerBlueprint / sizeof(ulong) / 8;
    private const ulong FnvOffset = 14695981039346656037;
    private const ulong FnvPrime = 1099511628211;
    private const ulong HashStep = 0x9e3779b97f4a7c15;

    private ulong[] propertyKeyBloomIndex = [];

    public long PropertyKeyBloomIndexBytes =>
        propertyKeyBloomIndex.LongLength * sizeof(ulong);
    public bool HasPropertyKeyBloomIndex =>
        propertyKeyBloomIndex.Length == cache.Count * PropertyKeyBloomWordsPerBlueprint;
    public double PropertyKeyBloomBuildElapsedMs { get; private set; }

    public void BuildPropertyKeyBloomIndex(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var index = new ulong[checked(cache.Count * PropertyKeyBloomWordsPerBlueprint)];

        for (int blueprintIndex = 0; blueprintIndex < cache.Count; blueprintIndex++)
        {
            var reader = new Utf8JsonReader(
                cache[blueprintIndex].RawUtf8,
                new JsonReaderOptions { MaxDepth = 128 });
            while (reader.Read())
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                    continue;

                ulong hash = reader.ValueIsEscaped
                    ? HashPropertyKey(Encoding.UTF8.GetBytes(reader.GetString()!))
                    : HashPropertyKey(reader.ValueSpan);
                SetPropertyKeyBits(index, blueprintIndex, hash);
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        propertyKeyBloomIndex = index;
        PropertyKeyBloomBuildElapsedMs = stopwatch.Elapsed.TotalMilliseconds;
    }

    private void InvalidatePropertyKeyBloomIndex()
    {
        if (propertyKeyBloomIndex.Length == 0)
            return;

        propertyKeyBloomIndex = [];
        PropertyKeyBloomBuildElapsedMs = 0;
    }

    private void ApplyPropertyKeyPrefilter(Matcher matcher, ref List<BlueprintHandle> toSearch)
    {
        if (!HasPropertyKeyBloomIndex)
            return;

        ulong[] requiredKeyHashes = RequiredPropertyKeys(matcher)
            .Where(key => key != "$type")
            .Distinct(StringComparer.Ordinal)
            .Select(key => HashPropertyKey(Encoding.UTF8.GetBytes(key)))
            .ToArray();
        if (requiredKeyHashes.Length == 0)
            return;

        var filtered = new List<BlueprintHandle>(toSearch.Count);
        foreach (var blueprint in toSearch)
        {
            bool containsAll = true;
            for (int i = 0; i < requiredKeyHashes.Length; i++)
            {
                if (MayContainPropertyKey(blueprint.DatabaseIndex, requiredKeyHashes[i]))
                    continue;

                containsAll = false;
                break;
            }

            if (containsAll)
                filtered.Add(blueprint);
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

    private static void SetPropertyKeyBits(ulong[] index, int blueprintIndex, ulong hash)
    {
        int offset = blueprintIndex * PropertyKeyBloomWordsPerBlueprint;
        hash = SaltPropertyKeyHash(hash, blueprintIndex);
        int firstBit = (int)(hash & (PropertyKeyBloomBitsPerBlueprint - 1));
        int secondBit = (int)((hash >> 32) & (PropertyKeyBloomBitsPerBlueprint - 1));
        index[offset + firstBit / 64] |= 1UL << (firstBit % 64);
        index[offset + secondBit / 64] |= 1UL << (secondBit % 64);
    }

    private bool MayContainPropertyKey(int blueprintIndex, ulong hash)
    {
        int offset = blueprintIndex * PropertyKeyBloomWordsPerBlueprint;
        hash = SaltPropertyKeyHash(hash, blueprintIndex);
        int firstBit = (int)(hash & (PropertyKeyBloomBitsPerBlueprint - 1));
        int secondBit = (int)((hash >> 32) & (PropertyKeyBloomBitsPerBlueprint - 1));
        return (propertyKeyBloomIndex[offset + firstBit / 64] & (1UL << (firstBit % 64))) != 0
            && (propertyKeyBloomIndex[offset + secondBit / 64] & (1UL << (secondBit % 64))) != 0;
    }

    private static ulong SaltPropertyKeyHash(ulong hash, int blueprintIndex) =>
        Mix(hash ^ ((ulong)blueprintIndex * HashStep));

    private static ulong HashPropertyKey(ReadOnlySpan<byte> value)
    {
        ulong hash = FnvOffset;
        foreach (byte item in value)
        {
            hash ^= item;
            hash *= FnvPrime;
        }
        return Mix(hash);
    }

    private static ulong Mix(ulong value)
    {
        value ^= value >> 30;
        value *= 0xbf58476d1ce4e5b9;
        value ^= value >> 27;
        value *= 0x94d049bb133111eb;
        return value ^ (value >> 31);
    }
}
