using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BlueprintExplorer
{
    public partial class SettingsView : Form
    {
        public SettingsView()
        {
            InitializeComponent();

            Form1.DarkenPropertyGrid(settingsPropView);
            Form1.DarkenControls(deleteBinz, deleteEditorCache, formSave, formCancel, cacheControlButtons, formActionButtons);
            settingsPropView.SelectedObject = new SettingsProxy();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void deleteEditorCache_Click(object sender, EventArgs e)
        {
            var userLocalFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BubblePrints", "cache");
            int failCount = 0;
            if (Directory.Exists(userLocalFolder))
            {
                foreach (var path in Directory.EnumerateFiles(userLocalFolder))
                {
                    try
                    {
                        File.Delete(path);
                    }
                    catch (Exception) {
                        failCount++;
                    }
                }
            }

            if (failCount > 0)
            {
                MessageBox.Show($"Failed to delete {failCount} editorcache files, they are probably open in another program", "Could not delete all files", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                MessageBox.Show($"Cleaned editor cache", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void deleteBinz_Click(object sender, EventArgs e)
        {
            var userLocalFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BubblePrints");
            int count = 0;
            int fail = 0;
            foreach (var path in Directory.EnumerateFiles(userLocalFolder))
            {
                var file = Path.GetFileName(path);
                if (file.EndsWith(".binz") && file != BlueprintDB.Instance.FileName)
                {
                    try
                    {
                        count++;
                        File.Delete(path);
                    }
                    catch (Exception) { fail++; }
                }
            }
            if (fail == 0)
                MessageBox.Show($"Cleaned {count} old binz files", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            else
                MessageBox.Show($"Tried to clean {count} old binz files, {fail} could not be deleted", "Could not delete all files", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void formSave_Click(object sender, EventArgs e)
        {
            (settingsPropView.SelectedObject as SettingsProxy).Sync();
            Properties.Settings.Default.Save();
            Close();
        }

        private void formCancel_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
