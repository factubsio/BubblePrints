using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using static BlueprintExplorer.BlueprintDB;

namespace BlueprintExplorer
{
    public static class BubblePrints
    {
        public delegate void TemplatesChangedDelegate(int oldCount, int newCount);
        public static event TemplatesChangedDelegate OnTemplatesChanged;

        public delegate void SettingsChangedDelegate();
        public static event SettingsChangedDelegate OnSettingsChanged;

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

        public static void Install()
        {
#if DEBUG
            AllocConsole();
#else
            if (Environment.GetEnvironmentVariable("BUBBLEPRINTS_DEBUG") != null)
            {
                AllocConsole();
            }
#endif

            if (!Directory.Exists(DataPath))
                Directory.CreateDirectory(DataPath);

            if (File.Exists(SettingsPath))
            {
                var raw = File.ReadAllText(SettingsPath);
                Settings = JsonSerializer.Deserialize<SettingsProxy>(raw);
                if (Settings.StrictJsonForEditor && Settings.EditorExportMode == ExportMode.Bubble)
                {
                    Settings.EditorExportMode = ExportMode.Json;
                    SaveSettings();
                }

            }
        }

        private static Regex ExtractVersionPattern = new(@"(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)(?<suffix>\.\d+|[a-zA-Z]*)");

        internal static GameVersion GetGameVersion(string wrathPath)
        {
            if (Game_Data == "Kingmaker_Data")
            {
                return new(2, 1, 4, "a", 0);
            }

            var versionPath = Path.Combine(wrathPath, Game_Data, "StreamingAssets", "Version.info");
            if (!File.Exists(versionPath))
            {
                throw new Exception("Cannot find Version.info in given wrath path");
            }

            var lines = File.ReadAllLines(versionPath);
            if (lines.Length == 0)
            {
                throw new Exception("Version.info is empty");
            }

            var split = lines[0].Split(" ");
            if (split.Length <= 2)
            {
                throw new Exception("Game Version string is invalid");
            }

            var maybeVersion = split[^2];
            var r = ExtractVersionPattern.Match(maybeVersion);
            if (!r.Success)
            {
                throw new Exception("Game Version string is invalid");
            }

            var major = int.Parse(r.Groups["major"].Value);
            var minor = int.Parse(r.Groups["minor"].Value);
            var patch = int.Parse(r.Groups["patch"].Value);
            var suffix = r.Groups["suffix"].Value;
            return new(major, minor, patch, suffix, 0);
        }

        internal static void SetWrathPath(bool forceSelect)
        {
            var path = Settings.WrathPath;
            if (forceSelect)
            {
                path = null;
            }
            if (path == null || path.Length == 0 || !File.Exists(Path.Combine(path, GameExe)))
            {
                var folderBrowser = new FolderBrowserDialog();
                folderBrowser.UseDescriptionForTitle = true;
                bool errored = false;

                while (true)
                {
                    if (errored)
                        folderBrowser.Description = $"Could not find {GameExe} at the selected folder";
                    else
                        folderBrowser.Description = $"Please select the the folder containing {GameExe}";

                    if (folderBrowser.ShowDialog() != DialogResult.OK)
                        return;

                    path = folderBrowser.SelectedPath;

                    static bool ContainsDirectory(string path, string directoryName) =>
                        Directory.GetDirectories(path)
                            .Select(path => Path.EndsInDirectorySeparator(path) ? Path.GetDirectoryName(path) : Path.GetFileName(path))
                            .Contains(directoryName);

                    if (ContainsDirectory(path, "WH40KRT_Data"))
                        BubblePrints.Game_Data = "WH40KRT_Data";
                    else
                    if (ContainsDirectory(path, "Wrath_Data"))
                        BubblePrints.Game_Data = "Wrath_Data";

                    var exePath = Path.Combine(path, GameExe);
                    if (File.Exists(exePath))
                        break;

                    errored = true;
                }
                if (!errored)
                {
                    Settings.WrathPath = path;
                    SaveSettings();
                }
            }

            StoredWrathPath = path;
        }

        public static string DataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BubblePrints");
        public static string MakeDataPath(string subpath) => Path.Combine(DataPath, subpath);
        public static string SettingsPath => MakeDataPath("settings.json");

        public static string GameExe => Game_Data switch
        {
            "Kingmaker_Data" => "Kingmaker.exe",
            "Wrath_Data" => "Wrath.exe",
            "WH40KRT_Data" => "WH40KRT.exe",
            _ => throw new NotSupportedException(),
        };

