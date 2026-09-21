using UnityEngine;
using UnityEngine.UI;
namespace Pupverse
{
    public sealed class CardView : MonoBehaviour
    {
        public CardData data;
        public bool showBonuses = true;
        public Text nameLabel, rarityLabel, elementLabel, abilityLabel, factLabel;
        public Text[] statValues;
        public Image[] statBars;
        public Image heroImage;
        public RawImage portrait;
        public void Bind(CardData card)
        {
            data = card;
            nameLabel.text = card.displayName;
            rarityLabel.text = card.rarity.ToString().ToUpperInvariant();
            elementLabel.text = card.series + "  /  " + card.element;
            if (abilityLabel) abilityLabel.text = card.abilityName.ToUpperInvariant();
            if (factLabel) factLabel.text = card.fact;
            for (int i = 0; i < 5; i++)
            {
                int boost = card.abilityBoosts.Get((CardStat)i);
                statValues[i].text = card.stats.Get((CardStat)i).ToString() + (showBonuses && boost > 0 ? "  <color=#80F2DA>+" + boost + "</color>" : "");
                if (statBars != null && i < statBars.Length && statBars[i]) statBars[i].fillAmount = card.stats.Get((CardStat)i) / 100f;
            }
            if (heroImage) { heroImage.sprite = card.heroArt; heroImage.gameObject.SetActive(card.heroArt != null); }
            if (portrait) { portrait.texture = card.originalCardArt; portrait.uvRect = card.portraitUV; portrait.gameObject.SetActive(card.heroArt == null); }
        }
        void Awake() { if (data) Bind(data); }
    }
}
