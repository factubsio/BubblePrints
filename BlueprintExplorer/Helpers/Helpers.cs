using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BlueprintExplorer
{
    static class Extension
    {
        public static string Truncate(this string obj, int length)
        {
            return obj.Substring(0, Math.Min(length, obj.Length));
        }

        public static string PermutedType(this string type) {
            var components = type.Split(".");
            if (components.Length <= 1)
                return type;
            var prefix = components.Last();
            var suffix = string.Join('.', components.Take(components.Length - 1));
            return $"{prefix} : {suffix}";
        }

    }

}
