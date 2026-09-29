using UnityEngine;

namespace Pupverse
{
    // Only private runtime materials use this finish. Source textures/material assets stay untouched.
    public static class CardFoilFinish
    {
        static readonly int Amount=Shader.PropertyToID("_FoilAmount"), Tint=Shader.PropertyToID("_FoilTint"),
            Prism=Shader.PropertyToID("_Prismatic"), Clock=Shader.PropertyToID("_FoilTime"), Seed=Shader.PropertyToID("_FoilSeed");
        public static Material Create(Shader shader) => new Material(shader!=null?shader:Shader.Find("Unlit/Texture"));
        public static void Apply(Material material,CardData card)
        {
            if(card!=null)material.mainTexture=card.originalCardArt;
            if(!material.HasProperty(Amount))return;
            float strength=0,prism=0;Color tint=new Color(.65f,.85f,1);
            switch(card!=null?card.rarity:CardRarity.Common)
            {
                case CardRarity.Rare: strength=.38f;break;
                case CardRarity.Epic: strength=.52f;prism=.5f;tint=new Color(.8f,.57f,1);break;
                case CardRarity.Legendary: strength=.65f;prism=.16f;tint=new Color(1,.76f,.3f);break;
                case CardRarity.Mythic: case CardRarity.Mystic: strength=.78f;prism=1;break;
            }
            uint hash=17;foreach(char c in card?.id??"")hash=unchecked(hash*31+c);
            material.SetFloat(Amount,strength);material.SetColor(Tint,tint);material.SetFloat(Prism,prism);
            material.SetFloat(Seed,(hash%1000)*.00628f);Tick(material);
        }
        public static void Tick(Material material)
        {
            if(material!=null && material.HasProperty(Clock))material.SetFloat(Clock,GameSettings.ReducedMotion?0:Time.unscaledTime);
        }
        public static string Label(CardRarity rarity)
        {
            switch(rarity)
            {
                case CardRarity.Rare:return "SILVER FOIL";
                case CardRarity.Epic:return "IRIDESCENT FOIL";
                case CardRarity.Legendary:return "GOLD FOIL";
                case CardRarity.Mythic:case CardRarity.Mystic:return "PRISMATIC FOIL";
                default:return "MATTE FINISH";
            }
        }
    }
}
