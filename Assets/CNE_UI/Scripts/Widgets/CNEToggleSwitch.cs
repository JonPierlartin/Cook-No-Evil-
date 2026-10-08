using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CookNoEvil.UI
{
    /// <summary>
    /// 50'ler salteri: krem kol yuvasinda kayar ("klak"), yaninda LED yanar, etiket ACIK/KAPALI olur.
    /// Ayni GameObject'te bir Toggle olmali.
    /// </summary>
    [RequireComponent(typeof(Toggle))]
    [AddComponentMenu("Cook No Evil/UI/Salter (Toggle)")]
    public class CNEToggleSwitch : MonoBehaviour, IPointerEnterHandler, ISelectHandler
    {
        [Tooltip("Kayan krem kol")] public RectTransform paddle;
        public float offX = -22f;
        public float onX = 22f;
        public float tiltDegrees = 9f;
        public Graphic led;
        public Graphic ledGlow;
        public Color ledOff = new Color(0.369f, 0.416f, 0.451f, 1f); // #5E6A73
        public Color ledOn = new Color(0.455f, 0.749f, 0.290f, 1f);  // #74BF4A
        [Range(0f, 1f)] public float glowAlpha = 0.7f;
        [Tooltip("ACIK / KAPALI yazisi (istege bagli)")] public CNELocalizedText stateLabel;
        public string onKey = "common.on";
        public string offKey = "common.off";

        Toggle toggle;

        void Awake()
        {
            toggle = GetComponent<Toggle>();
            toggle.onValueChanged.AddListener(OnToggled);
            Snap(toggle.isOn);
        }

        void OnDestroy()
        {
            if (toggle != null) toggle.onValueChanged.RemoveListener(OnToggled);
        }

        void OnDisable()
        {
            CNETween.Kill(this);
            if (toggle != null) Snap(toggle.isOn);
        }

        /// <summary>Ses ve olay tetiklemeden durumu ayarlar (ayarlar acilirken).</summary>
        public void SetIsOnSilently(bool on)
        {
            if (toggle == null) toggle = GetComponent<Toggle>();
            toggle.SetIsOnWithoutNotify(on);
            CNETween.Kill(this);
            Snap(on);
        }

        void OnToggled(bool on)
        {
            CNEUIAudio.Play(on ? CNEUISound.SwitchOn : CNEUISound.SwitchOff);
            UpdateLabel(on);
            if (paddle == null) return;

            var p = paddle;
            float fromX = p.anchoredPosition.x;
            float toX = on ? onX : offX;
            Color fromLed = led != null ? led.color : ledOff;
            Color toLed = on ? ledOn : ledOff;
            float fromGlow = ledGlow != null ? ledGlow.color.a : 0f;
            float toGlow = on ? glowAlpha : 0f;
            float tilt = tiltDegrees * (on ? -1f : 1f);

            CNETween.To(this, "switch", 0.18f, t =>
            {
                var pos = p.anchoredPosition;
                pos.x = Mathf.LerpUnclamped(fromX, toX, t);
                p.anchoredPosition = pos;
                p.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * tilt);
                float k = Mathf.Clamp01(t * 1.6f);
                if (led != null) led.color = Color.Lerp(fromLed, toLed, k);
                if (ledGlow != null)
                {
                    var g = ledGlow.color;
                    g.a = Mathf.Lerp(fromGlow, toGlow, k);
                    ledGlow.color = g;
                }
            }, t => CNEEase.OutBack(t, 1.8f), () => p.localRotation = Quaternion.identity);
        }

        void Snap(bool on)
        {
            if (paddle != null)
            {
                var pos = paddle.anchoredPosition;
                pos.x = on ? onX : offX;
                paddle.anchoredPosition = pos;
                paddle.localRotation = Quaternion.identity;
            }
            if (led != null) led.color = on ? ledOn : ledOff;
            if (ledGlow != null)
            {
                var g = ledGlow.color;
                g.a = on ? glowAlpha : 0f;
                ledGlow.color = g;
            }
            UpdateLabel(on);
        }

        void UpdateLabel(bool on)
        {
            if (stateLabel != null) stateLabel.SetKey(on ? onKey : offKey);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (toggle != null && toggle.IsInteractable()) CNEUIAudio.Play(CNEUISound.Hover);
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (eventData is PointerEventData || CNEButtonFeedback.SilentSelect) return;
            if (toggle != null && toggle.IsInteractable()) CNEUIAudio.Play(CNEUISound.Hover);
        }
    }
}
