using BlueprintExplorer;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace bprint;
public class DialogVisitor(DialogSource dialog, BlueprintDB db)
{
    private int level = 0;
    private DialogNode VisitDialogItem(BlueprintHandle item)
    {
        try
        {
            level++;


            if (item.UserData != null)
            {
                return item.UserData as DialogNode ?? throw new NotSupportedException();
            }


            return item.TypeName switch
            {
                "BlueprintCheck" => VisitCheck(item),
                "BlueprintCue" or "BlueprintSequenceExit" => VisitCue(item),
                "BlueprintCueSequence" => VisitCueSequence(item),
                "BlueprintAnswer" => VisitAnswer(item),
                "BlueprintAnswersList" => VisitAnswerList(item),
                "BlueprintBookPage" => VisitBookPage(item),
                _ => throw new NotSupportedException(item.TypeName)
            };
        }
        finally
        {
            level--;
        }
    }

    private DialogNode VisitCue(BlueprintHandle item)
    {
        var obj = item.EnsureObj;
        var type = item.TypeName == "BlueprintCue" ? DialogNodeType.Cue : DialogNodeType.CueSequenceExit;
        var text = type switch
        {
            DialogNodeType.Cue => obj.TryGetProperty("Text", out var textNode) ? textNode.ParseAsString(db) : $"?: {item.GuidText}",
            _ => "_",
        };
        var node = new DialogNode(type, dialog, item.Guid, text ?? "<null>", level);
        item.UserData = node;

        if (obj.True("ShowOnce"))
            node.Props["s1"] = obj.True("ShowOnceCurrentDialog") ? "local" : "global";


        if (obj.TryGetConditions(out var conds, out var _, out var op))
        {
            node.IsConditional = true;
            DescribeConditions(node, conds, op);
        }

        if (obj.TryGetActions("OnShow", out var showActions))
        {
            node.HasShowActions = true;
            DescribeActions(node, showActions, "show");
        }

        if (obj.TryGetActions("OnStop", out var stopActions))
        {
            node.HasShowActions = true;
            DescribeActions(node, stopActions, "stop");
        }

        foreach (var sub in obj.EnumerateReferences(db, "Answers"))
        {
            node.AddChild(VisitDialogItem(sub));
        }

        foreach (var sub in obj.GetProperty("Continue").EnumerateReferences(db, "Cues"))
        {
            node.AddChild(VisitDialogItem(sub));
        }

        return node;
    }


    private DialogNode VisitCueSequence(BlueprintHandle item)
    {
        var obj = item.EnsureObj;
        var node = new DialogNode(DialogNodeType.CueSequence, dialog, item.Guid, ". . .", level);
        item.UserData = node;

        node.Sequence = [];
        foreach (var sub in obj.EnumerateReferences(db, "Cues"))
        {
            node.Sequence.Add(VisitDialogItem(sub));
        }

        if (obj.TryDeRef(db, out var exit, "m_Exit"))
        {
            node.AddChild(VisitDialogItem(exit));
        }

        return node;
    }
    private DialogNode VisitCheck(BlueprintHandle item)
    {
        var obj = item.EnsureObj;


        var difficulty = obj.TryGetProperty("Difficulty", out var difficultyNode) ? difficultyNode.ToString() : "Custom";
        var stat = obj.GetProperty("Type");

        var node = new DialogNode(DialogNodeType.Check, dialog, item.Guid, $"DC: {difficulty}\nvs: {stat}", level);
        item.UserData = node;

        node.Props["dif"] = difficulty;
        if (difficulty == "Custom")
        {
            node.Props["dc"] = obj.Int("DC");
        }

        node.Props["vs"] = stat.GetString() ?? "unknown";

        if (obj.TryDeRef(db, out var onSuccess, "m_Success"))
        {
            node.AddChild(VisitDialogItem(onSuccess), "success");
        }

        if (obj.TryDeRef(db, out var onFail, "m_Fail"))
        {
            node.AddChild(VisitDialogItem(onFail), "fail");
        }

        return node;
    }
    private DialogNode VisitAnswerList(BlueprintHandle item)
    {
        var obj = item.EnsureObj;
        var node = new DialogNode(DialogNodeType.AnswerList, dialog, item.Guid, "[]", level);
        item.UserData = node;

        foreach (var answer in obj.EnumerateReferences(db, "Answers"))
        {
            node.AddChild(VisitDialogItem(answer));
        }

        return node;
    }

