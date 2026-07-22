using System.Text.Json.Nodes;
using BlueprintExplorer;

namespace BubbleprintMcpCore;

public sealed class BinzCatalog
{
    private static readonly string[] SupportedGames = ["Wrath", "KM", "RT", "DH"];

    public JsonObject List(bool includeRemote = false)
    {
        var entries = includeRemote
            ? new BinzManager().Available.ToArray()
            : ReadLocal().ToArray();

        return new JsonObject
        {
            ["dataPath"] = BubblePrints.DataPath,
            ["lastLoaded"] = TryResolveLastLoadedPath(),
            ["includeRemote"] = includeRemote,
            ["datasets"] = new JsonArray(entries
                .Select(ToJson)
                .ToArray()),
        };
    }

    public JsonObject Download(
        string game,
        string? version = null,
        bool force = false)
    {
        game = NormalizeGame(game);
        version = string.IsNullOrWhiteSpace(version) ? null : version.Trim();

        if (version is not null)
        {
            var cached = ReadLocal().FirstOrDefault(entry =>
                entry.Version.Game == game
                && entry.Version.Version.ToString().Equals(
                    version,
                    StringComparison.OrdinalIgnoreCase));
            if (cached is not null && !force)
                return DownloadResult("already_cached", cached);
        }

        var manager = new BinzManager();
        var candidates = manager.Available
            .Where(entry => entry.Version.Game == game)
            .ToArray();
        var selected = version is null
            ? candidates.FirstOrDefault()
            : candidates.FirstOrDefault(entry =>
                entry.Version.Version.ToString().Equals(
                    version,
                    StringComparison.OrdinalIgnoreCase));

        if (selected is null)
        {
            var requested = version is null ? "latest" : version;
            throw new InvalidOperationException(
                $"No {game} dataset version '{requested}' is available.");
        }

        if (selected.Local && !force)
            return DownloadResult("already_cached", selected);

        int progress = 0;
        manager.Download(selected, value => progress = value)
            .GetAwaiter()
            .GetResult();
        selected.Local = true;

        var result = DownloadResult("downloaded", selected);
        result["progress"] = progress;
        return result;
    }

    public static string ResolveOpenPath(string? path)
    {
        if (!string.IsNullOrWhiteSpace(path))
            return Path.GetFullPath(ExpandHome(path));

        return TryResolveLastLoadedPath()
            ?? throw new FileNotFoundException(
                $"BubblePrints settings were not found at '{BubblePrints.SettingsPath}', " +
                "or they do not specify LastLoaded. Provide 'path' explicitly.");
    }

    private static IEnumerable<Binz> ReadLocal()
    {
        if (!Directory.Exists(BubblePrints.DataPath))
            yield break;

        foreach (var path in Directory.EnumerateFiles(BubblePrints.DataPath, "*.binz"))
        {
            BinzVersion version;
            try
            {
                version = BinzManager.VersionFromFile(path);
            }
            catch (Exception)
            {
                continue;
            }

            yield return new Binz
            {
                Local = true,
                Path = path,
                Source = "local",
                Version = version,
            };
        }
    }

    private static string? TryResolveLastLoadedPath()
    {
        if (!File.Exists(BubblePrints.SettingsPath))
            return null;

        using var settings = System.Text.Json.JsonDocument.Parse(
            File.ReadAllBytes(BubblePrints.SettingsPath));
        if (!settings.RootElement.TryGetProperty("LastLoaded", out var value)
            || value.ValueKind != System.Text.Json.JsonValueKind.String
            || string.IsNullOrWhiteSpace(value.GetString()))
            return null;

        var lastLoaded = ExpandHome(value.GetString()!);
        var resolved = Path.IsPathRooted(lastLoaded)
            ? lastLoaded
            : Path.Combine(BubblePrints.DataPath, lastLoaded);
        return Path.GetFullPath(resolved);
    }

    private static string NormalizeGame(string game)
    {
        if (string.IsNullOrWhiteSpace(game))
            throw new ArgumentException("'game' is required.");

        var canonical = SupportedGames.FirstOrDefault(candidate =>
            candidate.Equals(game.Trim(), StringComparison.OrdinalIgnoreCase));
        return canonical ?? throw new ArgumentException(
            $"'game' must be one of: {string.Join(", ", SupportedGames)}.");
    }

    private static string ExpandHome(string path)
    {
        if (!path.StartsWith("~/", StringComparison.Ordinal))
            return path;

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, path[2..]);
    }

    private static JsonObject ToJson(Binz entry)
    {
        return new JsonObject
        {
            ["game"] = entry.Version.Game,
            ["version"] = entry.Version.Version.ToString(),
            ["cached"] = entry.Local,
            ["path"] = entry.Path,
            ["bytes"] = entry.Path is not null && File.Exists(entry.Path)
                ? new FileInfo(entry.Path).Length
                : null,
            ["source"] = entry.Source,
        };
    }

    private static JsonObject DownloadResult(string status, Binz entry)
    {
        var result = ToJson(entry);
        result["status"] = status;
        return result;
    }
}
