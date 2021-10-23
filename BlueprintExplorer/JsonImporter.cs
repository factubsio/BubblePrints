using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;
using System.Text.Json;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace BlueprintExplorer
{
    public class JsonImporter
    {

        private static BlueprintHandle LoadHandle(string filePath)
        {
            var rawJson = File.ReadAllText(filePath);
            BlueprintHandle blueprint = new();

            blueprint.Raw = rawJson;
            blueprint.obj = JsonSerializer.Deserialize<dynamic>(rawJson);

            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var lastDot = fileName.LastIndexOf('.');

            blueprint.Type = Path.GetFileName(Path.GetDirectoryName(filePath));
            blueprint.Name = fileName.Substring(0, lastDot);
            blueprint.GuidText = fileName.Substring(lastDot + 1);
            blueprint.guid = Guid.Parse(blueprint.GuidText).ToByteArray();

            return blueprint;
        }
        private static void MakeTable(SQLiteConnection con, string name, string cols, string extra_indices)
        {
            SQLiteCommand createTableCmd = con.CreateCommand();
            createTableCmd.CommandText = $"DROP TABLE IF EXISTS {name}";
            Console.WriteLine($"Executing: >{createTableCmd.CommandText}<");
            createTableCmd.ExecuteNonQuery();
            createTableCmd.CommandText = $"CREATE TABLE {name}({cols})";
            Console.WriteLine($"Executing: >{createTableCmd.CommandText}<");
            createTableCmd.ExecuteNonQuery();

            if (extra_indices != null)
            {
                foreach (var index in extra_indices.Split(','))
                {
                    var indexCleaned = index.Trim();
                    createTableCmd.CommandText = $"CREATE INDEX {indexCleaned} ON {name}({indexCleaned})";
                    Console.WriteLine($"Executing: >{createTableCmd.CommandText}<");
                    createTableCmd.ExecuteNonQuery();
                }
            }
        }

        public static void Import()
        {

#if false
            var dataDumpRoot = Environment.GetEnvironmentVariable("WrathDataDumpPath");
            List<string> blueprintPaths = Directory.EnumerateFiles(dataDumpRoot, "*.json", SearchOption.AllDirectories).ToList();

            Stopwatch watch = new();
            watch.Start();
            var blueprints = blueprintPaths.AsParallel().Select(LoadHandle).ToList();
            watch.Stop();

            var con = BlueprintDB.Instance.Connection;
            MakeTable(con, "blueprints_raw", "guid BLOB PRIMARY KEY, name TEXT, guidText TEXT, type TEXT, raw TEXT", "name, type");
            MakeTable(con, "blueprints_fwdrefs", "from_guid BLOB, to_guid BLOB, via_property TEXT", "from_guid, to_guid");

            SQLiteCommand insertRaw = con.CreateCommand();
            insertRaw.CommandText = @"INSERT INTO blueprints_raw(guid, name, guidText, type, raw) VALUES(@guid, @name, @guidText, @type, @raw)";
            SQLiteCommand insertReference = con.CreateCommand();
            insertReference.CommandText = @"INSERT INTO blueprints_fwdrefs(from_guid, to_guid, via_property) VALUES(@from_guid, @to_guid, @via_property)";

            using (var transaction = con.BeginTransaction())
            {
                foreach (var blueprint in blueprints)
                {
                    insertRaw.Parameters.AddWithValue("guid", blueprint.guid);
                    insertRaw.Parameters.AddWithValue("name", blueprint.Name);
                    insertRaw.Parameters.AddWithValue("guidText", blueprint.GuidText);
                    insertRaw.Parameters.AddWithValue("type", blueprint.Type);
                    insertRaw.Parameters.AddWithValue("raw", blueprint.Raw);

                    insertRaw.ExecuteNonQuery();

                    insertReference.Parameters.AddWithValue("from_guid", blueprint.guid);
                    foreach (var reference in blueprint.DirectReferences)
                    {
                        insertReference.Parameters.AddWithValue("to_guid", reference.to.ToByteArray());
                        insertReference.Parameters.AddWithValue("via_property", reference.path);
                        insertReference.ExecuteNonQuery();
                    }

                    // Console.WriteLine($"name:{blueprint.name} id:{blueprint.guidText}");
                }
                transaction.Commit();
            }
#endif
        }
    }

    public class BlueprintReference
    {
        public string path;
        public Guid to;

    }

    public class BlueprintHandle
    {
        public byte[] guid;
        public string GuidText { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string TypeName;
        public string Namespace;
        public string Raw { get; set; }
        public dynamic obj;
        public bool Parsed;

        public string LowerName;
        public string LowerType;

        public static bool TryGetReference(string value, out Guid guid)
        {
            guid = Guid.Empty;

            return false;
        }

        public class VisitedElement
        {
            public string key;
            public string value;
            public int levelDelta;
            public bool isObj;
            public string link;
            public bool Empty;
        }

        private static string ParseReference(string val)
        {
            if (val.StartsWith("Blueprint:"))
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

        public static IEnumerable<VisitedElement> Visit(JsonElement node, string name)
        {
            if (node.ValueKind == JsonValueKind.String)
            {
                string val = node.GetString();
                yield return new VisitedElement { key = name, value = val, link = ParseReference(val) };
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
                yield return new VisitedElement { key = name, levelDelta = 1 };
                int index = 0;
                foreach (var elem in node.EnumerateArray())
                {
                    foreach (var n in Visit(elem, index.ToString()))
                        yield return n;
                    index++;
                }
                yield return new VisitedElement { levelDelta = -1 };
            }
            else
            {
                yield return new VisitedElement { key = name, levelDelta = 1 };
                foreach (var elem in node.EnumerateObject())
                {
                    foreach (var n in Visit(elem.Value, elem.Name))
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

        [JsonIgnore]
        public List<BlueprintReference> DirectReferences
        {
            get
            {
                List<BlueprintReference> refs = new List<BlueprintReference>();

                GatherBlueprints("", refs, obj);

                return refs;
            }

        }
    }
}
