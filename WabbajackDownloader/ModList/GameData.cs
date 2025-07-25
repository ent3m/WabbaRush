using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;

namespace WabbajackDownloader.ModList;

internal record GameData
{
    /// Adapted from https://github.com/wabbajack-tools/wabbajack/blob/main/Wabbajack.DTOs/Game/GameRegistry.cs
    public static IReadOnlyDictionary<Game, GameData> Games = new Dictionary<Game, GameData>
    {
        {
            Game.Morrowind, new GameData
            {
                Game = Game.Morrowind,
                SteamIDs = new[] {22320},
                GOGIDs = new long[] {1440163901, 1435828767},
                NexusName = "morrowind",
                NexusGameId = 100,
                MO2Name = "Morrowind",
                MO2ArchiveName = "morrowind",
                BethNetID = 31,
                RegString =
                    @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\The Elder Scrolls III: Morrowind Game of the Year Edition",
                IconSource = "https://cdn2.steamgriddb.com/icon/661c1c090ff5831a647202397c61d73c/24/32x32.png"
            }
        },
        {
            Game.Oblivion, new GameData
            {
                Game = Game.Oblivion,
                NexusName = "oblivion",
                NexusGameId = 101,
                MO2Name = "Oblivion",
                MO2ArchiveName = "oblivion",
                SteamIDs = new[] {22330},
                GOGIDs = new long[] {1458058109},
                IconSource = "https://cdn2.steamgriddb.com/icon/e403262769f74b83009bffb6e3c0a3b7/32/32x32.png"
            }
        },

        {
            Game.Fallout3, new GameData
            {
                Game = Game.Fallout3,
                NexusName = "fallout3",
                NexusGameId = 120,
                MO2Name = "Fallout 3",
                MO2ArchiveName = "fallout3",
                SteamIDs = new[] {22300, 22370}, // base game and GotY
                GOGIDs = new long[] {1454315831}, // GotY edition
                IconSource = "https://cdn2.steamgriddb.com/icon/ac7ed855f313b05391de74046180fb34.png"
            }
        },
        {
            Game.FalloutNewVegas, new GameData
            {
                Game = Game.FalloutNewVegas,
                NexusName = "newvegas",
                NexusGameId = 130,
                MO2Name = "New Vegas",
                MO2ArchiveName = "falloutnv",
                SteamIDs = new[] {22380, 22490}, // normal and RU version
                GOGIDs = new long[] {1454587428},
                EpicGameStoreIDs = new[] {"dabb52e328834da7bbe99691e374cb84"},
                IconSource = "https://cdn2.steamgriddb.com/icon/c706723a17a2b2acec4f9ebc9f572e31.png"
            }
        },
        {
            Game.Skyrim, new GameData
            {
                Game = Game.Skyrim,
                NexusName = "skyrim",
                NexusGameId = 110,
                MO2Name = "Skyrim",
                MO2ArchiveName = "skyrim",
                SteamIDs = new[] {72850},
                CommonlyConfusedWith = new[] {Game.SkyrimSpecialEdition, Game.SkyrimVR},
                IconSource = "https://cdn2.steamgriddb.com/icon/58ee2794cc87707943624dc8db2ff5a0/8/32x32.png"
            }
        },
        {
            Game.SkyrimSpecialEdition, new GameData
            {
                Game = Game.SkyrimSpecialEdition,
                NexusName = "skyrimspecialedition",
                NexusGameId = 1704,
                MO2Name = "Skyrim Special Edition",
                MO2ArchiveName = "skyrimse",
                SteamIDs = new[] {489830},
                GOGIDs = new long[]
                {
                    1711230643,// The Elder Scrolls V: Skyrim Special Edition AKA Base Game
                    1801825368,// The Elder Scrolls V: Skyrim Anniversary Edition AKA The Store Bundle 
                    1162721350 // Upgrade DLC
                },
                CommonlyConfusedWith = new[] {Game.Skyrim, Game.SkyrimVR},
                IconSource = "https://cdn2.steamgriddb.com/icon/e1b90346c92331860b1391257a106bb1/32/32x32.png"
            }
        },
        {
            Game.Fallout4, new GameData
            {
                Game = Game.Fallout4,
                NexusName = "fallout4",
                NexusGameId = 1151,
                MO2Name = "Fallout 4",
                MO2ArchiveName = "fallout4",
                SteamIDs = new[] {377160},
                GOGIDs = new long[]{1998527297},
                CommonlyConfusedWith = new[] {Game.Fallout4VR},
                CanSourceFrom = new[] {Game.Fallout76},
                IconSource = "https://cdn2.steamgriddb.com/icon/578d9dd532e0be0cdd050b5bec4967a1.png"
            }
        },
        {
            Game.SkyrimVR, new GameData
            {
                Game = Game.SkyrimVR,
                NexusName = "skyrimspecialedition",
                NexusGameId = 1704,
                MO2Name = "Skyrim VR",
                MO2ArchiveName = "skyrimse",
                SteamIDs = new[] {611670},
                CommonlyConfusedWith = new[] {Game.Skyrim, Game.SkyrimSpecialEdition},
                CanSourceFrom = new[] {Game.SkyrimSpecialEdition},
                IconSource = "https://cdn2.steamgriddb.com/icon/75b3f26dde5a6c2a415464b05bd46fbc.png"
            }
        },
        {
            Game.Enderal, new GameData
            {
                Game = Game.Enderal,
                NexusName = "enderal",
                NexusGameId = 2736,
                MO2Name = "Enderal",
                MO2ArchiveName = "enderal",
                SteamIDs = new[] {1027920, 933480},
                CommonlyConfusedWith = new[] {Game.EnderalSpecialEdition},
                IconSource = "https://cdn2.steamgriddb.com/icon/6505e8a0c0e1a90d8da8879e49a437f0.png"
            }
        },
        {
            Game.EnderalSpecialEdition, new GameData
            {
                Game = Game.EnderalSpecialEdition,
                NexusName = "enderalspecialedition",
                NexusGameId = 3685,
                MO2Name = "Enderal Special Edition",
                MO2ArchiveName = "enderalse",
                SteamIDs = new[] {976620},
                GOGIDs = new long[] {1708684988},
                CommonlyConfusedWith = new[] {Game.Enderal},
                IconSource = "https://cdn2.steamgriddb.com/icon/104c6f99020b85465ae361a92d09a8d1.png"
            }
        },
        {
            Game.Fallout4VR, new GameData
            {
                Game = Game.Fallout4VR,
                NexusName = "fallout4",
                NexusGameId = 1151,
                MO2Name = "Fallout 4 VR",
                MO2ArchiveName = "Fallout4",
                SteamIDs = new[] {611660},
                CommonlyConfusedWith = new[] {Game.Fallout4},
                CanSourceFrom = new[] {Game.Fallout4},
                IconSource = "https://cdn2.steamgriddb.com/icon/9058c666789874c718d1976270cee814.png"
            }
        },
        {
            Game.DarkestDungeon, new GameData
            {
                Game = Game.DarkestDungeon,
                NexusName = "darkestdungeon",
                MO2Name = "Darkest Dungeon",
                NexusGameId = 804,
                SteamIDs = new[] {262060},
                GOGIDs = new long[] {1450711444},
                EpicGameStoreIDs = new[] {"b4eecf70e3fe4e928b78df7855a3fc2d"},
                IsGenericMO2Plugin = true,
                IconSource = "https://cdn2.steamgriddb.com/icon/b1d2128cee734a257c5e0d5c73bbdd1b.png"
            }
        },
        {
            Game.Dishonored, new GameData
            {
                Game = Game.Dishonored,
                NexusName = "dishonored",
                MO2Name = "Dishonored",
                MO2ArchiveName = "dishonored",
                NexusGameId = 802,
                SteamIDs = new[] {205100},
                GOGIDs = new long[] {1701063787},
                IconSource = "https://cdn2.steamgriddb.com/icon/6fcd734d28ae00944f8f7c68a219bbc5/32/32x32.png"
            }
        },
        {
            Game.Witcher, new GameData
            {
                Game = Game.Witcher,
                NexusName = "witcher",
                NexusGameId = 150,
                MO2Name = "The Witcher: Enhanced Edition",
                MO2ArchiveName = "witcher",
                SteamIDs = new[] {20900}, // normal and GotY
                GOGIDs = new long[] {1207658924}, // normal, GotY and both in packages
                IconSource = "https://cdn2.steamgriddb.com/icon/fd72ecaa23aa0a514a53c6a16eabb9c6.png"
            }
        },
        {
            Game.Witcher3, new GameData
            {
                Game = Game.Witcher3,
                NexusName = "witcher3",
                NexusGameId = 952,
                MO2Name = "The Witcher 3: Wild Hunt",
                MO2ArchiveName = "witcher3",
                SteamIDs = new[] {292030, 499450}, // normal and GotY
                GOGIDs = new long[]
                    {1207664643, 1495134320, 1207664663, 1640424747}, // normal, GotY and both in packages
                IconSource = "https://cdn2.steamgriddb.com/icon/2af9b1a840b4ecd522fe1cda88c8385e/32/32x32.png"
            }
        },
        {
            Game.StardewValley, new GameData
            {
                Game = Game.StardewValley,
                NexusName = "stardewvalley",
                MO2Name = "Stardew Valley",
                MO2ArchiveName = "stardewvalley",
                NexusGameId = 1303,
                SteamIDs = new[] {413150},
                GOGIDs = new long[] {1453375253},
                IsGenericMO2Plugin = true,
                IconSource = "https://cdn2.steamgriddb.com/icon/f6c4718557e1197ecdbe1b7ff52975d2.png"
            }
        },
        {
            Game.KingdomComeDeliverance, new GameData
            {
                Game = Game.KingdomComeDeliverance,
                NexusName = "kingdomcomedeliverance",
                MO2Name = "Kingdom Come: Deliverance",
                MO2ArchiveName = "kingdomcomedeliverance",
                NexusGameId = 2298,
                SteamIDs = new[] {379430},
                GOGIDs = new long[] {1719198803},
                IsGenericMO2Plugin = true,
                IconSource = "https://cdn2.steamgriddb.com/icon/1bdde90ebfdef547440410e79b1877bf.png"
            }
        },
        {
            Game.MechWarrior5Mercenaries, new GameData
            {
                Game = Game.MechWarrior5Mercenaries,
                NexusName = "mechwarrior5mercenaries",
                MO2Name = "Mechwarrior 5: Mercenaries",
                MO2ArchiveName = "mechwarrior5mercenaries",
                NexusGameId = 3099,
                EpicGameStoreIDs = new[] {"9fd39d8ac72946a2a10a887ce86e6c35"},
                IsGenericMO2Plugin = true,
                IconSource = "https://cdn2.steamgriddb.com/icon/c59bb6bab3096620efe78bdeb031f027/8/32x32.png"
            }
        },
        {
            Game.NoMansSky, new GameData
            {
                Game = Game.NoMansSky,
                NexusName = "nomanssky",
                NexusGameId = 1634,
                MO2Name = "No Man's Sky",
                SteamIDs = new[] {275850},
                GOGIDs = new long[] {1446213994},
                IconSource = "https://cdn2.steamgriddb.com/icon/970e789e0a92eab99bcabf36dfa6050c/32/32x32.png"
            }
        },
        {
            Game.DragonAgeOrigins, new GameData
            {
                Game = Game.DragonAgeOrigins,
                NexusName = "dragonage",
                NexusGameId = 140,
                MO2Name = "Dragon Age: Origins",
                SteamIDs = new[] {47810},
                OriginIDs = new[] {"DR:169789300", "DR:208591800"},
                EADesktopIDs = new [] // Possibly Wrong
                {
                    "9df89a8e-b201-4507-8a8d-bd6799fedb18",
                    "Origin.SFT.50.0000078",
                    "Origin.SFT.50.0000078",
                    "Origin.SFT.50.0000078",
                    "Origin.SFT.50.0000085",
                    "Origin.SFT.50.0000086",
                    "Origin.SFT.50.0000087",
                    "Origin.SFT.50.0000088",
                    "Origin.SFT.50.0000089",
                    "Origin.SFT.50.0000090",
                    "Origin.SFT.50.0000091",
                    "Origin.SFT.50.0000097",
                    "Origin.SFT.50.0000098"
                },
                GOGIDs = new long[] {1949616134},
                IconSource = "https://cdn2.steamgriddb.com/icon/b55d7ce2adb9449fc4dae6115cbbe30f/32/32x32.png"
            }
        },
        {
            Game.DragonAge2, new GameData
            {
                Game = Game.DragonAge2,
                NexusName = "dragonage2",
                NexusGameId = 141,
                MO2Name = "Dragon Age 2", // Probably wrong
                SteamIDs = new[] {1238040},
                OriginIDs = new[] {"OFB-EAST:59474", "DR:201797000"},
                EADesktopIDs = new [] // Possibly Wrong
                {
                    "Origin.SFT.50.0000073",
                    "Origin.SFT.50.0000255",
                    "Origin.SFT.50.0000256",
                    "Origin.SFT.50.0000257",
                    "Origin.SFT.50.0000288",
                    "Origin.SFT.50.0000310",
                    "Origin.SFT.50.0000311",
                    "Origin.SFT.50.0000356",
                    "Origin.SFT.50.0000385",
                    "Origin.SFT.50.0000429",
                    "Origin.SFT.50.0000449",
                    "Origin.SFT.50.0000452",
                    "Origin.SFT.50.0000453"
                },
                IconSource = "https://cdn2.steamgriddb.com/icon/a6a946f7265ed7f28a6425ee76621c3a/32/32x32.png"
            }
        },
        {
            Game.DragonAgeInquisition, new GameData
            {
                Game = Game.DragonAgeInquisition,
                NexusName = "dragonageinquisition",
                NexusGameId = 728,
                MO2Name = "Dragon Age: Inquisition", // Probably wrong
                SteamIDs = new[] {1222690},
                OriginIDs = new[] {"OFB-EAST:51937", "OFB-EAST:1000032"},
                IconSource = "https://cdn2.steamgriddb.com/icon/b98004311446c60521a8831075423c20.png"
            }
        },
        {
            Game.KerbalSpaceProgram, new GameData
            {
                Game = Game.KerbalSpaceProgram,
                NexusName = "kerbalspaceprogram",
                MO2Name = "Kerbal Space Program",
                NexusGameId = 272,
                SteamIDs = new[] {220200},
                GOGIDs = new long[] {1429864849},
                IsGenericMO2Plugin = true,
                IconSource = "https://cdn2.steamgriddb.com/icon/2ee4162f4a89db5fa43b3b08900ee370.png"
            }
        },
        {
            Game.Terraria, new GameData
            {
                Game = Game.Terraria,
                SteamIDs = new[] {1281930},
                MO2Name = "Terraria",
                IsGenericMO2Plugin = true,
                IconSource = "https://cdn2.steamgriddb.com/icon/e658047c67a80c47b5ba982ab520b59a.png"
            }
        },
        {
           Game.Cyberpunk2077, new GameData
           {
                Game = Game.Cyberpunk2077,
                SteamIDs = new[] {1091500},
                GOGIDs = new long[] {2093619782, 1423049311},
                EpicGameStoreIDs = new[] {"5beededaad9743df90e8f07d92df153f"},
                MO2Name = "Cyberpunk 2077",
                NexusName = "cyberpunk2077",
                NexusGameId = 3333,
                IsGenericMO2Plugin = true,
                IconSource = "https://cdn2.steamgriddb.com/icon/2d45da15db966ba887cf4e573989fcc8/32/32x32.png"
            }
        },
        {
           Game.Sims4, new GameData
           {
                Game = Game.Sims4,
                SteamIDs = new[] {1222670},
                MO2Name = "The Sims 4",
                NexusName = "thesims4",
                NexusGameId = 641,
                IsGenericMO2Plugin = true,
                IconSource = "https://cdn2.steamgriddb.com/icon/9fc664916bce863561527f06a96f5ff3/32/32x32.png"
            }
        },
        {
            Game.DragonsDogma, new GameData
            {
                Game = Game.DragonsDogma,
                SteamIDs = new[] {367500 },
                GOGIDs = new long[]{1242384383},
                MO2Name = "Dragon's Dogma: Dark Arisen",
                MO2ArchiveName = "dragonsdogma",
                NexusName = "dragonsdogma",
                NexusGameId = 1249,
                IsGenericMO2Plugin = true,
                IconSource = "https://cdn2.steamgriddb.com/icon/a830839bbb4a4022a84ff2b8af5c46e0.png"
            }
        },
        {
            Game.KarrynsPrison, new GameData
            {
                Game = Game.KarrynsPrison,
                SteamIDs = new[] { 1619750 },
                MO2Name = "Karryn's Prison",
                MO2ArchiveName = "karrynsprison",
                IsGenericMO2Plugin = false,
                IconSource = "https://cdn2.steamgriddb.com/icon/37286bc401299e97a564f6b42792eb6d.png"
            }
        },
        {
            Game.Valheim, new GameData
            {
                Game = Game.Valheim,
                SteamIDs = new[] { 892970 },
                MO2Name = "Valheim",
                MO2ArchiveName = "valheim",
                NexusName = "valheim",
                NexusGameId = 3667,
                IsGenericMO2Plugin = true,
                IconSource = "https://cdn2.steamgriddb.com/icon/dd055f53a45702fe05e449c30ac80df9/32/32x32.png"
            }
        },
        {
            Game.MountAndBlade2Bannerlord, new GameData
            {
                Game = Game.MountAndBlade2Bannerlord,
                NexusName = "mountandblade2bannerlord",
                NexusGameId = 3174,
                MO2Name = "Mount & Blade II: Bannerlord",
                MO2ArchiveName = "mountandblade2bannerlord",
                SteamIDs = new[] { 261550 },
                GOGIDs = new long[] {
                    1564781494, //Mount & Blade II: Bannerlord : Game
                    1681929523, //Mount & Blade II: Bannerlord - Digital Deluxe : Package
                    1802539526, //Mount & Blade II: Bannerlord : Package
                },
                IsGenericMO2Plugin = true,
                IconSource = "https://cdn2.steamgriddb.com/icon/811cf46d61c9ae564bf7fa4b5abc639b.png"
            }
        },
        {
            Game.FinalFantasy7Remake, new GameData
            {
                Game = Game.FinalFantasy7Remake,
                NexusName = "finalfantasy7remake",
                NexusGameId = 4202,
                MO2Name = "FINAL FANTASY VII REMAKE INTERGRADE",
                MO2ArchiveName = "finalfantasy7remake",
                SteamIDs = new[] { 1462040 },
                IsGenericMO2Plugin = true,
                IconSource = "https://cdn2.steamgriddb.com/icon/d9b47f916e531ac9ef2b0887ca72d698.png"
            }
        },
        {
            Game.BaldursGate3, new GameData
            {
                Game = Game.BaldursGate3,
                NexusName = "baldursgate3",
                NexusGameId = 3474,
                MO2Name = "Baldur's Gate 3",
                MO2ArchiveName = "baldursgate3",
                SteamIDs = [1086940],
                GOGIDs = [1456460669],
                IsGenericMO2Plugin = true,
                IconSource = "https://cdn2.steamgriddb.com/icon/cdb3fcd3d3fde62fe3b549a90793467e.png"

            }
        },
        {
            Game.Starfield, new GameData
            {
                Game = Game.Starfield,
                NexusName = "starfield",
                NexusGameId = 4187,
                MO2Name = "Starfield",
                MO2ArchiveName = "Starfield",
                SteamIDs = [1716740],
                IconSource = "https://cdn2.steamgriddb.com/icon/1a495bc86abe171f690e27192ea6c367.png"
            }
        },
        {
            Game.SevenDaysToDie, new GameData
            {
                Game = Game.SevenDaysToDie,
                MO2Name = "7 Days to Die",
                NexusName = "7daystodie",
                NexusGameId = 1059,
                MO2ArchiveName = "7daystodie",
                SteamIDs = [251570],
                IconSource = "https://cdn2.steamgriddb.com/icon/2b1f462a660e29c47acdcc25cb14d321.png",
            }
        },
        {
            Game.OblivionRemastered, new GameData
            {
                Game = Game.OblivionRemastered,
                MO2Name = "Oblivion Remastered",
                NexusName = "oblivionremastered",
                NexusGameId = 7587,
                MO2ArchiveName = "oblivionremastered",
                SteamIDs = [2623190],
                IconSource = "https://cdn2.steamgriddb.com/icon/0ee98f8910782a3277576e5839372116.png",
            }
        },
        {
            Game.Fallout76, new GameData
            {
                Game = Game.Fallout76,
                NexusName = "fallout76",
                NexusGameId = 2590,
                MO2Name = "Fallout 76",
                MO2ArchiveName = "fallout76",
                SteamIDs = new[] {1151340},
                CanSourceFrom = new[] {Game.Fallout4},
                IconSource = "https://cdn2.steamgriddb.com/icon/b7196f5fd0fce35ccadc7001fd067588/32/32x32.png"
            }
        },
        {
            Game.Fallout4London, new GameData
            {
                Game = Game.Fallout4London,
                NexusName = "fallout4london",
                NexusGameId = 6332,
                MO2Name = "Fallout 4 London",
                MO2ArchiveName = "Fallout4London",
                GOGIDs = new long[] {
                    1491728574, //Fallout: London : Game
                    1897848199, //Fallout: London One-click Edition : One-Click Mod Install
                },
                CommonlyConfusedWith = new[] {Game.Fallout4},
                CanSourceFrom = new[] {Game.Fallout4},
                IconSource = "https://cdn2.steamgriddb.com/icon/2cb3742470f550f41aea34a0702e4d63.png"
            }
            },
        {
            Game.Warhammer40kDarktide, new GameData
            {
                Game = Game.Warhammer40kDarktide,
                MO2Name = "Warhammer 40,000: Darktide",
                NexusName = "warhammer40kdarktide",
                NexusGameId = 4943,
                MO2ArchiveName = "warhammer40kdarktide",
                SteamIDs = [1361210],
                IconSource = "https://cdn2.steamgriddb.com/icon/da81831bd5b381d45aa1fae29aeb242f.png",
            }
        },
        {
            Game.Kotor2, new GameData
            {
                Game = Game.Kotor2,
                MO2Name = "STAR WARS Knights of the Old Republic II The Sith Lords",
                NexusName = "kotor2",
                NexusGameId = 198,
                MO2ArchiveName = "kotor2",
                SteamIDs = [208580],
                GOGIDs = new long[] { 1421404581 },
                IconSource = "https://cdn2.steamgriddb.com/icon/2d00f43f07911355d4151f13925ff292/24/32x32.png",
            }
        },
        {
            Game.VtMB, new GameData
            {
                Game = Game.VtMB,
                MO2Name = "Vampire - The Masquerade: Bloodlines",
                NexusName = "vampirebloodlines",
                NexusGameId = 437,
                MO2ArchiveName = "vampire",
                SteamIDs = [2600],
                GOGIDs = [
                    1207659240,//Game
                    1265943179//Game + Unofficial Patch Pre-Installed
                ],
                IconSource = "https://cdn2.steamgriddb.com/icon/d4ab2732ed8ac6a4b2d9734cf4c851d2/32/32x32.png",
            }
        },
        {
            Game.ModdingTools, new GameData
            {
                Game = Game.ModdingTools,
                MO2Name = "Modding Tools",
                MO2ArchiveName = "site",
                NexusName = "site",
                NexusGameId = 2295,
                IsGenericMO2Plugin = false,
            }
        }

    };

