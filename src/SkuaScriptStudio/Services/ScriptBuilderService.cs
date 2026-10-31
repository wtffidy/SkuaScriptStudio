namespace SkuaScriptStudio.Services;

public sealed class ScriptBuilderService
{
    public string GenerateHuntMonster(string map, string monster, string item, int quantity, string classType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(map);
        ArgumentException.ThrowIfNullOrWhiteSpace(monster);
        ArgumentException.ThrowIfNullOrWhiteSpace(item);
        ArgumentException.ThrowIfNullOrWhiteSpace(classType);

        string escapedMap = EscapeString(map);
        string escapedMonster = EscapeString(monster);
        string escapedItem = EscapeString(item);
        string safeClassType = classType is "Farm" or "Solo" or "Support" ? classType : "Farm";

        return $$"""
            Core.EquipClass(ClassType.{{safeClassType}});
            Core.HuntMonster("{{escapedMap}}", "{{escapedMonster}}", "{{escapedItem}}", {{Math.Max(1, quantity)}});
            """;
    }

    private static string EscapeString(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
