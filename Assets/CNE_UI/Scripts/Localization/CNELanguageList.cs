using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CookNoEvil.UI
{
    /// <summary>Dil secicide bir dilin gorunusu: bayrak, dilin kendi dilindeki adi, gerekirse ozel font.</summary>
    [Serializable]
    public class CNELanguageDef
    {
        [Tooltip("Unity Localization'daki Locale kodu (tr, en, de, pt-BR, ja...). Once tam kod, sonra dil kismi eslesir.")]
        public string code = "en";
        [Tooltip("Dilin kendi dilindeki adi: Türkçe, English, Deutsch... Bossa sistemin verdigi ad kullanilir.")]
        public string nativeName = "English";
        [Tooltip("Toon bayrak sprite'i (Art/Flags).")]
        public Sprite flag;
        [Tooltip("Bu dil icin ozel TMP font (Cince/Japonca/Korece icin gerekir). Bossa varsayilan font kalir.")]
        public TMP_FontAsset fontOverride;
    }

    /// <summary>
    /// Dillerin gorunus bilgisi (Assets/CNE_UI/Resources/CNE_Languages.asset). Hangi dillerin secilebilecegini
    /// Unity Localization belirler (Localization Settings'teki Locale'ler); bu liste yalnizca bayrak, ad ve
    /// siralamayi ekler. Listede olmayan bir dil de secicide gorunur (bayraksiz, sistemin verdigi adla).
    /// </summary>
    [CreateAssetMenu(menuName = "Cook No Evil/UI/Dil Gorunusleri", fileName = "CNE_Languages")]
    public class CNELanguageList : ScriptableObject
    {
        [Tooltip("Secicideki sira da buradan gelir.")]
        public List<CNELanguageDef> languages = new List<CNELanguageDef>();
    }
}