        public static string CurrentGame => Game_Data switch
        {
            "Kingmaker_Data" => "Kingmaker",
            "Wrath_Data" => "Wrath",
            "WH40KRT_Data" => "RT",
            _ => throw new NotSupportedException(),
        };


        // Change this for import new
        public static string Game_Data = "WH40KRT_Data";

        internal static void SaveSettings()
        {
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(Settings));
            OnSettingsChanged?.Invoke();
        }
        internal static void NotifyTemplatesChanged(int oldCount, int newCount) => OnTemplatesChanged?.Invoke(oldCount, newCount);
        internal static string GetBlueprintSource(string wrathPath) => Game_Data switch
        {
            "Wrath_Data" or "WH40KRT_Data" => Path.Combine(wrathPath, "blueprints.zip"),
            "Kingmaker_Data" => @"D:\Blueprints2.1.4.zip",
            _ => throw new NotSupportedException("unknown game: " + Game_Data)
        };
    }

    public enum ExportMode
    {
        Bubble,
        Json,
        JBP,
    }

    public class SettingsProxy
    {
        public void Sync()
        {
            int oldCount = BubblePrints.Settings.GeneratorTemplate?.Length ?? 0;

            var settings = BubblePrints.Settings;
            foreach (var p in GetType().GetProperties())
            {
                p.SetValue(settings, p.GetValue(this));
            }
            BubblePrints.SaveSettings();

            BubblePrints.NotifyTemplatesChanged(oldCount, GeneratorTemplate?.Length ?? 0);
        }
        internal void SetColumnSize(int index, int value)
        {
            if (value != SearchColumnSizesDefault[index])
                SearchColumnSizes[index] = value;
            else
                SearchColumnSizes[index] = -1;
        }

        internal int GetColumnSize(int index)
        {
            int savedValue = SearchColumnSizes[index];

            if (savedValue == -1)
                return SearchColumnSizesDefault[index];
            else
                return savedValue;
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
        [Browsable(false)]
        public bool StrictJsonForEditor { get; set; } = true;

        [Description("Bubble: easy-to-read text file, Json: strict json with some fields made more human readable, JBP: OwlcatTemplate compatible json")]
        [DisplayName("Export Mode for 'Open in Editor'")]
        [DefaultValue(ExportMode.Bubble)]
        public ExportMode EditorExportMode { get; set; } = ExportMode.Bubble;


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

        [Description("If true, install readline-style shortcuts in most text boxes")]
        [DisplayName("Readline shortcuts")]
        public bool UseReadlineShortcuts { get; set; } = false;

        [Description("If true, /searches inside blueprints will intepret the search term as a regular expression")]
        [DisplayName("Regex for local searches")]
        public bool RegexForLocalSearch { get; set; } = false;

        [Description("If true, the search results will always be shown")]
        [DisplayName("Pin search results")]
        public bool PinSearchResults { get; set; } = false;

        [Description("If true, disables any fun seasonal themes that may be in effect")]
        [DisplayName("Disable fun themes")]
        public bool NoSeasonalTheme { get; set; } = false;

        [Description("If true, the filter value is shared between all blueprint views")]
        [DisplayName("Share blueprint filter")]
        public bool ShareBlueprintFilter { get; set; } = false;

        [ReadOnly(true)]
        [Description("This path will be used if the 'check for updates' is false, or if there is no connectivity to check for any later version")]
        [DisplayName("Most recent blueprints loaded")]
        public string LastLoaded { get; set; }

        [Description("Size of the font for the blueprint viewer")]
        [DisplayName("Blueprint font size")]
        public int BlueprintFontSize { get; set; } = 12;

        [Description("Template string for blueprint template generation - see help for more details")]
        [DisplayName("Generator template")]
        //[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public Template[] GeneratorTemplate { get; set; }

        [Description("Column widths for search results (only takes effect when BubblePrints loads, automatically updated if you resize the columns)")]
        [DisplayName("Search column widths")]
        public int[] SearchColumnSizes { get; set; } = new int[]{-1, -1, -1};

        [Description("If true, the first item in the blueprint view (name, id, type) will be expanded by default")]
        [DisplayName("Expand blueprint name")]
        public bool BlueprintNameExpanded { get; set; } = false;

        private readonly static int[] SearchColumnSizesDefault = new int[]{800, 600, 450};
    }

    public class Template
    {
        public string Name { get; set; }
        public string Value { get; set; }

    }
}
