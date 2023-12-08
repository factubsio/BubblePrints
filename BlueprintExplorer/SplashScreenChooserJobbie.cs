using BlueprintExplorer.Properties;
using Microsoft.VisualBasic.Devices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
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
        private readonly BindingList<Binz> Available = new();
        private readonly Dictionary<BinzVersion, Binz> ByVersion = new();

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
                        Version = new()
                        {
                            Version = gv,
                            Game = "Wrath",
                        },
                        Source = "bubbles",
                    };
                    Available.Insert(0, binz);
                    ByVersion.Add(binz.Version, binz);
                }
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
                    Available.Insert(0, binz);
                    ByVersion.Add(v, binz);
                }
            }


            versions.SelectionChanged += OnSelectedRowChanged;
            versions.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            versions.MultiSelect = false;
            versions.DataSource = Available;


            if (!string.IsNullOrEmpty(BubblePrints.Settings.LastLoaded))
            {
                BinzVersion v = VersionFromFile(BubblePrints.Settings.LastLoaded);
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

        private void OnSelectedRowChanged(object sender, EventArgs e)
        {
            if (!TryGetSelected(out var selected, out var _))
            {
                delete.Enabled = false;
            }

            delete.Enabled = selected.Local;
        }

        private static Regex extractVersion = new(@"blueprints_raw_(\d+).(\d+)\.(\d+)(.)_(\d).binz");
        private static Regex extractVersionKM = new(@"blueprints_raw_km_(\d+).(\d+)\.(\d+)(.)_(\d).binz");
        private static Regex extractVersionRT = new(@"blueprints_raw_RT_(\d+).(\d+)\.(\d+)(.)_(\d).binz");

        private static BinzVersion VersionFromFile(string file)
        {
            Match match;
            string game = "Wrath";
            string fileName = Path.GetFileName(file);
            if (fileName.StartsWith("blueprints_raw_km"))
            {
                match = extractVersionKM.Match(file);
                game = "Kingmaker";
            }
            else if (fileName.StartsWith("blueprints_raw_RT"))
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
                Suffix = match.Groups[4].Value[0],
                Bubble = int.Parse(match.Groups[5].Value),
            };
            return new()
            {
                Version = v,
                Game = game,
            };
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

        private void ShowMain(Binz binz)
        {
            this.Hide();
            Form1 main = new()
            {
                Splash = this,
                Text = "BubblePrints - " + binz.Version.ToString()
            };
            main.Show();
        }

        private bool TryGetSelected(out Binz selected, out int index)
        {
            index = versions.SelectedRow();
            if (index < 0 || index >= Available.Count)
            {
                selected = null;
                return false;
            }
            else
            {
                selected = Available[index];
                return true;
            }
        }


        private async void DoLoadSelected(object sender, EventArgs e)
        {
            if (!TryGetSelected(out var toLoad, out var _))
            {
                return;
            }

            ShowLoadAnimation();

            BubblePrints.Game_Data = toLoad.Version.Game switch
            {
                "Wrath" => "Wrath_Data",
                "Kingmaker" => "Kingmaker_Data",
                "RT" => "WH40KRT_Data",
                _ => throw new NotSupportedException(),
            };

            if (!toLoad.Local)
            {
                if (toLoad.Version.Game != "Wrath")
                {
                    throw new Exception("Can only auto-download wrath binz");
                }

                loadAnim.Image = Resources.downloading;
                loadAnim.ShowProgressBar = true;
                loadAnim.Caption = "Downloading";
                const string host = "https://github.com/factubsio/BubblePrintsData/releases/download";
                string filename = BlueprintDB.FileNameFor(toLoad.Version.Version, toLoad.Version.Game);
                var latestVersionUrl = new Uri($"{host}/{toLoad.Version.Version}/{filename}");

                var client = new WebClient();

                string tmp = Path.Combine(CacheDir, "binz_download.tmp");

                if (File.Exists(tmp))
                    File.Delete(tmp);

                toLoad.Path = Path.Combine(CacheDir, filename);
                client.DownloadProgressChanged += (sender, e) => loadAnim.SetPercentSafe(e.ProgressPercentage);
                var download = client.DownloadFileTaskAsync(latestVersionUrl, tmp);
                await download;
                File.Move(tmp, toLoad.Path);
            }


            loadAnim.Image = Resources.hackerman;
            loadAnim.ShowProgressBar = false;
            loadAnim.Caption = "Loading";

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

            ShowMain(toLoad);
        }

 
        private async void DoImportFromGame(object sender, EventArgs e)
        {
            BubblePrints.SetWrathPath(true);
            if (!BubblePrints.TryGetWrathPath(out var wrathPath))
            {
                return;
            }

            var version = BubblePrints.GetGameVersion(wrathPath);
            var filename = BlueprintDB.FileNameFor(version, BubblePrints.CurrentGame);
            while (File.Exists(Path.Join(BubblePrints.DataPath, filename)))
            {
                version.Bubble++;
                filename = BlueprintDB.FileNameFor(version, BubblePrints.CurrentGame);
            }

            Console.WriteLine(version);

            if (version.Bubble > 0) {
                var result = MessageBox.Show("Do you want to overwrite this file?\n" +
                    $"    Yes: Overwrite - {version with {Bubble = version.Bubble - 1}}\n" +
                    $"    No: Extract but increment the version - {version}\n" +
                    $"    Cancel: Do nothing", "File already exists", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation);
                if (result == DialogResult.Cancel)
                {
                    return;
                }
                else if (result == DialogResult.Yes)
                {
                    version.Bubble = version.Bubble - 1;
                    filename = BlueprintDB.FileNameFor(version, BubblePrints.CurrentGame);
                }
            }

            ShowLoadAnimation();
            loadAnim.Image = Resources.extracting;
            loadAnim.ShowProgressBar = true;
            loadAnim.Caption = "Extracting";

            try
            {
                ConnectionProgress progress = new();
                var path = Path.Join(BubblePrints.DataPath, filename);
                var extract = Task.Run(() => BlueprintDB.Instance.ExtractFromGame(progress, wrathPath, path, version));
                var idle = Task.Run(() =>
                {
                    while (!extract.IsCompleted)
                    {
                        Thread.Sleep(60);
                        loadAnim.Caption = progress.Phase;
                        if (progress.EstimatedTotal == 0)
                        {
                            loadAnim.SetPercentSafe(0);
                        }
                        else
                        {
                            loadAnim.SetPercentSafe((100 * progress.Current) / progress.EstimatedTotal);
                        }
                    }
                });
                await extract;
                await idle;
                Binz binz = new()
                {
                    Local = true,
                    Version = new()
                    {
                        Version = version,
                        Game = BubblePrints.CurrentGame,
                    },
                    Path = path,
                    Source = "local",
                };
                Available.Add(binz);
                ByVersion[binz.Version] = binz;
                ShowMain(binz);
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Error extracting blueprints", ex.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }

        private async void DoDeleteSelected(object sender, EventArgs e)
        {
            if (!TryGetSelected(out var toDelete, out int index))
            {
                return;
            }

            if (toDelete.Source == "local")
            {
                if (MessageBox.Show("Are you sure you want to delete this locally-created file?",
                    "Locally-created!",
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation) != DialogResult.OK)
                {
                    return;
                }
            }

            File.Delete(toDelete.Path);
            if (toDelete.Source != "bubbles")
            {
                ByVersion.Remove(toDelete.Version);
                versions.Rows.RemoveAt(index);
            }
            else
            {
                toDelete.Local = false;
            }
        }
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
}
