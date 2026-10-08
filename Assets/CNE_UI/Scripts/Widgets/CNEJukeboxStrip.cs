using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

namespace CookNoEvil.UI
{
    /// <summary>
    /// Jukebox basligi: donen 45'lik plak + "A3 · Parca adi" karti + onceki/sonraki oklari.
    /// Ana menunun sol alt kosesinde ve Ayarlar'daki "Muzik parcasi" satirinda kullanilir.
    /// Muzigi kendisi calmaz: bir CNEMusicSource'u (projede oyunun MusicPlayer'i) gosterir ve ona
    /// "sonraki/onceki" der. Parca baska bir yerden degisse de (ornegin ayarlardaki serit) her kare fark eder.
    /// Kaynak onceki parcayi desteklemiyorsa geri oku gizlenir.
    /// </summary>
    [AddComponentMenu("Cook No Evil/UI/Jukebox Seridi")]
    public class CNEJukeboxStrip : MonoBehaviour
    {
        [Tooltip("Muzik calar. Ana menu kendi kaynagini buraya iletir; bossa serit yalnizca gorsel ve ses verir.")]
        public CNEMusicSource source;
        public RectTransform record;
        public TMP_Text slotCode;
        public TMP_Text title;
        public Button previousButton;
        public Button nextButton;
        [Tooltip("Plak dakikada kac tur doner")] public float rpm = 45f;
        [Tooltip("Parca yokken gorunen yazi (CNE_UI tablosu)")]
        public LocalizedString noTrackText = new LocalizedString(CNELocalization.TableName, "jukebox.no_track");
        [Tooltip("Tablo yuklenene kadar gorunen yazi")] public string noTrackFallback = "Parça yok";

        int pendingDirection = 1;
        CNEMusicSource boundSource;
        int knownIndex = int.MinValue;
        string knownTitle;
        string noTrackValue;
        float spin;
        Vector2 titleRest;
        bool restCaptured;
        bool subscribed;

        void OnEnable()
        {
            if (previousButton != null) previousButton.onClick.AddListener(OnPrevious);
            if (nextButton != null) nextButton.onClick.AddListener(OnNext);
            if (title != null && !restCaptured)
            {
                titleRest = title.rectTransform.anchoredPosition;
                restCaptured = true;
            }
            if (Application.isPlaying && noTrackText != null && !noTrackText.IsEmpty)
            {
                noTrackText.StringChanged += OnNoTrackText;
                subscribed = true;
            }
            BindSource();
        }

        void OnDisable()
        {
            if (previousButton != null) previousButton.onClick.RemoveListener(OnPrevious);
            if (nextButton != null) nextButton.onClick.RemoveListener(OnNext);
            if (subscribed)
            {
                noTrackText.StringChanged -= OnNoTrackText;
                subscribed = false;
            }
            CNETween.Kill(this);
            if (title != null && restCaptured)
            {
                title.rectTransform.anchoredPosition = titleRest;
                title.alpha = 1f;
            }
        }

        void OnNoTrackText(string value)
        {
            noTrackValue = value;
            if (knownIndex < 0) RefreshInstant();
        }

        void Update()
        {
            if (source != boundSource) BindSource();
            int index = source != null ? source.CurrentIndex : -1;
            string t = source != null ? source.CurrentTitle : null;
            if (index != knownIndex || t != knownTitle)
            {
                knownIndex = index;
                knownTitle = t;
                Slide(pendingDirection);
                pendingDirection = 1;
            }

            bool playing = source != null && source.IsPlaying;
            float target = playing ? rpm * 6f : 0f;              // derece/sn
            spin = Mathf.Lerp(spin, target, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 3f));
            if (record != null && spin > 0.01f) record.Rotate(0f, 0f, -spin * Time.unscaledDeltaTime);
        }

        /// <summary>Kaynak atanmis/degismisse geri okunu ve yaziyi ona gore hemen ayarlar.</summary>
        void BindSource()
        {
            boundSource = source;
            if (previousButton != null) previousButton.gameObject.SetActive(source == null || source.CanGoPrevious);
            knownIndex = source != null ? source.CurrentIndex : -1;
            knownTitle = source != null ? source.CurrentTitle : null;
            RefreshInstant();
        }

        void OnPrevious()
        {
            CNEUIAudio.Play(CNEUISound.NeedleDrop);
            pendingDirection = -1;
            if (source != null) source.Previous(); else Slide(-1);
        }

        void OnNext()
        {
            CNEUIAudio.Play(CNEUISound.NeedleDrop);
            pendingDirection = 1;
            if (source != null) source.Next(); else Slide(1);
        }

        void RefreshInstant()
        {
            if (slotCode != null) slotCode.text = knownIndex >= 0 ? "A" + (knownIndex + 1) : "A-";
            if (title == null) return;
            if (knownIndex >= 0 && !string.IsNullOrEmpty(knownTitle)) title.text = knownTitle;
            else title.text = string.IsNullOrEmpty(noTrackValue) ? noTrackFallback : noTrackValue;
        }

        /// <summary>Baslik: eski yazi sola kayip kaybolur, yenisi sagdan gelir.</summary>
        void Slide(int direction)
        {
            if (title == null || !isActiveAndEnabled || !Application.isPlaying)
            {
                RefreshInstant();
                return;
            }
            var rt = title.rectTransform;
            var label = title;
            Vector2 rest = titleRest;
            float d = 40f * direction;
            CNETween.To(this, "title", 0.11f, t =>
            {
                rt.anchoredPosition = rest + new Vector2(-d * t, 0f);
                label.alpha = 1f - t;
            }, CNEEase.InQuad, () =>
            {
                RefreshInstant();
                CNETween.To(this, "title", 0.2f, t =>
                {
                    rt.anchoredPosition = rest + new Vector2(Mathf.LerpUnclamped(d, 0f, t), 0f);
                    label.alpha = Mathf.Clamp01(t * 1.5f);
                }, x => CNEEase.OutBack(x, 1.5f));
            });
        }
    }
}
