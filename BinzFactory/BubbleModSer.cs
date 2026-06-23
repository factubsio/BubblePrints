using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BinzFactory;

internal static class BubbleModSer
{
    private static readonly HashSet<Type> objTypes = [];
    private static readonly HashSet<Type> enums = [];
    private static readonly Queue<Type> objQueue = [];

    private static readonly Dictionary<string, string> typeNames = [];

    private static ModuleContext modCtx = ModuleDef.CreateModuleContext();
    private static ModuleDefMD module = null!;
    private static Type simpleBlueprint = null!;
    private static Type blueprintComponent = null!;
    private static Type gameAction = null!;
    private static Type blueprintRef = null!;

    private static SchemaModel schema = new();

    private static readonly List<string> BlueprintSet = [
        .."Kingmaker.Blueprints.Classes".Items(
            "BlueprintFeature",
            "BlueprintCharacterClass",
            "BlueprintArchetype",
            "Selection.BlueprintFeatureSelection",
            "Prerequisites.PrerequisiteClassLevel"
        ),
        .."Kingmaker.UnitLogic.Mechanics.Properties".Items(
            "BlueprintUnitProperty",
            "FactRankGetter"
        ),
        "Kingmaker.UnitLogic.Buffs.Blueprints.BlueprintBuff",
        "Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility",
        "Kingmaker.UnitLogic.ActivatableAbilities.BlueprintActivatableAbility",
        .."Kingmaker.Blueprints.Items".Items(
            "Weapons.BlueprintItemWeapon",
            "Weapons.BlueprintWeaponType",
            "Armors.BlueprintArmorType",
            "Armors.BlueprintItemArmor",
            "Armors.BlueprintShieldType",
            "Equipment.BlueprintItemEquipmentBelt",
            "Equipment.BlueprintItemEquipmentFeet",
            "Equipment.BlueprintItemEquipmentGlasses",
            "Equipment.BlueprintItemEquipmentGloves",
            "Equipment.BlueprintItemEquipmentHand",
            "Equipment.BlueprintItemEquipmentHandSimple",
            "Equipment.BlueprintItemEquipmentHead",
            "Equipment.BlueprintItemEquipmentNeck",
            "Equipment.BlueprintItemEquipmentRing",
            "Equipment.BlueprintItemEquipmentShirt",
            "Equipment.BlueprintItemEquipmentShoulders",
            "Equipment.BlueprintItemEquipmentSimple",
            "Equipment.BlueprintItemEquipmentUsable",
            "Equipment.BlueprintItemEquipmentWrist"
        ),
    ];
 
    private static readonly List<string> ComponentSet = [
        .."Kingmaker.UnitLogic.FactLogic".Items(
            "AddStatBonus",
            "AddFacts",
            "AddCondition",
            "AddDamageDecline",
            "AddDamageResistanceEnergy",
            "AddDamageResistanceForce",
            "AddDamageResistanceHardness",
            "AddDamageResistancePhysical",
            "AddDamageTypeVulnerability"
        ),

        .."Kingmaker.Designers.Mechanics.Buffs".Items(
            "TemporaryHitPointsFromAbilityValue"
        ),

        .."Kingmaker.UnitLogic.Mechanics.Components".Items(
            "AddFactContextActions",
            "ContextRankConfig",
            "AddInitiatorAttackWithWeaponTrigger"
        ),

        .."Kingmaker.UnitLogic.Abilities.Components.CasterCheckers".Items(
            "AbilityCasterHasFacts",
            "AbilityCasterHasNoFacts"
        ),


        "Kingmaker.UnitLogic.Mechanics.ContextValue",
        "Kingmaker.Blueprints.Classes.LevelEntry",
    ];

    private static readonly List<string> ActionSet = [
        "Kingmaker.Designers.EventConditionActionSystem.Actions.AddFact",
        "Kingmaker.Designers.EventConditionActionSystem.Actions.GameLog",
        .."Kingmaker.UnitLogic.Mechanics.Actions".Items(
            "ContextActionSkillCheck",
            "ContextActionApplyBuff",
            "ContextActionDealDamage",
            "ContextActionSavingThrow",
            "ContextActionRemoveSelf",
            "ContextActionConditionalSaved",
            "ContextActionPush"
        )
    ];

