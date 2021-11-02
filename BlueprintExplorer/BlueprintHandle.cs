using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace BlueprintExplorer
{
    static class JsonExtensions
    {
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

        public static bool IsContainer(this JsonElement elem)
        {
            return elem.ValueKind == JsonValueKind.Object || elem.ValueKind == JsonValueKind.Array;
        }
        public static bool IsEmptyContainer(this JsonElement elem)
        {
            return (elem.ValueKind == JsonValueKind.Array && elem.GetArrayLength() == 0) || (elem.ValueKind == JsonValueKind.Object && elem.EnumerateObject().Count() == 0);
        }

        public static void Visit(this JsonElement elem, Action<int, JsonElement> arrayIt, Action<string, JsonElement> objIt, Action<string> valIt)
        {
            if (elem.ValueKind == JsonValueKind.Array)
            {
                int index = 0;
                foreach (JsonElement entry in elem.EnumerateArray())
                    arrayIt(index++, entry);
            }
            else if (elem.ValueKind == JsonValueKind.Object && elem.EnumerateObject().Count() > 0)
            {
                foreach (var entry in elem.EnumerateObject())
                    objIt(entry.Name, entry.Value);
            }
            else
            {
                valIt(elem.GetRawText());
            }
        }
    }

    public class BlueprintPropertyConverter : ExpandableObjectConverter
    {
        class NameValuePropertyDescriptor : SimplePropertyDescriptor
        {
            private readonly string _Category;
            private readonly object _Value;
            private readonly string _Description;
            public bool _Empty;

            public NameValuePropertyDescriptor(string category, string name, object value, Type parentType, string description = "") : base(parentType, name, value.GetType())
            {
                _Description = description;
                _Category = category;
                _Value = value;
            }

            public override object GetValue(object component)
            {
                return _Value;
            }

            public override void SetValue(object component, object value)
            {
                //no
            }

            public override string Description => _Description;

            public override bool IsReadOnly => !(PropertyType == typeof(LocalisedStringProxy));

            public override string Category => _Category;

            public override object GetEditor(Type editorBaseType)
            {
                if (this.PropertyType == typeof(LocalisedStringProxy))
                    return new LocalisedStringEditor();
                else
                    return base.GetEditor(editorBaseType);
            }
        }

        class PropertyCollectionBuilder
        {
            private PropertyDescriptorCollection Collection = new(Array.Empty<PropertyDescriptor>());
            private List<string> order = new();
            private List<string> empties = new();

            private static Type GetParentType(string category, Type type)
            {
                return type ?? (category == "base" ? typeof(BlueprintHandle) : typeof(NestedProxy));
            }

            private NameValuePropertyDescriptor AddInternal(string category, string name, object value, string description, Type parentType)
            {
                parentType = GetParentType(category, parentType);
                order.Add(name);
                var descriptor = new NameValuePropertyDescriptor(category, name, value, parentType, description);
                Collection.Add(descriptor);
                return descriptor;
            }
            private static Regex ParseLink = new(@"Blueprint:(.*?):");

            public void Add(string category, string name, string value, string description = "", Type parentType = null)
            {
                if (value.Length > 2 && value.FirstOrDefault() == '"' && value.LastOrDefault() == '"')
                {
                    value = value.Substring(1, value.Length - 2);
                }

                NameValuePropertyDescriptor prop;
                var maybeLink = ParseLink.Match(value);

                if (value.StartsWith("LocalizedString:") && value != "LocalizedString::")
                    prop = AddInternal(category, name, new LocalisedStringProxy(value), description, parentType);
                else if (maybeLink.Success)
                    prop = AddInternal(category, name, new BlueprintLink(value, maybeLink.Groups[1].Value), description, parentType);
                else
                    prop = AddInternal(category, name, value, description, parentType);


                if (value == "<empty>" || value == "Blueprint::NULL" || value == "LocalizedString::")
                {
                    prop._Empty = true;
                    order.RemoveAt(order.Count - 1);
                    empties.Add(name);
                }
            }
            public void Add(string category, string name, JsonElement value, string description = "", Type parentType = null)
            {
                AddInternal(category, name, new NestedProxy(value), description, parentType);
            }

            public PropertyDescriptorCollection Build()
            {
                return Collection; //.Sort(order.Concat(empties).ToArray());
            }

        }

        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
        {
            var bp = value as BlueprintHandle;

            PropertyCollectionBuilder propBuilder = new();

            propBuilder.Add("base", "Name", bp.Name, "Name of the blueprint");
            propBuilder.Add("base", "Type", bp.TypeName, "Type of the blueprint (not including its Namespace)");
            propBuilder.Add("base", "Namespace", bp.Namespace, "Namespace that the Type exists in");
            propBuilder.Add("base", "Guid", bp.GuidText, "Unique ID for this blueprint, used to cross-reference bluescripts and to load them");
            propBuilder.Add("base", "Properties", bp.obj, "All the good stuff!");

            return propBuilder.Build();
        }

        [EditorAttribute(typeof(LocalisedStringEditor), typeof(System.Drawing.Design.UITypeEditor))]
        internal class LocalisedStringProxy
        {
            static int prefixLen = $"LocalizedString:{Guid.Empty.ToString()}:".Length;
            internal readonly string Value;

            public LocalisedStringProxy(string value)
            {
                Value = value.Substring(prefixLen);
            }

            public override string ToString() => Value;
        }

        internal class BlueprintLink
        {
            internal readonly string Value;
            internal readonly string Link;

            public BlueprintLink(string value, string link)
            {
                Value = value;
                Link = link;
            }

            public override string ToString() => Value;
        }

        [TypeConverter(typeof(NestedConverter))]
        internal class NestedProxy
        {
            private static Dictionary<string, HashSet<string>> PropertiesByType = new();
            private static HashSet<string> empty = new();

            private static HashSet<string> FindTypedProperties(string typeName)
            {
                if (BubblePrints.Wrath == null)
                    return empty;
                if (!PropertiesByType.TryGetValue(typeName, out var list))
                {
                    var type = BubblePrints.Wrath.GetType(typeName);
                    if (type == null)
                    {
                        Console.WriteLine($"could not load type: {typeName}");
                        list = new();
                    }
                    else
                        list = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Select(f => f.Name).ToHashSet();
                    PropertiesByType[typeName] = list;
                }

                return list;
            }

            internal readonly JsonElement node;
            private string _ShortValue = "";
            public HashSet<string> TypedProperties;

            private static Regex ParseType = new(@"(.*)\.(.*), (.*)");

            public override string ToString() => _ShortValue;

            internal NestedProxy(JsonElement rootNode)
            {
                node = rootNode;

                if (node.ValueKind == JsonValueKind.Object)
                {
                    if (node.TryGetProperty("$type", out var typeVal))
                    {
                        var typeMatch = ParseType.Match(typeVal.GetString());
                        if (typeMatch.Success)
                        {
                            if (typeMatch.Groups[1].Value == "ActionList")
                                node = node.GetProperty("Actions");
                            else
                                TypedProperties = FindTypedProperties($"{typeMatch.Groups[1]}.{typeMatch.Groups[2]}");

                            //if (IsActionList)
                            //    _ShortValue = $"[{node.GetProperty("Actions").GetArrayLength()}] {typeMatch.Groups[1].Value}.Actions  [{typeMatch.Groups[2].Value}]";
                            if (node.ValueKind == JsonValueKind.Array)
                                _ShortValue = $"[{node.GetArrayLength()}] {typeMatch.Groups[2].Value}  [{typeMatch.Groups[3].Value}]";
                            else
                                _ShortValue = $"{typeMatch.Groups[2].Value}  [{typeMatch.Groups[3].Value}]";
                        }
                        else
                        {
                            _ShortValue = typeVal.GetString();
                        }
                    }
                }
            }

        }

        class LocalisedStringEditor : UITypeEditor
        {
            public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
            {
                return UITypeEditorEditStyle.DropDown;
            }

            public override bool IsDropDownResizable => true;

            public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
            {
                var proxy = value as LocalisedStringProxy;
                IWindowsFormsEditorService edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
                if (edSvc != null)
                {
                    // Display an angle selection control and retrieve the value.
                    var label = new RichTextBox();
                    var output = new StringBuilder();
                    label.Text = proxy.Value.Replace("\\n", Environment.NewLine);
                    label.Size = new System.Drawing.Size(400, 300);
                    edSvc.DropDownControl(label);
                }

                return value;
            }


        }

        class NestedConverter : ExpandableObjectConverter
        {
            public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
            {
                var props = new PropertyCollectionBuilder();
                var proxy = value as NestedProxy;
                var node = proxy.node;

                HashSet<string> remaining = null;
                if (proxy.TypedProperties != null)
                    remaining = new(proxy.TypedProperties);

                node.Visit((index, value) =>
                {
                    if (value.IsSimple())
                        props.Add("", $"[{index}]", value.GetRawText());
                    else if (value.IsEmptyContainer())
                        props.Add("", $"[{index}]", "<empty>");
                    else
                        props.Add("", $"[{index}]", value);

                }, (key, value) =>
                {
                    remaining?.Remove(key);
                    if (value.TryGetSimple(out var raw))
                    {
                        if (key != "$id")
                            props.Add("", key, raw);
                    }
                    else if (value.IsEmptyContainer())
                        props.Add("", key, "<empty>");
                    else
                        props.Add("", key, value);

                }, null);

                if (remaining != null)
                {
                    foreach (var rem in remaining)
                    {
                        props.Add("", rem, "<not-present>");
                    }
                }



                return props.Build();
            }

        }

    }

    [TypeConverter(typeof(BlueprintPropertyConverter))]
    public class BlueprintHandle : ISearchable {
        public byte[] guid;
        public string GuidText { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string TypeName;
        public string Namespace;
        public string Raw { get; set; }
        public dynamic obj;
        public bool Parsed;

        public string NameLower;
        public string TypeNameLower;
        public string NamespaceLower;

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

        internal static readonly MatchQuery.MatchProvider MatchProvider = new MatchQuery.MatchProvider(
                    obj => (obj as BlueprintHandle).NameLower,
                    obj => (obj as BlueprintHandle).TypeNameLower,
                    obj => (obj as BlueprintHandle).NamespaceLower);

        private MatchResult[] CreateResultArray()
        {
            return new MatchResult[] {
                    new MatchResult("name", this),
                    new MatchResult("type", this),
                    new MatchResult("space", this),
                };
        }
        public MatchResult[] GetMatches(int index)
        {
            if (_Matches[index] == null)
                _Matches[index] = CreateResultArray();
            return _Matches[index];
        }
        

        #endregion
        public void PrimeMatches(int count)
        {
            _Matches = new MatchResult[count][];
            for (int i = 0; i < count; i++)
                _Matches[i] = CreateResultArray();
        }

        public static bool TryGetReference(string value, out Guid guid) {
            guid = Guid.Empty;

            return false;
        }

        public class VisitedElement {
            public string key;
            public string value;
            public int levelDelta;
            public bool isObj;
            public string link;
            public bool Empty;
        }

        private static string ParseReference(string val) {
            if (val.StartsWith("Blueprint:")) {
                var components = val.Split(':');
                if (components.Length != 3 || components[1].Length == 0 || components[1] == "NULL") {
                    return null;
                }
                return components[1];
            }
            else {
                return null;
            }

        }

        private List<VisitedElement> _Elements;
        public List<VisitedElement> Elements
        {
            get
            {
                if (_Elements == null)
                    _Elements = Visit(this.obj, Name).ToList();
                return _Elements;
            }
        }

        public static IEnumerable<VisitedElement> Visit(JsonElement node, string name) {
            if (node.ValueKind == JsonValueKind.String) {
                string val = node.GetString();
                yield return new VisitedElement { key = name, value = val, link = ParseReference(val) };
            }
            else if (node.ValueKind == JsonValueKind.Number || node.ValueKind == JsonValueKind.True || node.ValueKind == JsonValueKind.False) {
                yield return new VisitedElement { key = name, value = node.GetRawText() };
            }
            else if (node.ValueKind == JsonValueKind.Null) {
                yield return new VisitedElement { key = name, value = "null" };
            }
            else if (node.ValueKind == JsonValueKind.Array) {
                yield return new VisitedElement { key = name, levelDelta = 1 };
                int index = 0;
                foreach (var elem in node.EnumerateArray()) {
                    foreach (var n in Visit(elem, index.ToString()))
                        yield return n;
                    index++;
                }
                yield return new VisitedElement { levelDelta = -1 };
            }
            else {
                yield return new VisitedElement { key = name, levelDelta = 1 };
                foreach (var elem in node.EnumerateObject()) {
                    foreach (var n in Visit(elem.Value, elem.Name))
                        yield return n;
                }
                yield return new VisitedElement { levelDelta = -1 };
            }

        }

        private static void GatherBlueprints(string path, List<BlueprintReference> refs, JsonElement node) {
            if (node.ValueKind == JsonValueKind.String) {
                var val = node.GetString();
                if (val.StartsWith("Blueprint:")) {
                    var components = val.Split(':');
                    if (components.Length != 3 || components[1].Length == 0 || components[1] == "NULL") {
                        return;
                    }
                    var guid = Guid.Parse(components[1]);
                    refs.Add(new BlueprintReference {
                        path = path,
                        to = guid
                    });
                }
            }
            else if (node.ValueKind == JsonValueKind.Array) {
                int index = 0;
                foreach (var element in node.EnumerateArray()) {
                    GatherBlueprints(path + "/" + index, refs, element);
                    index++;
                }
            }
            else if (node.ValueKind == JsonValueKind.Object) {
                foreach (var prop in node.EnumerateObject()) {
                    GatherBlueprints(path + "/" + prop.Name, refs, prop.Value);
                }
            }
        }

        [JsonIgnore]
        public List<BlueprintReference> DirectReferences {
            get {
                List<BlueprintReference> refs = new List<BlueprintReference>();

                GatherBlueprints("", refs, obj);

                return refs;
            }

        }
    }
}