    private DialogNode VisitAnswer(BlueprintHandle item)
    {
        var obj = item.EnsureObj;
        var node = new DialogNode(DialogNodeType.Answer, dialog, item.Guid, obj.GetProperty("Text").ParseAsString(db) ?? "<null>", level);
        item.UserData = node;

        if (obj.TryGetConditions(out var conds, out var _, out var op))
        {
            node.IsConditional = true;
            DescribeConditions(node, conds, op);
        }


        foreach (var cue in obj.GetProperty("NextCue").EnumerateReferences(db, "Cues"))
        {
            node.AddChild(VisitDialogItem(cue));
        }


        return node;
    }

    private DialogNode VisitBookPage(BlueprintHandle item)
    {
        var obj = item.EnsureObj;
        var node = new DialogNode(DialogNodeType.BookPage, dialog, item.Guid, "page", level);
        item.UserData = node;

        return node;
    }

    internal void Visit(BlueprintHandle dialogBp)
    {
        dialogBp.UserData = dialog;
        Console.WriteLine(dialogBp.Name);
        foreach (var firstCue in dialogBp.EnsureObj.GetProperty("FirstCue").EnumerateReferences(db, "Cues"))
        {
            dialog.First.Add(VisitDialogItem(firstCue));
        }
    }
    private void DescribeActions(DialogNode node, JsonElement actions, string name)
    {
        if (actions.GetArrayLength() > 1)
        {
            int actIndex = 0;
            foreach (var action in actions.EnumerateArray())
            {
                node.Props[$"{name}[{actIndex++}]"] = DescribeAction(action); ;
            }
        }
        else
        {
            node.Props[name] = DescribeAction(actions[0]);
        }

    }

    private void DescribeConditions(DialogNode node, JsonElement conds, string op)
    {
        if (conds.GetArrayLength() > 1)
        {
            node.Props["cond.op"] = op;
            int condIndex = 0;
            foreach (var cond in conds.EnumerateArray())
            {
                node.Props[$"c[{condIndex++}]"] = DescribeCondition(cond); ;
            }
        }
        else
        {
            node.Props["cond"] = DescribeCondition(conds[0]);
        }
    }

    private string DescribeAction(JsonElement action)
    {
        if (action.ValueKind == JsonValueKind.Null)
            return "null";

        var actType = action.Str("$type").NewTypeStr(db);

        return actType.Name switch
        {
            "Unrecruit" => ParseUnrecruit(action),
            "Recruit" => ParseRecruit(action),
            "StartEtude" => ParseEtudeAction(action, actType.Name),
            "SetObjectiveStatus" => ParseSetObjectiveStatus(action),
            "UnlockFlag" => ParseUnlockFlag(action),
            _ => ParseUnhandled(UnhandledActions, actType.Name),
        };
    }

    public static readonly HashSet<string> UnhandledActions = [];
    public static readonly HashSet<string> UnhandledConditions = [];

    private string DescribeCondition(JsonElement cond)
    {
        if (cond.ValueKind == JsonValueKind.Null)
            return "null";

        var condType = cond.Str("$type").NewTypeStr(db);
        var not = cond.True("Not");

        string notStr = not ? "NOT " : "";
        string desc = condType.Name switch
        {
            "EtudeStatus" => ParseEtudeStatus(cond),
            "FlagUnlocked" => ParseFlagUnlocked(cond),
            "CueSeen" => cond.GetDerefName(db, "m_Cue").Desc(cond.DlgScope("cue_seen")),
            "DialogSeen" => cond.GetDerefName(db, "m_Dialog").Desc("dialog_seen"),
            "AnswerSelected" => cond.GetDerefName(db, "m_Answer").Desc(cond.DlgScope("ans_selected")),
            "HasFact" => ParseHasFact(cond),
            "AreaVisited" => cond.GetDerefName(db, "m_Area").Desc("visited"),
            "ObjectiveStatus" => $"{cond.GetDerefName(db, "m_QuestObjective")} == {cond.GetProperty("State")}".Desc("objective"),
            "IsCompanionInParty" or "CompanionInParty" => ParseCompanionInParty(cond),
            _ => ParseUnhandled(UnhandledConditions, condType.Name),
        };

        return $"{notStr}{desc}";
    }

