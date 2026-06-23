namespace mTiles.ViewModels;

public interface IFileContent
{
    void RenameFile(string newName);

    static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(name.Where(c => !invalid.Contains(c))).Trim();
    }
}
