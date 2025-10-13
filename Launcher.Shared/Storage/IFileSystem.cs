namespace Launcher.Shared.Storage;

public interface IFileSystem
{
    void EnsureDirectory(string path);

    bool FileExists(string path);

    string ReadAllText(string path);

    void WriteAllText(string path, string contents);
}
