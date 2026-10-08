using System;
using UnityEngine;
using UnityEngine.UI;

namespace CookNoEvil.UI
{
    /// <summary>
    /// Ibreli VU metre: "TEST" salteri acikken mikrofon seviyesini gosterir, ibre konusmaya gore oynar
    /// (hizli cikis, yavas inis). Seviye bir CNEMicLevelSource'tan gelir; salter kapaliyken kaynak hic acilmaz.
    /// Gosterilen seviye, ayarlardaki mikrofon kazanciyla (1 = oldugu gibi) carpilir.
    /// </summary>
    [AddComponentMenu("Cook No Evil/UI/VU Metre (Mikrofon Testi)")]
    public class CNEVUMeter : MonoBehaviour
    {
        [Tooltip("Seviye kaynagi. Ayarlar karti kendi kaynagini iletir.")] public CNEMicLevelSource source;
        public RectTransform needle;
        [Tooltip("Ibre dinlenme acisi (sol)")] public float restAngle = 46f;
        [Tooltip("Ibre tam sapma acisi (sag)")] public float maxAngle = -46f;
        [Tooltip("TEST salteri")] public Toggle testToggle;
        [Tooltip("Durum yazisi (istege bagli)")] public CNELocalizedText statusLabel;
        public string noMicKey = "settings.mic_none";
        public string hintKey = "settings.mic_hint";
        [Tooltip("Olcek alt siniri (dB)")] public float floorDb = -48f;

        /// <summary>Mikrofon kazanci (1 = oldugu gibi). Ayarlar karti baglar; bossa 1.</summary>
        public Func<float> gain;

        float level;
        bool listening;

        void OnEnable()
        {
            if (testToggle != null)
            {
                testToggle.onValueChanged.AddListener(OnTestToggled);
                var sw = testToggle.GetComponent<CNEToggleSwitch>();
                if (sw != null) sw.SetIsOnSilently(false); else testToggle.SetIsOnWithoutNotify(false);
            }
            level = 0f;
            SetNeedle(0f);
            UpdateStatus();
        }

        void OnDisable()
        {
            if (testToggle != null) testToggle.onValueChanged.RemoveListener(OnTestToggled);
            StopListening();
        }

        void OnTestToggled(bool on)
        {
            if (on) StartListening(); else StopListening();
        }

        void StartListening()
        {
            if (source == null || !source.IsAvailable || !source.Begin())
            {
                UpdateStatus();
                var sw = testToggle != null ? testToggle.GetComponent<CNEToggleSwitch>() : null;
                if (sw != null) sw.SetIsOnSilently(false);
                CNEUIAudio.Play(CNEUISound.Error, 0.7f);
                return;
            }
            listening = true;
        }

        void StopListening()
        {
            if (listening && source != null) source.End();
            listening = false;
        }

        void UpdateStatus()
        {
            if (statusLabel == null) return;
            bool available = source != null && source.IsAvailable;
            statusLabel.SetKey(available ? hintKey : noMicKey);
        }

        void Update()
        {
            float target = 0f;
            if (listening && source != null)
            {
                float k = gain != null ? gain() : 1f;
                float rms = source.ReadRms() * k;
                float db = 20f * Mathf.Log10(Mathf.Max(rms, 1e-6f));
                target = Mathf.InverseLerp(floorDb, 0f, db);
            }
            float dt = Time.unscaledDeltaTime;
            float speed = target > level ? 28f : 5f; // VU balistigi: hizli cikis, yavas inis
            level = Mathf.Lerp(level, target, 1f - Mathf.Exp(-dt * speed));
            SetNeedle(level);
        }

        void SetNeedle(float value)
        {
            if (needle != null) needle.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(restAngle, maxAngle, value));
        }
    }
}
