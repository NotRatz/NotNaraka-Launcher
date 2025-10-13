using System.IO;

namespace Launcher.Shared.Storage;

public class FileSystem : IFileSystem
{
    public void EnsureDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new DirectoryNotFoundException("Path must not be null or whitespace.");
        }

        Directory.CreateDirectory(path);
    }

    public bool FileExists(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        return File.Exists(path);
    }

    public string ReadAllText(string path)
    {
        return File.ReadAllText(path);
    }

    public void WriteAllText(string path, string contents)
    {
        File.WriteAllText(path, contents);
    }
}
