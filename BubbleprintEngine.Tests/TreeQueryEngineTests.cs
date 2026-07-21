using System.Text.Json;
using BubbleprintEngine;

namespace BubbleprintEngine.Tests;

public class TreeQueryEngineTests
{
    [Fact]
    public void CheckMatch_DirectChild_Matches()
    {
        using var json = JsonDocument.Parse("""{"Child":{"Value":"expected"}}""");
        var matcher = Parse("Child.Value=expected");

        Assert.True(TreeQueryEngine.CheckMatch(matcher, json.RootElement));
    }

    [Fact]
    public void CheckMatch_ArrayItem_RequiresOneItemToMatch()
    {
        using var json = JsonDocument.Parse(
            """{"Components":[{"$type":"Healing"},{"$type":"DirectDamage"}]}""");
        var matcher = Parse("Components[$type~=Damage");

        Assert.True(TreeQueryEngine.CheckMatch(matcher, json.RootElement));
    }

    [Fact]
    public void CheckMatch_AndPredicates_AreScopedToSameNode()
    {
        using var json = JsonDocument.Parse(
            """{"Components":[{"$type":"Damage","Enabled":false},{"$type":"Healing","Enabled":true}]}""");
        var matcher = Parse("Components[($type=Damage & Enabled=true)");

        Assert.False(TreeQueryEngine.CheckMatch(matcher, json.RootElement));
    }

    [Fact]
    public void CheckMatch_PredicateAgainstPrimitive_ReturnsFalse()
    {
        using var json = JsonDocument.Parse("42");
        var matcher = Parse("$type=Damage");

        Assert.False(TreeQueryEngine.CheckMatch(matcher, json.RootElement));
    }

    [Fact]
    public void CheckMatch_Descendant_FindsNestedObjectThroughArrays()
    {
        using var json = JsonDocument.Parse(
            """{"Components":[{"Actions":[{"Nested":{"$type":"DirectDamage"}}]}]}""");
        var matcher = Parse("Components..$type~=Damage");

        Assert.True(TreeQueryEngine.CheckMatch(matcher, json.RootElement));
    }

    [Fact]
    public void CheckMatch_DescendantRelation_FindsTargetBelowMatchingAnchor()
    {
        using var json = JsonDocument.Parse(
            """
            {
              "$type": "BlueprintAbility",
              "Components": [
                {
                  "$type": "AbilityEffectRunAction",
                  "Actions": {
                    "Actions": [
                      {
                        "$type": "Conditional",
                        "IfTrue": {
                          "Actions": [
                            {"$type": "ContextActionDealDamage"}
                          ]
                        }
                      }
                    ]
                  }
                }
              ]
            }
            """);
        var matcher = Parse(
            "$type~=BlueprintAbility & Components[($type~=AbilityEffectRunAction .. $type~=ContextActionDealDamage)");

        Assert.True(TreeQueryEngine.CheckMatch(matcher, json.RootElement));
    }

    [Fact]
    public void CheckMatch_DescendantRelation_DoesNotUseTargetBelowDifferentAnchor()
    {
        using var json = JsonDocument.Parse(
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
            """);
        var matcher = Parse(
            "Components[($type~=AbilityEffectRunAction .. $type~=ContextActionDealDamage)");

        Assert.False(TreeQueryEngine.CheckMatch(matcher, json.RootElement));
    }

    [Fact]
    public void CheckMatch_ChainedDescendantRelations_PreserveAncestry()
    {
        using var json = JsonDocument.Parse(
            """
            {
              "$type": "Root",
              "Branch": {
                "$type": "Middle",
                "Nested": [{"$type": "Leaf"}]
              }
            }
            """);
        var matcher = Parse("$type=Root .. $type=Middle .. $type=Leaf");

        Assert.True(TreeQueryEngine.CheckMatch(matcher, json.RootElement));
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