    private string ParseHasFact(JsonElement cond)
    {
        string unitEval = "?";
        if (cond.TryGetProperty("Unit", out var unit) && unit.ValueKind != JsonValueKind.Null)
        {
            unitEval = unit.NewTypeStr(db).Name;
        }
        return $"{unitEval}, {cond.GetDerefName(db, "m_Fact")}".Desc("has_fact");
    }

    private static string ParseUnhandled(HashSet<string> set, string name)
    {
        set.Add(name);
        return name;
    }

    private string ParseUnlockFlag(JsonElement action) => $"unlock{{{action.GetDerefName(db, "m_flag")} => {action.Int("flagValue")}}}";

    private string ParseEtudeAction(JsonElement action, string type)
    {
        var prefix = type switch
        {
            "StartEtude" => "etude.start",
            _ => type,
        };
        string name;
        if (action.True("Evaluate"))
        {
            name = action.GetProperty("EtudeEvaluator").Str("type").NewTypeStr(db).Name;
        }
        else if (action.TryDeRef(db, out var etude, "Etude"))
        {
            name = etude.Clicky();
        }
        else
        {
            name = "NOT_FOUND";
        }

        return name.Desc(prefix);
    }

    private string ParseSetObjectiveStatus(JsonElement action) => $"{action.GetDerefName(db, "m_Objective")} = {action.GetProperty("Status")}".Desc("set_objective");

    private string ParseUnrecruit(JsonElement action)
    {
        action.TryGetCompanionName(db, "m_CompanionBlueprint", out var name);
        return name.Desc("unrecruit");
    }
    private string ParseRecruit(JsonElement action)
    {
        action.TryGetCompanionName(db, "m_CompanionBlueprint", out var name);
        return name.Desc("recruit");
    }

    private string ParseFlagUnlocked(JsonElement cond)
    {
        var flagName = cond.GetDerefName(db, "m_ConditionFlag");

        if (cond.TryGetProperty("SpecifiedValues", out var specified) && specified.GetArrayLength() > 0)
        {
            // THIS IS NOT USED???
            string inc = cond.True("ExceptSpecifiedValues") ? "not" : "is";
            var vals = specified.EnumerateArray().Select(x => x.GetInt32());
            return "flag{" + flagName + inc + "[" + string.Join(',', vals) + "]}";
        }
        else
        {
            return flagName.Desc("flag");
        }
    }
    private string ParseCompanionInParty(JsonElement cond)
    {
        if (!cond.TryGetCompanionName(db, "m_companion", out var name))
        {
            return "companion{NOT_FOUND}";
        }

        return $"{name}.{CompanionFlags(cond)}".Desc("companion");
    }

    private string ParseEtudeStatus(JsonElement cond) => "etude{" + cond.GetDerefName(db, "m_Etude") + "." + EtudeFlags(cond) + "}";

    private static Func<JsonElement, string> MakeFlags(params (string Path, string Flag)[] vals)
    {
        return elem =>
        {
            List<string> flags = [];
            foreach (var (path, flag) in vals)
                if (elem.True(path)) flags.Add(flag);
            return flags.Count == 0 ? "_" : string.Join('.', flags);
        };
    }

    private static Func<JsonElement, string> CompanionFlags = MakeFlags(
        ("MatchWhenActive", "a"),
        ("MatchWhenDetached", "d"),
        ("MatchWhenRemote", "r"),
        ("MatchWhenEx", "x"),
        ("MatchWhenDead", "k"),
        ("IncludeDead", "k")
        );

    private static Func<JsonElement, string> EtudeFlags = MakeFlags(
        ("NotStarted", "n"),
        ("Started", "s"),
        ("Playing", "p"),
        ("CompletionInProgress", "i"),
        ("Completed", "c")
        );

}

public class DialogSource(Guid id)
{
    public readonly List<DialogNode> First = [];
    public Guid Id => id;

    public readonly List<DialogNode> All = [];
}

public enum DialogNodeType
{
    AnswerList,
    Answer,
    Cue,
    CueSequence,
    BookPage,
    Check,
    CueSequenceExit,
}

public class DialogNode
{
    public DialogNode(DialogNodeType type, DialogSource source, Guid id, string text, int level)
    {
        Type = type;
        Dialog = source;
        Id = id;
        Text = text;
        Level = level;

        DialogTreeCmdlet.NodeToDialog.TryAdd(id, source.Id);
        source.All.Add(this);
    }

