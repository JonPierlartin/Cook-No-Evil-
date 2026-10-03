using UnityEngine;
using UnityEngine.UI;

// Tarif kitapçığındaki tek giriş: bir resim ve altında/yanında adı (içindekilerde kategori, açılımda malzeme).
// Yalnızca gösterir.
public class RecipeBookEntry : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Text label;

    public void Show(Sprite sprite, string text)
    {
        icon.sprite = sprite;
        icon.enabled = sprite != null;
        label.text = text;
    }
}