    /// <summary>
    /// Retrieve a GameData from an ArchiveName
    /// </summary>
    public static FrozenDictionary<string, GameData> GameLookup = Games.Values
        .DistinctBy<GameData, string?>(g => g.MO2ArchiveName ?? g.NexusName, StringComparer.InvariantCultureIgnoreCase)
        .ToFrozenDictionary<GameData, string>(g => g.MO2ArchiveName ?? g.NexusName ?? "", StringComparer.InvariantCultureIgnoreCase);

    public Game Game { get; internal init; }

    public bool IsGenericMO2Plugin { get; internal init; }

    public string? MO2ArchiveName { get; internal init; }

    public string? NexusName { get; internal init; }

    // Nexus DB id for the game, used in some specific situations
    public long NexusGameId { get; internal init; }
    public string? MO2Name { get; internal init; }

    // to get steam ids: https://steamdb.info
    public int[] SteamIDs { get; internal init; } = Array.Empty<int>();

    // to get gog ids: https://www.gogdb.org
    public long[] GOGIDs { get; internal init; } = Array.Empty<long>();

    // to get these ids, split the numbers from the letters in file names found in
    // C:\ProgramData\Origin\LocalContent\{game name)\*.mfst
    // So for DA:O this is "DR208591800.mfst" -> "DR:208591800"
    // EAPlay games may have @subscription appended to the file name
    public string[] OriginIDs { get; set; } = Array.Empty<string>();

    public string[] EADesktopIDs { get; set; } = Array.Empty<string>();

    public string[] EpicGameStoreIDs { get; internal init; } = Array.Empty<string>();

    // to get BethNet IDs: check the registry
    public int BethNetID { get; internal init; }

    //for BethNet games only!
    public string RegString { get; internal init; } = string.Empty;

    // Games that this game are commonly confused with, for example Skyrim SE vs Skyrim LE
    public Game[] CommonlyConfusedWith { get; set; } = Array.Empty<Game>();

    /// <summary>
    ///     Other games this game can pull source files from (if the game is installed on the user's machine)
    /// </summary>
    public Game[] CanSourceFrom { get; set; } = Array.Empty<Game>();

    /// <summary>
    /// URI to an ICO / PNG, preferred size 32x32
    /// </summary>
    public string IconSource { get; set; } = @"Resources/Icons/wabbajack.ico";
}