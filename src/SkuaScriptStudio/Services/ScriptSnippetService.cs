using SkuaScriptStudio.Models;

namespace SkuaScriptStudio.Services;

/// <summary>
/// Provides core-aware Skua snippets with the include directives required by each core.
/// </summary>
public sealed class ScriptSnippetService
{
    private static readonly string[] CoreBotsIncludes = ["Scripts/CoreBots.cs"];
    private static readonly string[] CoreFarmsIncludes = ["Scripts/CoreBots.cs", "Scripts/CoreFarms.cs"];
    private static readonly string[] CoreDailiesIncludes = ["Scripts/CoreBots.cs", "Scripts/CoreDailies.cs"];
    private static readonly string[] CoreAdvancedIncludes = ["Scripts/CoreBots.cs", "Scripts/CoreAdvanced.cs"];

    private static readonly ScriptSnippet[] Snippets =
    [
        // CoreBots: shared runtime, inventory, quests, combat, movement and logging.
        Create("CoreBots script scaffold", "Standard script class, CoreBots instance and setup/shutdown lifecycle.", """
            //cs_include Scripts/CoreBots.cs
            using Skua.Core.Interfaces;

            public class MyScript
            {
                public IScriptInterface Bot => IScriptInterface.Instance;
                public CoreBots Core => CoreBots.Instance;

                public void ScriptMain(IScriptInterface bot)
                {
                    Core.SetOptions();

                    // Your script logic here.

                    Core.SetOptions(false);
                }
            }
            """, "CoreBots", "Setup", CoreBotsIncludes),
        Create("Set up CoreBots", "Apply the shared bot options before running script logic.", "Core.SetOptions();", "CoreBots", "Setup", CoreBotsIncludes),
        Create("Restore CoreBots options", "Restore shared bot options when the script finishes.", "Core.SetOptions(false);", "CoreBots", "Setup", CoreBotsIncludes),
        Create("Equip farming class", "Equip the configured farming class and gear.", "Core.EquipClass(ClassType.Farm);", "CoreBots", "Equipment", CoreBotsIncludes),
        Create("Equip solo class", "Equip the configured solo class and gear.", "Core.EquipClass(ClassType.Solo);", "CoreBots", "Equipment", CoreBotsIncludes),
        Create("Equip support class", "Equip the configured support class and gear.", "Core.EquipClass(ClassType.Support);", "CoreBots", "Equipment", CoreBotsIncludes),
        Create("Equip specific gear", "Equip one or more items by name.", "Core.Equip(\"Item Name\", \"Another Item\");", "CoreBots", "Equipment", CoreBotsIncludes),
        Create("Hunt monster", "Farm an item from a monster on a map.", "Core.HuntMonster(\"map\", \"monster\", \"item\", 1);", "CoreBots", "Combat", CoreBotsIncludes),
        Create("Hunt monster by map ID", "Hunt a monster using its map ID.", "Core.HuntMonsterMapID(\"map\", 12345, \"item\", 1);", "CoreBots", "Combat", CoreBotsIncludes),
        Create("Kill monster at cell", "Kill a monster at a specific cell and pad.", "Core.KillMonster(\"map\", \"Enter\", \"Spawn\", \"monster\", \"item\", 1);", "CoreBots", "Combat", CoreBotsIncludes),
        Create("Hunt monster quest", "Register a quest and farm its monster targets.", "Core.HuntMonsterQuest(12345, (\"map\", \"monster\", ClassType.Farm));", "CoreBots", "Quests", CoreBotsIncludes),
        Create("Register quests", "Register one or more quest IDs.", "Core.RegisterQuests(12345, 12346);", "CoreBots", "Quests", CoreBotsIncludes),
        Create("Accept quest", "Ensure a quest is accepted before farming.", "Core.EnsureAccept(12345);", "CoreBots", "Quests", CoreBotsIncludes),
        Create("Complete quest", "Ensure a quest is completed.", "Core.EnsureComplete(12345);", "CoreBots", "Quests", CoreBotsIncludes),
        Create("Complete quest with item", "Complete a quest using a specific item ID.", "Core.EnsureComplete(12345, 67890);", "CoreBots", "Quests", CoreBotsIncludes),
        Create("Check inventory", "Check whether the required item quantity is owned.", "if (Core.CheckInventory(\"Item Name\", 1))\n    Core.Logger(\"Item owned\");", "CoreBots", "Inventory", CoreBotsIncludes),
        Create("Wait for inventory", "Wait until the requested item quantity is owned or the bot is stopped.", "while (!Bot.ShouldExit && !Core.CheckInventory(\"Item Name\", 1))\n    Core.Sleep();", "CoreBots", "Inventory", CoreBotsIncludes),
        Create("Add drops", "Whitelist items so they are collected while farming.", "Core.AddDrop(\"Item Name\", \"Another Item\");", "CoreBots", "Inventory", CoreBotsIncludes),
        Create("Remove drops", "Remove items from the drop whitelist.", "Core.RemoveDrop(\"Item Name\");", "CoreBots", "Inventory", CoreBotsIncludes),
        Create("Bank items", "Move one or more inventory items to the bank.", "Core.ToBank(\"Item Name\", \"Another Item\");", "CoreBots", "Inventory", CoreBotsIncludes),
        Create("Unbank items", "Retrieve one or more items from the bank.", "Core.Unbank(\"Item Name\", \"Another Item\");", "CoreBots", "Inventory", CoreBotsIncludes),
        Create("Buy shop item", "Buy an item from a shop by name.", "Core.BuyItem(\"map\", 1234, \"Item Name\", 1);", "CoreBots", "Shops", CoreBotsIncludes),
        Create("Sell item", "Sell a quantity of an item.", "Core.SellItem(\"Item Name\", 1);", "CoreBots", "Shops", CoreBotsIncludes),
        Create("Join map", "Join a map, optionally specifying cell and pad.", "Core.Join(\"map\", \"Enter\", \"Spawn\");", "CoreBots", "Movement", CoreBotsIncludes),
        Create("Jump to cell", "Move to a cell and pad in the current map.", "Core.Jump(\"Enter\", \"Spawn\");", "CoreBots", "Movement", CoreBotsIncludes),
        Create("Sleep", "Pause the script using the CoreBots delay helper.", "Core.Sleep(1000);", "CoreBots", "Flow", CoreBotsIncludes),
        Create("Log message", "Write a general script status message.", "Core.Logger(\"Starting task...\");", "CoreBots", "Logging", CoreBotsIncludes),
        Create("Log farming progress", "Log an item farming target and quantity.", "Core.FarmingLogger(\"Item Name\", 100);", "CoreBots", "Logging", CoreBotsIncludes),
        Create("Rest", "Restore health and mana using the shared helper.", "Core.Rest();", "CoreBots", "Combat", CoreBotsIncludes),
        Create("Configure aggro", "Enable or disable monster aggro handling.", "Core.ConfigureAggro(true);", "CoreBots", "Combat", CoreBotsIncludes),
        Create("Skip completed quest", "Skip work when a quest has already been completed.", "if (Core.isCompletedBefore(12345))\n    return;", "CoreBots", "Quests", CoreBotsIncludes),

        // CoreFarms: gold, experience, reputation and item farming helpers.
        Create("CoreFarms instance", "Declare the shared CoreFarms helper.", "private static CoreFarms Farm { get; } = new();", "CoreFarms", "Setup", CoreFarmsIncludes),
        Create("Farm gold", "Run the standard CoreFarms gold routine.", "Farm.Gold();", "CoreFarms", "Gold", CoreFarmsIncludes),
        Create("Farm Honor Hall gold", "Use the Honor Hall gold farming routine.", "Farm.HonorHall(100000000);", "CoreFarms", "Gold", CoreFarmsIncludes),
        Create("Farm Love Potion gold", "Use the Love Potion gold farming routine.", "Farm.LovePotion(100000000);", "CoreFarms", "Gold", CoreFarmsIncludes),
        Create("Farm BattleGroundE gold", "Use the BattleGroundE gold farming routine.", "Farm.BattleGroundE(100000000);", "CoreFarms", "Gold", CoreFarmsIncludes),
        Create("Farm experience", "Farm experience toward a target level.", "Farm.Experience(100);", "CoreFarms", "Experience", CoreFarmsIncludes),
        Create("Farm experience and rank class", "Farm experience and rank up the current class.", "Farm.Experience(100, rankUpClass: true);", "CoreFarms", "Experience", CoreFarmsIncludes),
        Create("Farm Icestorm Arena", "Use the Icestorm Arena leveling routine.", "Farm.IcestormArena(100);", "CoreFarms", "Experience", CoreFarmsIncludes),
        Create("Farm Seven Circles War", "Use the Seven Circles War XP and gold routine.", "Farm.SevenCirclesWar(100, 100000000);", "CoreFarms", "Experience", CoreFarmsIncludes),
        Create("Farm all faction ranks", "Run the combined faction rank farming routine.", "Farm.GetAllRanks(doDeathPit: true);", "CoreFarms", "Reputation", CoreFarmsIncludes),
        Create("Farm Arcangrove reputation", "Farm Arcangrove reputation to the selected rank.", "Farm.ArcangroveREP(10);", "CoreFarms", "Reputation", CoreFarmsIncludes),
        Create("Farm Evil reputation", "Farm Evil reputation to the selected rank.", "Farm.EvilREP(10);", "CoreFarms", "Reputation", CoreFarmsIncludes),
        Create("Farm Good reputation", "Farm Good reputation to the selected rank.", "Farm.GoodREP(10);", "CoreFarms", "Reputation", CoreFarmsIncludes),
        Create("Farm Chaos reputation", "Farm Chaos reputation to the selected rank.", "Farm.ChaosREP(10);", "CoreFarms", "Reputation", CoreFarmsIncludes),
        Create("Farm Doomwood reputation", "Farm Doomwood reputation to the selected rank.", "Farm.DoomWoodREP(10);", "CoreFarms", "Reputation", CoreFarmsIncludes),
        Create("Farm Brightoak reputation", "Farm Brightoak reputation to the selected rank.", "Farm.BrightoakREP(10);", "CoreFarms", "Reputation", CoreFarmsIncludes),
        Create("Farm Yokai reputation", "Farm Yokai reputation to the selected rank.", "Farm.YokaiREP(10);", "CoreFarms", "Reputation", CoreFarmsIncludes),
        Create("Farm Ravenloss reputation", "Farm Ravenloss reputation to the selected rank.", "Farm.RavenlossREP(10);", "CoreFarms", "Reputation", CoreFarmsIncludes),
        Create("Farm Hollowborn reputation", "Farm Hollowborn reputation to the selected rank.", "Farm.HollowbornREP(10);", "CoreFarms", "Reputation", CoreFarmsIncludes),
        Create("Farm Blacksmithing reputation", "Farm Blacksmithing reputation to the selected rank.", "Farm.BlacksmithingREP(10);", "CoreFarms", "Reputation", CoreFarmsIncludes),
        Create("Farm Fishing reputation", "Farm Fishing reputation to the selected rank.", "Farm.FishingREP(10);", "CoreFarms", "Reputation", CoreFarmsIncludes),
        Create("Farm Blade of Awe reputation", "Farm Blade of Awe reputation and optionally farm the Blade of Awe.", "Farm.BladeofAweREP(10, farmBoA: true);", "CoreFarms", "Reputation", CoreFarmsIncludes),
        Create("Farm Alchemy reputation", "Farm Alchemy reputation to the selected rank.", "Farm.AlchemyREP(10);", "CoreFarms", "Reputation", CoreFarmsIncludes),
        Create("Farm Death Pit token", "Farm Death Pit Tokens.", "Farm.DeathPitToken(\"Death Pit Token\", 30);", "CoreFarms", "Items", CoreFarmsIncludes),
        Create("Farm Dragon Runestone", "Farm Dragon Runestones.", "Farm.DragonRunestone(100);", "CoreFarms", "Items", CoreFarmsIncludes),
        Create("Farm Swag Token A", "Use the CoreFarms Swag Token A farming routine.", "Farm.SwagTokenA(100);", "CoreFarms", "Items", CoreFarmsIncludes),
        Create("Farm boosts", "Obtain a selected boost type and quantity.", "Farm.GetBoost(\"Gold\", 1);", "CoreFarms", "Boosts", CoreFarmsIncludes),
        Create("Toggle boost", "Enable or disable a boost type.", "Farm.ToggleBoost(BoostType.Gold, true);", "CoreFarms", "Boosts", CoreFarmsIncludes),
        Create("Farm Battle Under B", "Farm an item using the Battle Under B routine.", "Farm.BattleUnderB(\"Undead Energy\", 100);", "CoreFarms", "Items", CoreFarmsIncludes),

        // CoreDailies: daily quest, reward and item routines.
        Create("CoreDailies instance", "Declare the shared CoreDailies helper.", "private static CoreDailies Daily { get; } = new();", "CoreDailies", "Setup", CoreDailiesIncludes),
        Create("Run daily routine", "Run a daily quest with a map, monster and item target.", "Daily.DailyRoutine(12345, \"map\", \"monster\", \"item\", 1);", "CoreDailies", "Daily quests", CoreDailiesIncludes),
        Create("Check daily completion", "Check a daily quest and optionally unbank required items.", "Daily.CheckDailyv2(12345, true, true, \"Item Name\");", "CoreDailies", "Daily quests", CoreDailiesIncludes),
        Create("Complete NSoD daily", "Run the Necrotic Sword of Doom daily routine.", "Daily.NSoDDaily();", "CoreDailies", "Daily quests", CoreDailiesIncludes),
        Create("Complete NSoD daily without sword checks", "Run the NSoD daily routine while skipping sword checks.", "Daily.NSoDDaily(IgnoreSwords: true);", "CoreDailies", "Daily quests", CoreDailiesIncludes),
        Create("Claim free daily boost", "Claim a free daily boost using the default reward selection.", "Daily.FreeDailyBoost();", "CoreDailies", "Daily rewards", CoreDailiesIncludes),
        Create("Collect Elders Blood", "Collect the Elders Blood daily item.", "Daily.EldersBlood();", "CoreDailies", "Daily items", CoreDailiesIncludes),
        Create("Collect Sparrows Blood", "Collect a requested quantity of Sparrows Blood.", "Daily.SparrowsBlood(3);", "CoreDailies", "Daily items", CoreDailiesIncludes),
        Create("Collect Pearl of Nulgath", "Collect the daily Pearl of Nulgath.", "Daily.PearlOfNulgath();", "CoreDailies", "Daily items", CoreDailiesIncludes),
        Create("Collect Shadow Shroud", "Collect the daily Shadow Shroud.", "Daily.ShadowShroud();", "CoreDailies", "Daily items", CoreDailiesIncludes),
        Create("Collect Crypto Token", "Collect the daily Crypto Token.", "Daily.CryptoToken();", "CoreDailies", "Daily items", CoreDailiesIncludes),
        Create("Collect Power Gem", "Collect the daily Power Gem.", "Daily.PowerGem();", "CoreDailies", "Daily items", CoreDailiesIncludes),
        Create("Collect Golden Inquisitor", "Collect the Golden Inquisitor daily reward.", "Daily.GoldenInquisitor();", "CoreDailies", "Daily items", CoreDailiesIncludes),
        Create("Collect Design Notes", "Collect the daily Design Notes reward.", "Daily.DesignNotes();", "CoreDailies", "Daily items", CoreDailiesIncludes),
        Create("Collect Moglin Pets", "Run the daily Moglin Pets routine.", "Daily.MoglinPets();", "CoreDailies", "Daily items", CoreDailiesIncludes),
        Create("Collect monthly treasure chest keys", "Collect monthly treasure chest keys.", "Daily.MonthlyTreasureChestKeys();", "CoreDailies", "Daily items", CoreDailiesIncludes),
        Create("Spin Wheel of Doom", "Run the daily Wheel of Doom routine.", "Daily.WheelofDoom();", "CoreDailies", "Daily rewards", CoreDailiesIncludes),
        Create("Mine crafting metals", "Collect daily crafting metals.", "Daily.MineCrafting(quant: 2);", "CoreDailies", "Daily items", CoreDailiesIncludes),
        Create("Collect Hardcore Metals", "Collect Hardcore Metals.", "Daily.HardCoreMetals(quant: 1);", "CoreDailies", "Daily items", CoreDailiesIncludes),
        Create("Complete BeastMaster Challenge", "Run the BeastMaster daily challenge.", "Daily.BeastMasterChallenge();", "CoreDailies", "Daily quests", CoreDailiesIncludes),
        Create("Complete Cryomancer daily", "Run the Cryomancer daily routine.", "Daily.Cryomancer();", "CoreDailies", "Daily quests", CoreDailiesIncludes),
        Create("Complete Pyromancer daily", "Run the Pyromancer daily routine.", "Daily.Pyromancer();", "CoreDailies", "Daily quests", CoreDailiesIncludes),
        Create("Complete Collector Class daily", "Run the Collector Class daily routine.", "Daily.CollectorClass();", "CoreDailies", "Daily quests", CoreDailiesIncludes),
        Create("Complete ShadowScythe Class daily", "Run the ShadowScythe Class daily routine.", "Daily.ShadowScytheClass();", "CoreDailies", "Daily quests", CoreDailiesIncludes),
        Create("Collect Elden Ruby", "Collect a requested quantity of Elden Ruby.", "Daily.EldenRuby(25);", "CoreDailies", "Daily items", CoreDailiesIncludes),
        Create("Collect NCS Gem", "Collect a requested quantity of NCS Gem.", "Daily.NCSGem(15);", "CoreDailies", "Daily items", CoreDailiesIncludes),
        Create("Collect Enchanted Dark Blood", "Collect a requested quantity of Enchanted Dark Blood.", "Daily.EnchantedDarkBlood(10);", "CoreDailies", "Daily items", CoreDailiesIncludes),
        Create("Collect Dage's Scroll Fragment", "Collect Dage's Scroll Fragment and optionally bank it.", "Daily.DagesScrollFragment(ToBank: false);", "CoreDailies", "Daily items", CoreDailiesIncludes),

        // CoreAdvanced: boost-aware farming, ultra combat, merges and enhancements.
        Create("CoreAdvanced instance", "Declare the shared CoreAdvanced helper.", "private static CoreAdvanced Adv { get; } = new();", "CoreAdvanced", "Setup", CoreAdvancedIncludes),
        Create("Boosted monster hunt", "Hunt a monster while using the CoreAdvanced boost-aware routine.", "Adv.BoostHuntMonster(\"map\", \"monster\", \"item\", 1);", "CoreAdvanced", "Combat", CoreAdvancedIncludes),
        Create("Boosted monster kill", "Kill a monster at a specific cell while using boost-aware farming.", "Adv.BoostKillMonster(\"map\", \"Enter\", \"Spawn\", \"monster\", \"item\", 1);", "CoreAdvanced", "Combat", CoreAdvancedIncludes),
        Create("Kill ultra boss", "Use the ultra boss helper with a specific map, cell, pad and monster.", "Adv.KillUltra(\"map\", \"Boss\", \"Left\", \"Ultra Boss\", \"item\", 1);", "CoreAdvanced", "Combat", CoreAdvancedIncludes),
        Create("Kill with aura handling", "Use the aura-aware kill helper with an aura list.", "Adv.KillWithAura(\"map\", \"Boss\", \"Left\", \"Boss Name\", new[] { \"Aura Name\" }, item: \"Item Name\", quant: 1);", "CoreAdvanced", "Combat", CoreAdvancedIncludes),
        Create("Buy item with CoreAdvanced", "Buy an item using the advanced shop helper.", "Adv.BuyItem(\"map\", 1234, \"Item Name\", 1);", "CoreAdvanced", "Shops and merges", CoreAdvancedIncludes),
        Create("Buy item by ID with CoreAdvanced", "Buy an item using its item ID.", "Adv.BuyItem(\"map\", 1234, 67890, 1);", "CoreAdvanced", "Shops and merges", CoreAdvancedIncludes),
        Create("Start merge purchase", "Run a merge purchase and supply the ingredient selection logic.", "Adv.StartBuyAllMerge(\"map\", 1234, () => { /* Add ingredient purchase logic here. */ });", "CoreAdvanced", "Shops and merges", CoreAdvancedIncludes),
        Create("Rank up class", "Rank up a class and restore the equipped gear afterward.", "Adv.RankUpClass(\"Class Name\");", "CoreAdvanced", "Classes and gear", CoreAdvancedIncludes),
        Create("Store and restore gear", "Store current gear before a routine and restore it afterward.", "Adv.GearStore();\n// After the routine completes:\nAdv.GearStore(Restore: true);", "CoreAdvanced", "Classes and gear", CoreAdvancedIncludes),
        Create("Enhance equipped gear", "Apply selected enhancements to equipped gear.", "Adv.EnhanceEquipped(EnhancementType.Lucky);", "CoreAdvanced", "Enhancements", CoreAdvancedIncludes),
        Create("Enhance one item", "Apply enhancements to a named item.", "Adv.EnhanceItem(\"Weapon Name\", EnhancementType.Lucky);", "CoreAdvanced", "Enhancements", CoreAdvancedIncludes),
        Create("Check minimum boost", "Check whether equipped gear meets a minimum boost percentage.", "if (Adv.HasMinimalBoost(GenericGearBoost.gold, 25))\n    Core.Logger(\"Gold boost requirement met\");", "CoreAdvanced", "Boosts", CoreAdvancedIncludes),
        Create("Smart enhance class", "Choose enhancements based on the current or specified class.", "Adv.SmartEnhance(\"Class Name\");", "CoreAdvanced", "Enhancements", CoreAdvancedIncludes)
    ];

    /// <summary>
    /// Returns all snippets in their display order.
    /// </summary>
    public IReadOnlyList<ScriptSnippet> GetSnippets() => Snippets;

    /// <summary>
    /// Returns the supported core names for the snippet filter.
    /// </summary>
    public IReadOnlyList<string> GetCores() => ["All cores", "CoreBots", "CoreFarms", "CoreDailies", "CoreAdvanced"];

    /// <summary>
    /// Returns the distinct snippet categories in their display order.
    /// </summary>
    public IReadOnlyList<string> GetCategories() => ["All categories", .. Snippets.Select(snippet => snippet.Category).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(category => category, StringComparer.OrdinalIgnoreCase)];

    private static ScriptSnippet Create(string name, string description, string code, string core, string category, string[] includes) =>
        new(name, description, code.Trim(), core, category, includes);
}
