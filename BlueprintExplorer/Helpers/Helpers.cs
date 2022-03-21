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

        public static void ForEachControl(this Control root, Action<Control> act)
        {
            Queue<Control> frontier = new();
            frontier.Enqueue(root);

            while (frontier.Count > 0)
            {
                var c = frontier.Dequeue();
                act(c);
                for (int i = 0; i < c.Controls.Count; i++)
                    frontier.Enqueue(c.Controls[i]);
            }


        }

        public static void AddMouseClickRecursively(this Control root, MouseEventHandler handler) => ForEachControl(root, c => c.MouseClick += handler);
        public static void AddKeyDownRecursively(this Control root, KeyEventHandler handler) => ForEachControl(root, c => c.KeyDown += handler);
    }

}
