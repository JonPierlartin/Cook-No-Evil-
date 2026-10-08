using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CookNoEvil.UI
{
    /// <summary>
    /// Ayarlar karti. Yalnizca gorunumdur: degerleri bir CNESettingsSource'tan okur ve ona yazar
    /// (projede oyunun GameSettings'ine bagli kaynak). Kaynak atanmamissa kart kilitli acilir ve konsola yazar.
    /// Ayarlar aninda uygulanir; kart kapaninca kaynagin Save'i cagrilir.
    /// Desteklenmeyen satirlar gizlenir, bos kalan bolum basliklari da; kartin boyu icerige gore kisalir.
    /// Tam ekran dogrudan Screen.fullScreenMode'dur (Unity bunu kendisi hatirlar).
    /// ESC / gamepad "geri" karti kapatir.
    /// </summary>
    [AddComponentMenu("Cook No Evil/UI/Ayarlar Paneli")]
    public class CNESettingsPanel : MonoBehaviour
    {
        [Serializable]
        public class SliderRow
        {
            public CNEFloatSetting setting;
            [Tooltip("Satirin kendisi (desteklenmezse gizlenir)")] public GameObject row;
            public Slider slider;
        }

        [Serializable]
        public class Section
        {
            [Tooltip("Bolum basligi; satirlarinin hepsi gizliyse o da gizlenir")] public GameObject header;
            public GameObject[] rows = new GameObject[0];
        }

        [Header("Kaynaklar")]
        [Tooltip("Ayar deposu. Projede oyunun ayarlarina bagli kaynak atanir; bossa kart kilitli acilir.")]
        public CNESettingsSource settings;
        [Tooltip("Mikrofon testi icin seviye kaynagi (istege bagli). Bossa test satiri gizlenir.")]
        public CNEMicLevelSource micLevelSource;

        public CNEPanelTransition transition;

        [Header("Satirlar")]
        public List<SliderRow> sliders = new List<SliderRow>();
        public Toggle fullscreen;
        public GameObject fullscreenRow;
        [Tooltip("Tam ekran satiri gorunsun mu?")] public bool showFullscreen = true;
        public CNERotarySelector look;
        public GameObject lookRow;
        public CNEVUMeter micTest;
        public GameObject micTestRow;
        [Tooltip("Dil secimi satiri (bayrakli kartlar)")] public GameObject languageRow;
        [Tooltip("Projede tek dil varken dil satirini gizle")] public bool hideLanguageWhenSingle = true;
        public Section[] sections = new Section[0];

        [Header("Yerlesim")]
        [Tooltip("Boyu icerige gore ayarlanan kart")] public RectTransform card;
        [Tooltip("Kartin icerik sutunlari (VerticalLayoutGroup)")] public RectTransform[] columns = new RectTransform[0];
        public float cardTopPadding = 96f;
        public float cardBottomPadding = 140f;
        public float minCardHeight = 520f;

        [Header("Dugmeler")]
        public Button resetButton;
        public Button closeButton;

        public UnityEvent onClosed = new UnityEvent();

        readonly List<CNEChoice> lookOptions = new List<CNEChoice>();
        bool hooked;
        bool pulling;
        bool warnedNoSource;
        int languageCount = -1;

        void Awake()
        {
            Hook();
        }

        void OnEnable()
        {
            Pull();
        }

        /// <summary>Kaynaklari baglar (ana menu cagirir). Panelde zaten atanmis olan korunur.</summary>
        public void Bind(CNESettingsSource settingsSource, CNEMicLevelSource micSource)
        {
            if (settings == null) settings = settingsSource;
            if (micLevelSource == null) micLevelSource = micSource;
        }

        void Hook()
        {
            if (hooked) return;
            hooked = true;
            for (int i = 0; i < sliders.Count; i++)
            {
                var row = sliders[i];
                if (row == null || row.slider == null) continue;
                var setting = row.setting;
                row.slider.onValueChanged.AddListener(v =>
                {
                    if (!pulling && settings != null) settings.SetFloat(setting, v);
                });
            }
            if (fullscreen != null) fullscreen.onValueChanged.AddListener(OnFullscreenChanged);
            if (look != null) look.onValueChanged.AddListener(OnLookChanged);
            if (resetButton != null) resetButton.onClick.AddListener(ResetToDefaults);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        /// <summary>Widget'lari kaynaktaki degerlerle doldurur (ses ve olay tetiklemeden), satirlari gosterir/gizler.</summary>
        public void Pull()
        {
            Hook();
            bool hasSource = settings != null;
            if (!hasSource && !warnedNoSource && Application.isPlaying)
            {
                warnedNoSource = true;
                Debug.LogError("[CNE UI] Ayarlar kartinin ayar kaynagi atanmadi (CNESettingsPanel.settings ya da CNEMainMenu.settingsSource). Kart kilitli.", this);
            }

            pulling = true;
            try
            {
                for (int i = 0; i < sliders.Count; i++)
                {
                    var row = sliders[i];
                    if (row == null) continue;
                    bool show = !hasSource || settings.Supports(row.setting);
                    if (row.row != null) row.row.SetActive(show);
                    if (row.slider == null) continue;
                    row.slider.interactable = hasSource;
                    if (!hasSource || !show) continue;
                    var spec = settings.GetSpec(row.setting);
                    row.slider.minValue = spec.min;
                    row.slider.maxValue = spec.max;
                    row.slider.SetValueWithoutNotify(settings.GetFloat(row.setting));
                    var fb = row.slider.GetComponent<CNESliderFeedback>();
                    if (fb != null)
                    {
                        fb.format = spec.format;
                        fb.Refresh();
                    }
                }

                if (fullscreenRow != null) fullscreenRow.SetActive(showFullscreen);
                if (fullscreen != null)
                {
                    bool on = Screen.fullScreenMode != FullScreenMode.Windowed;
                    var sw = fullscreen.GetComponent<CNEToggleSwitch>();
                    if (sw != null) sw.SetIsOnSilently(on);
                    else fullscreen.SetIsOnWithoutNotify(on);
                }

                lookOptions.Clear();
                if (hasSource) settings.GetLookOptions(lookOptions);
                if (lookRow != null) lookRow.SetActive(lookOptions.Count >= 2);
                if (look != null && lookOptions.Count >= 2)
                {
                    look.SetOptions(lookOptions);
                    look.SetWithoutNotify(IndexOfLook(settings.Look));
                }

                if (micTestRow != null) micTestRow.SetActive(micLevelSource != null);
                if (micTest != null)
                {
                    micTest.source = micLevelSource;
                    micTest.gain = ReadMicGain;
                }

                if (resetButton != null) resetButton.interactable = hasSource;
            }
            finally
            {
                pulling = false;
            }

            UpdateLanguageRow();
            if (Application.isPlaying) CNELocalization.WhenReady(OnLocalizationReady);
            RefreshLayout();
        }

        float ReadMicGain()
        {
            return settings != null && settings.Supports(CNEFloatSetting.MicGain) ? settings.GetFloat(CNEFloatSetting.MicGain) : 1f;
        }

        int IndexOfLook(string id)
        {
            string wanted = id ?? string.Empty;
            for (int i = 0; i < lookOptions.Count; i++)
                if (string.Equals(lookOptions[i].id ?? string.Empty, wanted, StringComparison.Ordinal)) return i;
            return 0;
        }

        void OnLookChanged(int index)
        {
            if (pulling || settings == null || index < 0 || index >= lookOptions.Count) return;
            settings.Look = lookOptions[index].id ?? string.Empty;
        }

        static void OnFullscreenChanged(bool on)
        {
            var mode = on ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            if (Screen.fullScreenMode != mode) Screen.fullScreenMode = mode;
        }

        void OnLocalizationReady()
        {
            if (this == null || !isActiveAndEnabled) return;
            languageCount = CNELocalization.LanguageCount;
            UpdateLanguageRow();
            RefreshLayout();
        }

        void UpdateLanguageRow()
        {
            if (languageRow == null) return;
            // Diller yuklenene kadar (ilk karede) satir gizli kalir; yuklenince sayiya gore acilir.
            bool show = languageCount >= 2 || (!hideLanguageWhenSingle && languageCount >= 1);
            languageRow.SetActive(show);
        }

        /// <summary>Bos bolum basliklarini gizler ve kartin boyunu icerige gore ayarlar.</summary>
        public void RefreshLayout()
        {
            for (int i = 0; i < sections.Length; i++)
            {
                var s = sections[i];
                if (s == null || s.header == null) continue;
                bool any = false;
                for (int r = 0; r < s.rows.Length && !any; r++)
                    any = s.rows[r] != null && s.rows[r].activeSelf;
                s.header.SetActive(any);
            }

            if (card == null || columns.Length == 0) return;
            float content = 0f;
            for (int i = 0; i < columns.Length; i++)
            {
                var col = columns[i];
                if (col == null) continue;
                LayoutRebuilder.ForceRebuildLayoutImmediate(col);
                content = Mathf.Max(content, LayoutUtility.GetPreferredHeight(col));
            }
            var size = card.sizeDelta;
            size.y = Mathf.Max(minCardHeight, cardTopPadding + content + cardBottomPadding);
            card.sizeDelta = size;
        }

        public void Open()
        {
            // Kart menu gizlenirken yerinde gizlendiyse nesnesi hala aciktir; OnEnable gelmeyecegi icin degerleri burada cek.
            if (gameObject.activeInHierarchy) Pull();
            if (transition != null) transition.Show();
            else gameObject.SetActive(true);
        }

        public void Close()
        {
            if (settings != null) settings.Save();
            if (transition != null) transition.Hide();
            else gameObject.SetActive(false);
            onClosed.Invoke();
        }

        /// <summary>
        /// Menu gizlenirken cagrilir: aciksa kaydeder ve karti SetActive cagirmadan gizler
        /// (ust nesnenin OnDisable'i icinde SetActive hata verir). Sonraki Open() normal acar.
        /// </summary>
        public void CloseSilently()
        {
            if (transition == null || !transition.IsVisible) return;
            if (settings != null) settings.Save();
            transition.HideInPlace();
        }

        public void ResetToDefaults()
        {
            if (settings == null) return;
            settings.ResetToDefaults();
            Pull();
        }

        void Update()
        {
            if (transition != null && !transition.IsVisible) return;
            if (CNEInput.CancelPressedThisFrame()) Close();
        }
    }
}
