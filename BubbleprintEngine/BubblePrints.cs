using System.ComponentModel;
using System.Text.Json;

namespace BlueprintExplorer;

public static class BubblePrints
{
    public delegate void TemplatesChangedDelegate(int oldCount, int newCount);
    public static event TemplatesChangedDelegate OnTemplatesChanged;

    public delegate void SettingsChangedDelegate();
    public static event SettingsChangedDelegate OnSettingsChanged;

    public static SettingsProxy Settings = new();

#if !NO_CONSOLE
    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern bool AllocConsole();
#endif

    public static void Install()
    {
#if !NO_CONSOLE
#if DEBUG
        AllocConsole();
#else
        if (Environment.GetEnvironmentVariable("BUBBLEPRINTS_DEBUG") != null)
        {
            AllocConsole();
        }
#endif
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

    public static string DataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BubblePrints");
    public static string MakeDataPath(string subpath) => Path.Combine(DataPath, subpath);
    public static string SettingsPath => MakeDataPath("settings.json");


    public static void SaveSettings()
    {
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(Settings));
        OnSettingsChanged?.Invoke();
    }
    public static void NotifyTemplatesChanged(int oldCount, int newCount) => OnTemplatesChanged?.Invoke(oldCount, newCount);
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
    public void SetColumnSize(int index, int value)
    {
        if (value != SearchColumnSizesDefault[index])
            SearchColumnSizes[index] = value;
        else
            SearchColumnSizes[index] = -1;
    }

    public int GetColumnSize(int index)
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
    //[Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
    public string Editor { get; set; }

    [Description("Argument(s) that will be passed to External Editor, {blueprint} will be replaced with the path to the generated file")]
    [DisplayName("External Editor - Template")]
    public string ExternalEditorTemplate { get; set; } = "{blueprint}";

    [Description("Full path to your Wrath folder (i.e. the folder containing Wrath.exe")]
    [DisplayName("Wrath Install Folder")]
    //[Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
    public string WrathPath { get; set; }

    [Description("Full path to your Kingmaker folder (i.e. the folder containing Kingmaker.exe")]
    [DisplayName("Kingmaker Install Folder")]
    //[Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
    public string KMPath { get; set; }

    [Description("Full path to your Rogue Trader folder (i.e. the folder containing WH40KRT.exe")]
    [DisplayName("Rogue Trader Install Folder")]
    //[Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
    public string RTPath { get; set; }

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

    [Description("Density of seasonal effects like snow fall, 1-10")]
    [DisplayName("Seasonal effect density")]
    public int SeasonalDensity { get; set; } = 10;

    [Description("Template string for blueprint template generation - see help for more details")]
    [DisplayName("Generator template")]
    //[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public Template[] GeneratorTemplate { get; set; }

    [Description("Column widths for search results (only takes effect when BubblePrints loads, automatically updated if you resize the columns)")]
    [DisplayName("Search column widths")]
    public int[] SearchColumnSizes { get; set; } = new int[] { -1, -1, -1 };

    [Description("If true, the first item in the blueprint view (name, id, type) will be expanded by default")]
    [DisplayName("Expand blueprint name")]
    public bool BlueprintNameExpanded { get; set; } = false;

    private readonly static int[] SearchColumnSizesDefault = new int[] { 800, 600, 450 };
}

public class Template
{
    public string Name { get; set; }
    public string Value { get; set; }

}

public interface IFolderChooser
{
    void Prepare();
    bool Choose(string exe, out string choice);
}
