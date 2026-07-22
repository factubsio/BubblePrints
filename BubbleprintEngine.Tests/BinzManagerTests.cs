using BlueprintExplorer;

namespace BubbleprintEngine.Tests;

public class BinzManagerTests
{
    [Theory]
    [InlineData(
        "blueprints_raw_RT_1.6.1.514_0.binz",
        "RT",
        "514")]
    [InlineData(
        "blueprints_raw_DH_1.2.0.123_0.binz",
        "DH",
        "123")]
    [InlineData(
        "blueprints_raw_KM_2.1.7b_2.binz",
        "KM",
        "b")]
    public void VersionFromFile_NormalizesSuffixForManifestEquality(
        string filename,
        string game,
        string suffix)
    {
        var version = BinzManager.VersionFromFile(filename);

        Assert.Equal(game, version.Game);
        Assert.Equal(suffix, version.Version.Suffix);
        Assert.Equal(
            filename,
            BlueprintDB.FileNameFor(version.Version, version.Game));
    }
}
