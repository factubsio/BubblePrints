using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BlueprintExplorer
{
    public static class TemplateRunner
    {

        public static string Lookup(BlueprintHandle bp, string key)
        {
            return key switch
            {
                "guid" => bp.GuidText,
                "type" => bp.TypeName,
                "name" => bp.Name,
                _ => null,
            };
        }
        public static string Format(string value, string []format)
        {
            foreach (var item in format)
            {
                value =  item switch
                {
                    "" => value,
                    "firstLower" => char.ToLower(value[0]) + value[1..],
                    _ => value,
                };
            }
            return value;
        }

        public struct TemplateFragment
        {
            public bool IsVariable;

            public bool IsError;

            public string Raw;
            public string Object;
            public string Property;

            public string[] Format;
        }

        public static IEnumerable<TemplateFragment> Iterate(string template)
        {
            var m = regex.Matches(template);

            int copyFrom = 0;
            for (int i = 0; i < m.Count; i++)
            {
                int copyTo = m[i].Index;
                yield return new()
                {
                    IsVariable = false,
                    Raw = template[copyFrom..copyTo],
                };

                copyFrom = m[i].Index + m[i].Length;

                string[] key = m[i].Value[2..^1].Split(':', StringSplitOptions.RemoveEmptyEntries);
                var type = key[0].Split('.');
                if (type.Length != 2)
                {
                    yield return new() { IsError = true, Raw = m[i].Value, };
                    continue;
                }

                yield return new()
                {
                    IsVariable = true,

                    Raw = m[i].Value,
                    Object = type[0],
                    Property = type[1],

                    Format = key.Length > 1 ? key[1..] : null,
                };
            }

            if (copyFrom != template.Length)
            {
                yield return new()
                {
                    IsVariable = false,
                    Raw = template[copyFrom..],
                };
            }
        }

    private static Regex regex = new("(@{.*?})");
        public static string Execute(string template, BlueprintHandle bp)
        {
            StringBuilder sb = new();

            //var template = "{@{bp.guid}hello { } @{bp.type} \"@{bp.name}";
            var m = regex.Matches(template);

            int copyFrom = 0;
            for (int i = 0; i < m.Count; i++)
            {
                int copyTo = m[i].Index;
                sb.Append(template[copyFrom..copyTo]);

                copyFrom = m[i].Index + m[i].Length;

                string[] key = m[i].Value[2..^1].Split(':', StringSplitOptions.RemoveEmptyEntries);
                var type = key[0].Split('.');
                if (type.Length != 2)
                {
                    sb.Append(m[i].Value).Append("<<error:invalid key>>");
                    continue;
                }

                string value = "";
                if (type[0] == "bp")
                {
                    value = Lookup(bp, type[1]);
                }
                else
                {
                    sb.Append(m[i].Value).Append("<<error:unknown type>>");
                }

                if  (key.Length > 1)
                    value = Format(value, key[1..]);

                sb.Append(value);


            }
            if (copyFrom != template.Length)
                sb.Append(template[copyFrom..]);

            return sb.ToString();
        }
    }
}
