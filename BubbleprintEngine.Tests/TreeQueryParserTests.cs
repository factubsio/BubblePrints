using BubbleprintEngine;

namespace BubbleprintEngine.Tests;

public class TreeQueryParserTests
{
    [Fact]
    public void Parse_DirectChildAndArrayItem_BuildsExpectedTree()
    {
        var matcher = Parse("Components[$type~=Damage");

        var components = Assert.IsType<PathMatcher>(matcher);
        Assert.Equal("Components", components.KeyRequirement);
        Assert.Equal(NfaCombinator.ArrayItem, components.Combinator);

        var predicate = Assert.IsType<PredicateMatcher>(components.NextRequirement);
        Assert.Equal("$type ~= Damage", predicate.Desc);
    }

    [Fact]
    public void Parse_DescendantPath_BuildsDescendantMatcher()
    {
        var matcher = Parse("Components..$type~=Damage");

        var components = Assert.IsType<PathMatcher>(matcher);
        Assert.Equal("Components", components.KeyRequirement);
        Assert.Equal(NfaCombinator.Descendant, components.Combinator);
    }

    [Fact]
    public void Parse_DescendantRelation_BuildsScopedRelation()
    {
        var matcher = Parse(
            "Components[($type~=AbilityEffectRunAction .. $type~=ContextActionDealDamage)");

        var components = Assert.IsType<PathMatcher>(matcher);
        var relation = Assert.IsType<RelationMatcher>(components.NextRequirement);
        Assert.Equal(NfaCombinator.Descendant, relation.Combinator);

        var anchor = Assert.IsType<PredicateMatcher>(relation.Anchor);
        Assert.Equal("$type ~= AbilityEffectRunAction", anchor.Desc);

        var target = Assert.IsType<PredicateMatcher>(relation.Target);
        Assert.Equal("$type ~= ContextActionDealDamage", target.Desc);
    }

    [Fact]
    public void Parse_ChainedRelations_AreRightAssociative()
    {
        var matcher = Parse("$type~=Root .. $type~=Middle .. $type~=Leaf");

        var rootRelation = Assert.IsType<RelationMatcher>(matcher);
        var nestedRelation = Assert.IsType<RelationMatcher>(rootRelation.Target);
        Assert.Equal("$type ~= Middle", Assert.IsType<PredicateMatcher>(nestedRelation.Anchor).Desc);
        Assert.Equal("$type ~= Leaf", Assert.IsType<PredicateMatcher>(nestedRelation.Target).Desc);
    }

    [Fact]
    public void Parse_MalformedRelation_ReportsError()
    {
        var parser = new Parser();
        parser.Reset(Lexer.Tokenize("$type~=Root .."));

        var matcher = parser.Parse();

        Assert.IsType<NullMatcher>(matcher);
        Assert.True(parser.Error);
        Assert.NotEmpty(parser.ErrorMessage);
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
