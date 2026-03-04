using ALaCarte.Core.Abstractions;

namespace ALaCarte.Core.Services;

public class FileSystemService : IFileSystem
{
    public bool DirectoryExists(string path) => Directory.Exists(path);

    public void CreateDirectory(string path) => Directory.CreateDirectory(path);

    public string[] GetFiles(string path, string searchPattern, SearchOption searchOption) =>
        Directory.GetFiles(path, searchPattern, searchOption);

    public string[] GetDirectories(string path) => Directory.GetDirectories(path);

    public bool FileExists(string path) => File.Exists(path);

    public Task<string> ReadAllTextAsync(string path, CancellationToken ct = default) =>
        File.ReadAllTextAsync(path, ct);

    public Task WriteAllTextAsync(string path, string contents, CancellationToken ct = default) =>
        File.WriteAllTextAsync(path, contents, ct);

    public void CopyFile(string source, string destination, bool overwrite) =>
        File.Copy(source, destination, overwrite);

    public string Combine(params string[] paths) => Path.Combine(paths);

    public string GetFullPath(string path) => Path.GetFullPath(path);

    public string GetFileName(string path) => Path.GetFileName(path);

    public string GetFileNameWithoutExtension(string path) => Path.GetFileNameWithoutExtension(path);

    public string? GetDirectoryName(string path) => Path.GetDirectoryName(path);

    public string GetRelativePath(string relativeTo, string path) => Path.GetRelativePath(relativeTo, path);

    public string GetCurrentDirectory() => Directory.GetCurrentDirectory();

    private static readonly string[] DefaultExcludedDirectories = ["obj", "bin", "node_modules", "dist"];

    public void CopyDirectory(string source, string destination, string[]? excludedDirectories = null)
    {
        var excluded = excludedDirectories ?? DefaultExcludedDirectories;

        Directory.CreateDirectory(destination);

        foreach (var file in Directory.GetFiles(source, "*", SearchOption.TopDirectoryOnly))
        {
            var destFile = Path.Combine(destination, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }

        foreach (var dir in Directory.GetDirectories(source))
        {
            var dirName = Path.GetFileName(dir);
            if (excluded.Contains(dirName))
                continue;

            CopyDirectory(dir, Path.Combine(destination, dirName), excludedDirectories);
        }
    }
}
