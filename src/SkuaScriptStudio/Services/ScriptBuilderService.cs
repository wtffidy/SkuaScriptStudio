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
        int indentation = 2;

        for (int index = 0; index < steps.Count; index++)
        {
            ScriptBuilderStep step = steps[index] ?? throw new ArgumentException($"Block {index + 1} is null.", nameof(steps));
            string generated = GenerateAction(step, index);

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

            if (step.Action is "If item missing" or "Repeat N times")
            {
                bodyLines.Add(Indent("{", indentation));
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
            //cs_include Scripts/CoreDailies.cs
            //cs_include Scripts/CoreAdvanced.cs
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
                    ExecuteScript();
                    Core.SetOptions(false);
                }

                private void ExecuteScript()
                {
            {{body}}
                }
            }
            """;
    }

    private static string GenerateAction(ScriptBuilderStep step, int index)
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
                $"Core.HuntMonster({map}, {target}, {item}, {quantity}, isTemp: {step.IsTemporary.ToString().ToLowerInvariant()});",
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
            "Generic farming preset" when !string.IsNullOrWhiteSpace(step.Item) && HasQuestIds(step.Value) && HasHuntEntries(step.Hunts) =>
                GenerateGenericFarm(step, registerQuests: true),
            "Simple farming preset" when !string.IsNullOrWhiteSpace(step.Item) && HasHuntEntries(step.Hunts) =>
                GenerateGenericFarm(step, registerQuests: false),
            "If item missing" when !string.IsNullOrWhiteSpace(step.Item) =>
                $"if (!Core.CheckInventory({item}, {quantity}))",
            "Repeat N times" when quantity > 0 =>
                $"for (int repeat{index} = 0; repeat{index} < {quantity} && !Bot.ShouldExit; repeat{index}++)",
            "Register quests" when HasQuestIds(step.Value) =>
                $"Core.RegisterQuests({step.Value});",
            "Cancel registered quests" =>
                "Core.CancelRegisteredQuests();",
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

    private static string GenerateGenericFarm(ScriptBuilderStep step, bool registerQuests)
    {
        string item = SymbolDisplay.FormatLiteral(step.Item.Trim(), quote: true);
        int quantity = Math.Max(1, step.Quantity);
        List<string> lines =
        [
            $"if (Core.CheckInventory({item}, {quantity}))",
            "    return;",
            $"Core.FarmingLogger({item}, {quantity});",
            $"while (!Bot.ShouldExit && !Core.CheckInventory({item}, {quantity}))",
            "{"
        ];

        if (registerQuests)
            lines.Insert(3, $"Core.RegisterQuests({step.Value});");

        foreach (string entry in step.Hunts.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = entry.Split('|', StringSplitOptions.TrimEntries);
            if (parts.Length is < 4 or > 5 || !HasValues(parts[0], parts[1], parts[2]) || !int.TryParse(parts[3], out int huntQuantity) || huntQuantity < 1)
                throw new ArgumentException("Each hunt must use map|mob|item|quant|isTemp. The isTemp value is optional.");

            bool isTemporary = parts.Length == 5
                ? bool.TryParse(parts[4], out bool parsedIsTemporary) ? parsedIsTemporary : throw new ArgumentException($"Invalid isTemp value in hunt: {entry}")
                : step.IsTemporary;
            string mapLiteral = SymbolDisplay.FormatLiteral(parts[0], quote: true);
            string monsterLiteral = SymbolDisplay.FormatLiteral(parts[1], quote: true);
            string huntItemLiteral = SymbolDisplay.FormatLiteral(parts[2], quote: true);
            lines.Add($"    Core.HuntMonster({mapLiteral}, {monsterLiteral}, {huntItemLiteral}, {huntQuantity}, isTemp: {isTemporary.ToString().ToLowerInvariant()});");
        }

        lines.Add("}");
        return string.Join(Environment.NewLine, lines);
    }

    private static bool HasHuntEntries(string hunts) => !string.IsNullOrWhiteSpace(hunts);
    private static bool HasValues(params string[] values) => values.All(value => !string.IsNullOrWhiteSpace(value));

    private static bool HasCsvValues(string value) => value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Length > 0;

    private static string FormatStringArguments(string value) => string.Join(", ", value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Select(name => SymbolDisplay.FormatLiteral(name, quote: true)));

    private static bool HasQuestIds(string value) =>
        value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Length > 0 &&
        value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).All(id => int.TryParse(id, out int questId) && questId > 0);

    private static bool TryGetId(string value, out int id) => int.TryParse(value, out id) && id > 0;

    private static string Indent(string source, int levels)
    {
        string indentation = new(' ', levels * 4);
        string normalized = source.Replace("\r\n", "\n", StringComparison.Ordinal);
        return string.Join(Environment.NewLine, normalized.Split('\n').Select(line => indentation + line));
    }
}
