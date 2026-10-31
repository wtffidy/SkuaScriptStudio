using SkuaScriptStudio.Models;

namespace SkuaScriptStudio.Services;

/// <summary>
/// Reads and writes script documents without blocking the UI thread.
/// </summary>
public sealed class ScriptFileService
{
    /// <summary>
    /// Opens a script from disk.
    /// </summary>
    public async Task<ScriptDocument> OpenAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string fullPath = Path.GetFullPath(path);
        string source = await File.ReadAllTextAsync(fullPath, cancellationToken).ConfigureAwait(false);

        return new ScriptDocument
        {
            Name = Path.GetFileName(fullPath),
            FilePath = fullPath,
            Source = source,
            LastModifiedUtc = File.GetLastWriteTimeUtc(fullPath),
            IsDirty = false
        };
    }

    /// <summary>
    /// Saves a consistent source snapshot using an atomic replacement.
    /// Changes made in the editor while the write is in progress remain marked as unsaved.
    /// </summary>
    public async Task SaveAsync(ScriptDocument document, string path, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string fullPath = Path.GetFullPath(path);
        string? directory = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrWhiteSpace(directory))
            throw new InvalidOperationException("The destination directory could not be determined.");

        Directory.CreateDirectory(directory);

        string sourceSnapshot = document.Source;
        string temporaryPath = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            await File.WriteAllTextAsync(temporaryPath, sourceSnapshot, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, fullPath, true);

            document.Name = Path.GetFileName(fullPath);
            document.FilePath = fullPath;
            document.LastModifiedUtc = File.GetLastWriteTimeUtc(fullPath);
            document.IsDirty = !string.Equals(document.Source, sourceSnapshot, StringComparison.Ordinal);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }
}
