using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CookNoEvil.UI
{
    /// <summary>Butonun anlami; tiklama sesini belirler.</summary>
    public enum CNEButtonRole
    {
        Normal = 0,   // lila   : tus geri donus sesi
        Primary = 1,  // marul  : servis zili "ding" (LOBIYI AC, KATIL, BASLAT)
        Back = 2,     // peynir : yumusak "tok" (GERI, VAZGEC, KAPAT, CIKIS)
        Danger = 3    // domates: tus sesi; hata/buzzer yalnizca reddedilince
    }

    /// <summary>
    /// "Diner tusu" geri bildirimi: hover'da hafif kalkar ve krom pariltisi gecer, basinca golgesine
    /// gomulur ve ezilir, birakinca hafifce asarak yerine oturur. Sesler rol ve olaya gore calar.
    /// Fare, klavye ve gamepad (Select/Submit) ile ayni sekilde calisir.
    /// Kurulum: kokte Button; gorsel parcalar "Face" (yuz) adli cocukta, golge ayri cocukta.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Cook No Evil/UI/Buton Geri Bildirimi")]
    public class CNEButtonFeedback : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler,
        ISelectHandler, IDeselectHandler, ISubmitHandler
    {
        /// <summary>Kodla secim yapilirken (panel acilisi) hover sesi calmasin diye kisa sureli acilir.</summary>
        public static bool SilentSelect;

        [Header("Parcalar")]
        [Tooltip("Basinca gomulen yuz. Bos: bu nesne (yerlesim grubundaysa yuzu ayri cocuk yapin).")]
        public RectTransform face;
        [Tooltip("Hover'da yuzun uzerinden gecen parilti (yuzde Mask olmali). Istege bagli.")]
        public RectTransform glint;

        [Header("Ses")]
        public CNEButtonRole role = CNEButtonRole.Normal;
        public CNEUISound hoverSound = CNEUISound.Hover;
        public CNEUISound pressSound = CNEUISound.Press;
        [Tooltip("None: role gore secilir (Primary = zil, Back = tok, digerleri = tus donusu).")]
        public CNEUISound clickSound = CNEUISound.None;
        [Tooltip("Kapatin: tiklama sesini panel gecisi gibi baska bir sey caliyorsa.")]
        public bool playClickSound = true;
        public CNEUISound deniedSound = CNEUISound.Error;

        [Header("Animasyon")]
        public float hoverScale = 1.04f;
        public float hoverLift = 2f;
        [Tooltip("Golge ofsetiyle ayni olmali; yuz golgenin uzerine oturur.")]
        public float pressSink = 5f;
        public Vector2 pressSquash = new Vector2(1.03f, 0.93f);
        public float hoverTime = 0.12f;
        public float pressTime = 0.06f;
        public float releaseTime = 0.24f;
        [Tooltip("Birakinca asma miktari (OutBack).")]
        public float releaseOvershoot = 2.4f;

        Selectable selectable;
        Vector2 faceRestPosition;
        Vector3 faceRestScale = Vector3.one;
        bool restCaptured;
        bool hovered;
        bool pressed;
        Vector2 currentOffset;
        Vector3 currentScale = Vector3.one;
        float denyOffset;

        RectTransform Face { get { return face != null ? face : (RectTransform)transform; } }

        bool Interactable { get { return selectable == null || selectable.IsInteractable(); } }

        CNEUISound ResolvedClickSound
        {
            get
            {
                if (clickSound != CNEUISound.None) return clickSound;
                switch (role)
                {
                    case CNEButtonRole.Primary: return CNEUISound.Confirm;
                    case CNEButtonRole.Back: return CNEUISound.Back;
                    default: return CNEUISound.Release;
                }
            }
        }

        void Awake()
        {
            selectable = GetComponent<Selectable>();
        }

        void OnEnable()
        {
            CaptureRest();
        }

        void OnDisable()
        {
            CNETween.Kill(this);
            hovered = false;
            pressed = false;
            currentOffset = Vector2.zero;
            currentScale = Vector3.one;
            denyOffset = 0f;
            if (restCaptured) Apply();
        }

        void CaptureRest()
        {
            if (restCaptured) return;
            var f = Face;
            faceRestPosition = f.anchoredPosition;
            faceRestScale = f.localScale;
            restCaptured = true;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovered = true;
            if (!Interactable) return;
            CNEUIAudio.Play(hoverSound);
            if (!pressed) AnimateTo(new Vector2(0f, hoverLift), new Vector3(hoverScale, hoverScale, 1f), hoverTime, CNEEase.OutCubic);
            PlayGlint();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
            if (!pressed) AnimateRest();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (!Interactable)
            {
                Deny();
                return;
            }
            pressed = true;
            CNEUIAudio.Play(pressSound);
            AnimatePress();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || !pressed) return;
            pressed = false;
            AnimateRelease();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || !Interactable) return;
            if (playClickSound) CNEUIAudio.Play(ResolvedClickSound);
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (eventData is PointerEventData || SilentSelect || !Interactable) return; // fareyle secimde hover zaten caldi
            CNEUIAudio.Play(hoverSound);
            if (!pressed) AnimateTo(new Vector2(0f, hoverLift), new Vector3(hoverScale, hoverScale, 1f), hoverTime, CNEEase.OutCubic);
            PlayGlint();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if (!hovered && !pressed) AnimateRest();
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (!Interactable)
            {
                Deny();
                return;
            }
            CNEUIAudio.Play(pressSound);
            if (playClickSound) CNEUIAudio.Play(ResolvedClickSound);
            AnimatePress();
            pressed = false;
            CNETween.To(this, "submit", pressTime + 0.02f, t => { }, CNEEase.Linear, AnimateRelease);
        }

        void AnimatePress()
        {
            AnimateTo(new Vector2(0f, -pressSink), new Vector3(pressSquash.x, pressSquash.y, 1f), pressTime, CNEEase.OutQuad);
        }

        void AnimateRelease()
        {
            bool lift = hovered || (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject);
            var offset = lift ? new Vector2(0f, hoverLift) : Vector2.zero;
            var scale = lift ? new Vector3(hoverScale, hoverScale, 1f) : Vector3.one;
            float overshoot = releaseOvershoot;
            AnimateTo(offset, scale, releaseTime, t => CNEEase.OutBack(t, overshoot));
        }

        void AnimateRest()
        {
            AnimateTo(Vector2.zero, Vector3.one, hoverTime, CNEEase.OutCubic);
        }

        void AnimateTo(Vector2 offset, Vector3 scale, float time, Func<float, float> ease)
        {
            CaptureRest();
            Vector2 fromOffset = currentOffset;
            Vector3 fromScale = currentScale;
            CNETween.To(this, "move", time, t =>
            {
                currentOffset = Vector2.LerpUnclamped(fromOffset, offset, t);
                currentScale = Vector3.LerpUnclamped(fromScale, scale, t);
                Apply();
            }, ease);
        }

        void Apply()
        {
            var f = Face;
            f.anchoredPosition = faceRestPosition + currentOffset + new Vector2(denyOffset, 0f);
            f.localScale = Vector3.Scale(faceRestScale, currentScale);
        }

        /// <summary>Pasif butona basilinca: kisa "hayir" titremesi + buzzer (kisik).</summary>
        public void Deny()
        {
            CaptureRest();
            CNEUIAudio.Play(deniedSound, 0.6f);
            CNETween.To(this, "deny", 0.32f, t =>
            {
                denyOffset = Mathf.Sin(t * Mathf.PI * 6f) * (1f - t) * 7f;
                Apply();
            }, CNEEase.Linear, () =>
            {
                denyOffset = 0f;
                Apply();
            });
        }

        void PlayGlint()
        {
            if (glint == null) return;
            var f = Face;
            float travel = f.rect.width * 0.5f + glint.rect.width;
            var g = glint;
            g.gameObject.SetActive(true);
            CNETween.To(this, "glint", 0.38f, t =>
            {
                var p = g.anchoredPosition;
                p.x = Mathf.Lerp(-travel, travel, t);
                g.anchoredPosition = p;
            }, CNEEase.InOutCubic);
        }
    }
}
