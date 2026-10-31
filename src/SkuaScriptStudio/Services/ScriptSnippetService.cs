using SkuaScriptStudio.Models;

namespace SkuaScriptStudio.Services;

/// <summary>
/// Provides the built-in snippets available in the script workspace.
/// </summary>
public sealed class ScriptSnippetService
{
    private static readonly ScriptSnippet[] Snippets =
    [
        new("Equip class", "Equip a farming class before starting an activity.", "Core.EquipClass(ClassType.Farm);"),
        new("Hunt monster", "Farm an item from a monster on a map.", "Core.HuntMonster(\"map\", \"monster\", \"item\", 1);"),
        new("Register quest", "Register a quest by its numeric ID.", "Core.RegisterQuests(12345);"),
        new("Wait for inventory", "Keep waiting until the requested item quantity is owned.", "while (!Bot.ShouldExit && !Core.CheckInventory(\"Item Name\", 1))\n{\n    Core.Sleep();\n}"),
        new("Hunt monster quest", "Register a quest with a monster farming target.", "Core.HuntMonsterQuest(\n    12345,\n    (\"map\", \"monster\", ClassType.Farm)\n);")
    ];

    /// <summary>
    /// Returns the built-in snippets in their display order.
    /// </summary>
    public IReadOnlyList<ScriptSnippet> GetSnippets() => Snippets;
}
