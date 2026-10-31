namespace SkuaScriptStudio.Models;

/// <summary>
/// Represents a C# diagnostic with its source location.
/// </summary>
public sealed record ScriptDiagnostic(string Severity, string Id, string Message, int Line, int Column)
{
    /// <summary>
    /// Returns a readable diagnostic summary for the diagnostics list.
    /// </summary>
    public override string ToString() => $"{Severity} · Line {Line}, Column {Column} · {Message} ({Id})";
}
