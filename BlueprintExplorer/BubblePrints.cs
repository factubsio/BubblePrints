using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BlueprintExplorer
{
    public static class BubblePrints
    {
        private static string StoredWrathPath = null;
        public static bool TryGetWrathPath(out string path)
        {
            path = StoredWrathPath;
            return path != null;
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        private static bool console = false;
        public static Assembly Wrath;

        internal static void SetupLogging()
        {
#if DEBUG
            if (console) return;
            AllocConsole();
#endif
        }

        internal static void SetWrathPath()
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
                        return;
                        
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

            StoredWrathPath = path;
        }
    }
}
