using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static BlueprintExplorer.BlueprintHandle;

namespace BlueprintExplorer
{
    public class FieldModification
    {
        public string OldValue { get; set; }
        public string NewValue { get; set; }

        public string Path { get; set; }
    }
    public class BlueprintDiff
    {
        public List<FieldModification> Modifications = new();
    }

    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            BubblePrints.Install();

            //var prevProgress = new BlueprintDB.ConnectionProgress();
            //var load = Task.Run(() => BlueprintDB.Instance.TryConnect(prevProgress));

            //var print = Task.Run(() =>
            //{
            //    while (true)
            //    {
            //        Console.WriteLine(prevProgress.Status);
            //        if (load.IsCompleted)
            //            return;

            //        Thread.Sleep(500);
            //    }

            //});

            //print.Wait();

            //return;

            //var curr = new BlueprintDB();
            //var currProgress = new BlueprintDB.ConnectionProgress();
            //var currLoad = curr.TryConnect(currProgress, @"C:\Users\worce\AppData\Local\BubblePrints\blueprints_raw_1.2.0A_1.binz");

            //Task.WaitAll(prevLoad, currLoad);

            //HashSet<Guid> brandNew = new(curr.Blueprints.Keys);

            //using var f = File.CreateText(@"D:\bpdiffs.txt");

            ////foreach (var (guid, handle) in curr.Blueprints)
            //var guid = Guid.Parse("d3ddec98-a1b5-0464-0916-96547113ea2c");
            //var handle = curr.Blueprints[guid];
            //{
            //    if (prev.Blueprints.TryGetValue(guid, out var old))
            //    {
            //        Dictionary<string, string> valuesByPathOld = new();
            //        Dictionary<string, string> valuesByPathNew = new();

            //        foreach (var (node, path) in ElementVisitor.Visit(old))
            //        {
            //            valuesByPathOld[path + "/" + node.key] = node.value;
            //        }
            //        foreach (var (node, path) in ElementVisitor.Visit(handle))
            //        {
            //            valuesByPathNew[path + "/" + node.key] = node.value;
            //        }

            //        File.WriteAllLines(@"D:\old.txt", valuesByPathOld.Select(kv => $"{kv.Key}: {kv.Value}"));
            //        File.WriteAllLines(@"D:\new.txt", valuesByPathNew.Select(kv => $"{kv.Key}: {kv.Value}"));

            //        var diff = new BlueprintDiff();
            //        foreach (var k in valuesByPathNew)
            //        {
            //            if (valuesByPathOld.TryGetValue(k.Key, out var oldValue))
            //            {
            //                if (oldValue != k.Value)
            //                {
            //                    diff.Modifications.Add(new()
            //                    {
            //                        Path = k.Key,
            //                        NewValue = k.Value,
            //                        OldValue = oldValue,
            //                    });
            //                }
            //            }
            //            else
            //            {
            //                diff.Modifications.Add(new()
            //                {
            //                    Path = k.Key,
            //                    NewValue = k.Value,
            //                    OldValue = null,
            //                });
            //            }
            //        }
            //        if (diff.Modifications.Count > 0)
            //        {
            //            f.WriteLine("diffs for: " + guid + " / " + handle.Name);
            //            foreach (var mod in diff.Modifications)
            //                f.WriteLine($"{mod.Path}  ::   {mod.OldValue} => {mod.NewValue}");
            //        }
            //        handle.UserData = diff;
            //        brandNew.Remove(guid);
            //    }
            //}

            SetProcessDPIAware();


            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(new Form1());

        }


        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();
    }
}
