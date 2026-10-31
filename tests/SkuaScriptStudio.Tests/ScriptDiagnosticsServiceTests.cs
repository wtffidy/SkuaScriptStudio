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

        ScriptDiagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("Error", diagnostic.Severity);
        Assert.StartsWith("CS", diagnostic.Id, StringComparison.Ordinal);
        Assert.True(diagnostic.Line > 0);
        Assert.True(diagnostic.Column > 0);
    }

    [Fact]
    public void Analyze_ReportsWarnings()
    {
        IReadOnlyList<ScriptDiagnostic> diagnostics = _service.Analyze("#warning Review this script");

        ScriptDiagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("Warning", diagnostic.Severity);
    }

    [Fact]
    public void Analyze_RejectsNullSource()
    {
        Assert.Throws<ArgumentNullException>(() => _service.Analyze(null!));
    }
}
