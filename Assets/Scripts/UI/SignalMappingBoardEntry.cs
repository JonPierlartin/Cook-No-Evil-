using UnityEngine;
using UnityEngine.UI;

// Duvar malzeme panosunun tek girişi: bir malzemenin resmi ve adı. Kanal değeri (numara / yön) YAZILMAZ — değeri
// girişin panodaki YERİ anlatır (bkz. SignalMappingBoard). Yalnızca gösterir.
public class SignalMappingBoardEntry : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Text label;

    public void Show(ItemType item)
    {
        icon.sprite = item.Icon;
        icon.enabled = item.Icon != null;
        label.text = item.DisplayName;
    }
}
