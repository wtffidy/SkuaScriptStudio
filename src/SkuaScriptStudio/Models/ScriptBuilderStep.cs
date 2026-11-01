namespace SkuaScriptStudio.Models;

/// <summary>
/// Represents one user-configured action in a generated Skua script.
/// </summary>
public sealed class ScriptBuilderStep
{
    public string Action { get; init; } = string.Empty;
    public string Map { get; init; } = string.Empty;
    public string Target { get; init; } = string.Empty;
    public string Item { get; init; } = string.Empty;
    public int Quantity { get; init; } = 1;
    public string Value { get; init; } = string.Empty;
    public string ClassType { get; init; } = "Farm";

    public override string ToString() => Action switch
    {
        "Hunt for an item" => $"Hunt {Target} in {Map} for {Item} × {Quantity}",
        "Accept a quest" => $"Accept quest #{Value}",
        "Complete a quest" => $"Complete quest #{Value}",
        "Equip a class" => $"Equip {ClassType} class",
        "Travel to a map" => $"Travel to {Map}",
        "Check inventory" => $"Check for {Quantity} × {Item}",
        "Log a message" => $"Log: {Value}",
        "Wait" => $"Wait {Quantity} seconds",
        _ => Action
    };
}
