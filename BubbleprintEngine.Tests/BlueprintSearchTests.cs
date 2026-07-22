using System.Text;
using System.Text.Json;
using BlueprintExplorer;

namespace BubbleprintEngine.Tests;

public class BlueprintSearchTests
{
    [Fact]
    public void SearchBlueprints_TreeOnlyQuery_FiltersBlueprints()
    {
        var db = new BlueprintDB("Test");
        db.AddBlueprint(Blueprint(
            "00000000-0000-0000-0000-000000000001",
            "Wanted",
            """{"$type":"WantedBlueprint"}"""));
        db.AddBlueprint(Blueprint(
            "00000000-0000-0000-0000-000000000002",
            "Other",
            """{"$type":"OtherBlueprint"}"""));

        var diagnostics = new BlueprintDB.SearchDiagnostics();
        var results = db.SearchBlueprints(
            "^$type~=Wanted^",
            new ScoreBuffer(),
            CancellationToken.None,
            diagnostics);

        var result = Assert.Single(results);
        Assert.Equal("Wanted", result.Name);
        Assert.Equal(1, diagnostics.TreeCandidates);
        Assert.Equal(1, diagnostics.TreeCandidatesEvaluated);
        Assert.False(diagnostics.TreeTruncated);
    }

    [Fact]
    public void SearchBlueprints_ScopedPartialTreeQuery_FiltersBlueprints()
    {
        var db = new BlueprintDB("Test");
        db.AddBlueprint(Blueprint(
            "00000000-0000-0000-0000-000000000001",
            "Wanted",
            """
            {
              "$type": "BlueprintAbility",
              "Components": [{
                "$type": "AbilityEffectRunAction",
                "Nested": [{"$type": "ContextActionDealDamage"}]
              }]
            }
            """));
        db.AddBlueprint(Blueprint(
            "00000000-0000-0000-0000-000000000002",
            "WrongScope",
            """
            {
              "$type": "BlueprintAbility",
              "Components": [
                {"$type": "AbilityEffectRunAction"},
                {
                  "$type": "OtherComponent",
                  "Nested": [{"$type": "ContextActionDealDamage"}]
                }
              ]
            }
            """));

        var results = db.SearchBlueprints(
            "^$type~=BlueprintAbility & Components[($type~=AbilityEffectRunAction .. $type~=ContextActionDealDamage)^",
            new ScoreBuffer(),
            CancellationToken.None);

        var result = Assert.Single(results);
        Assert.Equal("Wanted", result.Name);
    }

    [Fact]
    public void SearchBlueprints_RootTypePredicate_PrefiltersBeforeTreeEvaluation()
    {
        var db = new BlueprintDB("Test");
        db.AddBlueprint(Blueprint(
            "00000000-0000-0000-0000-000000000001",
            "Wanted",
            """{"$type":"Wanted"}"""));
        db.AddBlueprint(Blueprint(
            "00000000-0000-0000-0000-000000000002",
            "Other",
            """{"$type":"Other"}"""));

        var diagnostics = new BlueprintDB.SearchDiagnostics();
        var results = db.SearchBlueprints(
            "^$type=Wanted^",
            new ScoreBuffer(),
            CancellationToken.None,
            diagnostics);

        Assert.Single(results);
        Assert.Equal(1, diagnostics.TreeCandidates);
        Assert.Equal(1, diagnostics.TreeCandidatesEvaluated);
        Assert.Equal(2 * sizeof(ushort), db.RootTypeIndexBytes);
        Assert.Equal(2, db.RootTypeCount);
    }

    [Fact]
    public void SearchBlueprints_TypePredicates_AutomaticallyPrefilterCandidates()
    {
        var db = new BlueprintDB("Test")
        {
            FlatIndexToTypeName =
            [
                "Test.AbilityEffectRunAction",
                "Test.ContextActionDealDamage",
            ],
        };

        var wanted = Blueprint(
            "00000000-0000-0000-0000-000000000001",
            "Wanted",
            """
            {
              "Components": [{
                "$type": "AbilityEffectRunAction",
                "Nested": [{"$type": "ContextActionDealDamage"}]
              }]
            }
            """);
        wanted.ComponentIndex = [0, 1];
        db.AddBlueprint(wanted);

        var missingTarget = Blueprint(
            "00000000-0000-0000-0000-000000000002",
            "MissingTarget",
            """
            {
              "Components": [{
                "$type": "AbilityEffectRunAction",
                "Nested": [{"$type": "ContextActionSpawnFx"}]
              }]
            }
            """);
        missingTarget.ComponentIndex = [0];
        db.AddBlueprint(missingTarget);

        var diagnostics = new BlueprintDB.SearchDiagnostics();
        var results = db.SearchBlueprints(
            "^Components[($type~=AbilityEffectRunAction .. $type~=ContextActionDealDamage)^",
            new ScoreBuffer(),
            CancellationToken.None,
            diagnostics);

        Assert.Single(results);
        Assert.Equal(1, diagnostics.TypePresenceCandidates);
        Assert.Equal(1, diagnostics.TreeCandidates);
    }

