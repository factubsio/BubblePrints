using BlueprintExplorer.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static BlueprintExplorer.BlueprintDB;

namespace BlueprintExplorer
{
    public partial class SplashScreenChooserJobbie : Form
    {
        List<Binz> Available = new();
        Dictionary<GameVersion, Binz> ByVersion = new();
        public SplashScreenChooserJobbie()
        {
            InitializeComponent();
            loadAnim = new();

            try
            {
                if (!Directory.Exists(CacheDir))
                    Directory.CreateDirectory(CacheDir);

                Console.WriteLine("setting available = from web");
                using var web = new WebClient();

                var raw = web.DownloadString("https://raw.githubusercontent.com/factubsio/BubblePrintsData/main/versions.json");
                var versions = JsonSerializer.Deserialize<JsonElement>(raw);

                foreach (var version in versions.EnumerateArray())
                {
                    GameVersion gv = new()
                    {
                        Major = version[0].GetInt32(),
                        Minor = version[1].GetInt32(),
                        Patch = version[2].GetInt32(),
                        Suffix = version[3].GetString()[0],
                        Bubble = version[4].GetInt32(),
                    };
                    Binz binz = new()
                    {
                        Version = gv,
                    };
                    Available.Add(binz);
                    ByVersion.Add(gv, binz);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }


            foreach (var file in Directory.EnumerateFiles(BubblePrints.DataPath, "*.binz"))
            {
                GameVersion v = VersionFromFile(file);
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
                    };
                    Available.Add(binz);
                    ByVersion.Add(v, binz);
                }
            }

            Available.Reverse();
            versions.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            versions.MultiSelect = false;
            versions.DataSource = Available;

            if (!string.IsNullOrEmpty(BubblePrints.Settings.LastLoaded))
            {
                GameVersion v = VersionFromFile(BubblePrints.Settings.LastLoaded);
                for (int i = 0; i < Available.Count; i++)
                {
                    if (v.Equals(Available[i].Version))
                    {
                        versions.ClearSelection();
                        versions.Rows[i].Selected = true;
                        versions.CurrentCell = versions.Rows[i].Cells[0];
                        break;
                    }
                }
            }
        }

        private static Regex extractVersion = new(@"blueprints_raw_(\d+).(\d+)\.(\d+)(.)_(\d).binz");
        private static GameVersion VersionFromFile(string file)
        {
            var match = extractVersion.Match(file);
            GameVersion v = new()
            {
                Major = int.Parse(match.Groups[1].Value),
                Minor = int.Parse(match.Groups[2].Value),
                Patch = int.Parse(match.Groups[3].Value),
                Suffix = match.Groups[4].Value[0],
                Bubble = int.Parse(match.Groups[5].Value),
            };
            return v;
        }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        readonly AnimatedImageBox loadAnim;

        private void ShowLoadAnimation()
        {
            load.Enabled = false;
            import.Enabled = false;
            versions.Parent.Controls.Remove(versions);
            mainLayout.Controls.Add(loadAnim, 0, 1);
            loadAnim.Dock = DockStyle.Fill;
        }

        private async void DoLoadSelected(object sender, EventArgs e)
        {
            int index = versions.SelectedRow();
            if (index < 0 || index >= Available.Count)
            {
                return;
            }

            ShowLoadAnimation();

            Binz toLoad = Available[index];
            if (!toLoad.Local)
            {
                loadAnim.Image = Resources.downloading;
                const string host = "https://github.com/factubsio/BubblePrintsData/releases/download";
                string filename = BlueprintDB.FileNameFor(toLoad.Version);
                var latestVersionUrl = new Uri($"{host}/{toLoad.Version}/{filename}");

                var client = new WebClient();

                string tmp = Path.Combine(CacheDir, "binz_download.tmp");

                if (File.Exists(tmp))
                    File.Delete(tmp);

                toLoad.Path = Path.Combine(CacheDir, filename);
                client.DownloadProgressChanged += (sender, e) => loadAnim.Invoke(new Action(() => loadAnim.Percent = e.ProgressPercentage));
                var download = client.DownloadFileTaskAsync(latestVersionUrl, tmp);
                await download;
                File.Move(tmp, toLoad.Path);
            }

            loadAnim.Image = Resources.hackerman;

            var loadProgress = new BlueprintDB.ConnectionProgress();
            var initialize = Task.Run(() => BlueprintDB.Instance.TryConnect(loadProgress, toLoad.Path));
            var idle = Task.Run(() =>
            {
                while (!initialize.IsCompleted)
                {
                    Thread.Sleep(60);
                    loadAnim.IncrementSafe();
                }
            });
            await initialize;
            await idle;
            this.Hide();
            Form1 main = new()
            {
                Splash = this,
                Text = "BubblePrints - " + toLoad.Version.ToString()
            };
            main.Show();
        }

        private void DoImportFromGame(object sender, EventArgs e)
        {

        }
    }

    public class Binz
    {
        public string Path;
        public bool Local { get; set; }
        public GameVersion Version { get; set; }
    }
}