    private static readonly HashSet<string> BannedComponentPrefixes = [
        "Kingmaker.UnitLogic.UnitFactComponentDelegate",
        "Kingmaker.EntitySystem.EntityFactComponentDelegate",
    ];
    private static Dictionary<string, object> FindFieldInit(Type type)
    {
        var dnlibType = module.Find(type.FullName, isReflectionName: true);
        if (dnlibType == null) return [];

        var ctor = dnlibType.Methods.FirstOrDefault(m => m.IsConstructor && !m.IsStatic && m.Parameters.Count == 1);
        if (ctor == null) return [];

        Dictionary<string, object> defaults = [];
        var instrs = ctor.Body.Instructions;

        for (int i = 2; i < instrs.Count; i++)
        {
            // Pattern: [ldarg.0] -> [load constant] -> [stfld field]
            if (instrs[i].OpCode == OpCodes.Stfld && instrs[i - 2].OpCode == OpCodes.Ldarg_0)
            {
                var field = (IField)instrs[i].Operand;
                var valInstr = instrs[i - 1];

                // Simplified value extraction (expand for floats/bools/etc)
                object? val = valInstr.OpCode.Code switch
                {
                    Code.Ldc_I4 => valInstr.Operand,
                    Code.Ldc_I4_S => Convert.ToInt32((sbyte)valInstr.Operand), // Tiny int
                    Code.Ldstr => valInstr.Operand,
                    Code.Ldc_I4_0 => 0,
                    Code.Ldc_I4_1 => 1, // ... handle 2-8
                    _ => null
                };

                if (val != null) defaults[field.Name] = val;
            }
        }

        return defaults;
    }

    private static string currentContext = "_";

    internal static void Bubbble(IGameDefinitions game, List<Type?> types)
    {
        module = ModuleDefMD.Load(game.Wrath.Location, modCtx);

        var customAsembly = game.LoadAssembly(@"D:\steamlib\steamapps\common\Pathfinder Second Adventure\Mods\BubbleModRunnerWrath\BubbleModRunnerWrath.dll");

        simpleBlueprint = game.Wrath.GetType("Kingmaker.Blueprints.SimpleBlueprint") ?? throw new NotSupportedException();
        blueprintComponent = game.Wrath.GetType("Kingmaker.Blueprints.BlueprintComponent") ?? throw new NotSupportedException();
        gameAction = game.Wrath.GetType("Kingmaker.ElementsSystem.GameAction") ?? throw new NotSupportedException();
        blueprintRef = game.Wrath.GetType("Kingmaker.Blueprints.BlueprintReferenceBase") ?? throw new NotSupportedException();

        objTypes.Add(simpleBlueprint);

        objQueue.Enqueue(customAsembly.GetType("Bubble.StrangeMagic.Truenamer.RecitationCheck") ?? throw new NotSupportedException());
        objQueue.Enqueue(customAsembly.GetType("Bubble.StrangeMagic.Truenamer.ContextActionPull") ?? throw new NotSupportedException());

        objQueue.Enqueue(customAsembly.GetType("BubbleModRunnerWrath.Components.CustomTooltipComponent") ?? throw new NotSupportedException());
        objQueue.Enqueue(customAsembly.GetType("BubbleModRunnerWrath.Components.SimpleConditional") ?? throw new NotSupportedException());

        foreach (var blueprintWanted in BlueprintSet)
        {
            objQueue.Enqueue(game.Wrath.GetType(blueprintWanted) ?? throw new NotSupportedException());
        }

        foreach (var componentWanted in ComponentSet)
        {
            objQueue.Enqueue(game.Wrath.GetType(componentWanted) ?? throw new NotSupportedException());
        }

        foreach (var actionWanted in ActionSet)
        {
            objQueue.Enqueue(game.Wrath.GetType(actionWanted) ?? throw new NotSupportedException());
        }

        while (objQueue.TryDequeue(out var type))
        {
            if (typeNames.TryGetValue(type.Name, out var currentFullName))
            {
                if (currentFullName == type.FullName) continue;
                else throw new NotSupportedException($"blueprint name clash {type.FullName} vs {currentFullName}");
            }
            else
            {
                typeNames.Add(type.Name, type.FullName!);
            }

            if (type.BaseType != null && IncludeType(type.BaseType) && objTypes.Add(type.BaseType))
            {
                objQueue.Enqueue(type.BaseType);
            }

            objTypes.Add(type);
        }

        JsonSerializerOptions opts = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        };

