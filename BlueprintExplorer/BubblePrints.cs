using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Design;

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
                    else
                        folderBrowser.Description = "Please select the the folder containing Wrath.exe";

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

    public class SettingsProxy
    {
        public void Sync()
        {
            var settings = Properties.Settings.Default;
            foreach (var p in GetType().GetProperties())
            {
                string prop = p.Name;
                settings.GetType().GetProperty(prop).SetValue(settings, p.GetValue(this));
            }
        }

        public SettingsProxy()
        {
            var settings = Properties.Settings.Default;
            foreach (var p in GetType().GetProperties())
            {
                string prop = p.Name;
                p.SetValue(this, settings.GetType().GetProperty(prop).GetValue(settings));
            }
        }

        [Description("Full path to your preferred text editor for viewing raw blueprints")]
        [DisplayName("External Editor")]
        [Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
        public string Editor { get; set; }

        [Description("Full path to the blueprints.binz file, only edit this if you're sure what you're doing")]
        [DisplayName("Blueprints DB")]
        [Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
        public string BlueprintDBPath { get; set; }

        [Description("Full path to your Wrath folder (i.e. the folder containing Wrath.exe")]
        [DisplayName("Wrath Install Folder")]
        [Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
        public string WrathPath { get; set; }

        [Description("If set, clicking on a blueprint in the search results will automatically open it in the external editor")]
        [DisplayName("Always Open Externally")]
        public bool AlwaysOpenInEditor { get; set; }

        [Description("If set, clicking on a link in the blueprint view will automatically open the link target")]
        [DisplayName("Follow link on click")]
        public bool EagerFollowLink { get; set; }

        [Description("If true, the external editor will display strict json. If false, the external editor will display human-friendly text")]
        [DisplayName("Generate json for 'Open in Editor'")]
        public bool StrictJsonForEditor { get; set; }

        [Description("If true, the blueprint view will expand all fields automatically")]
        [DisplayName("Expand all properties")]
        public bool EagerExpand { get; set; }

        [Description("If true, use a dark theme, you must restart the application for this to take effect")]
        [DisplayName("Dark Mode (*)")]
        public bool DarkMode { get; set; }
    }
}
