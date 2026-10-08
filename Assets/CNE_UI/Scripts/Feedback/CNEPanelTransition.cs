using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CookNoEvil.UI
{
    /// <summary>
    /// Panel gecisi "tabela iner": kart yukaridan duser, hafifce asip oturur ve asili tabela gibi
    /// sonumlenerek salinir. Kapanirken hizla yukari kalkar. Arka plan karartilir.
    /// Kok: tam ekran RectTransform + CanvasGroup. Cocuklar: Dim (karartma) ve Card (kart).
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    [AddComponentMenu("Cook No Evil/UI/Panel Gecisi (Tabela)")]
    public class CNEPanelTransition : MonoBehaviour
    {
        [Tooltip("Inip kalkan kart. Bos: bu nesne.")] public RectTransform card;
        [Tooltip("Acilinca yeniden yanacak neon baslik (istege bagli).")] public CNENeonFlicker titleNeon;
        public float dropDistance = 140f;
        public float showTime = 0.34f;
        public float hideTime = 0.16f;
        public float swingDegrees = 2.2f;
        [Tooltip("Acilinca secilecek oge (klavye/gamepad gezintisi icin).")] public Selectable firstSelected;
        public bool playSounds = true;
        public bool startHidden = true;
        public UnityEvent onShown = new UnityEvent();
        public UnityEvent onHidden = new UnityEvent();

        CanvasGroup group;
        Vector2 cardRest;
        bool restCaptured;
        bool visible;

        public bool IsVisible { get { return visible; } }

        void Awake()
        {
            group = GetComponent<CanvasGroup>();
            if (card == null) card = (RectTransform)transform;
            CaptureRest();
            if (startHidden && !visible)
            {
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;
            }
        }

        void CaptureRest()
        {
            if (restCaptured || card == null) return;
            cardRest = card.anchoredPosition;
            restCaptured = true;
        }

        public void Toggle()
        {
            if (visible) Hide(); else Show();
        }

        public void Show()
        {
            if (group == null) group = GetComponent<CanvasGroup>();
            if (card == null) card = (RectTransform)transform;
            gameObject.SetActive(true);
            CaptureRest();
            visible = true;
            group.interactable = true;
            group.blocksRaycasts = true;
            transform.SetAsLastSibling();
            if (playSounds) CNEUIAudio.Play(CNEUISound.PanelOpen);

            var c = card;
            Vector2 rest = cardRest;
            float drop = dropDistance;
            CNETween.To(this, "panel", showTime, t =>
            {
                c.anchoredPosition = rest + new Vector2(0f, Mathf.LerpUnclamped(drop, 0f, t));
            }, t => CNEEase.OutBack(t, 1.6f), () => onShown.Invoke());

            float a0 = group.alpha;
            var g = group;
            CNETween.To(this, "fade", showTime * 0.6f, t => g.alpha = Mathf.Lerp(a0, 1f, t), CNEEase.OutQuad);

            float swing = swingDegrees;
            CNETween.To(this, "swing", 0.95f, t =>
            {
                c.localRotation = Quaternion.Euler(0f, 0f, swing * CNEEase.Swing(t, 2.2f, 3.5f));
            }, CNEEase.Linear, () => c.localRotation = Quaternion.identity);

            if (titleNeon != null) titleNeon.Reignite();
            SelectFirst();
        }

        public void Hide()
        {
            Hide(false);
        }

        public void Hide(bool instant)
        {
            if (group == null) group = GetComponent<CanvasGroup>();
            if (!gameObject.activeInHierarchy && !visible) return;
            visible = false;
            group.interactable = false;
            group.blocksRaycasts = false;
            if (instant || !gameObject.activeInHierarchy)
            {
                FinishHide();
                return;
            }
            if (playSounds) CNEUIAudio.Play(CNEUISound.PanelClose);
            CNETween.Kill(this, "swing");
            var c = card;
            Vector2 from = c.anchoredPosition;
            Vector2 to = cardRest + new Vector2(0f, dropDistance * 0.6f);
            CNETween.To(this, "panel", hideTime, t => c.anchoredPosition = Vector2.LerpUnclamped(from, to, t), CNEEase.InCubic, FinishHide);
            float a0 = group.alpha;
            var g = group;
            CNETween.To(this, "fade", hideTime, t => g.alpha = Mathf.Lerp(a0, 0f, t), CNEEase.InQuad);
        }

        /// <summary>
        /// Paneli SetActive cagirmadan, animasyonsuz gizler (gorunmez, tiklanmaz). Ust nesne kapanirken
        /// (OnDisable icinde) kullanilir; o sirada SetActive cagirmak Unity'de hata verir.
        /// Sonraki Show() her zamanki gibi acar.
        /// </summary>
        public void HideInPlace()
        {
            if (group == null) group = GetComponent<CanvasGroup>();
            CNETween.Kill(this);
            visible = false;
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            if (card != null && restCaptured)
            {
                card.anchoredPosition = cardRest;
                card.localRotation = Quaternion.identity;
            }
            onHidden.Invoke();
        }

        void FinishHide()
        {
            group.alpha = 0f;
            if (card != null)
            {
                card.anchoredPosition = cardRest;
                card.localRotation = Quaternion.identity;
            }
            gameObject.SetActive(false);
            onHidden.Invoke();
        }

        void SelectFirst()
        {
            var es = EventSystem.current;
            if (firstSelected == null || es == null) return;
            CNEButtonFeedback.SilentSelect = true;
            es.SetSelectedGameObject(firstSelected.gameObject);
            CNEButtonFeedback.SilentSelect = false;
        }
    }
}
