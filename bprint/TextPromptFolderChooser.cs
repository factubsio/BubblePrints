using BlueprintExplorer;

namespace bprint;

internal class TextPromptFolderChooser : IFolderChooser
{
    public bool Choose(string exe, out string choice)
    {
        Console.Write($"folder where {exe} is located> ");
        choice = "";
        var line = Console.ReadLine();
        if (line == null)
            return false;

        choice = line;
        return true;
    }

    public void Prepare() { }
}

internal class FixedGamePath(string path) : IFolderChooser
{
    int attempts = 0;
    public bool Choose(string exe, out string choice)
    {
        choice = path;
        return attempts++ == 0;
    }
    public void Prepare() { }
}