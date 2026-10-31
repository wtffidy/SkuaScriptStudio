using SkuaScriptStudio.Services;
using Xunit;

namespace SkuaScriptStudio.Tests;

public sealed class ScriptDiagnosticsServiceTests
{
    private readonly ScriptDiagnosticsService _service = new();

    [Fact]
    public void Analyze_ReturnsNoIssuesForValidSyntax()
    {
        IReadOnlyList<string> diagnostics = _service.Analyze("public class TestScript { public void Run() { } }");

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Analyze_ReportsSyntaxErrorsWithLineAndColumn()
    {
        IReadOnlyList<string> diagnostics = _service.Analyze("public class TestScript\n{\n    public void Run(\n}");

        Assert.NotEmpty(diagnostics);
        Assert.Contains(diagnostics, diagnostic =>
            diagnostic.StartsWith("Error · Line ", StringComparison.Ordinal) &&
            diagnostic.Contains("Column ", StringComparison.Ordinal));
    }

    [Fact]
    public void Analyze_RejectsNullSource()
    {
        Assert.Throws<ArgumentNullException>(() => _service.Analyze(null!));
    }
}
