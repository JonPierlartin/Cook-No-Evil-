using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CookNoEvil.UI
{
    /// <summary>
    /// Surgu (ayar cubugu) geri bildirimi: lila topuz tutulunca buyur, her %5 adimda kademe tiki,
    /// deger ekrani (cyan rakam) degisince hafifce ziplar. Rakamlarin yazilisi CNEValueFormat'tan gelir
    /// (ayar karti kaynaktaki araliga gore ayarlar).
    /// </summary>
    [RequireComponent(typeof(Slider))]
    [AddComponentMenu("Cook No Evil/UI/Surgu Geri Bildirimi")]
    public class CNESliderFeedback : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler
    {
        [Tooltip("Lila topuz (Handle)")] public RectTransform knob;
        [Tooltip("Deger ekrani (istege bagli)")] public TMP_Text valueLabel;
        [Tooltip("Percent: deger x 100 (0,8 -> 80; mikrofon 1,0 -> 100). Decimal: 1.25")] public CNEValueFormat format = CNEValueFormat.Percent;
        [Tooltip("Kac adimda bir tik sesi (0..1 araliginda)")] public float tickStep = 0.05f;
        public float grabScale = 1.18f;

        Slider slider;
        float lastTick;
        bool grabbed;

        void Awake()
        {
            slider = GetComponent<Slider>();
            slider.onValueChanged.AddListener(OnValueChanged);
            lastTick = slider.normalizedValue;
            UpdateLabel(false);
        }

        void OnDestroy()
        {
            if (slider != null) slider.onValueChanged.RemoveListener(OnValueChanged);
        }

        void OnDisable()
        {
            CNETween.Kill(this);
            grabbed = false;
            if (knob != null) knob.localScale = Vector3.one;
            if (valueLabel != null) valueLabel.rectTransform.localScale = Vector3.one;
        }

        /// <summary>Deger kodla (SetValueWithoutNotify) degistiginde ekrani gunceller.</summary>
        public void Refresh()
        {
            if (slider == null) slider = GetComponent<Slider>();
            lastTick = slider.normalizedValue;
            UpdateLabel(false);
        }

        void OnValueChanged(float value)
        {
            float n = slider.normalizedValue;
            if (Mathf.Abs(n - lastTick) >= tickStep - 0.0001f)
            {
                lastTick = Mathf.Round(n / tickStep) * tickStep;
                CNEUIAudio.Play(CNEUISound.SliderTick);
            }
            UpdateLabel(true);
        }

        void UpdateLabel(bool punch)
        {
            if (valueLabel == null || slider == null) return;
            valueLabel.text = format == CNEValueFormat.Percent
                ? Mathf.RoundToInt(slider.value * 100f).ToString(CultureInfo.InvariantCulture)
                : slider.value.ToString("0.00", CultureInfo.InvariantCulture);
            if (!punch || !isActiveAndEnabled) return;
            var rt = valueLabel.rectTransform;
            CNETween.To(this, "label", 0.16f, t =>
            {
                rt.localScale = new Vector3(1f, 1f + 0.18f * Mathf.Sin(t * Mathf.PI), 1f);
            }, CNEEase.Linear, () => rt.localScale = Vector3.one);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (slider.IsInteractable()) CNEUIAudio.Play(CNEUISound.Hover);
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (eventData is PointerEventData || CNEButtonFeedback.SilentSelect || !slider.IsInteractable()) return;
            CNEUIAudio.Play(CNEUISound.Hover);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || !slider.IsInteractable()) return;
            grabbed = true;
            CNEUIAudio.Play(CNEUISound.KnobTick);
            ScaleKnob(grabScale);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!grabbed) return;
            grabbed = false;
            ScaleKnob(1f);
        }

        void ScaleKnob(float target)
        {
            if (knob == null) return;
            var k = knob;
            float from = k.localScale.x;
            CNETween.To(this, "knob", 0.14f, t =>
            {
                float s = Mathf.LerpUnclamped(from, target, t);
                k.localScale = new Vector3(s, s, 1f);
            }, t => CNEEase.OutBack(t, 2f));
        }
    }
}
