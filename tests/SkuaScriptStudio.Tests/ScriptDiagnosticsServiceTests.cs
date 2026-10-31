using SkuaScriptStudio.Models;
using SkuaScriptStudio.Services;
using Xunit;

namespace SkuaScriptStudio.Tests;

public sealed class ScriptDiagnosticsServiceTests
{
    private readonly ScriptDiagnosticsService _service = new();

    [Fact]
    public void Analyze_ReturnsNoIssuesForValidSyntax()
    {
        IReadOnlyList<ScriptDiagnostic> diagnostics = _service.Analyze("public class TestScript { public void Run() { } }");

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Analyze_ReportsSyntaxErrorsWithLineAndColumn()
    {
        IReadOnlyList<ScriptDiagnostic> diagnostics = _service.Analyze("public class TestScript\n{\n    public void Run(\n}");

        Assert.Contains(diagnostics, diagnostic =>
            diagnostic.Severity == "Error" &&
            diagnostic.Id.StartsWith("CS", StringComparison.Ordinal) &&
            diagnostic.Line > 0 &&
            diagnostic.Column > 0);
    }

    [Fact]
    public void Analyze_ReportsWarnings()
    {
        IReadOnlyList<ScriptDiagnostic> diagnostics = _service.Analyze("#warning Review this script");

        ScriptDiagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("Warning", diagnostic.Severity);
    }

    [Fact]
    public async Task AnalyzeAsync_ReturnsDiagnostics()
    {
        IReadOnlyList<ScriptDiagnostic> diagnostics = await _service.AnalyzeAsync("#warning Review this script");

        Assert.Single(diagnostics);
        Assert.Equal("Warning", diagnostics[0].Severity);
    }

    [Fact]
    public async Task AnalyzeAsync_HonorsCancellation()
    {
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _service.AnalyzeAsync("public class TestScript {}", cancellation.Token));
    }

    [Fact]
    public void Analyze_RejectsNullSource()
    {
        Assert.Throws<ArgumentNullException>(() => _service.Analyze(null!));
    }
}
