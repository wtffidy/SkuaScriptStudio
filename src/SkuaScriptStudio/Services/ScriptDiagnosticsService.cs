using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SkuaScriptStudio.Models;

namespace SkuaScriptStudio.Services;

/// <summary>
/// Provides Roslyn-backed C# syntax diagnostics for editor documents.
/// </summary>
public sealed class ScriptDiagnosticsService
{
    /// <summary>
    /// Parses source code and returns syntax errors and warnings with source locations.
    /// </summary>
    public IReadOnlyList<ScriptDiagnostic> Analyze(string source)
    {
        ArgumentNullException.ThrowIfNull(source);

        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);
        return syntaxTree.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning)
            .OrderBy(diagnostic => diagnostic.Location.SourceSpan.Start)
            .Select(diagnostic => CreateDiagnostic(diagnostic))
            .ToArray();
    }

    private static ScriptDiagnostic CreateDiagnostic(Diagnostic diagnostic)
    {
        FileLinePositionSpan span = diagnostic.Location.GetLineSpan();
        int line = span.StartLinePosition.Line + 1;
        int column = span.StartLinePosition.Character + 1;

        return new ScriptDiagnostic(diagnostic.Severity.ToString(), diagnostic.Id, diagnostic.GetMessage(), line, column);
    }
}
