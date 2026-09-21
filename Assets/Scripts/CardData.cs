using System;
using UnityEngine;

namespace Pupverse
{
    public enum CardStat { Power, Speed, Intelligence, Defence, Luck }
    public enum CardRarity { Common, Uncommon, Rare, Epic, Legendary, Mythic, Mystic }

    [Serializable]
    public struct StatBlock
    {
        [Min(0)] public int power, speed, intelligence, defence, luck;
        public int Get(CardStat stat) => stat switch {
            CardStat.Power => power, CardStat.Speed => speed, CardStat.Intelligence => intelligence,
            CardStat.Defence => defence, CardStat.Luck => luck,
            _ => throw new ArgumentOutOfRangeException(nameof(stat))
        };
    }

    [CreateAssetMenu(menuName = "Pupverse/Card", fileName = "NewCard")]
    public sealed class CardData : ScriptableObject
    {
        public string id, displayName, series, element;
        public int year;
        public CardRarity rarity;
        public StatBlock stats;
        public string abilityName;
        [TextArea(2, 4)] public string abilityDescription;
        public StatBlock abilityBoosts;
        [TextArea(2, 4)] public string fact;
        public Texture2D originalCardArt;
        public Sprite heroArt;
        [Tooltip("Portrait window in the original card image; excludes baked-in stats.")]
        public Rect portraitUV = new Rect(0, 0, 1, 1);
        public int EffectiveValue(CardStat stat) => checked(stats.Get(stat) + abilityBoosts.Get(stat));
    }
}
