using Microsoft.CodeAnalysis.CSharp;
using SkuaScriptStudio.Models;

namespace SkuaScriptStudio.Services;

public sealed class ScriptBuilderService
{
    /// <summary>
    /// Generates a complete Skua script from ordered visual blocks, including nested control flow.
    /// </summary>
    public string GenerateScript(IReadOnlyList<ScriptBuilderStep> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);
        if (steps.Count == 0)
            throw new ArgumentException("Add at least one block before building your script.", nameof(steps));

        List<string> bodyLines = [];
        Stack<string> openBlocks = new();
        int indentation = 3;

        for (int index = 0; index < steps.Count; index++)
        {
            ScriptBuilderStep step = steps[index] ?? throw new ArgumentException($"Block {index + 1} is null.", nameof(steps));
            string generated = GenerateAction(step);

            if (step.Action is "End block" or "End repeat")
            {
                string expected = step.Action == "End block" ? "if" : "repeat";
                if (openBlocks.Count == 0 || openBlocks.Peek() != expected)
                    throw new ArgumentException($"Block {index + 1} closes an {expected.ToUpperInvariant()} block that is not open.");

                if (expected == "repeat")
                    bodyLines.Add(Indent("Core.Sleep();", indentation));

                indentation--;
                openBlocks.Pop();
                bodyLines.Add(Indent("}", indentation));
                continue;
            }

            bodyLines.Add(Indent($"// Block {index + 1}: {step.Action}", indentation));
            bodyLines.Add(Indent(generated, indentation));

            if (step.Action is "If item missing" or "Repeat until item owned")
            {
                openBlocks.Push(step.Action == "If item missing" ? "if" : "repeat");
                indentation++;
            }
        }

        if (openBlocks.Count > 0)
            throw new ArgumentException($"Close the {openBlocks.Peek().ToUpperInvariant()} block before building the script.");

        string body = string.Join(Environment.NewLine, bodyLines);
        return $$"""
            /*
            name: MyScript
            description: Generated Skua script.
            tags: generated, skua
            */
            //cs_include Scripts/CoreBots.cs
            //cs_include Scripts/CoreFarms.cs
            //cs_include Scripts/CoreStory.cs
            //cs_include Scripts/CoreArmyLite.cs
            //cs_include Scripts/SevenCircles.cs
            using Skua.Core.Interfaces;
            using Skua.Core.Models.Items;
            using Skua.Core.Options;

            public class MyScript
            {
                private IScriptInterface Bot => IScriptInterface.Instance;
                private CoreBots Core => CoreBots.Instance;

                public bool DontPreconfigure = true;
                public string[] MultiOptions = { "Generic", "Select" };

                public void ScriptMain(IScriptInterface bot)
                {
                    Core.SetOptions();
                    try
                    {
            {{body}}
                    }
                    finally
                    {
                        Core.SetOptions(false);
                    }
                }
            }
            """;
    }

    private static string GenerateAction(ScriptBuilderStep step)
    {
        string map = SymbolDisplay.FormatLiteral(step.Map.Trim(), quote: true);
        string target = SymbolDisplay.FormatLiteral(step.Target.Trim(), quote: true);
        string item = SymbolDisplay.FormatLiteral(step.Item.Trim(), quote: true);
        string cell = SymbolDisplay.FormatLiteral(step.Cell.Trim(), quote: true);
        string pad = SymbolDisplay.FormatLiteral(step.Pad.Trim(), quote: true);
        string value = SymbolDisplay.FormatLiteral(step.Value.Trim(), quote: true);
        int quantity = Math.Max(1, step.Quantity);

        return step.Action switch
        {
            "Hunt for an item" when HasValues(step.Map, step.Target, step.Item) =>
                $"Core.HuntMonster({map}, {target}, {item}, {quantity});",
            "Accept a quest" when TryGetId(step.Value, out int acceptQuestId) =>
                $"Core.EnsureAccept({acceptQuestId});",
            "Complete a quest" when TryGetId(step.Value, out int completeQuestId) =>
                $"Core.EnsureComplete({completeQuestId});",
            "Equip a class" =>
                $"Core.EquipClass(ClassType.{(step.ClassType is "Farm" or "Solo" or "Support" ? step.ClassType : "Farm")});",
            "Equip gear" when HasCsvValues(step.Value) =>
                $"Core.Equip({FormatStringArguments(step.Value)});",
            "Travel to a map" when !string.IsNullOrWhiteSpace(step.Map) =>
                $"Core.Join({map});",
            "Move to a cell" when HasValues(step.Cell, step.Pad) =>
                $"Core.Jump({cell}, {pad});",
            "Check inventory" when !string.IsNullOrWhiteSpace(step.Item) =>
                $"if (!Core.CheckInventory({item}, {quantity}))\n    Core.Logger(\"Still need the required item quantity.\");",
            "If item missing" when !string.IsNullOrWhiteSpace(step.Item) =>
                $"if (!Core.CheckInventory({item}, {quantity}))",
            "Repeat until item owned" when !string.IsNullOrWhiteSpace(step.Item) =>
                $"while (!Bot.ShouldExit && !Core.CheckInventory({item}, {quantity}))",
            "End block" or "End repeat" => string.Empty,
            "Wait for an item" when !string.IsNullOrWhiteSpace(step.Item) =>
                $"while (!Bot.ShouldExit && !Core.CheckInventory({item}, {quantity}))\n    Core.Sleep();",
            "Add drops" when HasCsvValues(step.Item) =>
                $"Core.AddDrop({FormatStringArguments(step.Item)});",
            "Remove drops" when HasCsvValues(step.Item) =>
                $"Core.RemoveDrop({FormatStringArguments(step.Item)});",
            "Bank items" when HasCsvValues(step.Item) =>
                $"Core.ToBank({FormatStringArguments(step.Item)});",
            "Unbank items" when HasCsvValues(step.Item) =>
                $"Core.Unbank({FormatStringArguments(step.Item)});",
            "Log a message" when !string.IsNullOrWhiteSpace(step.Value) =>
                $"Core.Logger({value});",
            "Log farming progress" when !string.IsNullOrWhiteSpace(step.Item) =>
                $"Core.FarmingLogger({item}, {quantity});",
            "Rest" =>
                "Core.Rest();",
            "Configure aggro" =>
                $"Core.ConfigureAggro({step.Enabled.ToString().ToLowerInvariant()});",
            "Wait" =>
                $"Core.Sleep({quantity * 1000});",
            _ => throw new ArgumentException($"The block '{step.Action}' is missing information or has an invalid value.")
        };
    }

    private static bool HasValues(params string[] values) => values.All(value => !string.IsNullOrWhiteSpace(value));

    private static bool HasCsvValues(string value) => value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Length > 0;

    private static string FormatStringArguments(string value) => string.Join(", ", value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Select(name => SymbolDisplay.FormatLiteral(name, quote: true)));

    private static bool TryGetId(string value, out int id) => int.TryParse(value, out id) && id > 0;

    private static string Indent(string source, int levels)
    {
        string indentation = new(' ', levels * 4);
        string normalized = source.Replace("\r\n", "\n", StringComparison.Ordinal);
        return string.Join(Environment.NewLine, normalized.Split('\n').Select(line => indentation + line));
    }
}
