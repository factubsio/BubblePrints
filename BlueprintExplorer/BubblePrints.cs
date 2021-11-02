using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BlueprintExplorer
{
    public static class BubblePrints
    {
        public static string WrathPath
        {
            get
            {
                var path = Properties.Settings.Default.WrathPath;
                if (path == null || path.Length == 0 || !File.Exists(Path.Combine(path, "Wrath.exe")))
                {
                    var folderBrowser = new FolderBrowserDialog();
                    folderBrowser.UseDescriptionForTitle = true;
                    bool errored = false;

                    string folderPath = null;

                    while (true)
                    {
                        if (errored)
                            folderBrowser.Description = "Could not find Wrath.exe at the selected folder";

                        if (folderBrowser.ShowDialog() != DialogResult.OK)
                            break;
                        folderPath = folderBrowser.SelectedPath;

                        var exePath = Path.Combine(folderPath, "Wrath.exe");
                        if (File.Exists(exePath))
                            break;

                        errored = true;
                    }
                    if (path != null)
                    {
                        path = folderPath;
                        Properties.Settings.Default.WrathPath = path;
                        Properties.Settings.Default.Save();
                    }
                }
                return path;
            }
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        private static bool console = false;

        internal static void SetupLogging()
        {
#if DEBUG
            if (console) return;
            AllocConsole();
#endif
        }
    }
}