    [Fact]
    public void SearchBlueprints_PropertyKeyPostingIndex_PrefiltersRequiredEscapedKey()
    {
        var db = new BlueprintDB("Test");
        db.AddBlueprint(Blueprint(
            "00000000-0000-0000-0000-000000000001",
            "Wanted",
            """{"\u0052areKey":"target"}"""));
        db.AddBlueprint(Blueprint(
            "00000000-0000-0000-0000-000000000002",
            "Other",
            """{"Unrelated":"target"}"""));
        db.BuildPropertyKeyPostingIndex();

        var diagnostics = new BlueprintDB.SearchDiagnostics();
        var results = db.SearchBlueprints(
            "^RareKey=target^",
            new ScoreBuffer(),
            CancellationToken.None,
            diagnostics);

        Assert.Equal("Wanted", Assert.Single(results).Name);
        Assert.True(db.HasPropertyKeyPostingIndex);
        Assert.Equal(2, db.PropertyKeyPostingCount);
        Assert.Equal(2, db.PropertyKeyPostingIndexBytes);
        Assert.Equal(1, diagnostics.PropertyKeyCandidates);
        Assert.Equal(1, diagnostics.TreeCandidatesEvaluated);
    }

    [Fact]
    public void AddBlueprint_InvalidatesPropertyKeyPostingIndex()
    {
        var db = new BlueprintDB("Test");
        db.AddBlueprint(Blueprint(
            "00000000-0000-0000-0000-000000000001",
            "Initial",
            """{"InitialKey":"yes"}"""));
        db.BuildPropertyKeyPostingIndex();

        db.AddBlueprint(Blueprint(
            "00000000-0000-0000-0000-000000000002",
            "Added",
            """{"LateKey":"yes"}"""));

        Assert.False(db.HasPropertyKeyPostingIndex);
        var results = db.SearchBlueprints(
            "^LateKey=yes^",
            new ScoreBuffer(),
            CancellationToken.None);
        Assert.Equal("Added", Assert.Single(results).Name);
    }

    [Fact]
    public void SearchBlueprints_ScalarValuePostingIndex_RejectsMissingExactValue()
    {
        var db = new BlueprintDB("Test");
        db.AddBlueprint(Blueprint(
            "00000000-0000-0000-0000-000000000001",
            "And",
            """{"Operation":"And"}"""));
        db.AddBlueprint(Blueprint(
            "00000000-0000-0000-0000-000000000002",
            "Or",
            """{"Operation":"Or"}"""));
        db.AddBlueprint(Blueprint(
            "00000000-0000-0000-0000-000000000003",
            "Other",
            """{"Unrelated":"Missing"}"""));
        db.BuildPropertyKeyPostingIndex();
        db.BuildScalarValuePostingIndex();

        var diagnostics = new BlueprintDB.SearchDiagnostics();
        var results = db.SearchBlueprints(
            "^Operation=Missing^",
            new ScoreBuffer(),
            CancellationToken.None,
            diagnostics);

        Assert.Empty(results);
        Assert.True(db.HasScalarValuePostingIndex);
        Assert.Equal(3, diagnostics.PropertyKeyCandidates);
        Assert.Equal(0, diagnostics.ScalarValueCandidates);
        Assert.Equal(0, diagnostics.TreeCandidatesEvaluated);
    }

    [Theory]
    [InlineData("Flag=true", 2)]
    [InlineData("Count=1", 2)]
    [InlineData("Empty=null", 2)]
    public void SearchBlueprints_ScalarValuePostingIndex_PreservesCrossTypeMatches(
        string predicate,
        int expected)
    {
        var db = new BlueprintDB("Test");
        db.AddBlueprint(Blueprint(
            "00000000-0000-0000-0000-000000000001",
            "Typed",
            """{"Flag":true,"Count":1,"Empty":null}"""));
        db.AddBlueprint(Blueprint(
            "00000000-0000-0000-0000-000000000002",
            "Strings",
            """{"Flag":"TRUE","Count":"1","Empty":"NULL"}"""));
        db.BuildPropertyKeyPostingIndex();
        db.BuildScalarValuePostingIndex();

        var diagnostics = new BlueprintDB.SearchDiagnostics();
        var results = db.SearchBlueprints(
            $"^{predicate}^",
            new ScoreBuffer(),
            CancellationToken.None,
            diagnostics);

        Assert.Equal(expected, results.Count);
        Assert.Equal(expected, diagnostics.ScalarValueCandidates);
    }

