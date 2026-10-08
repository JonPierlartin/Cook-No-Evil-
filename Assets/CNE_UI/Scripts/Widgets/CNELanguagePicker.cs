using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace CookNoEvil.UI
{
    /// <summary>
    /// Ayarlardaki dil secimi: Unity Localization'daki her Locale icin bir bayrakli kart olusturur ve
    /// secileni LocalizationSettings.SelectedLocale yapar (oyunun butun metinleri birlikte degisir).
    /// Bayrak ve dil adi Resources/CNE_Languages'ten gelir. Projeye dil (Locale) eklenince kart kendiliginden gelir.
    /// </summary>
    [AddComponentMenu("Cook No Evil/UI/Dil Secici")]
    public class CNELanguagePicker : MonoBehaviour
    {
        [Tooltip("Pasif sablon kart (kopyalanir)")] public CNELanguageChip chipTemplate;
        [Tooltip("Kartlarin dizildigi kap (Horizontal/Grid LayoutGroup)")] public RectTransform container;

        readonly List<CNELanguageChip> chips = new List<CNELanguageChip>();
        readonly List<CNELanguageOption> languages = new List<CNELanguageOption>();
        bool listening;

        void OnEnable()
        {
            if (!Application.isPlaying) return;
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
            listening = true;
            CNELocalization.WhenReady(OnReady);
        }

        void OnDisable()
        {
            if (!listening) return;
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
            listening = false;
        }

        void OnReady()
        {
            if (this == null || !isActiveAndEnabled) return;
            Build();
            Sync(false);
        }

        void Build()
        {
            if (chipTemplate == null) return;
            chipTemplate.gameObject.SetActive(false);
            CNELocalization.GetLanguages(languages);
            var parent = container != null ? container : (RectTransform)transform;
            while (chips.Count < languages.Count)
            {
                var chip = Instantiate(chipTemplate, parent);
                chips.Add(chip);
            }
            for (int i = 0; i < chips.Count; i++)
            {
                bool used = i < languages.Count;
                chips[i].gameObject.SetActive(used);
                if (used) chips[i].Setup(languages[i], this);
            }
        }

        public void Choose(CNELanguageOption language)
        {
            if (language == null) return;
            CNELocalization.SetLanguage(language.locale);
        }

        void OnLocaleChanged(Locale locale)
        {
            Sync(true);
        }

        void Sync(bool animate)
        {
            string current = CNELocalization.CurrentCode;
            for (int i = 0; i < chips.Count && i < languages.Count; i++)
                chips[i].SetSelected(string.Equals(chips[i].Code, current, System.StringComparison.OrdinalIgnoreCase), animate);
        }
    }
}
