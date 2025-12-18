using System.Windows.Forms;

namespace BlueprintExplorer;

public class FormsFolderChooser : IFolderChooser
{
    private FolderBrowserDialog folderBrowser;
    private int attempt = 0;

    public bool Choose(string exe, out string choice)
    {
        choice = null;

        if (attempt > 0)
            folderBrowser.Description = $"Could not find {exe} at the selected folder";
        else
            folderBrowser.Description = $"Please select the the folder containing {exe}";

        attempt++;

        if (folderBrowser.ShowDialog() != DialogResult.OK)
            return false;

        choice = folderBrowser.SelectedPath;
        return true;
    }

    public void Prepare()
    {
        folderBrowser = new()
        {
            UseDescriptionForTitle = true
        };
    }
}