    [Fact]
    public void SearchBlueprints_ScalarValuePostingIndex_IntersectsExactPredicates()
    {
        var db = new BlueprintDB("Test");
        db.AddBlueprint(Blueprint(
            "00000000-0000-0000-0000-000000000001",
            "Both",
            """{"First":"yes","Second":"target"}"""));
        db.AddBlueprint(Blueprint(
            "00000000-0000-0000-0000-000000000002",
            "FirstOnly",
            """{"First":"yes","Second":"other"}"""));
        db.AddBlueprint(Blueprint(
            "00000000-0000-0000-0000-000000000003",
            "SecondOnly",
            """{"First":"no","Second":"target"}"""));
        db.BuildPropertyKeyPostingIndex();
        db.BuildScalarValuePostingIndex();

        var diagnostics = new BlueprintDB.SearchDiagnostics();
        var results = db.SearchBlueprints(
            "^First=yes & Second=target^",
            new ScoreBuffer(),
            CancellationToken.None,
            diagnostics);

        Assert.Equal("Both", Assert.Single(results).Name);
        Assert.Equal(1, diagnostics.ScalarValueCandidates);
        Assert.Equal(1, diagnostics.TreeCandidatesEvaluated);
    }

    [Fact]
    public void SearchBlueprints_ScalarValuePostingIndex_BypassesFuzzyAndLongValues()
    {
        string longValue = new('x', 65);
        var db = new BlueprintDB("Test");
        db.AddBlueprint(Blueprint(
            "00000000-0000-0000-0000-000000000001",
            "Wanted",
            $$"""{"Text":"target","Long":"{{longValue}}"}"""));
        db.AddBlueprint(Blueprint(
            "00000000-0000-0000-0000-000000000002",
            "Other",
            """{"Text":"other"}"""));
        db.BuildPropertyKeyPostingIndex();
        db.BuildScalarValuePostingIndex();

        var diagnostics = new BlueprintDB.SearchDiagnostics();
        var fuzzyResults = db.SearchBlueprints(
            "^Text~=arg^",
            new ScoreBuffer(),
            CancellationToken.None,
            diagnostics);

        Assert.Equal("Wanted", Assert.Single(fuzzyResults).Name);
        Assert.Equal(2, diagnostics.ScalarValueCandidates);

        var longResults = db.SearchBlueprints(
            $"^Long={longValue}^",
            new ScoreBuffer(),
            CancellationToken.None,
            diagnostics);

        Assert.Equal("Wanted", Assert.Single(longResults).Name);
        Assert.Equal(1, diagnostics.ScalarValueCandidates);
    }

    [Fact]
    public void SearchBlueprints_ScalarValuePostingIndex_FallsBackForHighCardinalityKey()
    {
        var db = new BlueprintDB("Test");
        for (int i = 0; i < 257; i++)
        {
            db.AddBlueprint(Blueprint(
                $"00000000-0000-0000-0000-{i + 1:D12}",
                $"Blueprint{i}",
                $$"""{"HighCardinality":"value{{i}}"}"""));
        }
        db.BuildPropertyKeyPostingIndex();
        db.BuildScalarValuePostingIndex();

        var diagnostics = new BlueprintDB.SearchDiagnostics();
        var results = db.SearchBlueprints(
            "^HighCardinality=value0^",
            new ScoreBuffer(),
            CancellationToken.None,
            diagnostics);

        Assert.Equal("Blueprint0", Assert.Single(results).Name);
        Assert.Equal(1, db.ScalarValueDisqualifiedKeyCount);
        Assert.Equal(257, diagnostics.ScalarValueCandidates);
    }

    [Fact]
    public void AddBlueprint_InvalidatesScalarValuePostingIndex()
    {
        var db = new BlueprintDB("Test");
        db.AddBlueprint(Blueprint(
            "00000000-0000-0000-0000-000000000001",
            "Initial",
            """{"Status":"old"}"""));
        db.BuildScalarValuePostingIndex();

        db.AddBlueprint(Blueprint(
            "00000000-0000-0000-0000-000000000002",
            "Added",
            """{"Status":"new"}"""));

        Assert.False(db.HasScalarValuePostingIndex);
        var results = db.SearchBlueprints(
            "^Status=new^",
            new ScoreBuffer(),
            CancellationToken.None);
        Assert.Equal("Added", Assert.Single(results).Name);
    }

