using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookNoEvil.UI
{
    /// <summary>
    /// Dil secicideki tek dil karti: toon bayrak + dilin kendi adi. Secilince bayrak dalgalanir,
    /// kart hafifce ziplar, LED yanar. CNELanguagePicker sablondan kopyalar.
    /// Hover/basma sesi ve animasyonu ayni nesnedeki CNEButtonFeedback'ten gelir.
    /// </summary>
    [AddComponentMenu("Cook No Evil/UI/Dil Karti")]
    public class CNELanguageChip : MonoBehaviour
    {
        public Button button;
        [Tooltip("Secilince ziplayan kap (CNEButtonFeedback'in yuzunden farkli bir nesne olmali)")]
        public RectTransform body;
        public Image flag;
        public TMP_Text label;
        [Tooltip("Secili oldugunda gorunen lila cerceve")] public Graphic selectedRing;
        public Graphic led;
        public Color ledOff = new Color(0.369f, 0.416f, 0.451f, 1f);
        public Color ledOn = new Color(0.455f, 0.749f, 0.290f, 1f);
        public Color labelOn = new Color(0.141f, 0.137f, 0.227f, 1f);
        public Color labelOff = new Color(0.369f, 0.416f, 0.451f, 1f);

        CNELanguageOption language;
        CNELanguagePicker owner;
        bool selected;

        public string Code { get { return language != null ? language.code : null; } }

        public void Setup(CNELanguageOption option, CNELanguagePicker picker)
        {
            language = option;
            owner = picker;
            if (flag != null)
            {
                flag.sprite = option.flag;
                flag.enabled = option.flag != null;
            }
            if (label != null)
            {
                label.text = option.displayName;
                if (option.fontOverride != null) label.font = option.fontOverride;
            }
            if (button != null)
            {
                button.onClick.RemoveListener(OnChosen);
                button.onClick.AddListener(OnChosen);
            }
            name = "Lang_" + option.code;
        }

        void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(OnChosen);
        }

        void OnChosen()
        {
            CNEUIAudio.Play(CNEUISound.FlagFlap);
            Flutter();
            if (owner != null) owner.Choose(language);
        }

        public void SetSelected(bool on, bool animate)
        {
            bool changed = on != selected;
            selected = on;
            if (selectedRing != null)
            {
                var c = selectedRing.color;
                c.a = on ? 1f : 0f;
                selectedRing.color = c;
            }
            if (led != null) led.color = on ? ledOn : ledOff;
            if (label != null) label.color = on ? labelOn : labelOff;
            if (animate && changed && on && body != null && isActiveAndEnabled)
            {
                var b = body;
                CNETween.To(this, "pop", 0.26f, t =>
                {
                    float s = Mathf.LerpUnclamped(1.12f, 1f, t);
                    b.localScale = new Vector3(s, s, 1f);
                }, x => CNEEase.OutBack(x, 2.2f), () => b.localScale = Vector3.one);
            }
        }

        void Flutter()
        {
            if (flag == null || !isActiveAndEnabled) return;
            var f = flag.rectTransform;
            CNETween.To(this, "flutter", 0.5f, t =>
            {
                float damp = 1f - t;
                f.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI * 5f) * 7f * damp);
                float sx = 1f + 0.07f * Mathf.Sin(t * Mathf.PI * 4f + 0.6f) * damp;
                f.localScale = new Vector3(sx, 2f - sx, 1f);
            }, CNEEase.Linear, () =>
            {
                f.localRotation = Quaternion.identity;
                f.localScale = Vector3.one;
            });
        }

        void OnDisable()
        {
            CNETween.Kill(this);
            if (body != null) body.localScale = Vector3.one;
            if (flag != null)
            {
                flag.rectTransform.localRotation = Quaternion.identity;
                flag.rectTransform.localScale = Vector3.one;
            }
        }
    }
}
