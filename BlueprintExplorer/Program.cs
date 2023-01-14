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
