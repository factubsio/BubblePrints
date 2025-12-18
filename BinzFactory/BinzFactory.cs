using BlueprintExplorer;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BinzFactory;

public static partial class BinzImporter
{
    private static readonly List<GameIdentification> KnownGames =
    [
        new("WH40KRT_Data", "RT", typeof(RtGame)),
        new("WH40KDH_Data", "DH", typeof(DhGame)),
        new("Wrath_Data", "Wrath", typeof(WrathGame)),
        new("Kingmaker_Data", "KM", typeof(KmGame)),
    ];

    private static GameIdentification? GameForPath(string gamePath) => KnownGames.FirstOrDefault(x => x.Matches(gamePath));

    private static readonly Regex ExtractVersionPattern = MakeExtractVersionPattern();
    public static BlueprintExplorer.BlueprintDB.GameVersion GetGameVersion(string gamePath)
    {
        string Game_Data = GameDataFromPath(gamePath);
        if (Game_Data == "Kingmaker_Data")
        {
            return new(2, 1, 7, "b", 0);
        }

        var versionPath = Path.Combine(gamePath, Game_Data, "StreamingAssets", "Version.info");
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

    public static string GetGameName(string gamePath) => GameForPath(gamePath)?.Name ?? "unknown";
    private static string GameDataFromPath(string gamePath) => GameForPath(gamePath)?.DataFolder ?? "unknown";

    public static BlueprintDB Import(ConnectionProgress progress, string gamePath, string path, BlueprintDB.GameVersion version)
    {
        IGameDefinitions game = GameForPath(gamePath)?.Create(gamePath) ?? throw new NotSupportedException();

        var db = BinzImportExport.ExtractFromGame(game, progress);

        BinzImportExport.WriteBlueprints(db, progress, game, path, version);

        return db;
    }

    [GeneratedRegex(@"(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)(?<suffix>\.\d+|[a-zA-Z]*)")]
    private static partial Regex MakeExtractVersionPattern();
}


internal record class GameIdentification(string DataFolder, string Name, Type GameType)
{
    public IGameDefinitions? Create(string gamePath) => GameType.GetConstructor([typeof(string), typeof(string)])?.Invoke([gamePath, DataFolder]) as IGameDefinitions;

    internal bool Matches(string path) =>
        Directory.GetDirectories(path)
            .Select(path => Path.EndsInDirectorySeparator(path) ? Path.GetDirectoryName(path) : Path.GetFileName(path))
            .Contains(DataFolder);
}

