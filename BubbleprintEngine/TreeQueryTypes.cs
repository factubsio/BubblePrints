using System.Text.Json;

namespace BubbleprintEngine;
public enum NfaCombinator
{
    None,         // End of query (Implicitly accepting if we are here)
    Child,        // >
    Descendant,   // >>
    ArrayItem,
}
public abstract record Matcher;

public record PathMatcher(string KeyRequirement, NfaCombinator Combinator, Matcher NextRequirement) : Matcher
{
    public override string ToString()
    {
        var combinatorString = Combinator switch
        {
            NfaCombinator.Child => ".",
            NfaCombinator.ArrayItem => "[",
            _ => "?"
        };
        return $"{KeyRequirement} {combinatorString} {NextRequirement}";
    }
}
public record PredicateMatcher(Func<JsonElement, bool> Predicate, string Desc = "") : Matcher
{
    public override string ToString() => Desc;
}

public record AndMatcher(List<Matcher> Matchers) : Matcher
{
    public override string ToString() => $"({string.Join(" & ", Matchers.Select(m => m.ToString()))})";
}

public record class NfaState(Func<JsonElement, bool> LocalMatcher)
{
    // If LocalMatcher passes, we spawn these search agents for the next level(s)
    public List<NfaTransition> Transitions { get; } = [];

    // If true, a node matching 'LocalMatcher' is considered a result
    public bool IsFinal { get; init; }
}

public record class NfaTransition(
    NfaCombinator Combinator, // Child (>) or Descendant (>>)
    NfaState TargetState      // The requirements for the next node
);

public static class TreeQueryGenerator
{
    public static Func<JsonElement, bool> CheckKV(string key, string val, bool fuzzy)
    {
        bool isNum = long.TryParse(val, out var num);
        bool isBool = bool.TryParse(val.ToLower(), out var boolVal);
        return e =>
        {
            if (!e.TryGetProperty(key, out var prop))
                return false;

            if (prop.ValueKind == JsonValueKind.True && isBool && boolVal)
                return true;
            else if (prop.ValueKind == JsonValueKind.False && isBool && !boolVal)
                return true;
            else if (prop.ValueKind == JsonValueKind.Number && isNum)
                return prop.GetInt64() == num;
            else if (prop.ValueKind == JsonValueKind.String || prop.ValueKind == JsonValueKind.Null)
            {
                var str = prop.ValueKind == JsonValueKind.Null ? "null" : prop.GetString()!;
                if (fuzzy)
                    return str.Contains(val, StringComparison.InvariantCultureIgnoreCase);
                else
                    return str.Equals(val, StringComparison.InvariantCultureIgnoreCase);
            }
            else
                return false;
        };
    }

}
