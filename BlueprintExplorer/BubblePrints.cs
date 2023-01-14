﻿using Krypton.Toolkit;
using System;
using System.Collections;
using System.Collections.Generic;
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
        public delegate void TemplatesChangedDelegate(int oldCount, int newCount);
        public static event TemplatesChangedDelegate OnTemplatesChanged;

        public delegate void SettingsChangedDelegate();
        public static event SettingsChangedDelegate OnSettingsChanged;

        public static SettingsProxy Settings = new();
        private static string StoredGamePath = null;
        public static bool TryGetGamePath(out string path)
        {
            path = StoredGamePath;
            return path != null;
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        public static Assembly GameAssembly { get; private set; }

        public static void Install()
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
                if (Settings.StrictJsonForEditor && Settings.EditorExportMode == ExportMode.Bubble)
                {
                    Settings.EditorExportMode = ExportMode.Json;
                    SaveSettings();
                }

            }
        }

        internal static void InitializeForGame(GameFamily game)
        {
            if (Game != GameFamily.NotSpecified && Game != game)
                throw new Exception("game mismatch");

            var (exe, path, dll) = game switch
            {
                GameFamily.WrathOfTheRighteous => ("Wrath.exe", Settings.WrathPath, Path.Combine("Wrath_Data", "Managed", "Assembly-CSharp.dll")),
                GameFamily.RogueTrader1 => ("WH40KRT.exe", Settings.RogueTraderPath, Path.Combine("WH40KRT_Data", "Managed", "Code.dll")),
                _ => throw new Exception(),
            };

            Game = game;

            if (string.IsNullOrEmpty(path) || !File.Exists(Path.Combine(path, exe)))
            {
                var folderBrowser = new FolderBrowserDialog();
                folderBrowser.UseDescriptionForTitle = true;
                bool errored = false;

                while (true)
                {
                    if (errored)
                        folderBrowser.Description = $"Could not find {exe} at the selected folder";
                    else
                        folderBrowser.Description = $"Please select the the folder containing {exe}";

                    if (folderBrowser.ShowDialog() != DialogResult.OK)
                        return;

                    path = folderBrowser.SelectedPath;

                    var exePath = Path.Combine(path, exe);
                    if (File.Exists(exePath))
                        break;

                    errored = true;
                }
                if (!errored)
                {
                    if (game == GameFamily.RogueTrader1)
                        Settings.RogueTraderPath = path;
                    else if (game == GameFamily.WrathOfTheRighteous)
                        Settings.WrathPath = path;

                    SaveSettings();
                }
            }


            GameAssembly = Assembly.LoadFrom(Path.Combine(path, dll));

            StoredGamePath = path;
        }

        public static string DataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BubblePrints");
        public static string MakeDataPath(string subpath) => Path.Combine(DataPath, subpath);
        public static string SettingsPath => MakeDataPath("settings.json");

        public static string GameDataFolder
        {
            get
            {
                return Game switch
                {
                    GameFamily.RogueTrader1 => "WH40KRT_Data",
                    GameFamily.WrathOfTheRighteous => "Wrath_Data",
                    _ => throw new Exception(),
                };
            }

        }

        internal static void SaveSettings()
        {
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(Settings));
            OnSettingsChanged?.Invoke();
        }
        internal static void NotifyTemplatesChanged(int oldCount, int newCount) => OnTemplatesChanged?.Invoke(oldCount, newCount);
        public static GameFamily Game { get; private set; } = GameFamily.NotSpecified;
    }
    public enum GameFamily
    {
        NotSpecified,
        WrathOfTheRighteous,
        RogueTrader1,
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

        [Description("Full path to your RT folder (i.e. the folder containing WH40KRT.exe")]
        [DisplayName("RT Install Folder")]
        [Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
        public string RogueTraderPath { get; set; }

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
