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


    }

}