        foreach (var objType in objTypes)
        {
            if (objType == simpleBlueprint) continue;
            BlueprintDefinition def = new();
            if (objType.IsAssignableTo(simpleBlueprint))
                def.Category = "blueprint";
            else if (objType.IsAssignableTo(blueprintComponent))
                def.Category = "component";
            else if (objType.IsAssignableTo(gameAction))
                def.Category = "action";
            else
                def.Category = "generic";


                var bpName = objType.Name;
            def.Extends = objType.BaseType!.Name;

            var defaults = FindFieldInit(objType);

            foreach (var field in objType.GetSerializableFields())
            {
                currentContext = $"obj:{objType.Name}.{field.Name}";

                var prop = TypeToProp(field.FieldType, defaults.GetValueOrDefault(field.Name));

                string name = field.Name;
                if (field.Name.StartsWith("m_"))
                {
                    name = name[2..];
                    prop.mPrefix = true;
                }
                def.Properties.Add(name, prop);
            }
            //Console.WriteLine(blueprint.FullName);
            //Console.WriteLine(JsonSerializer.Serialize(def, opts));
            schema.Types.Add(objType.Name, def);
        }

        foreach (var reflType in enums)
        {
            var type = module.Find(reflType.FullName!, true);
            EnumDefinition def = new(type.FullName); 
            var enumNames = new List<string>();
            // Enums are stored as static literal fields
            foreach (var field in type.Fields)
            {
                if (field.IsLiteral && field.IsStatic)
                {
                    def.Values.Add(new()
                    {
                        Name = field.Name,
                    });
                }
            }

            var name = type.Name;
            if (type.DeclaringType != null)
            {
                name = $"{type.DeclaringType.Name}_{type.Name}";
            }

            if (!schema.Enums.TryAdd(name, def))
            {
                Console.Error.WriteLine($"enum name clash {name}: {def.FullType} -- {schema.Enums[name].FullType}");
                throw new Exception("duplicate enum name");
            }

        }

        var typeToBase = objTypes
                .Where(x => x.Name != "SimpleBlueprint")
                .Select(x => new
                {
                    x.Name,
                    Base = OverrideBaseType(x.BaseType),
                })
                .ToList();

        const string path = @"C:\Users\worce\source\repos\bubblemodding\bubblemod-vscode\src";
        {
            using FileStream file = new(Path.Combine(path, "wrath.ts"), FileMode.Create);
            using StreamWriter wr = new(file);
            wr.WriteLine("import { Schema } from './schema';");
            wr.Write("export const wrathTypes: Schema = ");
            wr.Write(JsonSerializer.Serialize(schema, opts));
            wr.WriteLine(";");
            wr.Flush();
            file.Flush();
        }

        {
            using FileStream file = new(Path.Combine(path, "wrathTypeHierarchy.ts"), FileMode.Create);
            using StreamWriter wr = new(file);
            wr.Write("export const wrathTypeHierarchy = ");
            wr.Write(JsonSerializer.Serialize(typeToBase, opts));
            wr.WriteLine(";");
            wr.Flush();
            file.Flush();
        }