    [Fact]
    public void SearchBlueprints_PropertyKeyPostingIndex_IntersectsRequiredKeys()
    {
        var db = new BlueprintDB("Test");
        db.AddBlueprint(Blueprint(
            "00000000-0000-0000-0000-000000000001",
            "Both",
            """{"First":true,"Second":true}"""));
        db.AddBlueprint(Blueprint(
            "00000000-0000-0000-0000-000000000002",
            "FirstOnly",
            """{"First":true}"""));
        db.AddBlueprint(Blueprint(
            "00000000-0000-0000-0000-000000000003",
            "SecondOnly",
            """{"Second":true}"""));
        db.BuildPropertyKeyPostingIndex();

        var diagnostics = new BlueprintDB.SearchDiagnostics();
        var results = db.SearchBlueprints(
            "^First & Second^",
            new ScoreBuffer(),
            CancellationToken.None,
            diagnostics);

        Assert.Equal("Both", Assert.Single(results).Name);
        Assert.Equal(1, diagnostics.PropertyKeyCandidates);

        results = db.SearchBlueprints(
            "^MissingKey^",
            new ScoreBuffer(),
            CancellationToken.None,
            diagnostics);

        Assert.Empty(results);
        Assert.Equal(0, diagnostics.PropertyKeyCandidates);
        Assert.Equal(0, diagnostics.TreeCandidatesEvaluated);
    }

    [Fact]
    public void SearchBlueprints_StreamingMode_DoesNotPopulateJsonElementCache()
    {
        var db = new BlueprintDB("Test")
        {
            PreferStreamingTreeQueries = true,
        };
        var wanted = Blueprint(
            "00000000-0000-0000-0000-000000000001",
            "Wanted",
            """{"RareKey":"target"}""");
        var other = Blueprint(
            "00000000-0000-0000-0000-000000000002",
            "Other",
            """{"Unrelated":"target"}""");
        db.AddBlueprint(wanted);
        db.AddBlueprint(other);

        var diagnostics = new BlueprintDB.SearchDiagnostics();
        var results = db.SearchBlueprints(
            "^RareKey=target^",
            new ScoreBuffer(),
            CancellationToken.None,
            diagnostics);

        Assert.Equal("Wanted", Assert.Single(results).Name);
        Assert.Equal(2, diagnostics.TreeCandidatesStreamed);
        Assert.Equal(JsonValueKind.Undefined, wanted._obj.ValueKind);
        Assert.Equal(JsonValueKind.Undefined, other._obj.ValueKind);
    }

    [Fact]
    public void SearchBlueprints_InvalidTreeQuery_ThrowsExplicitError()
    {
        var db = new BlueprintDB("Test");
        db.AddBlueprint(Blueprint(
            "00000000-0000-0000-0000-000000000001",
            "Wanted",
            """{"$type":"WantedBlueprint"}"""));

        var error = Assert.Throws<ArgumentException>(() => db.SearchBlueprints(
            "^$type~=Wanted ..^",
            new ScoreBuffer(),
            CancellationToken.None));

        Assert.Contains("Invalid tree query", error.Message);
    }

    [Fact]
    public void BlueprintHandle_Utf8RawAndParsedJson_PreserveCyrillicText()
    {
        const string text = "Проверка урона";
        string raw = $$"""{"$type":"Test.Blueprint","Description":"{{text}}"}""";
        byte[] rawUtf8 = Encoding.UTF8.GetBytes(raw);
        var blueprint = Blueprint(
            "00000000-0000-0000-0000-000000000001",
            "Cyrillic",
            raw);

        Assert.Equal(rawUtf8, blueprint.RawUtf8);
        Assert.Equal(rawUtf8.Length, blueprint.RawByteLength);
        Assert.True(blueprint.RawByteLength > blueprint.Raw.Length);
        Assert.Equal(raw, blueprint.Raw);
        Assert.Equal(text, blueprint.EnsureObj.GetProperty("Description").GetString());
    }

    private static BlueprintHandle Blueprint(string guid, string name, string raw)
    {
        var blueprint = new BlueprintHandle
        {
            GuidText = guid,
            Name = name,
            Type = $"Test.{name}",
            _RawUtf8 = Encoding.UTF8.GetBytes(raw),
        };
        blueprint.ParseType();
        return blueprint;
    }
}
