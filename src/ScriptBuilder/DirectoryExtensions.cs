using System.IO;

static class DirectoryExtensions
{
    public static void Delete(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }
        Directory.Delete(path, true);
    }

    public static void DuplicateDirectory(string source, string destination)
    {
        foreach (var dirPath in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(dirPath.Replace(source, destination));
        }

        foreach (var newPath in Directory.GetFiles(source, "*.*", SearchOption.AllDirectories))
        {
            File.Copy(newPath, newPath.Replace(source, destination), true);
        }
    }

}