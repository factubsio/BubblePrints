using BlueprintExplorer;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BinzFactory;

public static class BinzImporter
{
    public static Assembly Wrath;

    // Change this for import new
    public static string Game_Data { get; private set; } = "WH40KRT_Data";

    public static string CurrentGame => Game_Data switch
    {
        "Kingmaker_Data" => "KM",
        "Wrath_Data" => "Wrath",
        "WH40KRT_Data" => "RT",
        _ => throw new NotSupportedException(),
    };

    //public static void LoadAssemblies(string gameData, string dllName)
    //{
    //    if (TryGetWrathPath(out var wrathPath))
    //    {
    //        var gamePath = Path.Combine(wrathPath, gameData, "Managed");
    //        var resolver = new PathAssemblyResolver(Directory.EnumerateFiles(gamePath, "*.dll"));
    //        var _mlc = new MetadataLoadContext(resolver);
    //        Wrath = _mlc.LoadFromAssemblyPath(Path.Combine(gamePath, dllName));
    //    }

    //}

    public static string GetBlueprintSource(string wrathPath) => Game_Data switch
    {
        "Wrath_Data" or "WH40KRT_Data" or "Kingmaker_Data" => Path.Combine(wrathPath, "blueprints.zip"),
        _ => throw new NotSupportedException("unknown game: " + Game_Data)
    };

    private static Regex ExtractVersionPattern = new(@"(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)(?<suffix>\.\d+|[a-zA-Z]*)");
    public static BlueprintExplorer.BlueprintDB.GameVersion GetGameVersion(string wrathPath)
    {
        if (Game_Data == "Kingmaker_Data")
        {
            return new(2, 1, 7, "b", 0);
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

    public static BlueprintDB Import(ConnectionProgress progress, string gamePath, string path, BlueprintDB.GameVersion version)
    {

        static bool ContainsDirectory(string path, string directoryName)
        {
            var dirs = Directory.GetDirectories(path);

            return dirs
                .Select(path => Path.EndsInDirectorySeparator(path) ? Path.GetDirectoryName(path) : Path.GetFileName(path))
                .Contains(directoryName);
        }

        if (ContainsDirectory(gamePath, "WH40KRT_Data"))
            Game_Data = "WH40KRT_Data";
        else if (ContainsDirectory(gamePath, "Wrath_Data"))
            Game_Data = "Wrath_Data";
        else if (ContainsDirectory(gamePath, "Kingmaker_Data"))
            Game_Data = "Kingmaker_Data";

        IGameImporter importer = new FromZipImporter();
        return importer.ExtractFromGame(progress, gamePath, path, version);
    }

    public static string GameExe => Game_Data switch
    {
        "Kingmaker_Data" => "Kingmaker.exe",
        "Wrath_Data" => "Wrath.exe",
        "WH40KRT_Data" => "WH40KRT.exe",
        _ => throw new NotSupportedException(),
    };

}

public interface IGameImporter
{
    public BlueprintDB ExtractFromGame(ConnectionProgress progress, string wrathPath, string outputFile, BlueprintExplorer.BlueprintDB.GameVersion version);
}

public static class ReferenceExtractor
{
    public static void VisitObjects(JsonElement node, HashSet<string> types)
    {
        if (node.ValueKind == JsonValueKind.Array)
        {
            foreach (var elem in node.EnumerateArray())
                VisitObjects(elem, types);
        }
        else if (node.ValueKind == JsonValueKind.Object)
        {
            if (node.TryGetProperty("$type", out var raw))
            {
                if (BinzImporter.CurrentGame == "KM")
                {
                    types.Add(raw.NewTypeStr().FullName);
                }
                else
                {
                    types.Add(raw.NewTypeStr().Guid);
                }
            }
            foreach (var elem in node.EnumerateObject())
                VisitObjects(elem.Value, types);
        }

    }

}
