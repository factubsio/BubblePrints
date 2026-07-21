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
            RelationMatcher relation => CheckRelationMatch(relation, node),
            _ => false,
        };

    }

    public static bool CheckPathMatch(PathMatcher p, JsonElement node)
    {
        if (node.ValueKind != JsonValueKind.Object)
            return false;

        if (!node.TryGetProperty(p.KeyRequirement, out var child))
            return false;

        return p.Combinator switch
        {
            NfaCombinator.ArrayItem when child.ValueKind == JsonValueKind.Array => child.EnumerateArray().Any(item => CheckMatch(p.NextRequirement, item)),
            NfaCombinator.Child => CheckMatch(p.NextRequirement, child),
            NfaCombinator.Descendant => CheckDescendantMatch(p.NextRequirement, child),
            _ => false
        };
    }

    private static bool CheckRelationMatch(RelationMatcher relation, JsonElement node)
    {
        if (!CheckMatch(relation.Anchor, node))
            return false;

        return relation.Combinator switch
        {
            NfaCombinator.Child => EnumerateChildren(node).Any(
                child => CheckMatch(relation.Target, child)),
            NfaCombinator.Descendant => EnumerateChildren(node).Any(
                child => CheckDescendantMatch(relation.Target, child)),
            _ => false,
        };
    }

    private static bool CheckDescendantMatch(Matcher matcher, JsonElement node)
    {
        if (CheckMatch(matcher, node))
            return true;

        return node.ValueKind switch
        {
            JsonValueKind.Object => node.EnumerateObject().Any(property => CheckDescendantMatch(matcher, property.Value)),
            JsonValueKind.Array => node.EnumerateArray().Any(child => CheckDescendantMatch(matcher, child)),
            _ => false,
        };
    }

    private static IEnumerable<JsonElement> EnumerateChildren(JsonElement node)
    {
        return node.ValueKind switch
        {
            JsonValueKind.Object => node.EnumerateObject().Select(property => property.Value),
            JsonValueKind.Array => node.EnumerateArray(),
            _ => [],
        };
    }
}
