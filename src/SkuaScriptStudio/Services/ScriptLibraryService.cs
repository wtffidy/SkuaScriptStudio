using SkuaScriptStudio.Models;

namespace SkuaScriptStudio.Services;

/// <summary>
/// Manages the user's local, application-owned script library.
/// </summary>
public sealed class ScriptLibraryService
{
    private readonly string _libraryDirectory;

    public ScriptLibraryService(string? libraryDirectory = null)
    {
        _libraryDirectory = libraryDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SkuaScriptStudio",
            "Library");
    }

    /// <summary>
    /// Imports a script into the local library and returns its stored entry.
    /// </summary>
    public async Task<ScriptLibraryEntry> ImportAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);

        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("The selected script could not be found.", sourcePath);

        if (!string.Equals(Path.GetExtension(sourcePath), ".cs", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Only C# script files (.cs) can be imported.", nameof(sourcePath));

        Directory.CreateDirectory(_libraryDirectory);

        string fileName = Path.GetFileName(sourcePath);
        string destinationPath = Path.Combine(_libraryDirectory, fileName);

        StringComparison pathComparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        if (string.Equals(Path.GetFullPath(sourcePath), Path.GetFullPath(destinationPath), pathComparison))
            return CreateEntry(destinationPath);

        string name = Path.GetFileNameWithoutExtension(fileName);
        string extension = Path.GetExtension(fileName);
        int suffix = 1;

        await using FileStream source = new(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            destinationPath = Path.Combine(
                _libraryDirectory,
                suffix == 1 ? fileName : $"{name} ({suffix}){extension}");

            FileStream destination;
            try
            {
                destination = new FileStream(destinationPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
            }
            catch (IOException) when (File.Exists(destinationPath))
            {
                suffix++;
                continue;
            }

            try
            {
                await using (destination)
                {
                    source.Position = 0;
                    await source.CopyToAsync(destination, cancellationToken);
                }
            }
            catch
            {
                try
                {
                    File.Delete(destinationPath);
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }

                throw;
            }

            return CreateEntry(destinationPath);
        }
    }

    /// <summary>
    /// Returns all C# scripts in the library, ordered by most recently modified.
    /// </summary>
    public IReadOnlyList<ScriptLibraryEntry> GetEntries()
    {
        if (!Directory.Exists(_libraryDirectory))
            return [];

        return Directory.EnumerateFiles(_libraryDirectory, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(CreateEntry)
            .OrderByDescending(entry => entry.LastModifiedUtc)
            .ToArray();
    }

    /// <summary>
    /// Enumerates library entries on a worker thread to keep filesystem access off the UI thread.
    /// </summary>
    public Task<IReadOnlyList<ScriptLibraryEntry>> GetEntriesAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run<IReadOnlyList<ScriptLibraryEntry>>(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!Directory.Exists(_libraryDirectory))
                return [];

            List<ScriptLibraryEntry> entries = [];
            foreach (string path in Directory.EnumerateFiles(_libraryDirectory, "*.cs", SearchOption.TopDirectoryOnly))
            {
                cancellationToken.ThrowIfCancellationRequested();
                entries.Add(CreateEntry(path));
            }

            return entries.OrderByDescending(entry => entry.LastModifiedUtc).ToArray();
        }, cancellationToken);
    }

    /// <summary>
    /// Removes a script from the application-owned library.
    /// </summary>
    public void Remove(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        string fullPath = Path.GetFullPath(filePath);
        string fullLibraryPath = Path.GetFullPath(_libraryDirectory);
        string relativePath = Path.GetRelativePath(fullLibraryPath, fullPath);
        StringComparison pathComparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        if (relativePath == "." || relativePath == ".." || relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", pathComparison) || Path.IsPathRooted(relativePath))
            throw new InvalidOperationException("Only scripts inside the local library can be removed.");

        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }

    private static ScriptLibraryEntry CreateEntry(string path) => new()
    {
        Name = Path.GetFileName(path),
        FilePath = path,
        LastModifiedUtc = File.GetLastWriteTimeUtc(path)
    };
}