    public readonly int Level;
    public readonly List<DialogNode> From = [];
    public readonly List<OutputPort> To = [];
    public List<DialogNode>? Sequence = null;

    public string Condition = "";

    public readonly DialogSource Dialog;
    public readonly DialogNodeType Type;
    public readonly Guid Id;
    public readonly string Text;

    public readonly Dictionary<string, object> Props = [];

    public string IdString => Id.ToString("N");

    public bool IsConditional = false;
    public bool HasShowActions = false;
    public bool HasStopActions = false;

    internal void AddChild(DialogNode childNode, string name = "")
    {
        To.Add(new(childNode, name));
        childNode.From.Add(this);
    }
}

public static class Exts2
{
    public static bool TryGetActions(this JsonElement obj, string node, out JsonElement oActions)
    {
        var hasActions = obj.TryGetProperty(node, out var actionList);

        if (hasActions && actionList.TryGetProperty("Actions", out var actions) && actions.GetArrayLength() > 0)
        {
            oActions = actions;
            return true;
        }

        oActions = default;
        return false;
    }
    public static bool TryGetConditions(this JsonElement obj, out JsonElement oConds, out bool not, out string op)
    {
        var hasCondChecker = obj.TryGetProperty("ShowConditions", out var condChecker) || obj.TryGetProperty("Conditions", out condChecker);

        if (hasCondChecker && condChecker.TryGetProperty("Conditions", out var conds) && conds.GetArrayLength() > 0)
        {
            op = condChecker.TryGetProperty("Operation", out var opNode) ? opNode.ToString() : "And";
            not = condChecker.True("Not");
            oConds = conds;
            return true;
        }

        op = "";
        not = default;
        oConds = default;
        return false;
    }

    public static string Clicky(this BlueprintHandle bp) => $"^{bp.Name}|{bp.GuidText}$";
    public static string GetDerefName(this JsonElement obj, BlueprintDB db, string node, string or = "NOT_FOUND")
    {
        if (!obj.TryDeRef(db, out var u, node))
        {
            return or;
        }
        return u.Clicky();
    }

    public static bool TryGetCompanionName(this JsonElement obj, BlueprintDB db, string node, out string name)
    {
        name = "NOT_FOUND";
        if (!obj.TryDeRef(db, out var u, node))
        {
            return false;
        }

        name = u.EnsureObj.GetProperty("LocalizedName").ParseAsString(db);
        return true;
    }
    public static string DlgScope(this JsonElement obj, string prefix) => obj.True("CurrentDialog") ? $"{prefix}_l" : $"{prefix}_g";

    public static string Desc(this string contents, string prefix) => $"{prefix}{{{contents}}}";
}

public record class OutputPort(DialogNode Target, string Name);


public static class DialogTreeCmdlet
{
    public static readonly Dictionary<Guid, Guid> NodeToDialog = [];

    [Cmdlet("dialog")]
    public static async Task GenerateDialogTrees(string[] args)
    {
        string game = args[1];
        var db = await Program.LoadDB(game);

        var baseFolder = Path.Combine(@"C:\users\worce\source\dialogs");
        var basePath = Path.Combine(baseFolder, game);

        Directory.CreateDirectory(basePath);

        foreach (var dialog in db.BlueprintsInOrder)
        {
            if (dialog.TypeName == "BlueprintDialog")
            {
                DialogSource dialogSource = new(dialog.Guid);
                DialogVisitor visitor = new(dialogSource, db);
                visitor.Visit(dialog);

                var json = DialogSer.ToJson(dialogSource);

                var filePath = Path.Combine(basePath, $"{dialog.GuidText}.json");

                await using var file = new FileStream(filePath + ".gz", FileMode.Create);
                await using var gz = new GZipStream(file, CompressionLevel.Fastest);

                gz.Write(json);
                //File.WriteAllBytes(filePath, json);
            }
        }
        File.WriteAllText(Path.Combine(basePath, "index.json"), JsonSerializer.Serialize(DialogTreeCmdlet.NodeToDialog));

        File.WriteAllLines(Path.Combine(baseFolder, "todo"),
            Enumerable.Concat(
                DialogVisitor.UnhandledConditions,
                DialogVisitor.UnhandledActions));
    }

}
