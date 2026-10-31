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
    public IReadOnlyList<ScriptDiagnostic> Analyze(string source, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        cancellationToken.ThrowIfCancellationRequested();

        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, cancellationToken: cancellationToken);
        List<ScriptDiagnostic> diagnostics = [];

        foreach (Diagnostic diagnostic in syntaxTree.GetDiagnostics(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (diagnostic.Severity is not (DiagnosticSeverity.Error or DiagnosticSeverity.Warning))
                continue;

            FileLinePositionSpan span = diagnostic.Location.GetLineSpan();
            diagnostics.Add(new ScriptDiagnostic(
                diagnostic.Severity.ToString(),
                diagnostic.Id,
                diagnostic.GetMessage(),
                span.StartLinePosition.Line + 1,
                span.StartLinePosition.Character + 1));
        }

        return diagnostics;
    }

    /// <summary>
    /// Runs syntax analysis on a worker thread so large scripts do not freeze the UI.
    /// </summary>
    public Task<IReadOnlyList<ScriptDiagnostic>> AnalyzeAsync(string source, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        return Task.Run(() => Analyze(source, cancellationToken), cancellationToken);
    }
}
