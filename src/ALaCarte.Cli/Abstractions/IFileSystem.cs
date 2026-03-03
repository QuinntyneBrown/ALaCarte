namespace ALaCarte.Cli.Abstractions;

public interface IFileSystem
{
    bool DirectoryExists(string path);
    void CreateDirectory(string path);
    string[] GetFiles(string path, string searchPattern, SearchOption searchOption);
    string[] GetDirectories(string path);
    bool FileExists(string path);
    Task<string> ReadAllTextAsync(string path, CancellationToken ct = default);
    Task WriteAllTextAsync(string path, string contents, CancellationToken ct = default);
    void CopyFile(string source, string destination, bool overwrite);
    string Combine(params string[] paths);
    string GetFullPath(string path);
    string GetFileName(string path);
    string GetFileNameWithoutExtension(string path);
    string? GetDirectoryName(string path);
    string GetRelativePath(string relativeTo, string path);
    string GetCurrentDirectory();
}
