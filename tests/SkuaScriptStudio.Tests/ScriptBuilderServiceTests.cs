using SkuaScriptStudio.Services;
using Xunit;

namespace SkuaScriptStudio.Tests;

public sealed class ScriptBuilderServiceTests
{
    private readonly ScriptBuilderService _service = new();

    [Fact]
    public void GenerateHuntMonster_UsesRequestedValues()
    {
        string result = _service.GenerateHuntMonster("battleundere", "Treasure Pile", "Treasure Relic", 12, "Farm");

        Assert.Contains("Core.EquipClass(ClassType.Farm);", result);
        Assert.Contains("Core.HuntMonster(\"battleundere\", \"Treasure Pile\", \"Treasure Relic\", 12);", result);
    }

    [Fact]
    public void GenerateHuntMonster_EscapesStringValues()
    {
        string result = _service.GenerateHuntMonster("map\\name", "A \"quoted\" monster", "Item", 1, "Solo");

        Assert.Contains("\"map\\\\name\"", result);
        Assert.Contains("\"A \\\"quoted\\\" monster\"", result);
    }

    [Fact]
    public void GenerateHuntMonster_ClampsQuantityToOne()
    {
        string result = _service.GenerateHuntMonster("map", "monster", "item", 0, "Farm");

        Assert.EndsWith(", 1);", result.Trim());
    }

    [Fact]
    public void GenerateHuntMonster_FallsBackToFarmForUnknownClass()
    {
        string result = _service.GenerateHuntMonster("map", "monster", "item", 1, "Invalid");

        Assert.Contains("Core.EquipClass(ClassType.Farm);", result);
    }

    [Theory]
    [InlineData("", "monster", "item", "Farm")]
    [InlineData("map", "", "item", "Farm")]
    [InlineData("map", "monster", "", "Farm")]
    [InlineData("map", "monster", "item", "")]
    public void GenerateHuntMonster_RejectsMissingRequiredValues(string map, string monster, string item, string classType)
    {
        Assert.Throws<ArgumentException>(() => _service.GenerateHuntMonster(map, monster, item, 1, classType));
    }
}
