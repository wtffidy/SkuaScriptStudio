using Microsoft.CodeAnalysis.CSharp;
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

        Assert.Contains("name: MyScript", result);
        Assert.Contains("description: Generated Skua script.", result);
        Assert.Contains("//cs_include Scripts/CoreBots.cs", result);
        Assert.Contains("//cs_include Scripts/CoreFarms.cs", result);
        Assert.Contains("//cs_include Scripts/CoreStory.cs", result);
        Assert.Contains("//cs_include Scripts/CoreArmyLite.cs", result);
        Assert.Contains("//cs_include Scripts/SevenCircles.cs", result);
        Assert.Contains("using Skua.Core.Interfaces;", result);
        Assert.Contains("using Skua.Core.Models.Items;", result);
        Assert.Contains("using Skua.Core.Options;", result);
        Assert.Contains("private IScriptInterface Bot => IScriptInterface.Instance;", result);
        Assert.Contains("private CoreBots Core => CoreBots.Instance;", result);
        Assert.Contains("public bool DontPreconfigure = true;", result);
        Assert.Contains("public string[] MultiOptions", result);
        Assert.Contains("public void ScriptMain(IScriptInterface bot)", result);
        Assert.Contains("Core.SetOptions(false);", result);
        Assert.Contains("// Block 1: Equip a class", result);
        Assert.Contains("// Block 2: Hunt for an item", result);
        Assert.Contains("// Block 3: Complete a quest", result);
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
    public void GenerateScript_ExpandsMultipleGearNames()
    {
        ScriptBuilderStep[] steps =
        [
            new() { Action = "Equip gear", Value = "Blade of Awe, Cape of Awe" }
        ];

        string result = _service.GenerateScript(steps);

        Assert.Contains("Core.Equip(\"Blade of Awe\", \"Cape of Awe\");", result);
    }

    [Fact]
    public void GenerateScript_GeneratesSafeInventoryWaitAndDropActions()
    {
        ScriptBuilderStep[] steps =
        [
            new() { Action = "Add drops", Item = "Token" },
            new() { Action = "Wait for an item", Item = "Token", Quantity = 25 },
            new() { Action = "Remove drops", Item = "Token" }
        ];

        string result = _service.GenerateScript(steps);

        Assert.Contains("Core.AddDrop(\"Token\");", result);
        Assert.Contains("while (!Bot.ShouldExit && !Core.CheckInventory(\"Token\", 25))", result);
        Assert.Contains("Core.RemoveDrop(\"Token\");", result);
    }

    [Fact]
    public void GenerateScript_ExpandsMultipleInventoryItemNames()
    {
        ScriptBuilderStep[] steps =
        [
            new() { Action = "Add drops", Item = "Token A, Token B" },
            new() { Action = "Bank items", Item = "Token A, Token B" }
        ];

        string result = _service.GenerateScript(steps);

        Assert.Contains("Core.AddDrop(\"Token A\", \"Token B\");", result);
        Assert.Contains("Core.ToBank(\"Token A\", \"Token B\");", result);
    }

    [Fact]
    public void GenerateScript_UsesCellPadAndAggroSettings()
    {
        ScriptBuilderStep[] steps =
        [
            new() { Action = "Move to a cell", Cell = "Boss", Pad = "Left" },
            new() { Action = "Configure aggro", Enabled = false }
        ];

        string result = _service.GenerateScript(steps);

        Assert.Contains("Core.Jump(\"Boss\", \"Left\");", result);
        Assert.Contains("Core.ConfigureAggro(false);", result);
    }


    [Fact]
    public void GenerateScript_ProducesValidCSharpSyntax()
    {
        ScriptBuilderStep[] steps =
        [
            new() { Action = "Equip a class", ClassType = "Farm" },
            new() { Action = "Wait for an item", Item = "Token", Quantity = 10 },
            new() { Action = "Log a message", Value = "Finished \"safely\"" }
        ];

        string result = _service.GenerateScript(steps);
        Microsoft.CodeAnalysis.SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(result);

        Assert.Empty(syntaxTree.GetDiagnostics().Where(diagnostic => diagnostic.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
    }

    [Fact]
    public void GenerateScript_RejectsAnEmptySequence()
    {
        Assert.Throws<ArgumentException>(() => _service.GenerateScript([]));
    }

    [Fact]
    public void GenerateScript_NestsInventoryConditionsAndRepeatLoops()
    {
        ScriptBuilderStep[] steps =
        [
            new() { Action = "If item missing", Item = "Quest Token", Quantity = 10 },
            new() { Action = "Repeat N times", Quantity = 10 },
            new() { Action = "Hunt for an item", Map = "battleundere", Target = "Treasure Pile", Item = "Quest Token", Quantity = 10 },
            new() { Action = "End repeat" },
            new() { Action = "End block" }
        ];

        string result = _service.GenerateScript(steps);
        Microsoft.CodeAnalysis.SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(result);

        Assert.Contains("if (!Core.CheckInventory(\"Quest Token\", 10))", result);
        Assert.Contains("repeat1 < 10 && !Bot.ShouldExit", result);
        Assert.True(result.IndexOf("while (!Bot.ShouldExit", StringComparison.Ordinal) < result.IndexOf("Core.HuntMonster(", StringComparison.Ordinal));
        Assert.Empty(syntaxTree.GetDiagnostics().Where(diagnostic => diagnostic.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
    }

    [Fact]
    public void GenerateScript_RejectsMismatchedControlFlowClosers()
    {
        ScriptBuilderStep[] steps = [new() { Action = "If item missing", Item = "Token" }, new() { Action = "End repeat" }];

        Assert.Throws<ArgumentException>(() => _service.GenerateScript(steps));
    }

    [Fact]
    public void GenerateScript_RejectsUnclosedControlFlowBlocks()
    {
        ScriptBuilderStep[] steps = [new() { Action = "Repeat N times", Quantity = 3 }];

        Assert.Throws<ArgumentException>(() => _service.GenerateScript(steps));
    }
    [Fact]
    public void GenerateScript_RegistersMultipleQuestsAndCanCancelThem()
    {
        ScriptBuilderStep[] steps =
        [
            new() { Action = "Register quests", Value = "12345, 12346" },
            new() { Action = "Cancel registered quests" }
        ];

        string result = _service.GenerateScript(steps);

        Assert.Contains("Core.RegisterQuests(12345, 12346);", result);
        Assert.Contains("Core.CancelRegisteredQuests();", result);
        Microsoft.CodeAnalysis.SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(result);
        Assert.Empty(syntaxTree.GetDiagnostics().Where(diagnostic => diagnostic.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
    }

    [Fact]
    public void GenerateScript_RejectsInvalidRegisteredQuestIds()
    {
        ScriptBuilderStep[] steps = [new() { Action = "Register quests", Value = "12345, nope" }];

        Assert.Throws<ArgumentException>(() => _service.GenerateScript(steps));
    }

    [Fact]
    public void GenerateScript_UsesRepeatCountAndUniqueNestedCounters()
    {
        ScriptBuilderStep[] steps =
        [
            new() { Action = "Repeat N times", Quantity = 3 },
            new() { Action = "Repeat N times", Quantity = 2 },
            new() { Action = "Rest" },
            new() { Action = "End repeat" },
            new() { Action = "End repeat" }
        ];

        string result = _service.GenerateScript(steps);

        Assert.Contains("int repeat0 = 0; repeat0 < 3", result);
        Assert.Contains("int repeat1 = 0; repeat1 < 2", result);
        Microsoft.CodeAnalysis.SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(result);
        Assert.Empty(syntaxTree.GetDiagnostics().Where(diagnostic => diagnostic.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
    }

}
