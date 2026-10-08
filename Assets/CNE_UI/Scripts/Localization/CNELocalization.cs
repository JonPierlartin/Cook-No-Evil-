using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace CookNoEvil.UI
{
    /// <summary>Dil secicideki bir dil: Unity Localization'daki Locale + kitin gorunus bilgisi.</summary>
    public sealed class CNELanguageOption
    {
        public Locale locale;
        public string code;
        public string displayName;
        public Sprite flag;
        public TMP_FontAsset fontOverride;
    }

    /// <summary>
    /// Unity Localization'a ince bir yardimci. Dil secimi ve metinler projenin tek dil sisteminden gelir
    /// (Localization Settings, Locale'ler, string tablolari); kit ayri bir dil durumu tutmaz.
    ///  - Kit metinleri "CNE_UI" tablosundadir. Tablo, Assets/CNE_UI/Localization/CNE_Strings.csv'den
    ///    "Tools > Cook No Evil > UI Kit" kurulumuyla yazilir (UIStrings'e dokunulmaz).
    ///  - Secilebilir diller Localization Settings'teki Locale'lerdir; kit yalnizca bayrak/ad ekler (CNE_Languages).
    ///  - Secimin kalici olmasi Localization Settings'teki baslangic secicilerine baglidir (Player Pref Locale Selector).
    /// </summary>
    public static class CNELocalization
    {
        public const string TableName = "CNE_UI";
        const string DisplayInfoPath = "CNE_Languages";

        static CNELanguageList displayInfo;
        static bool displayInfoLoaded;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            displayInfo = null;
            displayInfoLoaded = false;
        }

        /// <summary>Localization hazir olunca (Locale'ler ve tablolar yuklenince) cagirir; hazirsa hemen.</summary>
        public static void WhenReady(Action action)
        {
            if (action == null) return;
            var op = LocalizationSettings.InitializationOperation;
            if (op.IsDone) action();
            else op.Completed += _ => action();
        }

        /// <summary>Secili dilin kodu (ornegin "tr"). Localization hazir degilse null.</summary>
        public static string CurrentCode
        {
            get
            {
                if (!LocalizationSettings.InitializationOperation.IsDone) return null;
                var locale = LocalizationSettings.SelectedLocale;
                return locale != null ? locale.Identifier.Code : null;
            }
        }

        /// <summary>Projede tanimli dil sayisi (Localization hazir degilse 0).</summary>
        public static int LanguageCount
        {
            get
            {
                if (!LocalizationSettings.InitializationOperation.IsDone) return 0;
                var provider = LocalizationSettings.AvailableLocales;
                return provider != null && provider.Locales != null ? provider.Locales.Count : 0;
            }
        }

        /// <summary>Secilebilir diller; CNE_Languages'teki siraya gore, listede olmayanlar sonda.</summary>
        public static void GetLanguages(List<CNELanguageOption> results)
        {
            results.Clear();
            if (!LocalizationSettings.InitializationOperation.IsDone) return;
            var provider = LocalizationSettings.AvailableLocales;
            if (provider == null || provider.Locales == null) return;
            foreach (var locale in provider.Locales)
                if (locale != null) results.Add(Describe(locale));
            results.Sort((a, b) => OrderOf(a.code).CompareTo(OrderOf(b.code)));
        }

        public static void SetLanguage(Locale locale)
        {
            if (locale == null) return;
            if (LocalizationSettings.SelectedLocale != locale) LocalizationSettings.SelectedLocale = locale;
        }

        public static CNELanguageOption Describe(Locale locale)
        {
            string code = locale.Identifier.Code;
            var info = FindDisplayInfo(code);
            var option = new CNELanguageOption();
            option.locale = locale;
            option.code = code;
            option.flag = info != null ? info.flag : null;
            option.fontOverride = info != null ? info.fontOverride : null;
            option.displayName = info != null && !string.IsNullOrEmpty(info.nativeName) ? info.nativeName : NativeName(locale);
            return option;
        }

        /// <summary>Kodun gorunus bilgisi: once tam kod (pt-BR), sonra dil kismi (pt).</summary>
        public static CNELanguageDef FindDisplayInfo(string code)
        {
            var list = DisplayInfo;
            if (list == null || list.languages == null || string.IsNullOrEmpty(code)) return null;
            for (int i = 0; i < list.languages.Count; i++)
            {
                var def = list.languages[i];
                if (def != null && string.Equals(def.code, code, StringComparison.OrdinalIgnoreCase)) return def;
            }
            string language = LanguagePart(code);
            for (int i = 0; i < list.languages.Count; i++)
            {
                var def = list.languages[i];
                if (def != null && string.Equals(LanguagePart(def.code), language, StringComparison.OrdinalIgnoreCase)) return def;
            }
            return null;
        }

        static CNELanguageList DisplayInfo
        {
            get
            {
                if (!displayInfoLoaded)
                {
                    displayInfoLoaded = true;
                    displayInfo = Resources.Load<CNELanguageList>(DisplayInfoPath);
                }
                return displayInfo;
            }
        }

        static int OrderOf(string code)
        {
            var list = DisplayInfo;
            if (list == null || list.languages == null) return int.MaxValue;
            var def = FindDisplayInfo(code);
            int i = def != null ? list.languages.IndexOf(def) : -1;
            return i >= 0 ? i : int.MaxValue;
        }

        static string LanguagePart(string code)
        {
            if (string.IsNullOrEmpty(code)) return string.Empty;
            int dash = code.IndexOf('-');
            return dash > 0 ? code.Substring(0, dash) : code;
        }

        static string NativeName(Locale locale)
        {
            try
            {
                var culture = locale.Identifier.CultureInfo;
                if (culture != null && !string.IsNullOrEmpty(culture.NativeName))
                    return culture.TextInfo.ToTitleCase(culture.NativeName);
            }
            catch (CultureNotFoundException)
            {
            }
            return locale.LocaleName;
        }
    }
}
