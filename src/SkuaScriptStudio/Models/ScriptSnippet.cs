namespace SkuaScriptStudio.Models;

/// <summary>
/// Represents a reusable C# snippet and its core metadata.
/// </summary>
public sealed record ScriptSnippet(string Name, string Description, string Code, string Core, string Category, string[] RequiredIncludes)
{
    /// <summary>
    /// Returns the snippet name for list presentation.
    /// </summary>
    public override string ToString() => Name;
}
