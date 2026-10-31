using SkuaScriptStudio.Services;
using Xunit;

namespace SkuaScriptStudio.Tests;

public sealed class ScriptSnippetServiceTests
{
    private readonly ScriptSnippetService _service = new();

    [Fact]
    public void GetSnippets_ReturnsBuiltInSkuaPatterns()
    {
        IReadOnlyList<Models.ScriptSnippet> snippets = _service.GetSnippets();

        Assert.Contains(snippets, snippet => snippet.Name == "Hunt monster");
        Assert.Contains(snippets, snippet => snippet.Name == "Equip class");
        Assert.Contains(snippets, snippet => snippet.Name == "Hunt monster quest");
    }

    [Fact]
    public void GetSnippets_ProvidesNonEmptyCodeAndDescriptions()
    {
        IReadOnlyList<Models.ScriptSnippet> snippets = _service.GetSnippets();

        Assert.NotEmpty(snippets);
        Assert.All(snippets, snippet =>
        {
            Assert.False(string.IsNullOrWhiteSpace(snippet.Description));
            Assert.False(string.IsNullOrWhiteSpace(snippet.Code));
        });
    }
}
