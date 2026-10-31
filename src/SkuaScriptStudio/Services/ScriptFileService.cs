using SkuaScriptStudio.Models;

namespace SkuaScriptStudio.Services;

public sealed class ScriptFileService
{
    public async Task<ScriptDocument> OpenAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string source = await File.ReadAllTextAsync(path, cancellationToken);

        return new ScriptDocument
        {
            Name = Path.GetFileName(path),
            FilePath = path,
            Source = source,
            LastModifiedUtc = File.GetLastWriteTimeUtc(path),
            IsDirty = false
        };
    }

    public async Task SaveAsync(ScriptDocument document, string path, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        await File.WriteAllTextAsync(path, document.Source, cancellationToken);
        document.Name = Path.GetFileName(path);
        document.FilePath = path;
        document.LastModifiedUtc = DateTime.UtcNow;
        document.IsDirty = false;
    }
}
