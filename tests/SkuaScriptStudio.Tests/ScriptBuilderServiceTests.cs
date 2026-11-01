using SkuaScriptStudio.Models;
using SkuaScriptStudio.Services;
using Xunit;

namespace SkuaScriptStudio.Tests;

public sealed class ScriptBuilderServiceTests
{
    private readonly ScriptBuilderService _service = new();

    [Fact]
    public void GenerateScript_CreatesCompleteScaffoldAndPreservesStepOrder()
    {
        ScriptBuilderStep[] steps =
        [
            new() { Action = "Equip a class", ClassType = "Farm" },
            new() { Action = "Hunt for an item", Map = "battleundere", Target = "Treasure Pile", Item = "Treasure Relic", Quantity = 12 },
            new() { Action = "Complete a quest", Value = "12345" }
        ];

        string result = _service.GenerateScript(steps);

        Assert.Contains("using Skua.Core.Interfaces;", result);
        Assert.Contains("public void ScriptMain(IScriptInterface bot)", result);
        Assert.Contains("Core.SetOptions(false);", result);
        Assert.True(result.IndexOf("Core.EquipClass(ClassType.Farm);", StringComparison.Ordinal) < result.IndexOf("Core.HuntMonster(", StringComparison.Ordinal));
        Assert.True(result.IndexOf("Core.HuntMonster(", StringComparison.Ordinal) < result.IndexOf("Core.EnsureComplete(12345);", StringComparison.Ordinal));
    }

    [Fact]
    public void GenerateScript_EscapesStringValues()
    {
        ScriptBuilderStep[] steps =
        [
            new() { Action = "Hunt for an item", Map = "map\\name", Target = "A \"quoted\" monster", Item = "Item", Quantity = 1 }
        ];

        string result = _service.GenerateScript(steps);

        Assert.Contains("\"map\\\\name\"", result);
        Assert.Contains("\"A \\\"quoted\\\" monster\"", result);
    }

    [Theory]
    [InlineData("Hunt for an item", "", "monster", "item", "")]
    [InlineData("Hunt for an item", "map", "", "item", "")]
    [InlineData("Hunt for an item", "map", "monster", "", "")]
    [InlineData("Accept a quest", "", "", "", "not-a-number")]
    [InlineData("Complete a quest", "", "", "", "0")]
    [InlineData("Travel to a map", "", "", "", "")]
    [InlineData("Log a message", "", "", "", "")]
    public void GenerateScript_RejectsIncompleteSteps(string action, string map, string target, string item, string value)
    {
        ScriptBuilderStep[] steps = [new() { Action = action, Map = map, Target = target, Item = item, Value = value }];

        Assert.Throws<ArgumentException>(() => _service.GenerateScript(steps));
    }

    [Fact]
    public void GenerateScript_RejectsAnEmptySequence()
    {
        Assert.Throws<ArgumentException>(() => _service.GenerateScript([]));
    }
}
