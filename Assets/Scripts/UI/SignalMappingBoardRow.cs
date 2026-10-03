using UnityEngine;
using UnityEngine.UI;

// Duvar malzeme panosunun tek satırı: bir kanal değeri (numara / yön oku) + o değere eşleşen malzemenin ikonu.
// Yalnızca gösterir; içerik SignalMappingBoard'dan gelir.
public class SignalMappingBoardRow : MonoBehaviour
{
    [Tooltip("Değerin ikonu (ör. yön oku). SignalValue.icon doluysa kullanılır.")]
    [SerializeField] private Image valueIcon;
    [Tooltip("Değerin yazısı (ör. numara). SignalValue.icon boşsa displayName yazılır.")]
    [SerializeField] private Text valueLabel;
    [SerializeField] private Image itemIcon;

    public void Show(SignalValue value, ItemType item)
    {
        bool hasValueIcon = value.Icon != null;
        valueIcon.enabled = hasValueIcon;
        valueIcon.sprite = value.Icon;
        valueLabel.enabled = !hasValueIcon;
        valueLabel.text = hasValueIcon ? string.Empty : value.DisplayName;

        itemIcon.sprite = item.Icon;
        itemIcon.enabled = item.Icon != null;
    }
}
