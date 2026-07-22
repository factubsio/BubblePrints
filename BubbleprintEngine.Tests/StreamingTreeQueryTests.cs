using System.Text;
using System.Text.Json;
using BubbleprintEngine;

namespace BubbleprintEngine.Tests;

public class StreamingTreeQueryTests
{
    public static TheoryData<string, string> Queries =>
        new()
        {
            {
                "Child.Value=expected",
                """{"Child":{"Value":"expected"}}"""
            },
            {
                "Components[$type~=Damage",
                """{"Components":[{"$type":"Healing"},{"$type":"DirectDamage"}]}"""
            },
            {
                "Components[($type=Damage & Enabled=true)",
                """{"Components":[{"$type":"Damage","Enabled":false},{"$type":"Healing","Enabled":true}]}"""
            },
            {
                "Components..$type~=Damage",
                """{"Components":[{"Actions":[{"Nested":{"$type":"DirectDamage"}}]}]}"""
            },
            {
                "Components[($type~=AbilityEffectRunAction .. $type~=ContextActionDealDamage)",
                """
                {
                  "Components": [
                    {
                      "$type": "AbilityEffectRunAction",
                      "Actions": [{"$type": "ContextActionSpawnFx"}]
                    },
                    {
                      "$type": "UnrelatedComponent",
                      "Actions": [{"$type": "ContextActionDealDamage"}]
                    }
                  ]
                }
                """
            },
            {
                "$type=Root .. $type=Middle .. $type=Leaf",
                """{"$type":"Root","Branch":{"$type":"Middle","Nested":[{"$type":"Leaf"}]}}"""
            },
            {
                "Enabled=true & Count=42 & Optional=null",
                """{"Enabled":true,"Count":42,"Optional":null}"""
            },
            {
                "RareKey=target",
                """{"\u0052areKey":"target"}"""
            },
            {
                "Rare=yes",
                """{"Rare":"yes","Rare":"no"}"""
            },
            {
                "$type=Damage",
                "42"
            },
        };

    [Theory]
    [MemberData(nameof(Queries))]
    public void CheckMatch_MatchesJsonElementEvaluator(string query, string raw)
    {
        var matcher = Parse(query);
        Assert.True(StreamingTreeQuery.TryCompile(matcher, out var streaming));
        using var document = JsonDocument.Parse(raw);

        bool expected = TreeQueryEngine.CheckMatch(matcher, document.RootElement);
        bool actual = streaming.CheckMatch(Encoding.UTF8.GetBytes(raw));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TryCompile_CustomPredicateWithoutMetadata_FallsBack()
    {
        var matcher = new PredicateMatcher(_ => true);

        Assert.False(StreamingTreeQuery.TryCompile(matcher, out _));
    }

    [Fact]
    public void TryCompile_QueryBeyondBitCapacity_FallsBack()
    {
        var matcher = new AndMatcher(Enumerable.Range(0, 65)
            .Select(index => (Matcher)new PredicateMatcher(
                _ => true,
                Key: $"Key{index}"))
            .ToList());

        Assert.False(StreamingTreeQuery.TryCompile(matcher, out _));
    }

    private static Matcher Parse(string query)
    {
        var parser = new Parser();
        parser.Reset(Lexer.Tokenize(query));

        var matcher = parser.Parse();

        Assert.False(parser.Error);
        return matcher;
    }
}
