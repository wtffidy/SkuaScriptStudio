namespace SkuaScriptStudio.Models;

public sealed class ScriptDocument
{
    public string Name { get; set; } = "Untitled.cs";
    public string? FilePath { get; set; }
    public string Source { get; set; } = string.Empty;
    public DateTime LastModifiedUtc { get; set; } = DateTime.UtcNow;
    public bool IsDirty { get; set; }
}
