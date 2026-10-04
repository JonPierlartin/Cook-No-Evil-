using UnityEngine;
using UnityEngine.UI;

// Çarkın tek seçeneğinin içeriği: zemin (kâğıt yuvarlak — dilim görünümünde kapalı), ikon ve ikon yoksa yazı.
// Yalnızca gösterir.
public class WheelSlot : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private Image icon;
    [SerializeField] private Text label;

    public Image Background => background;

    public void Show(Sprite sprite, string text, bool showBackground)
    {
        background.enabled = showBackground;
        icon.sprite = sprite;
        icon.enabled = sprite != null;
        // İkonu olan seçenek yalnızca ikonla, olmayan adıyla gösterilir.
        label.enabled = sprite == null;
        label.text = sprite == null ? text : string.Empty;
    }

    public void SetTint(Color color)
    {
        background.color = color;
        icon.color = color;
    }
}
