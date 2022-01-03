using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace BlueprintExplorer
{
    public static class BubblePrints
    {
        public static SettingsProxy Settings = new();
        private static string StoredWrathPath = null;
        public static bool TryGetWrathPath(out string path)
        {
            path = StoredWrathPath;
            return path != null;
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        public static Assembly Wrath;

        internal static void Install()
        {
#if DEBUG
            AllocConsole();
#endif

            if (!Directory.Exists(DataPath))
                Directory.CreateDirectory(DataPath);

            if (File.Exists(SettingsPath))
            {
                var raw = File.ReadAllText(SettingsPath);
                Settings = JsonSerializer.Deserialize<SettingsProxy>(raw);
            }
        }

        internal static void SetWrathPath()
        {

            var path = BubblePrints.Settings.WrathPath;
            if (path == null || path.Length == 0 || !File.Exists(Path.Combine(path, "Wrath.exe")))
            {
                var folderBrowser = new FolderBrowserDialog();
                folderBrowser.UseDescriptionForTitle = true;
                bool errored = false;

                while (true)
                {
                    if (errored)
                        folderBrowser.Description = "Could not find Wrath.exe at the selected folder";
                    else
                        folderBrowser.Description = "Please select the the folder containing Wrath.exe";

                    if (folderBrowser.ShowDialog() != DialogResult.OK)
                        return;
                        
                    path = folderBrowser.SelectedPath;

                    var exePath = Path.Combine(path, "Wrath.exe");
                    if (File.Exists(exePath))
                        break;

                    errored = true;
                }
                if (!errored)
                {
                    BubblePrints.Settings.WrathPath = path;
                    BubblePrints.SaveSettings();
                }
            }

            StoredWrathPath = path;
        }

        public static string DataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BubblePrints");
        public static string MakeDataPath(string subpath) => Path.Combine(DataPath, subpath);
        public static string SettingsPath => MakeDataPath("settings.json");


        internal static void SaveSettings() => File.WriteAllText(BubblePrints.SettingsPath, JsonSerializer.Serialize(BubblePrints.Settings));
    }

    public class SettingsProxy
    {
        public void Sync()
        {
            var settings = BubblePrints.Settings;
            foreach (var p in GetType().GetProperties())
            {
                p.SetValue(settings, p.GetValue(this));
            }
            BubblePrints.SaveSettings();
        }


        public SettingsProxy() { }

        public SettingsProxy(SettingsProxy settings)
        {
            foreach (var p in GetType().GetProperties())
            {
                p.SetValue(this, p.GetValue(settings));
            }
        }

        [Description("Full path to your preferred text editor for viewing raw blueprints")]
        [DisplayName("External Editor - Path")]
        [Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
        public string Editor { get; set; }

        [Description("Argument(s) that will be passed to External Editor, {blueprint} will be replaced with the path to the generated file")]
        [DisplayName("External Editor - Template")]
        public string ExternalEditorTemplate { get; set; } = "{blueprint}";

        [Description("Full path to your Wrath folder (i.e. the folder containing Wrath.exe")]
        [DisplayName("Wrath Install Folder")]
        [Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
        public string WrathPath { get; set; }

        [Description("If set, clicking on a blueprint in the search results will automatically open it in the external editor")]
        [DisplayName("Always Open Externally")]
        public bool AlwaysOpenInEditor { get; set; }

        [Description("If true, the external editor will display strict json. If false, the external editor will display human-friendly text")]
        [DisplayName("Generate json for 'Open in Editor'")]
        public bool StrictJsonForEditor { get; set; } = true;

        [Description("If true, the blueprint view will expand all fields automatically")]
        [DisplayName("Expand all properties")]
        public bool EagerExpand { get; set; } = true;

        [Description("If true, use a dark theme, you must restart the application for this to take effect")]
        [DisplayName("Dark Mode (*)")]
        public bool DarkMode { get; set; }

        [Description("If true, checks to see if there is a newer version of the application available and notifies you (does not automatically update)")]
        [DisplayName("Check for updates")]
        public bool CheckForUpdates { get; set; } = true;

        [Description("If true, checks to see if there is a later version of the blueprints file and automatically updates your current version")]
        [DisplayName("Check for new blueprints")]
        public bool CheckForNewBP { get; set; } = true;

        [Description("If true, disables any fun seasonal themes that may be in effect")]
        [DisplayName("Disable fun themes")]
        public bool NoSeasonalTheme { get; set; } = false;

        [ReadOnly(true)]
        [Description("This path will be used if the 'check for updates' is false, or if there is no connectivity to check for any later version")]
        [DisplayName("Most recent blueprints loaded")]
        public string LastLoaded { get; set; }
    }
}
