using Microsoft.CodeAnalysis.CSharp;
using SkuaScriptStudio.Models;

namespace SkuaScriptStudio.Services;

public sealed class ScriptBuilderService
{
    /// <summary>
    /// Generates a complete Skua script from the ordered actions configured in the visual builder.
    /// </summary>
    public string GenerateScript(IReadOnlyList<ScriptBuilderStep> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);
        if (steps.Count == 0)
            throw new ArgumentException("Add at least one step before building your script.", nameof(steps));

        List<string> actions = [];
        foreach (ScriptBuilderStep step in steps)
        {
            ArgumentNullException.ThrowIfNull(step);
            actions.Add(GenerateAction(step));
        }

        string body = string.Join(Environment.NewLine + Environment.NewLine, actions);
        return $$"""
            using Skua.Core.Interfaces;

            public class MyScript
            {
                public IScriptInterface Bot => IScriptInterface.Instance;
                public CoreBots Core => CoreBots.Instance;

                public void ScriptMain(IScriptInterface bot)
                {
                    Core.SetOptions();
                    try
                    {
            {{Indent(body, 3)}}
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
        string value = SymbolDisplay.FormatLiteral(step.Value.Trim(), quote: true);
        int quantity = Math.Max(1, step.Quantity);

        return step.Action switch
        {
            "Hunt for an item" when !string.IsNullOrWhiteSpace(step.Map) && !string.IsNullOrWhiteSpace(step.Target) && !string.IsNullOrWhiteSpace(step.Item) =>
                $"Core.HuntMonster({map}, {target}, {item}, {quantity});",
            "Accept a quest" when int.TryParse(step.Value, out int acceptQuestId) && acceptQuestId > 0 =>
                $"Core.EnsureAccept({acceptQuestId});",
            "Complete a quest" when int.TryParse(step.Value, out int completeQuestId) && completeQuestId > 0 =>
                $"Core.EnsureComplete({completeQuestId});",
            "Equip a class" =>
                $"Core.EquipClass(ClassType.{(step.ClassType is "Farm" or "Solo" or "Support" ? step.ClassType : "Farm")});",
            "Travel to a map" when !string.IsNullOrWhiteSpace(step.Map) =>
                $"Core.Join({map});",
            "Check inventory" when !string.IsNullOrWhiteSpace(step.Item) =>
                $"if (!Core.CheckInventory({item}, {quantity}))\n    Core.Logger(\"Still need the required item quantity.\");",
            "Log a message" when !string.IsNullOrWhiteSpace(step.Value) =>
                $"Core.Logger({value});",
            "Wait" =>
                $"Core.Sleep({quantity * 1000});",
            _ => throw new ArgumentException($"The step '{step.Action}' is missing information or has an invalid value.")
        };
    }

    private static string Indent(string source, int levels)
    {
        string indentation = new(' ', levels * 4);
        string normalized = source.Replace("\r\n", "\n", StringComparison.Ordinal);
        return string.Join(Environment.NewLine, normalized.Split('\n').Select(line => indentation + line));
    }
}
