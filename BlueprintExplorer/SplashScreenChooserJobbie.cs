using BinzFactory;
using BlueprintExplorer.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static BlueprintExplorer.BlueprintDB;

namespace BlueprintExplorer;

public partial class SplashScreenChooserJobbie : Form
{
    private BinzManager binMan;
    public SplashScreenChooserJobbie()
    {
        InitializeComponent();
        loadAnim = new();

        binMan = new();

        versions.SelectionChanged += OnSelectedRowChanged;
        versions.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        versions.MultiSelect = false;
        versions.DataSource = binMan.Available;

        if (!string.IsNullOrEmpty(BubblePrints.Settings.LastLoaded))
        {
            BinzVersion v = BinzManager.VersionFromFile(BubblePrints.Settings.LastLoaded);
            for (int i = 0; i < binMan.Available.Count; i++)
            {
                if (v.Equals(binMan.Available[i].Version))
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
            Text = $"BubblePrints - {binz.Version}"
        };
        main.Show();
    }

    private bool TryGetSelected(out Binz selected, out int index)
    {
        index = versions.SelectedRow();
        if (index < 0 || index >= binMan.Available.Count)
        {
            selected = null;
            return false;
        }
        else
        {
            selected = binMan.Available[index];
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

        if (!toLoad.Local)
        {
            if (toLoad.Version.Game != "KM" && toLoad.Version.Game != "Wrath" && toLoad.Version.Game != "RT")
            {
                throw new Exception("Can only auto-download km, wrath and rt binz");
            }

            loadAnim.Image = Resources.downloading;
            loadAnim.ShowProgressBar = true;
            loadAnim.Caption = "Downloading";

            await binMan.Download(toLoad, pct => loadAnim.SetPercentSafe(pct));
        }


        loadAnim.Image = Resources.hackerman;
        loadAnim.ShowProgressBar = false;
        loadAnim.Caption = "Loading";

        var loadProgress = new ConnectionProgress();
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
        var queryPath = new FormsFolderChooser();
        queryPath.Prepare();

        if (!queryPath.Choose("Game.exe", out string gamePath))
            return;

        var version = BinzImporter.GetGameVersion(gamePath);
        var gameName = BinzImporter.GetGameName(gamePath);
        var filename = BlueprintDB.FileNameFor(version, gameName);
        while (File.Exists(Path.Join(BubblePrints.DataPath, filename)))
        {
            version.Bubble++;
            filename = BlueprintDB.FileNameFor(version, gameName);
        }

        Console.WriteLine(version);

        if (version.Bubble > 0)
        {
            var result = MessageBox.Show("Do you want to overwrite this file?\n" +
                $"    Yes: Overwrite - {version with { Bubble = version.Bubble - 1 }}\n" +
                $"    No: Extract but increment the version - {version}\n" +
                $"    Cancel: Do nothing", "File already exists", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation);
            if (result == DialogResult.Cancel)
            {
                return;
            }
            else if (result == DialogResult.Yes)
            {
                version.Bubble--;
                filename = BlueprintDB.FileNameFor(version, gameName);
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
            var extract = Task.Run(() => BinzImporter.Import(progress, gamePath, path, version));
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
            BlueprintDB.SetInstance(await extract);
            await idle;
            Binz binz = new()
            {
                Local = true,
                Version = new()
                {
                    Version = version,
                    Game = gameName,
                },
                Path = path,
                Source = "local",
            };
            binMan.Available.Add(binz);
            binMan.ByVersion[binz.Version] = binz;
            ShowMain(binz);
        }
        catch (Exception)
        {
            //MessageBox.Show("Error extracting blueprints", ex.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);
            throw;
        }
    }

    private void DoDeleteSelected(object sender, EventArgs e)
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
            binMan.ByVersion.Remove(toDelete.Version);
            versions.Rows.RemoveAt(index);
        }
        else
        {
            toDelete.Local = false;
        }
    }
}