        // FIXME(bubbles) DO NOT DO THIS
        Environment.Exit(0);
    }

    private static bool IncludeType(Type type)
    {
        if (type == blueprintComponent || type == gameAction || type == simpleBlueprint || type.FullName == "System.Object") return false;
        if (BannedComponentPrefixes.Any(a => type.FullName!.StartsWith(a))) return false;

        return true;
    }

    public static string? OverrideBaseType(Type? baseType)
    {
        if (baseType == null) return null;

        if (BannedComponentPrefixes.Any(prefix => baseType.FullName!.StartsWith(prefix))) return null;

        return baseType.Name;

    }

    private static readonly Dictionary<string, List<string>> ParseTrees = [];

    private static PropertyDefinition TypeToProp(Type type, object? v)
    {
        if (type.IsEnum)
        {
            enums.Add(type);
            return new EnumProperty(type.Name, null);
        }

        if (type.Name == "BlueprintComponent")
        {
            return new ComponentProperty();
        }

        if (type.IsAssignableTo(blueprintRef))
        {
            Type refType;
            if (type.BaseType!.IsGenericType)
            {
                refType = type.BaseType.GetGenericArguments()[0];
            }
            else
            {
                throw new NotSupportedException(type.FullName);
            }

            return new ReferenceProperty()
            {
                ReferencedType = refType.Name
            };
        }

        List<string>? localParseTree = null;
        if (type.GetInterface("IListLike") != null)
        {
            localParseTree = [
                ..type.GetCustomAttributesData()
                    .Where(t => t.AttributeType.Name == "BubbleParseableAttribute")
                    .Select(x => (string)x.ConstructorArguments[0].Value!)
            ];
        }

        PropertyDefinition prop = type switch
        {
            { FullName: "System.String" } => new StringProperty(null),
            { FullName: "Kingmaker.Localization.LocalizedString" } => new StringProperty(null),
            { FullName: "System.Int32" } => new NumberProperty(v != null ? (int)v : 0),
            { FullName: "System.Int64" } => new NumberProperty(v != null ? (long)v : 0),
            { FullName: "System.UInt32" } => new NumberProperty(v != null ? (uint)v : 0),
            { FullName: "System.UInt64" } => new NumberProperty(v != null ? (ulong)v : 0),
            { FullName: "System.Single" } => new NumberProperty(v != null ? (float)v : 0),
            { FullName: "System.Double" } => new NumberProperty(v != null ? (double)v : 0),
            { FullName: "System.Boolean" } => new BooleanProperty(false),
            { FullName: "UnityEngine.Sprite" } => new SpriteProperty(),
            { FullName: "Kingmaker.ElementsSystem.ActionList" } => new ListProperty() { ElementType = new ObjectProperty("GameAction") },
            { FullName: "Kingmaker.ResourceLinks.PrefabLink" } => new ResourceLinkProperty("prefab"),
            { FullName: "Kingmaker.ResourceLinks.EquipmentEntityLink" } => new ResourceLinkProperty("ee"),
            { FullName: "Kingmaker.Utility.Feet" } => new DistanceFeetProperty(),
            { FullName: "Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility+MaterialComponentData" } => new ComplexProperty("material"),
            { FullName: "Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig+CustomProgressionItem" } => new ComplexProperty("progression"),
            { FullName: "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionSkillCheck+ConditionalDCIncrease" } => new ComplexProperty("skill_cond_dc_increase"),
            { FullName: "Kingmaker.UnitLogic.Mechanics.Actions.ContextActionSavingThrow+ConditionalDCIncrease" } => new ComplexProperty("save_cond_dc_increase"),
            { FullName: "Kingmaker.Blueprints.Items.Weapons.WeaponVisualParameters" } => new ComplexProperty("WeaponVisualParameters"),
            { FullName: "Kingmaker.Blueprints.Items.Armors.ArmorVisualParameters" } => new ComplexProperty("ArmorVisualParameters"),
            { FullName: "Kingmaker.ElementsSystem.UnitEvaluator" } => new ComplexProperty("unit_eval"),
            { FullName: "Kingmaker.UnitLogic.Mechanics.ContextDurationValue" } => new ComplexProperty("ctx_duration"),
            { FullName: "Kingmaker.UnitLogic.Mechanics.ContextDiceValue" } => new ComplexProperty("ctx_dice"),
            { FullName: "Kingmaker.RuleSystem.DiceFormula" } => new ComplexProperty("dice_formula"),
            { FullName: "Kingmaker.UnitLogic.Mechanics.ContextValue" } => new ComplexProperty("ContextValue"),
            { FullName: "Kingmaker.Blueprints.Classes.LevelEntry" } => new ComplexProperty("LevelEntry"),
            { FullName: "Kingmaker.UnitLogic.Mechanics.Properties.PropertySettings" } => new ComplexProperty("prop_settings"),
            { FullName: "Kingmaker.RuleSystem.Rules.Damage.DamageTypeDescription" } => new ComplexProperty("dmg_type_desc"),
            { FullName: "Kingmaker.UnitLogic.FactLogic.UnitConditionExceptions" } => new ComplexProperty("unit_condition_exceptions"),
            { FullName: "BubbleModRunnerWrath.Components.SimpleCondition" } => new FromListProperty("SimpleCondition"),
            { IsArray: true } => new ListProperty(),
            { FullName: var listName, IsGenericType: true } when (listName?.StartsWith("System.Collections.Generic.List") == true) => new ListProperty(),
            _ => throw new NotSupportedException(type.FullName),
        };

        if (localParseTree != null && prop is FromListProperty listLike)
        {
            BlueprintDefinition def = new();
            schema.Types.Add(listLike.Value, def);
            def.ParseTree = localParseTree;
            ParseTrees.Add(listLike.Value, localParseTree);
        }

        if (prop is ListProperty list && list.ElementType == null)
        {
            Type subType = (type.IsArray ? type.GetElementType() : type.GetGenericArguments()[0]) ?? throw new NotSupportedException(type.FullName);
            list.ElementType = TypeToProp(subType, null);
        }

        return prop;
    }

    public static IEnumerable<FieldInfo> GetSerializableFields(this Type type)
    {
        var all = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        return all.Where(f => f.IsPublic || f.CustomAttributes.Any(x => x.AttributeType.FullName == "UnityEngine.SerializeReference" || x.AttributeType.FullName == "UnityEngine.SerializeField"));
    }

    public static IEnumerable<string> Items(this string ns, params string[] items) => items.Select(i => $"{ns}.{i}");

}

