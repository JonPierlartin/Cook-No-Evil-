using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

namespace CookNoEvil.UI
{
    /// <summary>
    /// TMP metnini Unity Localization'daki bir girdiye (varsayilan tablo: CNE_UI) baglar.
    /// Dil degisince harfler, menu panosundaki gibi bir an dikeyde donup yeni dile gecer.
    /// Tablo yuklenene kadar prefabdaki yazi (Turkce) gorunur.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    [AddComponentMenu("Cook No Evil/UI/Yerellestirilmis Metin")]
    public class CNELocalizedText : MonoBehaviour
    {
        [Tooltip("Metnin tablo + anahtar baglantisi")]
        public LocalizedString text = new LocalizedString();
        [Tooltip("Dil degisince harfler bir an donup degissin.")]
        public bool flipOnChange = true;

        TMP_Text label;
        TMP_FontAsset defaultFont;
        object[] formatArgs;
        string raw;
        string shownCode;
        bool subscribed;

        /// <summary>Bagli anahtar (CNE_UI tablosunda).</summary>
        public string Key
        {
            get { return text != null ? text.TableEntryReference.Key : null; }
        }

        void Awake()
        {
            CacheLabel();
        }

        void OnEnable()
        {
            Subscribe();
        }

        void OnDisable()
        {
            Unsubscribe();
            CNETween.Kill(this, "flip");
            transform.localScale = Vector3.one;
        }

        void CacheLabel()
        {
            if (label != null) return;
            label = GetComponent<TMP_Text>();
            if (label != null && defaultFont == null) defaultFont = label.font;
        }

        void Subscribe()
        {
            if (subscribed || !Application.isPlaying || text == null || text.IsEmpty) return;
            text.StringChanged += OnStringChanged;
            subscribed = true;
        }

        void Unsubscribe()
        {
            if (!subscribed) return;
            text.StringChanged -= OnStringChanged;
            subscribed = false;
        }

        /// <summary>Anahtari degistirir (tablo: CNE_UI). Bagliysa yeni metin hemen yuklenir.</summary>
        public void SetKey(string key)
        {
            if (text == null) text = new LocalizedString();
            if (string.Equals(Key, key, StringComparison.Ordinal) && !text.IsEmpty) return;
            raw = null; // eski anahtarin cevirisi yeni anahtar yuklenene kadar yeniden basilmasin
            text.SetReference(CNELocalization.TableName, key);
            if (isActiveAndEnabled) Subscribe();
        }

        /// <summary>Son gelen ceviriyi yeniden yazar (metni baska bir kod degistirdiyse). Henuz ceviri yoksa bir sey yapmaz.</summary>
        public void Reapply()
        {
            if (raw != null) Show(raw);
        }

        /// <summary>Metindeki {0}, {1}... yerlerine gelecek degerler (ornek: surum numarasi).</summary>
        public void SetArgs(params object[] args)
        {
            formatArgs = args;
            if (raw != null) Show(raw);
        }

        void OnStringChanged(string value)
        {
            raw = value;
            string code = CNELocalization.CurrentCode;
            bool languageChanged = shownCode != null && code != null && code != shownCode;
            shownCode = code;
            if (languageChanged && flipOnChange && isActiveAndEnabled) Flip();
            else Show(value);
        }

        void Show(string value)
        {
            CacheLabel();
            if (label == null) return;
            string shown = value ?? string.Empty;
            if (formatArgs != null && formatArgs.Length > 0)
            {
                try { shown = string.Format(shown, formatArgs); }
                catch (FormatException) { }
            }
            label.text = shown;

            var def = CNELocalization.FindDisplayInfo(shownCode);
            var font = def != null && def.fontOverride != null ? def.fontOverride : defaultFont;
            if (font != null && label.font != font) label.font = font;
        }

        void Flip()
        {
            var t = transform;
            bool swapped = false;
            CNETween.To(this, "flip", 0.18f, p =>
            {
                if (!swapped && p >= 0.5f)
                {
                    swapped = true;
                    Show(raw);
                }
                float s = p < 0.5f ? 1f - p * 2f : (p - 0.5f) * 2f;
                t.localScale = new Vector3(1f, Mathf.Max(0.0001f, s), 1f);
            }, CNEEase.Linear, () =>
            {
                t.localScale = Vector3.one;
                if (!swapped) Show(raw);
            });
        }
    }
}
