using UnityEngine;
using UnityEngine.EventSystems;

namespace CookNoEvil.UI
{
    /// <summary>Doner secicinin tiklanabilir etiketi: tiklayinca o kademeyi secer.</summary>
    [AddComponentMenu("Cook No Evil/UI/Doner Secici Etiketi")]
    public class CNERotaryOption : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerDownHandler
    {
        public CNERotarySelector owner;
        public int option;

        // Basmayi etiket ustlensin ki tiklama ust nesneye (dugmeye) gitmesin.
        public void OnPointerDown(PointerEventData eventData) { }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (owner != null && eventData.button == PointerEventData.InputButton.Left) owner.SelectOption(option);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (owner != null && owner.IsInteractable()) CNEUIAudio.Play(CNEUISound.Hover);
        }
    }
}
