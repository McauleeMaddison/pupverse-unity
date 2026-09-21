using UnityEngine;
namespace Pupverse
{
    public static class UITheme
    {
        public static readonly Color Navy = Hex("090F24");
        public static readonly Color Panel = Hex("15213A");
        public static readonly Color Mint = Hex("80F2DA");
        public static readonly Color Lavender = Hex("B8A1FF");
        public static readonly Color Pink = Hex("FF98CB");
        public static readonly Color Muted = Hex("9EAEC9");
        public static Color Hex(string value) { ColorUtility.TryParseHtmlString("#" + value, out var c); return c; }
    }
}
