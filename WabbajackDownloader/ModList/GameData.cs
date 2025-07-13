using System.Collections.Generic;

namespace WabbajackDownloader.ModList;

internal record GameData(string ArchiveName, string NexusName, long NexusGameID)
{
    /// <summary>
    /// Retrieve a GameData from an ArchiveName
    /// </summary>
    public static IReadOnlyDictionary<string, GameData> GameLookup = new Dictionary<string, GameData>
    {
        {
            "morrowind", new GameData("morrowind", "morrowind", 100)
        },
        {
            "oblivion", new GameData("oblivion", "oblivion", 101)
        },
        {
            "fallout3", new GameData("fallout3", "fallout3", 120)
        },
        {
            "falloutnv", new GameData("falloutnv", "newvegas", 130)
        },
        {
            "skyrim", new GameData("skyrim", "skyrim", 110)
        },
        {
            "skyrimse", new GameData("skyrimse", "skyrimspecialedition", 1704)
        },
        {
            "fallout4", new GameData("fallout4", "fallout4", 1151)
        },
        {
            "enderal", new GameData("enderal", "enderal", 2736)
        },
        {
            "enderalse", new GameData("enderalse", "enderalspecialedition", 3685)
        },
        {
            "dishonored", new GameData("dishonored", "dishonored", 802)
        },
        {
            "witcher", new GameData("witcher", "witcher", 150)
        },
        {
            "witcher3", new GameData("witcher3", "witcher3", 952)
        },
        {
            "stardewvalley", new GameData("stardewvalley", "stardewvalley", 1303)
        },
        {
            "kingdomcomedeliverance", new GameData("kingdomcomedeliverance", "kingdomcomedeliverance", 2298)
        },
        {
            "mechwarrior5mercenaries", new GameData("mechwarrior5mercenaries", "mechwarrior5mercenaries", 3099)
        },
        {
            "dragonsdogma", new GameData("dragonsdogma", "dragonsdogma", 1249)
        },
        {
            "valheim", new GameData("valheim", "valheim", 3667)
        },
        {
            "mountandblade2bannerlord", new GameData("mountandblade2bannerlord", "mountandblade2bannerlord", 3174)
        },
        {
            "finalfantasy7remake", new GameData("finalfantasy7remake", "finalfantasy7remake", 4202)
        },
        {
            "baldursgate3", new GameData("baldursgate3", "baldursgate3", 3474)
        },
        {
            "Starfield", new GameData("Starfield", "starfield", 4187)
        },
        {
            "7daystodie", new GameData("7daystodie", "7daystodie", 1059)
        },
        {
            "oblivionremastered", new GameData("oblivionremastered", "oblivionremastered", 7587)
        },
        {
            "site", new GameData("site", "site", 2295)
        }
    };
}