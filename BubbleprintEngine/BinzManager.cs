using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Numerics;
using System.Text.Json;
using System.Text.RegularExpressions;
using static BlueprintExplorer.BlueprintDB;

namespace BlueprintExplorer;

public class BinzManager
{
    public readonly BindingList<Binz> Available = [];
    public readonly Dictionary<BinzVersion, Binz> ByVersion = [];

    private readonly List<Binz> PreSort = [];

    public BinzManager()
    {
        try
        {
            if (!Directory.Exists(CacheDir))
                Directory.CreateDirectory(CacheDir);

            Console.WriteLine("setting available = from web");
            using var web = new HttpClient();
            ParseWebJson(web, "https://raw.githubusercontent.com/factubsio/BubblePrintsData/main/versions.json", "Wrath");
            ParseWebJson(web, "https://raw.githubusercontent.com/factubsio/BubblePrintsData/main/versions_RT.json", "RT");
            ParseWebJson(web, "https://raw.githubusercontent.com/factubsio/BubblePrintsData/main/versions_KM.json", "KM");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
        }

        foreach (var file in Directory.EnumerateFiles(BubblePrints.DataPath, "*.binz"))
        {
            BinzVersion v = VersionFromFile(file);
            if (ByVersion.TryGetValue(v, out var binz))
            {
                binz.Local = true;
                binz.Path = file;
            }
            else
            {
                binz = new()
                {
                    Local = true,
                    Path = file,
                    Version = v,
                    Source = "local",
                };
                PreSort.Insert(0, binz);
                ByVersion.Add(v, binz);
            }
        }
        PreSort.Sort((a, b) =>
        {
            var tmp = b.Version.Game.CompareTo(a.Version.Game);
            if (tmp == 0) return CompareTo(GetNumifiedVersion(a.Version.Version.ToString()), GetNumifiedVersion(b.Version.Version.ToString()));
            else return tmp;
        });
        PreSort.ForEach(i => Available.Insert(0, i));

    }

