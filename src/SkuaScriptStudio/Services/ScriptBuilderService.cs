using Microsoft.CodeAnalysis.CSharp;

namespace SkuaScriptStudio.Services;

public sealed class ScriptBuilderService
{
    public string GenerateHuntMonster(string map, string monster, string item, int quantity, string classType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(map);
        ArgumentException.ThrowIfNullOrWhiteSpace(monster);
        ArgumentException.ThrowIfNullOrWhiteSpace(item);
        ArgumentException.ThrowIfNullOrWhiteSpace(classType);

        string escapedMap = SymbolDisplay.FormatLiteral(map, quote: true);
        string escapedMonster = SymbolDisplay.FormatLiteral(monster, quote: true);
        string escapedItem = SymbolDisplay.FormatLiteral(item, quote: true);
        string safeClassType = classType is "Farm" or "Solo" or "Support" ? classType : "Farm";

        return $$"""
            Core.EquipClass(ClassType.{{safeClassType}});
            Core.HuntMonster({{escapedMap}}, {{escapedMonster}}, {{escapedItem}}, {{Math.Max(1, quantity)}});
            """;
    }

}
