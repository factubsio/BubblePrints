using System;
using System.Collections.Generic;
using System.IO;

namespace BlueprintExplorer
{
    public class TextExporter
    {
        class ElementWriteState
        {
            public bool IsObj;
            public int Children;
        }
        public static void Export(TextWriter stream, BlueprintHandle blueprint)
        {
            bool json = BubblePrints.Settings.StrictJsonForEditor;
            int level = 0;
            Stack<ElementWriteState> stack = new();
            stack.Push(new() { IsObj = true });

            void Quote(string str)
            {
                if (json)
                    stream.Write('"');
                stream.Write(str);
                if (json)
                    stream.Write('"');
            }

            void WriteMultiLine(string key, string value)
            {
                if (json)
                {
                    WriteLine(key, value.Replace("\r\n", "\\n").Replace("\n", "\\n"), null);
                    return;
                }
                stream.WriteLine();
                stream.Write("".PadLeft(level * 4));
                if (stack.Peek().IsObj)
                {
                    stream.Write(key);
                    stream.Write(": ");
                }

                int indent = level * 4 + key.Length + 2;

                var lines = value.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);

                for (int i = 0; i < lines.Length; i++)
                {
                    if (i > 0)
                        stream.Write("".PadLeft(indent));
                    stream.Write(lines[i]);
                    if (i != lines.Length - 1)
                        stream.WriteLine();
                }
            }

            void WriteLine(string key, string value, string link)
            {
                if (json)
                {
                    if (stack.Peek().Children > 1)
                        stream.Write(',');
                }
                stream.WriteLine();
                stream.Write("".PadLeft(level * 4));
                if (stack.Peek().IsObj)
                {
                    Quote(key);
                    stream.Write(": ");
                }

                if (link != null)
                {
                    if (BlueprintDB.Instance.Blueprints.TryGetValue(Guid.Parse(link), out var target))
                        value = "link: " + link + " (" + target.Name + " :" + target.TypeName + ")";
                    else
                        value = "link: " + link + " (dead)";
                }

                Quote(value);
            }

            void Open(string key, bool obj)
            {
                if (json)
                {
                    if (stack.Peek().Children > 0)
                        stream.Write(',');
                    stream.WriteLine();
                }
                stream.Write("".PadLeft(level * 4));
                if (stack.Peek().IsObj)
                {
                    Quote(key);
                    stream.Write(": ");
                }
                if (json)
                    stream.Write(obj ? '{' : '[');
            }
            void Close(ElementWriteState state)
            {
                if (state.Children > 0)
                {
                    if (json)
                    {
                        stream.WriteLine();
                        stream.Write("".PadLeft(level * 4));
                    }
                }
                else
                {
                    if (!json)
                        stream.WriteLine(" [empty]");
                }
                if (json)
                    stream.Write(state.IsObj ? '}' : ']');
            }

            if (json)
            {
                stack.Push(new());
                stack.Peek().IsObj = true;
                stream.WriteLine("{");
                level++;
                WriteLine("guid", blueprint.GuidText, null);
            }

            foreach (var elem in blueprint.Elements)
            {
                if (elem.levelDelta < 0)
                {
                    var closeObj = stack.Pop();
                    level--;
                    Close(closeObj);
                }
                else if (elem.levelDelta == 0)
                {
                    stack.Peek().Children++;
                    WriteLine(elem.key, elem.value, elem.link);
                }
                else if (elem.levelDelta > 0)
                {
                    stack.Peek().Children++;
                    Open(elem.key, elem.isObj);
                    stack.Push(new() { IsObj = elem.isObj });
                    level++;

                    var renderedString = JsonExtensions.ParseAsString(elem.Node);
                    if (renderedString != null)
                    {
                        stack.Peek().Children++;
                        WriteMultiLine("LocalisedValue", renderedString);
                    }

                }

            }
            if (json)
            {
                level--;
                Close(stack.Pop());
            }

        }
    }
}
