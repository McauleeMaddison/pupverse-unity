#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Pupverse.Editor
{
    public static class PupverseAllCardDataBuilder
    {
        const string CardsRoot = "Assets/Cards";
        const string ArtRoot = "Assets/Art/Cards";

        sealed class CardSpec
        {
            public string set;
            public string assetName;
            public string id;
            public string displayName;
            public int year;
            public CardRarity rarity;
            public string element;
            public string imageFile;

            public StatBlock stats;

            public string abilityName;
            public string abilityDescription;
            public StatBlock boosts;

            public bool sourceBacked;

            public CardSpec(
                string set,
                string assetName,
                string id,
                string displayName,
                int year,
                CardRarity rarity,
                string element,
                string imageFile,
                int power,
                int speed,
                int intelligence,
                int defence,
                int luck,
                string abilityName,
                string abilityDescription,
                int boostPower = 0,
                int boostSpeed = 0,
                int boostIntelligence = 0,
                int boostDefence = 0,
                int boostLuck = 0,
                bool sourceBacked = true
            )
            {
                this.set = set;
                this.assetName = assetName;
                this.id = id;
                this.displayName = displayName;
                this.year = year;
                this.rarity = rarity;
                this.element = element;
                this.imageFile = imageFile;

                stats = MakeStats(
                    power,
                    speed,
                    intelligence,
                    defence,
                    luck
                );

                this.abilityName = abilityName;
                this.abilityDescription = abilityDescription;

                boosts = MakeStats(
                    boostPower,
                    boostSpeed,
                    boostIntelligence,
                    boostDefence,
                    boostLuck
                );

                this.sourceBacked = sourceBacked;
            }
        }

        [MenuItem("Pupverse/Cards/Build All 57 CardData Assets")]
        public static void BuildAllCards()
        {
            EnsureFolder(CardsRoot);
            EnsureFolder(CardsRoot + "/Crypto");
            EnsureFolder(CardsRoot + "/Cyber");
            EnsureFolder(CardsRoot + "/Alien");

            List<CardSpec> cards = BuildCardSpecs();

            int created = 0;
            int updated = 0;
            int missingArt = 0;
            int sourceBacked = 0;
            int newlyBalanced = 0;

            foreach (CardSpec spec in cards)
            {
                string folder =
                    CardsRoot + "/" + SetFolder(spec.set);

                string existingRootPath =
                    CardsRoot + "/" +
                    spec.assetName +
                    ".asset";

                string targetPath =
                    folder + "/" +
                    spec.assetName +
                    ".asset";

                CardData card =
                    AssetDatabase.LoadAssetAtPath<CardData>(
                        existingRootPath
                    );

                bool isNew = false;

                if (card == null)
                {
                    card =
                        AssetDatabase.LoadAssetAtPath<CardData>(
                            targetPath
                        );
                }

                if (card == null)
                {
                    card =
                        ScriptableObject.CreateInstance<CardData>();

                    AssetDatabase.CreateAsset(
                        card,
                        targetPath
                    );

                    isNew = true;
                    created++;
                }
                else
                {
                    updated++;
                }

                card.id = spec.id;
                card.displayName = spec.displayName;
                card.series = spec.set;
                card.element = spec.element;
                card.year = spec.year;
                card.rarity = spec.rarity;

                card.stats = spec.stats;

                card.abilityName =
                    spec.abilityName;

                card.abilityDescription =
                    spec.abilityDescription;

                card.abilityBoosts =
                    spec.boosts;

                string artPath =
                    ArtRoot + "/" +
                    SetArtFolder(spec.set) +
                    "/" +
                    spec.imageFile;

                Texture2D art =
                    AssetDatabase.LoadAssetAtPath<Texture2D>(
                        artPath
                    );

                if (art == null)
                {
                    Debug.LogWarning(
                        "Missing artwork for " +
                        spec.displayName +
                        ": " +
                        artPath
                    );

                    missingArt++;
                }
                else
                {
                    card.originalCardArt = art;
                }

                /*
                 * Do not overwrite Brooklyn/Raven/Jinx's
                 * existing hand-tuned portrait settings.
                 */
                if (isNew)
                {
                    card.heroArt = null;

                    card.portraitUV =
                        new Rect(
                            0f,
                            0f,
                            1f,
                            1f
                        );
                }

                if (string.IsNullOrWhiteSpace(card.fact))
                {
                    card.fact =
                        spec.displayName +
                        " enters the PupVerse arena with " +
                        spec.abilityName +
                        ".";
                }

                if (spec.sourceBacked)
                    sourceBacked++;
                else
                    newlyBalanced++;

                EditorUtility.SetDirty(card);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            DefaultAsset cardsFolder =
                AssetDatabase.LoadAssetAtPath<DefaultAsset>(
                    CardsRoot
                );

            Selection.activeObject = cardsFolder;

            Debug.Log(
                "PupVerse CardData build complete.\n" +
                "Total cards: " + cards.Count + "\n" +
                "Created: " + created + "\n" +
                "Updated: " + updated + "\n" +
                "Existing source-backed data: " +
                sourceBacked + "\n" +
                "New balanced PupVerse data: " +
                newlyBalanced + "\n" +
                "Missing artwork: " + missingArt
            );
        }

        static List<CardSpec> BuildCardSpecs()
        {
            return new List<CardSpec>
            {
                // =====================================================
                // CRYPTOPUPS
                // =====================================================

                new CardSpec(
                    "CryptoPups",
                    "Ace",
                    "crypto-astro",
                    "Ace",
                    2025,
                    CardRarity.Epic,
                    "Ghostglitch",
                    "ace-front.png.jpeg",
                    81, 89, 85, 77, 82,
                    "Phantom Swipe",
                    "Ace phases through danger, gaining +18 Speed and improved evasion for one round.",
                    boostSpeed: 18
                ),

                new CardSpec(
                    "CryptoPups",
                    "Bandit",
                    "crypto-bandit",
                    "Bandit",
                    2024,
                    CardRarity.Rare,
                    "Nightwind",
                    "bandit-front.png",
                    88, 92, 76, 84, 73,
                    "Shadow Dash",
                    "Bandit dashes through the shadows, gaining +25 Speed for one round.",
                    boostSpeed: 25
                ),

                new CardSpec(
                    "CryptoPups",
                    "Brooklyn",
                    "crypto-brooklyn",
                    "Brooklyn",
                    2024,
                    CardRarity.Epic,
                    "Wavebyte",
                    "brooklyn-front.png",
                    85, 87, 78, 80, 79,
                    "Coast Rush",
                    "Brooklyn charges with ocean momentum, gaining +15 Speed and +10 Power for one round.",
                    boostPower: 10,
                    boostSpeed: 15
                ),

                new CardSpec(
                    "CryptoPups",
                    "CharlieAndBuster",
                    "crypto-charlie-buster",
                    "Charlie & Buster",
                    2025,
                    CardRarity.Legendary,
                    "Twin Circuit",
                    "charlie&buster-front.png",
                    88, 84, 86, 82, 79,
                    "Double Trouble",
                    "Charlie & Buster synchronise their twin crypto cores, gaining +12 Power and +12 Speed.",
                    boostPower: 12,
                    boostSpeed: 12,
                    sourceBacked: false
                ),

                new CardSpec(
                    "CryptoPups",
                    "Dallas",
                    "crypto-dallas",
                    "Dallas",
                    2025,
                    CardRarity.Rare,
                    "Chainbyte",
                    "dallas-front.png",
                    84, 79, 77, 88, 72,
                    "Lockdown Bark",
                    "Dallas locks down the arena firewall, gaining +16 Defence and +8 Power.",
                    boostPower: 8,
                    boostDefence: 16,
                    sourceBacked: false
                ),

                new CardSpec(
                    "CryptoPups",
                    "Darla",
                    "crypto-darla",
                    "Darla",
                    2025,
                    CardRarity.Epic,
                    "Emerald Cipher",
                    "darla-front.png",
                    76, 82, 91, 78, 86,
                    "Code Bloom",
                    "Darla releases an emerald data bloom, gaining +16 Intelligence and +10 Luck.",
                    boostIntelligence: 16,
                    boostLuck: 10,
                    sourceBacked: false
                ),

                new CardSpec(
                    "CryptoPups",
                    "Gemstone",
                    "crypto-gemstone",
                    "Gemstone",
                    2024,
                    CardRarity.Legendary,
                    "Sunpetal",
                    "gemstone-front.png",
                    82, 76, 88, 81, 92,
                    "Prism Bloom",
                    "Gemstone releases radiant flower light, gaining +18 Luck and +12 Intelligence for one round.",
                    boostIntelligence: 12,
                    boostLuck: 18
                ),

                new CardSpec(
                    "CryptoPups",
                    "Heath",
                    "crypto-heath",
                    "Heath",
                    2025,
                    CardRarity.Epic,
                    "Neon Pulse",
                    "heath-front.png",
                    81, 90, 79, 74, 85,
                    "Flash Step",
                    "Heath pulses through the network at extreme speed, gaining +16 Speed and +8 Luck.",
                    boostSpeed: 16,
                    boostLuck: 8,
                    sourceBacked: false
                ),

                new CardSpec(
                    "CryptoPups",
                    "Liam",
                    "crypto-liam",
                    "Liam",
                    2025,
                    CardRarity.Rare,
                    "Blue Circuit",
                    "liam-front.png",
                    79, 86, 82, 76, 80,
                    "Packet Rush",
                    "Liam floods the circuit with rapid packets, gaining +14 Speed and +10 Intelligence.",
                    boostSpeed: 14,
                    boostIntelligence: 10,
                    sourceBacked: false
                ),

                new CardSpec(
                    "CryptoPups",
                    "Lola",
                    "crypto-lola",
                    "Lola",
                    2025,
                    CardRarity.Legendary,
                    "Aqua Matrix",
                    "lola-front.png",
                    77, 88, 90, 81, 87,
                    "Mirror Cache",
                    "Lola mirrors incoming data streams, gaining +14 Intelligence and +12 Luck.",
                    boostIntelligence: 14,
                    boostLuck: 12,
                    sourceBacked: false
                ),

                new CardSpec(
                    "CryptoPups",
                    "Maple",
                    "crypto-maple",
                    "Maple",
                    2025,
                    CardRarity.Epic,
                    "Violet Node",
                    "maple-front.png",
                    83, 80, 86, 85, 78,
                    "Node Shield",
                    "Maple hardens a violet security node, gaining +14 Defence and +10 Intelligence.",
                    boostIntelligence: 10,
                    boostDefence: 14,
                    sourceBacked: false
                ),

                new CardSpec(
                    "CryptoPups",
                    "Marlin",
                    "crypto-marlin",
                    "Marlin",
                    2025,
                    CardRarity.Epic,
                    "Redline",
                    "marlin-front.png",
                    90, 84, 78, 79, 73,
                    "Redline Charge",
                    "Marlin overclocks his redline core, gaining +16 Power and +10 Speed.",
                    boostPower: 16,
                    boostSpeed: 10,
                    sourceBacked: false
                ),

                new CardSpec(
                    "CryptoPups",
                    "Parker",
                    "crypto-parker",
                    "Parker",
                    2025,
                    CardRarity.Rare,
                    "Purple Vault",
                    "parker-front.png",
                    80, 77, 84, 89, 75,
                    "Vault Guard",
                    "Parker deploys a hardened crypto vault, gaining +16 Defence and +8 Intelligence.",
                    boostIntelligence: 8,
                    boostDefence: 16,
                    sourceBacked: false
                ),

                new CardSpec(
                    "CryptoPups",
                    "Proxy",
                    "crypto-proxy",
                    "Proxy",
                    2025,
                    CardRarity.Legendary,
                    "Gold Protocol",
                    "proxy-front.png",
                    86, 82, 92, 80, 84,
                    "Golden Override",
                    "Proxy overrides the arena protocol, gaining +16 Intelligence and +12 Power.",
                    boostPower: 12,
                    boostIntelligence: 16,
                    sourceBacked: false
                ),

                new CardSpec(
                    "CryptoPups",
                    "Raven",
                    "crypto-raven",
                    "Raven",
                    2025,
                    CardRarity.Mythic,
                    "Dark Nova",
                    "raven-front.png",
                    91, 82, 90, 84, 77,
                    "Nova Howl",
                    "Raven channels dark star energy, gaining +18 Power and +14 Intelligence for one round.",
                    boostPower: 18,
                    boostIntelligence: 14
                ),

                new CardSpec(
                    "CryptoPups",
                    "Sammy",
                    "crypto-sammy",
                    "Sammy",
                    2025,
                    CardRarity.Rare,
                    "Green Cache",
                    "sammy-front.png",
                    78, 83, 85, 82, 88,
                    "Lucky Cache",
                    "Sammy discovers a hidden green cache, gaining +14 Luck and +10 Intelligence.",
                    boostIntelligence: 10,
                    boostLuck: 14,
                    sourceBacked: false
                ),

                new CardSpec(
                    "CryptoPups",
                    "Sugar",
                    "crypto-sugar",
                    "Sugar",
                    2025,
                    CardRarity.Epic,
                    "Amber Byte",
                    "sugar-front.png",
                    82, 87, 81, 75, 91,
                    "Sugar Rush",
                    "Sugar overloads an amber byte stream, gaining +15 Speed and +12 Luck.",
                    boostSpeed: 15,
                    boostLuck: 12,
                    sourceBacked: false
                ),

                // =====================================================
                // CYBERPUPS
                // =====================================================

                new CardSpec(
                    "CyberPups",
                    "Elsie",
                    "cyber-elsie",
                    "Elsie",
                    2025,
                    CardRarity.Epic,
                    "Lunar Circuit",
                    "elsie-front.png",
                    76, 81, 84, 79, 91,
                    "Moonstep Surge",
                    "Elsie powers through a lunar circuit, gaining +14 Luck and +12 Intelligence.",
                    boostIntelligence: 12,
                    boostLuck: 14
                ),

                new CardSpec(
                    "CyberPups",
                    "Flipper",
                    "cyber-flipper",
                    "Flipper",
                    2025,
                    CardRarity.Epic,
                    "Aqua Servo",
                    "flipper-front.png",
                    78, 93, 84, 75, 82,
                    "Jetstream Kick",
                    "Flipper fires an aqua servo burst, gaining +18 Speed and +8 Power.",
                    boostPower: 8,
                    boostSpeed: 18,
                    sourceBacked: false
                ),

                new CardSpec(
                    "CyberPups",
                    "Gibbons",
                    "cyber-gibbons",
                    "Gibbons",
                    2025,
                    CardRarity.Rare,
                    "Steel Grapple",
                    "gibbons-front.png",
                    87, 74, 79, 90, 70,
                    "Servo Grip",
                    "Gibbons locks his servo frame into place, gaining +16 Defence and +10 Power.",
                    boostPower: 10,
                    boostDefence: 16,
                    sourceBacked: false
                ),

                new CardSpec(
                    "CyberPups",
                    "Jinx",
                    "cyber-jinx",
                    "Jinx",
                    2025,
                    CardRarity.Legendary,
                    "Ember Shield",
                    "jinx-front.png",
                    89, 76, 83, 93, 71,
                    "Plasma Bulwark",
                    "Jinx activates a burning shield wall, gaining +18 Defence and +10 Power for one round.",
                    boostPower: 10,
                    boostDefence: 18
                ),

                new CardSpec(
                    "CyberPups",
                    "Katie",
                    "cyber-katie",
                    "Katie",
                    2025,
                    CardRarity.Epic,
                    "Rose Circuit",
                    "katie-front.png",
                    80, 86, 88, 78, 84,
                    "Pink Pulse",
                    "Katie releases a rose-coloured neural pulse, gaining +14 Intelligence and +12 Speed.",
                    boostSpeed: 12,
                    boostIntelligence: 14,
                    sourceBacked: false
                ),

                new CardSpec(
                    "CyberPups",
                    "Kimiko",
                    "cyber-kimiko",
                    "Kimiko",
                    2025,
                    CardRarity.Legendary,
                    "Candy Nebula",
                    "kimiko-front.png",
                    82, 88, 91, 76, 85,
                    "Orbit Sugar Rush",
                    "Kimiko spins through a candy-coloured nebula, gaining +15 Speed and +13 Intelligence.",
                    boostSpeed: 15,
                    boostIntelligence: 13
                ),

                new CardSpec(
                    "CyberPups",
                    "Leo",
                    "cyber-leo",
                    "Leo",
                    2025,
                    CardRarity.Legendary,
                    "Solar Mech",
                    "leo-front.png",
                    93, 82, 85, 86, 74,
                    "Lion Core",
                    "Leo ignites his solar mech core, gaining +18 Power and +10 Defence.",
                    boostPower: 18,
                    boostDefence: 10,
                    sourceBacked: false
                ),

                new CardSpec(
                    "CyberPups",
                    "LittleLenny",
                    "cyber-little-lenny",
                    "Little Lenny",
                    2025,
                    CardRarity.Rare,
                    "Micro Core",
                    "littlelenny-front.png",
                    70, 91, 86, 72, 90,
                    "Tiny Turbo",
                    "Little Lenny proves size means nothing, gaining +16 Speed and +12 Luck.",
                    boostSpeed: 16,
                    boostLuck: 12,
                    sourceBacked: false
                ),

                new CardSpec(
                    "CyberPups",
                    "Lucy",
                    "cyber-lucy",
                    "Lucy",
                    2025,
                    CardRarity.Epic,
                    "Violet Link",
                    "lucy-front.png",
                    77, 84, 92, 80, 83,
                    "Neural Link",
                    "Lucy links directly into the arena grid, gaining +17 Intelligence and +9 Luck.",
                    boostIntelligence: 17,
                    boostLuck: 9,
                    sourceBacked: false
                ),

                new CardSpec(
                    "CyberPups",
                    "Metallo",
                    "cyber-metallo",
                    "Metallo",
                    2025,
                    CardRarity.Epic,
                    "Steel Nova",
                    "metallo-front.png",
                    91, 68, 80, 94, 70,
                    "Titan Shell",
                    "Metallo locks into titan armour mode, gaining +20 Defence and +10 Power for one round.",
                    boostPower: 10,
                    boostDefence: 20
                ),

                new CardSpec(
                    "CyberPups",
                    "Nova",
                    "cyber-nova",
                    "Nova",
                    2025,
                    CardRarity.Mythic,
                    "Star Circuit",
                    "nova-front.png",
                    94, 90, 93, 84, 88,
                    "Supernova Protocol",
                    "Nova executes a supernova combat protocol, gaining +18 Power and +14 Intelligence.",
                    boostPower: 18,
                    boostIntelligence: 14,
                    sourceBacked: false
                ),

                new CardSpec(
                    "CyberPups",
                    "Nukie",
                    "cyber-nukie",
                    "Nukie",
                    2025,
                    CardRarity.Mythic,
                    "Starforge",
                    "nukie-front.png",
                    78, 92, 86, 74, 88,
                    "Halo Dash",
                    "Nukie bends a glowing orbit ring, gaining +16 Intelligence and +12 Speed for one round.",
                    boostSpeed: 12,
                    boostIntelligence: 16
                ),

                new CardSpec(
                    "CyberPups",
                    "Shannon",
                    "cyber-shannon",
                    "Shannon",
                    2025,
                    CardRarity.Epic,
                    "Teal Reactor",
                    "shannon-front.png",
                    82, 85, 87, 88, 79,
                    "Reactor Sync",
                    "Shannon synchronises her teal reactor, gaining +12 Defence and +12 Intelligence.",
                    boostIntelligence: 12,
                    boostDefence: 12,
                    sourceBacked: false
                ),

                new CardSpec(
                    "CyberPups",
                    "Ultron",
                    "cyber-ultron",
                    "Ultron",
                    2025,
                    CardRarity.Mythic,
                    "Omega Alloy",
                    "ultron-front.png",
                    95, 78, 94, 96, 65,
                    "Omega Lock",
                    "Ultron enters omega lockdown, gaining +20 Defence and +16 Power.",
                    boostPower: 16,
                    boostDefence: 20,
                    sourceBacked: false
                ),

                new CardSpec(
                    "CyberPups",
                    "Victor",
                    "cyber-victor",
                    "Victor",
                    2025,
                    CardRarity.Legendary,
                    "Crimson Servo",
                    "victor-front.png",
                    91, 88, 82, 85, 76,
                    "Victory Drive",
                    "Victor pushes his crimson servos beyond their limit, gaining +16 Power and +12 Speed.",
                    boostPower: 16,
                    boostSpeed: 12,
                    sourceBacked: false
                ),

                // =====================================================
                // ALIENPUPS
                // =====================================================

                new CardSpec(
                    "AlienPups",
                    "Aqualis",
                    "alien-aqualis",
                    "Aqualis",
                    2025,
                    CardRarity.Rare,
                    "Moon Dune",
                    "aqualis-front.png",
                    73, 78, 71, 80, 76,
                    "Lunar Drift",
                    "Aqualis rides the moonlit dunes, gaining +14 Speed and +12 Defence for one round.",
                    boostSpeed: 14,
                    boostDefence: 12
                ),

                new CardSpec(
                    "AlienPups",
                    "AqualisCoral",
                    "alien-aqualis-coral",
                    "Aqualis",
                    2025,
                    CardRarity.Rare,
                    "Coral Nebula",
                    "aqualis2-front.png",
                    68, 81, 79, 74, 85,
                    "Tide Blink",
                    "Aqualis slips through a shimmering coral warp, gaining +12 Speed and +8 Intelligence for one round.",
                    boostSpeed: 12,
                    boostIntelligence: 8
                ),

                new CardSpec(
                    "AlienPups",
                    "Aster",
                    "alien-aster",
                    "Aster",
                    2025,
                    CardRarity.Epic,
                    "Prism Moon",
                    "aster-front.png",
                    75, 84, 86, 70, 88,
                    "Prism Step",
                    "Aster glides across moonlight trails, gaining +10 Speed and +12 Intelligence for one round.",
                    boostSpeed: 10,
                    boostIntelligence: 12
                ),

                new CardSpec(
                    "AlienPups",
                    "Astra",
                    "alien-astra",
                    "Astra",
                    2025,
                    CardRarity.Legendary,
                    "Starveil",
                    "astra-front.png",
                    84, 89, 91, 77, 86,
                    "Starveil Shift",
                    "Astra slips behind a veil of starlight, gaining +15 Intelligence and +12 Speed.",
                    boostSpeed: 12,
                    boostIntelligence: 15,
                    sourceBacked: false
                ),

                new CardSpec(
                    "AlienPups",
                    "Bibi",
                    "alien-bibi",
                    "Bibi",
                    2025,
                    CardRarity.Rare,
                    "Bubble Sprite",
                    "bibi-front.png",
                    67, 82, 80, 73, 94,
                    "Cosmic Giggle",
                    "Bibi bends probability with cosmic mischief, gaining +18 Luck and +10 Speed.",
                    boostSpeed: 10,
                    boostLuck: 18,
                    sourceBacked: false
                ),

                new CardSpec(
                    "AlienPups",
                    "Bloopa",
                    "alien-bloopa",
                    "Bloopa",
                    2025,
                    CardRarity.Epic,
                    "Bubble Nova",
                    "bloopa-front.png",
                    70, 69, 88, 75, 90,
                    "Prism Bubble",
                    "Bloopa conjures a moonlit bubble shield, gaining +16 Intelligence and +13 Defence for one round.",
                    boostIntelligence: 16,
                    boostDefence: 13
                ),

                new CardSpec(
                    "AlienPups",
                    "BlossomByte",
                    "alien-blossom-byte",
                    "Blossom Byte",
                    2025,
                    CardRarity.Rare,
                    "Dream Mist",
                    "blossombyte-front.png",
                    69, 78, 85, 67, 92,
                    "Mist Charm",
                    "Blossom Byte wraps the field in dream mist, gaining +14 Luck and +8 Intelligence for one round.",
                    boostIntelligence: 8,
                    boostLuck: 14
                ),

                new CardSpec(
                    "AlienPups",
                    "Cosmabit",
                    "alien-cosmabit",
                    "Cosmabit",
                    2025,
                    CardRarity.Epic,
                    "Cosmic Flame",
                    "cosmobit-front.png",
                    87, 91, 93, 82, 79,
                    "Orbital Dash",
                    "Cosmabit dashes in an orbital arc, gaining +20 Speed and +15 Defence.",
                    boostSpeed: 20,
                    boostDefence: 15
                ),

                new CardSpec(
                    "AlienPups",
                    "EclipsePop",
                    "alien-eclipse-pop",
                    "Eclipse Pop",
                    2025,
                    CardRarity.Mythic,
                    "Neon Halo",
                    "eclipse-front.png",
                    81, 76, 92, 79, 87,
                    "Glowshift",
                    "Eclipse Pop activates a luminous disguise aura, gaining +20 Intelligence and +10 Luck for one round.",
                    boostIntelligence: 20,
                    boostLuck: 10
                ),

                new CardSpec(
                    "AlienPups",
                    "Halo",
                    "alien-halo",
                    "Halo",
                    2025,
                    CardRarity.Mythic,
                    "Solar Halo",
                    "halo-front.png",
                    86, 88, 95, 82, 90,
                    "Halo Ascension",
                    "Halo rises through a solar ring, gaining +18 Intelligence and +14 Luck.",
                    boostIntelligence: 18,
                    boostLuck: 14,
                    sourceBacked: false
                ),

                new CardSpec(
                    "AlienPups",
                    "Limebyte",
                    "alien-limebyte",
                    "Limebyte",
                    2025,
                    CardRarity.Epic,
                    "Lunar Ice",
                    "limebyte-front.png",
                    82, 91, 87, 79, 74,
                    "Frost Byte",
                    "Limebyte unleashes binary frost, gaining +18 Speed and +14 Defence for one round.",
                    boostSpeed: 18,
                    boostDefence: 14
                ),

                new CardSpec(
                    "AlienPups",
                    "Lunabop",
                    "alien-lunabop",
                    "Lunabop",
                    2025,
                    CardRarity.Legendary,
                    "Moon Pop",
                    "lunabop-front.png",
                    80, 82, 76, 73, 91,
                    "Orbit Pop",
                    "Lunabop launches a glowing moon orb, gaining +14 Luck and +9 Power for one round.",
                    boostPower: 9,
                    boostLuck: 14
                ),

                new CardSpec(
                    "AlienPups",
                    "MikoAndShroomi",
                    "alien-miko-shroomi",
                    "Miko & Shroomi",
                    2025,
                    CardRarity.Epic,
                    "Spore Nebula",
                    "miko&shroomi-front.png",
                    75, 80, 89, 84, 92,
                    "Spore Sync",
                    "Miko & Shroomi synchronise through a cosmic spore network, gaining +15 Luck and +12 Intelligence.",
                    boostIntelligence: 12,
                    boostLuck: 15,
                    sourceBacked: false
                ),

                new CardSpec(
                    "AlienPups",
                    "Mintara",
                    "alien-mintara",
                    "Mintara",
                    2025,
                    CardRarity.Rare,
                    "Sky Burst",
                    "mintara-front.png",
                    68, 74, 77, 72, 85,
                    "Cloud Dash",
                    "Mintara glides on pastel winds, gaining +14 Speed and +11 Luck for one round.",
                    boostSpeed: 14,
                    boostLuck: 11
                ),

                new CardSpec(
                    "AlienPups",
                    "MintaraNebula",
                    "alien-mintara-nebula-spirit",
                    "Mintara",
                    2025,
                    CardRarity.Legendary,
                    "Nebula Spirit",
                    "mintara2-front.png",
                    77, 74, 92, 68, 89,
                    "Spirit Glow",
                    "Mintara summons a nebula familiar, gaining +16 Intelligence and +8 Luck for one round.",
                    boostIntelligence: 16,
                    boostLuck: 8
                ),

                new CardSpec(
                    "AlienPups",
                    "NovaAndUmbra",
                    "alien-nova-umbra",
                    "Nova & Umbra",
                    2025,
                    CardRarity.Mythic,
                    "Twin Orbit",
                    "nova&umbra-front.png",
                    88, 83, 90, 81, 78,
                    "Dual Nova",
                    "Nova & Umbra fuse lunar and solar energy, gaining +12 Power and +12 Intelligence for one round.",
                    boostPower: 12,
                    boostIntelligence: 12
                ),

                new CardSpec(
                    "AlienPups",
                    "Petalia",
                    "alien-petalia",
                    "Petalia",
                    2025,
                    CardRarity.Epic,
                    "Aurora Bloom",
                    "petalia-front.png",
                    72, 86, 88, 69, 93,
                    "Petal Burst",
                    "Petalia scatters glowing bloom dust, gaining +15 Luck and +10 Speed for one round.",
                    boostSpeed: 10,
                    boostLuck: 15
                ),

                new CardSpec(
                    "AlienPups",
                    "PrismFang",
                    "alien-prism-fang",
                    "Prism Fang",
                    2025,
                    CardRarity.Legendary,
                    "Aurora Fang",
                    "prismfang-front.png",
                    89, 86, 83, 78, 74,
                    "Star Slash",
                    "Prism Fang channels aurora energy, gaining +17 Speed and +15 Power for one round.",
                    boostPower: 15,
                    boostSpeed: 17
                ),

                new CardSpec(
                    "AlienPups",
                    "Rosette",
                    "alien-rosette",
                    "Rosette",
                    2025,
                    CardRarity.Epic,
                    "Ghost Bloom",
                    "rosette-front.png",
                    76, 82, 79, 71, 91,
                    "Phantom Petal",
                    "Rosette summons a glowing bloom spirit, gaining +15 Luck and +10 Speed for one round.",
                    boostSpeed: 10,
                    boostLuck: 15
                ),

                new CardSpec(
                    "AlienPups",
                    "Solaro",
                    "alien-solaro",
                    "Solaro",
                    2025,
                    CardRarity.Legendary,
                    "Comet Flare",
                    "solaro-front.png",
                    83, 87, 82, 76, 79,
                    "Solar Stride",
                    "Solaro surges with comet light, gaining +14 Power and +9 Speed for one round.",
                    boostPower: 14,
                    boostSpeed: 9
                ),

                new CardSpec(
                    "AlienPups",
                    "Tiko",
                    "alien-tiko",
                    "Tiko",
                    2025,
                    CardRarity.Epic,
                    "Starlight Shield",
                    "tiko-front.png",
                    78, 92, 89, 93, 74,
                    "Cosmic Bubble",
                    "Tiko summons a protective starlight bubble, gaining +15 Defence for one round.",
                    boostDefence: 15
                ),

                new CardSpec(
                    "AlienPups",
                    "VegaAndPip",
                    "alien-vega-pip",
                    "Vega & Pip",
                    2025,
                    CardRarity.Legendary,
                    "Twin Comet",
                    "vega&pip-front.png",
                    88, 92, 87, 79, 85,
                    "Comet Pair",
                    "Vega & Pip slingshot around one another, gaining +16 Speed and +12 Power.",
                    boostPower: 12,
                    boostSpeed: 16,
                    sourceBacked: false
                ),

                new CardSpec(
                    "AlienPups",
                    "Verdix",
                    "alien-verdix",
                    "Verdix",
                    2025,
                    CardRarity.Epic,
                    "Sky Sprout",
                    "verdix-front.png",
                    74, 79, 84, 71, 87,
                    "Sprout Dash",
                    "Verdix channels sky-seed energy, gaining +11 Speed and +11 Luck for one round.",
                    boostSpeed: 11,
                    boostLuck: 11
                ),

                new CardSpec(
                    "AlienPups",
                    "VexaFang",
                    "alien-vexa-fang",
                    "Vexa Fang",
                    2025,
                    CardRarity.Legendary,
                    "Neon Rage",
                    "vexafang-front.png",
                    92, 88, 84, 80, 73,
                    "Psionic Snarl",
                    "Vexa Fang unleashes a telepathic howl, gaining +18 Power and +12 Intelligence for one round.",
                    boostPower: 18,
                    boostIntelligence: 12
                ),

                new CardSpec(
                    "AlienPups",
                    "Zenith",
                    "alien-zenith",
                    "Zenith",
                    2025,
                    CardRarity.Mythic,
                    "Apex Star",
                    "zenith-front.png",
                    96, 90, 94, 88, 86,
                    "Zenith Burst",
                    "Zenith releases the full force of an apex star, gaining +18 Power and +16 Intelligence.",
                    boostPower: 18,
                    boostIntelligence: 16,
                    sourceBacked: false
                )
            };
        }

        static StatBlock MakeStats(
            int power,
            int speed,
            int intelligence,
            int defence,
            int luck
        )
        {
            return new StatBlock
            {
                power = power,
                speed = speed,
                intelligence = intelligence,
                defence = defence,
                luck = luck
            };
        }

        static string SetFolder(string set)
        {
            switch (set)
            {
                case "CryptoPups":
                    return "Crypto";

                case "CyberPups":
                    return "Cyber";

                default:
                    return "Alien";
            }
        }

        static string SetArtFolder(string set)
        {
            switch (set)
            {
                case "CryptoPups":
                    return "crypto";

                case "CyberPups":
                    return "cyber";

                default:
                    return "alien";
            }
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent =
                Path.GetDirectoryName(path)
                    ?.Replace("\\", "/");

            string folderName =
                Path.GetFileName(path);

            if (string.IsNullOrEmpty(parent))
                return;

            EnsureFolder(parent);

            AssetDatabase.CreateFolder(
                parent,
                folderName
            );
        }
    }
}

#endif
