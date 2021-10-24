using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BlueprintExplorer {
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
        internal Dictionary<string, Func<string>> _Providers = null;
        public Dictionary<string, Func<string>> Providers { get {
                if (_Providers != null) return _Providers;
                _Providers = new() {
                    { "name", () => this.NameLower },
                    { "type", () => this.TypeNameLower },
                    { "space", () => this.NamespaceLower }
                };
                return _Providers;
            }
        }
        public Dictionary<string, MatchResult> Matches { get; set; } = new();

        #endregion

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
