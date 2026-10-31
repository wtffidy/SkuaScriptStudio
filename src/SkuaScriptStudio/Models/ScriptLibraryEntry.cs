namespace SkuaScriptStudio.Models;

public sealed class ScriptLibraryEntry
{
    public string Name { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
    public DateTime LastModifiedUtc { get; init; }

    public override string ToString() => Name;
}
