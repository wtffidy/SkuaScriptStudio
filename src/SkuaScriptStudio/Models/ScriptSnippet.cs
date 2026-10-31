namespace SkuaScriptStudio.Models;

/// <summary>
/// Represents a reusable C# snippet for common Skua script operations.
/// </summary>
public sealed record ScriptSnippet(string Name, string Description, string Code)
{
    /// <summary>
    /// Returns the snippet name for list presentation.
    /// </summary>
    public override string ToString() => Name;
}
