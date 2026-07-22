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
    public void SearchBlueprints_PropertyKeyBloomIndex_PrefiltersRequiredEscapedKey()
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
        db.BuildPropertyKeyBloomIndex();

        var diagnostics = new BlueprintDB.SearchDiagnostics();
        var results = db.SearchBlueprints(
            "^RareKey=target^",
            new ScoreBuffer(),
            CancellationToken.None,
            diagnostics);

        Assert.Equal("Wanted", Assert.Single(results).Name);
        Assert.True(db.HasPropertyKeyBloomIndex);
        Assert.Equal(
            2 * BlueprintDB.PropertyKeyBloomBitsPerBlueprint / 8,
            db.PropertyKeyBloomIndexBytes);
        Assert.Equal(1, diagnostics.PropertyKeyCandidates);
        Assert.Equal(1, diagnostics.TreeCandidatesEvaluated);
    }

    [Fact]
    public void AddBlueprint_InvalidatesPropertyKeyBloomIndex()
    {
        var db = new BlueprintDB("Test");
        db.AddBlueprint(Blueprint(
            "00000000-0000-0000-0000-000000000001",
            "Initial",
            """{"InitialKey":"yes"}"""));
        db.BuildPropertyKeyBloomIndex();

        db.AddBlueprint(Blueprint(
            "00000000-0000-0000-0000-000000000002",
            "Added",
            """{"LateKey":"yes"}"""));

        Assert.False(db.HasPropertyKeyBloomIndex);
        var results = db.SearchBlueprints(
            "^LateKey=yes^",
            new ScoreBuffer(),
            CancellationToken.None);
        Assert.Equal("Added", Assert.Single(results).Name);
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