// Root Schema
public class SchemaModel
{
    [JsonPropertyName("enums")]
    public Dictionary<string, EnumDefinition> Enums { get; set; } = [];

    [JsonPropertyName("types")]
    public Dictionary<string, BlueprintDefinition> Types { get; set; } = [];
}

// Blueprint & Component Definitions
public class DefinitionBase
{
    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("extends")]
    public string Extends { get; set; } = "";

    [JsonPropertyName("properties")]
    public Dictionary<string, PropertyDefinition> Properties { get; set; } = [];

    [JsonPropertyName("parse_tree")]
    public List<string>? ParseTree { get; set; } = null;
}

public class BlueprintDefinition : DefinitionBase
{
    [JsonPropertyName("allowedComponents")]
    public List<string> AllowedComponents { get; set; } = [];

    [JsonPropertyName("category")]
    public string Category { get; set; } = "";
}

public class ComponentDefinition : DefinitionBase { }

// Enums
public class EnumDefinition(string fullType)
{
    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("values")]
    public List<EnumValue> Values { get; set; } = [];

    [JsonIgnore]
    public string FullType => fullType;
}

public class EnumValue
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";
}

// Properties (Polymorphic)
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(StringProperty), "string")]
[JsonDerivedType(typeof(NumberProperty), "number")]
[JsonDerivedType(typeof(BooleanProperty), "boolean")]
[JsonDerivedType(typeof(EnumProperty), "enum")]
[JsonDerivedType(typeof(ObjectProperty), "object")]
[JsonDerivedType(typeof(ListProperty), "list")]
[JsonDerivedType(typeof(SpriteProperty), "sprite")]
[JsonDerivedType(typeof(DistanceFeetProperty), "dist_feet")]
[JsonDerivedType(typeof(ComplexProperty), "complex")]
[JsonDerivedType(typeof(FromListProperty), "as_list")]
[JsonDerivedType(typeof(ActionListProperty), "actions")]
[JsonDerivedType(typeof(ResourceLinkProperty), "resource")]
[JsonDerivedType(typeof(ReferenceProperty), "ref")]
[JsonDerivedType(typeof(ComponentProperty), "component")]
public abstract record class PropertyDefinition(string? Description = null, bool Required = false)
{
    [JsonIgnore]
    public bool mPrefix = false;
}


public record class StringProperty(string? Default) : PropertyDefinition;
public record class ReferenceProperty : PropertyDefinition
{
    public string ReferencedType { get; set; } = "";
}

public record class NumberProperty(double Default = 0) : PropertyDefinition;
public record class ComponentProperty : PropertyDefinition;

public record class BooleanProperty(bool? Default) : PropertyDefinition;

public record class EnumProperty(string EnumName, string? Default) : PropertyDefinition;
public record class SpriteProperty : PropertyDefinition;
public record class ActionListProperty : PropertyDefinition;
public record class ComplexProperty(string Value) : PropertyDefinition;
public record class FromListProperty(string Value) : PropertyDefinition;
public record class DistanceFeetProperty : PropertyDefinition;
public record class ResourceLinkProperty(string Resource) : PropertyDefinition;

public record class ObjectProperty(string TypeId) : PropertyDefinition
{
    public Dictionary<string, PropertyDefinition> Properties { get; set; } = [];
}

public record class ListProperty : PropertyDefinition
{
    public PropertyDefinition? ElementType { get; set; } = null;
}


public static class Bob
{

}
