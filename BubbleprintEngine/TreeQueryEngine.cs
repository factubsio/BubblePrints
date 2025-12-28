using System.Text.Json;

namespace BubbleprintEngine;

public static class TreeQueryEngine
{
    public static bool CheckMatch(Matcher matcher, JsonElement node)
    {
        return matcher switch
        {
            PredicateMatcher p => p.Predicate(node),
            AndMatcher a => a.Matchers.All(m => CheckMatch(m, node)),
            PathMatcher path => CheckPathMatch(path, node),
            _ => false,
        };

    }

    public static bool CheckPathMatch(PathMatcher p, JsonElement node)
    {
        if (!node.TryGetProperty(p.KeyRequirement, out var child))
            return false;

        return p.Combinator switch
        {
            NfaCombinator.ArrayItem when child.ValueKind == JsonValueKind.Array => child.EnumerateArray().Any(item => CheckMatch(p.NextRequirement, item)),
            NfaCombinator.Child => CheckMatch(p.NextRequirement, child),
            _ => false
        };
    }
}
