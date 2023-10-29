﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace BlueprintExplorer
{

    public interface IDisplayableElement
    {
        public string key { get; }
        public string value { get; }
        public int levelDelta { get; }
        public bool isObj { get; }
        public BlueprintLink? link { get; }
        //public string linkTarget;
        public bool Empty { get; }
        public JsonElement Node { get; }
        public (string Guid, string Name, string FullName) MaybeType { get; }
        public bool HasType { get; }
        public bool Last { get; }

    }


    public static class JsonExtensions
    {
        public static bool ContainsIgnoreCase(this string haystack, string needle) => haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);
        public static string ParseAsString(this JsonElement node, string nodeKey = null)
        {
            if (node.ValueKind != JsonValueKind.Object)
                return null;

            if (node.TryGetProperty("m_Key", out var strKey) && node.TryGetProperty("Shared", out var sharedString) && node.TryGetProperty("m_OwnerString", out _))
            {
                var key = strKey.GetString();
                if (key.Length == 0 && sharedString.ValueKind == JsonValueKind.Object && sharedString.TryGetProperty("stringkey", out var sharedKey))
                    key = sharedKey.GetString();

                if (key.Length > 0)
                {
                    if (BlueprintDB.Instance.Strings.TryGetValue(key, out var str))
                        return str;
                    else
                        return "<string-not-present>";
                }
                else
                {
                    return "<null-string>";
                }
            }
            if (nodeKey != "Shared" && node.TryGetProperty("assetguid", out var assetguidKey) && node.TryGetProperty("stringkey", out var dirsharedKey))
            {
                var key = dirsharedKey.GetString();
                if (key.Length > 0)
                {
                    if (BlueprintDB.Instance.Strings.TryGetValue(key, out var str))
                        return str;
                    else
                        return "<string-not-present>";
                }
                else
                {
                    return "<null-string>";
                }
            }
            return null;
        }

        private static Random rng = new();
        private static string[] christmas = { "🎄", "❄️", "🦌", "⛄", "🎅" };
        public static string Seasonal(this string str)
        {
            if (!SeasonalOverlay.InSeason)
                return str;

            if (SeasonalOverlay.NearChristmas)
            {
                var season = christmas;
                var index = Math.Abs(str.GetHashCode()) % season.Length;
                return $"{season[index]} {str} {season[index]}";
            }

            return str;
        }
        public static Guid Guid(this string str) => System.Guid.Parse(str);
        public static bool IsSimple(this JsonElement elem)
        {
            return !elem.IsContainer();
        }

        public static bool TryGetSimple(this JsonElement elem, out string str)
        {
            if (elem.IsContainer())
            {
                str = null;
                return false;
            }
            else
            {
                str = elem.GetRawText();
                return true;
            }
        }

        public static string ParseTypeString(this string str)
        {
            if (str == null)
                return str;
            return str.Substring(0, str.IndexOf(','));
        }
        public static string TypeString(this JsonElement elem)
        {
            return elem.Str("$type").ParseTypeString();
        }

        public static (string Guid, string Name, string FullName) NewTypeStr(this string raw, bool strict = true)
        {
            var comma = raw.LastIndexOf(',');
            var shortName = raw[(comma + 2)..];
            string guid = "";
            if (shortName == "mscorlib" || shortName.StartsWith("UnityEngine") || shortName == "Assembly-CSharp-firstpass" || raw.StartsWith("System") || raw.StartsWith("Unity"))
            {
                return ("", raw, raw);
            }
            else if (shortName != "Assembly-CSharp") {
                guid = raw[0..comma];

                if (BlueprintDB.Instance.GuidToFullTypeName.TryGetValue(guid, out var fullTypeName))
                    return (guid, shortName, fullTypeName);

                if (strict)
                    throw new Exception($"Cannot find type with that name: {shortName}");

                return (null, null, null);
            }
            else
            {
                return ("", raw[0..comma], raw[0..comma]);
            }


        }

        public static (string Guid, string Name, string FullName) NewTypeStr(this JsonElement elem, bool strict = true)
        {
            if (elem.ValueKind == JsonValueKind.String)
                return elem.GetString().NewTypeStr();
            else if (elem.ValueKind == JsonValueKind.Object)
                return elem.Str("$type").NewTypeStr(strict);
            else
                throw new Exception("invalid type query??");
        }

        public static bool Nullish(this JsonElement elem) => elem.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined;

        private static JsonElement Null = JsonDocument.Parse("null").RootElement;

        public static JsonElement Find(this JsonElement elem, params string[] path)
        {
            var curr = elem;
            foreach (var component in path)
            {
                if (curr.ValueKind == JsonValueKind.Null || curr.ValueKind == JsonValueKind.Undefined)
                    break;

                if (!curr.TryGetProperty(component, out curr)) {
                    return Null;
                }
            }
            return curr;
        }

        public static bool True(this JsonElement elem, string child)
        {
            return elem.TryGetProperty(child, out var ch) && ch.ValueKind == JsonValueKind.True;
        }
        public static string Str(this JsonElement elem, string child)
        {
            if (elem.ValueKind != JsonValueKind.Object)
                return null;
            if (!elem.TryGetProperty(child, out var childNode))
                return null;
            return childNode.GetString();
        }
        public static BlueprintHandle DeRef(this JsonElement elem)
        {
            if (!elem.TryDeRef(out var bp))
                throw new Exception("could not derefence blueprint from json element: " + elem);
            return bp;
        }

        public static bool TryDeRef(this JsonElement elem, out BlueprintHandle bp)
        {
            bp = null;
            if (elem.ValueKind == JsonValueKind.Null || elem.ValueKind == JsonValueKind.Undefined)
                return false;

            var link = BlueprintHandle.ParseReference(elem.GetString());

            if (link != null && BlueprintDB.Instance.Blueprints.TryGetValue(new(System.Guid.Parse(link)), out bp))
            {
                return true;

            }

            return false;

        }
        public static bool TryDeRef(this JsonElement elem, out BlueprintHandle bp, params string[] path)
        {
            var child = elem.Find(path);
            return child.TryDeRef(out bp);
        }

        public static float Float(this JsonElement elem, string child)
        {
            if (elem.TryGetProperty(child, out var prop))
                return (float)prop.GetDouble();
            return 0;
        }
        public static int Int(this JsonElement elem, string child)
        {
            if (elem.TryGetProperty(child, out var prop))
                return prop.GetInt32();
            return 0;
        }

        public static bool IsContainer(this JsonElement elem)
        {
            return elem.ValueKind == JsonValueKind.Object || elem.ValueKind == JsonValueKind.Array;
        }
        public static bool IsEmptyContainer(this JsonElement elem)
        {
            return (elem.ValueKind == JsonValueKind.Array && elem.GetArrayLength() == 0) || (elem.ValueKind == JsonValueKind.Object && !elem.EnumerateObject().Any());
        }

        public static void Visit(this JsonElement elem, Action<int, JsonElement> arrayIt, Action<string, JsonElement> objIt, Action<string> valIt, bool autoRecurse = false)
        {
            if (elem.ValueKind == JsonValueKind.Array)
            {
                int index = 0;
                foreach (JsonElement entry in elem.EnumerateArray())
                {
                    arrayIt(index++, entry);
                    if (autoRecurse)
                        entry.Visit(arrayIt, objIt, valIt, true);
                }
            }
            else if (elem.ValueKind == JsonValueKind.Object && elem.EnumerateObject().Any())
            {
                foreach (var entry in elem.EnumerateObject())
                {
                    objIt(entry.Name, entry.Value);
                    if (autoRecurse)
                        entry.Value.Visit(arrayIt, objIt, valIt, true);
                }
            }
            else
            {
                valIt?.Invoke(elem.GetRawText());
            }
        }
    }

    public interface IDisplayableElementCollection
    {
        public void EnsureParsed();
        public IEnumerable<IDisplayableElement> DisplayableElements { get; }
        string GuidText { get; }
        string ShortId { get; }
        string LongName { get; }
        string Name { get; }
        string Type { get; }
        string TypeName { get; }
    }

    public class BlueprintHandle : ISearchable, IDisplayableElementCollection
    {
        public static bool ShortType = false;
        public string LongName { get; set; }

        public string ShortId { get; set; }
        public string GuidText { get; set; }
        public long SecondaryId { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string TypeForResults
        {
            get
            {
                return ShortType ? Type[(Type.LastIndexOf('.') + 1)..] : Type;
            }
        }
        public string TypeName { get; set; }
        public string Namespace;
        public string Raw { get; set; }
        public JsonElement obj;
        public bool Parsed;

        public List<Guid> BackReferences = new();

        public string NameLower;
        public string TypeNameLower;
        public string NamespaceLower;
        public string ShortIdLower;

        #region ISearchable
        //internal Dictionary<string, Func<string>> _Providers = null;
        //public Dictionary<string, Func<string>> Providers { get {
        //        if (_Providers != null) return _Providers;
        //        _Providers = new() {
        //            { "name", () => this.NameLower },
        //            { "type", () => this.TypeNameLower },
        //            { "space", () => this.NamespaceLower }
        //        };
        //        return _Providers;
        //   }
        //}
        internal MatchResult[][] _Matches;
        public ushort[] ComponentIndex;
        public IEnumerable<string> ComponentsList => ComponentIndex.Select(i => BlueprintDB.Instance.FlatIndexToTypeName[i]);
        internal static readonly MatchQuery.MatchProvider MatchProvider = new(
                    obj => (obj as BlueprintHandle).NameLower,
                    obj => (obj as BlueprintHandle).TypeNameLower,
                    obj => (obj as BlueprintHandle).NamespaceLower,
                    obj => (obj as BlueprintHandle).GuidText,
                    obj => (obj as BlueprintHandle).ShortIdLower);

        private MatchResult[] CreateResultArray()
        {
            return new MatchResult[] {
                    new MatchResult("name", this),
                    new MatchResult("type", this),
                    new MatchResult("space", this),
                    new MatchResult("guid", this),
                    new MatchResult("did", this),
                };
        }
        public MatchResult[] GetMatches(int index)
        {
            if (_Matches[index] == null)
                _Matches[index] = CreateResultArray();
            return _Matches[index];
        }

        public JsonElement EnsureObj
        {
            get
            {
                EnsureParsed();
                return obj;
            }
        }


        public void EnsureParsed()
        {
            if (!Parsed)
            {
                obj = JsonSerializer.Deserialize<JsonElement>(Raw, new JsonSerializerOptions()
                {
                    MaxDepth = 128,
                    AllowTrailingCommas = true,
                });
                Parsed = true;
            }
        }


        #endregion
        public void PrimeMatches(int count)
        {
            _Matches = new MatchResult[count][];
            for (int i = 0; i < count; i++)
                _Matches[i] = CreateResultArray();
        }

        public class ElementVisitor
        {

            public static IEnumerable<(VisitedElement, string)> Visit(BlueprintHandle bp)
            {
                Stack<string> stack = new();
                foreach (var elem in BlueprintHandle.Visit(new(), bp.EnsureObj, bp.Name))
                {
                    if (elem.levelDelta > 0)
                    {
                        stack.Push(elem.key);
                        yield return (elem, string.Join("/", stack.Reverse()));
                    }
                    else if (elem.levelDelta < 0)
                        stack.Pop();
                    else
                        yield return (elem, string.Join("/", stack.Reverse()));

                }
            }

        }

        public class VisitedElement : IDisplayableElement
        {
            public string key { get; set; }
            public string value { get; set; }
            public int levelDelta { get; set; }
            public bool isObj { get; set; }
            public BlueprintLink? link { get; set; }
            //public string linkTarget;
            public bool Empty { get; set; }
            public JsonElement Node { get; set; }
            public (string Guid, string Name, string FullName) MaybeType { get; set; }
            public bool HasType => MaybeType.Name != null;
            public bool Last { get; set; }
        }

        public static string ParseReference(string val)
        {
            if (val.StartsWith("!bp_"))
            {
                if (val.Length > 4)
                    return val[4..];
                else
                    return null;
            }
            else if (val.StartsWith("Blueprint:"))
            {
                var components = val.Split(':');
                if (components.Length != 3 || components[1].Length == 0 || components[1] == "NULL")
                {
                    return null;
                }
                return components[1];
            }
            else
            {
                return null;
            }

        }

        public IEnumerable<VisitedElement> Elements
        {
            get
            {
                EnsureParsed();
                return Visit(new(), obj, Name);
            }
        }

        IEnumerable<IDisplayableElement> IDisplayableElementCollection.DisplayableElements => Elements;

        public static void VisitObjects(JsonElement node, HashSet<string> types)
        {
            if (node.ValueKind == JsonValueKind.Array)
            {
                foreach (var elem in node.EnumerateArray())
                    VisitObjects(elem, types);
            }
            else if (node.ValueKind == JsonValueKind.Object)
            {
                if (node.TryGetProperty("$type", out var raw))
                    types.Add(raw.NewTypeStr().Guid);
                foreach (var elem in node.EnumerateObject())
                    VisitObjects(elem.Value, types);
            }

        }

        public static IEnumerable<VisitedElement> Visit(JsonElement parent, JsonElement node, string name)
        {
            if (node.ValueKind == JsonValueKind.String)
            {
                string val = node.GetString();
                var link = ParseReference(val);
                long secondary = 0;
                if (BubblePrints.Game_Data == "Inkbound_Data" && name == "guid") {
                    link = val;
                    //parent.Find("m_FilePath")
                }

                BlueprintLink? bpLink = null;
                if (link != null)
                {
                    bpLink = new(Guid.Parse(link), secondary);
                }

                yield return new VisitedElement { key = name, value = val, link = bpLink };
            }
            else if (node.ValueKind == JsonValueKind.Number || node.ValueKind == JsonValueKind.True || node.ValueKind == JsonValueKind.False)
            {
                yield return new VisitedElement { key = name, value = node.GetRawText() };
            }
            else if (node.ValueKind == JsonValueKind.Null)
            {
                yield return new VisitedElement { key = name, value = "null" };
            }
            else if (node.ValueKind == JsonValueKind.Array)
            {
                yield return new VisitedElement { key = name, levelDelta = 1, Node = node };
                int index = 0;
                foreach (var elem in node.EnumerateArray())
                {
                    foreach (var n in Visit(node, elem, index.ToString()))
                        yield return n;
                    index++;
                }
                yield return new VisitedElement { levelDelta = -1 };
            }
            else
            {
                (string, string, string) maybeType = (null, null, null);
                if (node.TryGetProperty("$type", out var rawType))
                    maybeType = rawType.NewTypeStr();
                yield return new VisitedElement { key = name, levelDelta = 1, isObj = true, Node = node, MaybeType = maybeType };
                foreach (var elem in node.EnumerateObject())
                {
                    foreach (var n in Visit(node, elem.Value, elem.Name))
                        yield return n;
                }
                yield return new VisitedElement { levelDelta = -1 };
            }

        }

        private static void GatherBlueprints(string path, List<BlueprintReference> refs, JsonElement node)
        {
            if (node.ValueKind == JsonValueKind.String)
            {
                var val = node.GetString();
                if (val.StartsWith("Blueprint:"))
                {
                    var components = val.Split(':');
                    if (components.Length != 3 || components[1].Length == 0 || components[1] == "NULL")
                    {
                        return;
                    }
                    var guid = Guid.Parse(components[1]);
                    refs.Add(new BlueprintReference
                    {
                        path = path,
                        to = guid
                    });
                }
            }
            else if (node.ValueKind == JsonValueKind.Array)
            {
                int index = 0;
                foreach (var element in node.EnumerateArray())
                {
                    GatherBlueprints(path + "/" + index, refs, element);
                    index++;
                }
            }
            else if (node.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in node.EnumerateObject())
                {
                    GatherBlueprints(path + "/" + prop.Name, refs, prop.Value);
                }
            }
        }


        public IEnumerable<Guid> GetDirectReferences()
        {
            Stack<string> path = new();
            foreach (var element in Elements)
            {
                //if (element.levelDelta > 0)
                //{
                //    path.Push(element.key);
                //}
                //else if (element.levelDelta < 0)
                //{
                //    path.Pop();
                //}
                //else
                //{
                if (element.link != null)
                {
                    yield return element.link.Value.Guid;
                    //yield return new BlueprintReference
                    //{
                    //    path = string.Join("/", path.Reverse()),
                    //    to = Guid.Parse(element.link)
                    //};
                }
                //}
            }
        }

        internal void ParseType()
        {

            var components = Type.Split('.');
            if (components.Length <= 1)
                TypeName = Type;
            else
            {
                TypeName = components.Last();
                Namespace = string.Join('.', components.Take(components.Length - 1));
            }
        }
    }

    public class BlueprintReference
    {
        public string path;
        public Guid to;
    }
}
