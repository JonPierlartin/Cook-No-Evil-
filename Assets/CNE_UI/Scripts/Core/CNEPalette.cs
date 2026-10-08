using UnityEngine;

namespace CookNoEvil.UI
{
    /// <summary>
    /// Cook No Evil! renk paleti (projedeki claude/renk-paleti.md).
    /// UI yalnizca bu renkleri kullanir. Satir 0'daki sinyal renkleri (ketcap, hardal,
    /// mayonez, barbeku, sinyal fonu) bilerek burada YOK: onlar yalnizca sinyal carkinda kullanilir.
    /// </summary>
    public static class CNEPalette
    {
        public static readonly Color Navy = Hex(0x24233A);        // kontur ve metin
        public static readonly Color ShadowTint = Hex(0x3E4A7A);  // sert ofset golge
        public static readonly Color Mint = Hex(0xBFE3D6);        // Formika panel yuzu
        public static readonly Color MintDark = Hex(0x9CCDBD);
        public static readonly Color Chrome = Hex(0xC3CCD2);
        public static readonly Color ChromeDark = Hex(0x5E6A73);
        public static readonly Color ChromeMid = Hex(0xA3AEB6);
        public static readonly Color Iron = Hex(0x2C3237);        // dokme demir (tabela zemini)
        public static readonly Color Cream = Hex(0xF7F1E3);       // kagit, pano harfleri
        public static readonly Color CreamDark = Hex(0xE2DDD0);
        public static readonly Color Kraft = Hex(0xC8A27A);
        public static readonly Color Lettuce = Hex(0x74BF4A);     // devam / onay  (GIRIS tabelasi)
        public static readonly Color Cheese = Hex(0xF28C1E);      // ayril / cikis (CIKIS tabelasi)
        public static readonly Color Lila = Hex(0xA58FD8);        // notr / secili
        public static readonly Color LilaDark = Hex(0x7E68B5);
        public static readonly Color LilaLight = Hex(0xCDBFEE);
        public static readonly Color Tomato = Hex(0xE8553E);      // hata
        public static readonly Color Cyan = Hex(0x5FE3E0);        // sayilar (fritoz ekrani)
        public static readonly Color NeonLila = Hex(0x9B7FD4);
        public static readonly Color Turquoise = Hex(0x2E9E98);

        /// <summary>0xRRGGBB tamsayisindan renk.</summary>
        public static Color Hex(int rgb, float alpha = 1f)
        {
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, alpha);
        }

        public static Color WithAlpha(this Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
