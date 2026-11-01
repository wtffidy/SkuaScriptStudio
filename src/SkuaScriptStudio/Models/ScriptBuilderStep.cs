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
    public string Cell { get; init; } = "Enter";
    public string Pad { get; init; } = "Spawn";
    public int Quantity { get; init; } = 1;
    public string Value { get; init; } = string.Empty;
    public string ClassType { get; init; } = "Farm";
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Returns a plain-language summary for the builder's action list.
    /// </summary>
    public override string ToString() => Action switch
    {
        "Hunt for an item" => $"Hunt {Target} in {Map} for {Item} × {Quantity}",
        "Accept a quest" => $"Accept quest #{Value}",
        "Complete a quest" => $"Complete quest #{Value}",
        "Equip a class" => $"Equip {ClassType} class",
        "Equip gear" => $"Equip {Value}",
        "Travel to a map" => $"Travel to {Map}",
        "Move to a cell" => $"Move to {Cell} / {Pad}",
        "Check inventory" => $"Check for {Quantity} × {Item}",
        "Wait for an item" => $"Wait until you have {Quantity} × {Item}",
        "Add drops" => $"Collect: {Item}",
        "Remove drops" => $"Stop collecting: {Item}",
        "Bank items" => $"Bank: {Item}",
        "Unbank items" => $"Retrieve from bank: {Item}",
        "Log a message" => $"Log: {Value}",
        "Log farming progress" => $"Track {Quantity} × {Item}",
        "Rest" => "Restore health and mana",
        "Configure aggro" => $"Aggro handling: {(Enabled ? "On" : "Off")}",
        "Wait" => $"Wait {Quantity} seconds",
        _ => Action
    };
}
