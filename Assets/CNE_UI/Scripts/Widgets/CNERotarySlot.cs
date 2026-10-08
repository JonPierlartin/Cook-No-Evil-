using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookNoEvil.UI
{
    /// <summary>Doner secicinin bir kademesi: tik cizgisi, lamba ve tiklanabilir etiket. CNERotarySelector kurar.</summary>
    [AddComponentMenu("Cook No Evil/UI/Doner Secici Kademesi")]
    public class CNERotarySlot : MonoBehaviour
    {
        public RectTransform tick;
        public RectTransform lamp;
        public Graphic lampGraphic;
        public RectTransform labelRect;
        public TMP_Text label;
        public CNERotaryOption option;
        [Tooltip("Etiket anahtarla geliyorsa (istege bagli)")] public CNELocalizedText localized;
    }
}