    private static int CompareTo(List<BigInteger> a, List<BigInteger> b)
    {
        int maxLen = Math.Max(a.Count, b.Count);
        for (int i = 0; i < maxLen; i++)
        {
            BigInteger t = (i < a.Count) ? a[i] : 0;
            BigInteger g = (i < b.Count) ? b[i] : 0;
            if (t > g)
            {
                return 1;
            }
            if (t < g)
            {
                return -1;
            }
        }
        return 0;
    }
    private static List<BigInteger> GetNumifiedVersion(string version)
    {
        var comps = version.Split('.');
        var newComps = new List<BigInteger>();
        foreach (var comp in comps)
        {
            BigInteger num = 0;
            foreach (var c in comp)
            {
                try
                {
                    if (uint.TryParse(c.ToString(), out var n))
                    {
                        num = num * 10u + n;
                    }
                    else
                    {
                        int signedCharNumber = char.ToUpper(c) - ' ';
                        uint unsignedCharNumber = (uint)Math.Max(0, Math.Min(signedCharNumber, 99));
                        num = num * 100u + unsignedCharNumber;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error while trying to numify version component {comp}, continuing with {num}.\n{ex}");
                    break;
                }
            }
            newComps.Add(num);
        }
        return newComps;
    }
    public void ParseWebJson(HttpClient web, string url, string game)
    {
        var raw = web.GetStringAsync(url).Result;
        var versions = JsonSerializer.Deserialize<JsonElement>(raw);

        foreach (var version in versions.EnumerateArray())
        {
            GameVersion gv = new()
            {
                Major = version[0].GetInt32(),
                Minor = version[1].GetInt32(),
                Patch = version[2].GetInt32(),
                Suffix = version[3].GetString(),
                Bubble = version[4].GetInt32(),
            };
            Binz binz = new()
            {
                Version = new()
                {
                    Version = gv,
                    Game = game,
                },
                Source = "bubbles",
            };
            PreSort.Insert(0, binz);
            ByVersion.Add(binz.Version, binz);
        }
    }
    public static BinzVersion VersionFromFile(string file)
    {
        Match match;
        string game = "Wrath";
        string fileName = Path.GetFileName(file);
        if (fileName.StartsWith("blueprints_raw_KM", StringComparison.InvariantCultureIgnoreCase))
        {
            match = extractVersionKM.Match(file);
            game = "KM";
        }
        else if (fileName.StartsWith("blueprints_raw_DH", StringComparison.InvariantCultureIgnoreCase))
        {
            match = extractVersionDH.Match(file);
            game = "DH";
        }
        else if (fileName.StartsWith("blueprints_raw_RT", StringComparison.InvariantCultureIgnoreCase))
        {
            match = extractVersionRT.Match(file);
            game = "RT";
        }
        else
        {
            match = extractVersion.Match(file);
        }

        GameVersion v = new()
        {
            Major = int.Parse(match.Groups[1].Value),
            Minor = int.Parse(match.Groups[2].Value),
            Patch = int.Parse(match.Groups[3].Value),
            Suffix = match.Groups[4].Value,
            Bubble = int.Parse(match.Groups[5].Value),
        };
        return new()
        {
            Version = v,
            Game = game,
        };
    }

    public async Task Download(Binz toLoad, Action<int> onProgress)
    {
        const string host = "https://github.com/factubsio/BubblePrintsData/releases/download";
        string filename = BlueprintDB.FileNameFor(toLoad.Version.Version, toLoad.Version.Game);
        Uri latestVersionUrl = null;
        if (toLoad.Version.Game == "Wrath") latestVersionUrl = new Uri($"{host}/{toLoad.Version.Version}/{filename}");
        else if (toLoad.Version.Game == "RT") latestVersionUrl = new Uri($"{host}/RT_{toLoad.Version.Version}/{filename}");
        else if (toLoad.Version.Game == "KM") latestVersionUrl = new Uri($"{host}/KM_{toLoad.Version.Version}/{filename}");
        using var client = new HttpClient();
        string tmp = Path.Combine(CacheDir, "binz_download.tmp");
        if (File.Exists(tmp)) File.Delete(tmp);

        toLoad.Path = Path.Combine(CacheDir, filename);
        using var response = await client.GetAsync(latestVersionUrl, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        await using var file = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None);
        var buffer = new byte[81920];
        int read;
        long totalRead = 0;
        var total = response.Content.Headers.ContentLength ?? -1;
        int lastPct = 0;
        while ((read = await stream.ReadAsync(buffer)) > 0)
        {
            await file.WriteAsync(buffer.AsMemory(0, read));
            totalRead += read;
            int pct = (int)((totalRead * 100) / total);
            if (pct > lastPct)
            {
                onProgress(pct);
                lastPct = pct;
            }
        }
        file.Close();

        if (File.Exists(toLoad.Path)) File.Delete(toLoad.Path);
        File.Move(tmp, toLoad.Path);

    }

    private static readonly Regex extractVersion = new(@"blueprints_raw_(\d+).(\d+)\.(\d+)(.*)_(\d).binz");
    private static readonly Regex extractVersionKM = new(@"blueprints_raw_KM_(\d+).(\d+)\.(\d+)(.)_(\d).binz");
    private static readonly Regex extractVersionRT = new(@"blueprints_raw_RT_(\d+).(\d+)\.(\d+)(\.\d+|.*)_(\d).binz");
    private static readonly Regex extractVersionDH = new(@"blueprints_raw_DH_(\d+).(\d+)\.(\d+)(\.\d+|.*)_(\d).binz");

    public IEnumerable<Binz> Local => Available.Where(x => x.Local);
}
public class BinzVersion : IEquatable<BinzVersion>
{
    public GameVersion Version;
    public string Game;

    public override bool Equals(object obj) => Equals(obj as BinzVersion);
    public bool Equals(BinzVersion other) => other is not null && EqualityComparer<GameVersion>.Default.Equals(Version, other.Version) && Game == other.Game;
    public override int GetHashCode() => HashCode.Combine(Version, Game);

    public static bool operator ==(BinzVersion left, BinzVersion right)
    {
        return EqualityComparer<BinzVersion>.Default.Equals(left, right);
    }

    public static bool operator !=(BinzVersion left, BinzVersion right)
    {
        return !(left == right);
    }

    public override string ToString() => $"{Game} - {Version}";

}

public class Binz : INotifyPropertyChanged
{
    public string Path;
    private bool local;

    public bool Local
    {
        get => local; set
        {
            local = value;
            PropertyChanged?.Invoke(this, new(nameof(Local)));
        }
    }
    public string Source { get; set; }
    public BinzVersion Version { get; set; }

    public event PropertyChangedEventHandler PropertyChanged;
}
