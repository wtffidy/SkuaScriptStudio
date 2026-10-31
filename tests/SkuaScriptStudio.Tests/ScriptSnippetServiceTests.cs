using SkuaScriptStudio.Models;
using SkuaScriptStudio.Services;
using Xunit;

namespace SkuaScriptStudio.Tests;

public sealed class ScriptSnippetServiceTests
{
    private readonly ScriptSnippetService _service = new();

    [Fact]
    public void GetSnippets_ContainsAllSupportedCores()
    {
        string[] cores = _service.GetSnippets().Select(snippet => snippet.Core).Distinct().OrderBy(core => core).ToArray();

        Assert.Equal(new[] { "CoreAdvanced", "CoreBots", "CoreDailies", "CoreFarms" }, cores);
    }

    [Fact]
    public void GetSnippets_ProvidesRequiredCoreIncludes()
    {
        foreach (ScriptSnippet snippet in _service.GetSnippets())
        {
            Assert.NotEmpty(snippet.RequiredIncludes);
            Assert.Contains(snippet.RequiredIncludes, include => include == $"Scripts/{snippet.Core}.cs");
        }
    }

    [Fact]
    public void GetSnippets_ContainsRepresentativeMethodsFromEachCore()
    {
        string[] code = _service.GetSnippets().Select(snippet => snippet.Code).ToArray();

        Assert.Contains(code, snippet => snippet.Contains("Core.HuntMonster(", StringComparison.Ordinal));
        Assert.Contains(code, snippet => snippet.Contains("Farm.Gold(", StringComparison.Ordinal));
        Assert.Contains(code, snippet => snippet.Contains("Daily.NSoDDaily(", StringComparison.Ordinal));
        Assert.Contains(code, snippet => snippet.Contains("Adv.KillUltra(", StringComparison.Ordinal));
    }

    [Fact]
    public void GetSnippets_HaveNamesDescriptionsCategoriesAndCode()
    {
        foreach (ScriptSnippet snippet in _service.GetSnippets())
        {
            Assert.False(string.IsNullOrWhiteSpace(snippet.Name));
            Assert.False(string.IsNullOrWhiteSpace(snippet.Description));
            Assert.False(string.IsNullOrWhiteSpace(snippet.Category));
            Assert.False(string.IsNullOrWhiteSpace(snippet.Code));
        }
    }

    [Fact]
    public void GetCores_IncludesAllSupportedCores()
    {
        Assert.Equal(new[] { "All cores", "CoreBots", "CoreFarms", "CoreDailies", "CoreAdvanced" }, _service.GetCores());
    }

    [Fact]
    public void GetCategories_IncludesAllCategories()
    {
        string[] categories = _service.GetCategories().ToArray();

        Assert.Equal("All categories", categories[0]);
        Assert.Contains("Combat", categories);
        Assert.Contains("Reputation", categories);
        Assert.Contains("Daily quests", categories);
        Assert.Contains("Enhancements", categories);
    }
}
