using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CookNoEvil.UI
{
    /// <summary>
    /// Ana menudeki harfli pano satiri: hover'da satir basindaki ok lambasi yanar ve yazi saga kayar,
    /// basinca harfler sirayla ziplayip oluklarina oturur (CNELetterHop + "board" sesi).
    /// Ayni GameObject'te bir Button olmali (tiklama olayi oradan).
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Cook No Evil/UI/Menu Panosu Satiri")]
    public class CNEMenuBoardRow : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerClickHandler,
        ISelectHandler, IDeselectHandler, ISubmitHandler
    {
        [Tooltip("Saga kayan yazi")] public RectTransform label;
        [Tooltip("Basinca ziplayan harfler")] public CNELetterHop letters;
        [Tooltip("Ok lambasi")] public Graphic lamp;
        [Tooltip("Lambanin isigi")] public Graphic lampGlow;
        [Range(0f, 1f)] public float glowAlpha = 0.7f;
        public float shift = 12f;

        public CNEUISound hoverSound = CNEUISound.Hover;
        public CNEUISound pressSound = CNEUISound.BoardPress;
        [Tooltip("Tiklama tamamlaninca ek ses (None: yok). Ornek: CIKIS icin Back.")]
        public CNEUISound clickSound = CNEUISound.None;

        Selectable selectable;
        Vector2 labelRest;
        bool restCaptured;
        bool hovered;
        float lit;   // 0 = sonuk, 1 = yanik

        bool Interactable { get { return selectable == null || selectable.IsInteractable(); } }

        void Awake()
        {
            selectable = GetComponent<Selectable>();
        }

        void OnEnable()
        {
            CaptureRest();
            lit = 0f;
            ApplyLit(0f);
        }

        void OnDisable()
        {
            CNETween.Kill(this);
            hovered = false;
            if (restCaptured && label != null) label.anchoredPosition = labelRest;
            ApplyLit(0f);
        }

        void CaptureRest()
        {
            if (restCaptured || label == null) return;
            labelRest = label.anchoredPosition;
            restCaptured = true;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovered = true;
            if (!Interactable) return;
            CNEUIAudio.Play(hoverSound);
            SetLit(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
            if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject != gameObject) SetLit(false);
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (eventData is PointerEventData || CNEButtonFeedback.SilentSelect || !Interactable) return;
            CNEUIAudio.Play(hoverSound);
            SetLit(true);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if (!hovered) SetLit(false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || !Interactable) return;
            Press();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || !Interactable) return;
            CNEUIAudio.Play(clickSound);
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (!Interactable) return;
            Press();
            CNEUIAudio.Play(clickSound);
        }

        void Press()
        {
            CNEUIAudio.Play(pressSound);
            if (letters != null) letters.Hop();
            if (label != null)
            {
                CaptureRest();
                var rt = label;
                float baseX = labelRest.x + shift;
                CNETween.To(this, "dip", 0.18f, t =>
                {
                    float dip = Mathf.Sin(t * Mathf.PI) * 3f;
                    rt.anchoredPosition = new Vector2(Mathf.Lerp(rt.anchoredPosition.x, baseX, 0.5f), labelRest.y - dip);
                }, CNEEase.Linear, () => rt.anchoredPosition = new Vector2(baseX, labelRest.y));
            }
        }

        void SetLit(bool on)
        {
            CaptureRest();
            float from = lit;
            float to = on ? 1f : 0f;
            var rt = label;
            float startX = rt != null ? rt.anchoredPosition.x : 0f;
            float targetX = labelRest.x + (on ? shift : 0f);
            CNETween.Kill(this, "dip");
            CNETween.To(this, "light", on ? 0.14f : 0.12f, t =>
            {
                lit = Mathf.LerpUnclamped(from, to, Mathf.Clamp01(t));
                ApplyLit(lit);
                if (rt != null) rt.anchoredPosition = new Vector2(Mathf.LerpUnclamped(startX, targetX, t), labelRest.y);
            }, on ? (System.Func<float, float>)(t => CNEEase.OutBack(t, 1.4f)) : CNEEase.OutCubic);
        }

        void ApplyLit(float value)
        {
            if (lamp != null)
            {
                var c = lamp.color;
                c.a = value;
                lamp.color = c;
            }
            if (lampGlow != null)
            {
                var c = lampGlow.color;
                c.a = value * glowAlpha;
                lampGlow.color = c;
            }
        }
    }
}
