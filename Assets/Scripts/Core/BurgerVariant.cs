using System;
using System.Collections.Generic;
using UnityEngine;

// Menüdeki hazır bir hamburger varyantı (GDD 6.7: "müşterinin sipariş edebileceği hazır kombinasyon"). Varyant adı
// yalnızca seviye tasarımı etiketidir; oyuncuya isim olarak hiç gösterilmez (GDD 3.6). Görsel, müşteri pop-up'ı ve
// tarif kitapçığında AYNI resimdir (GDD 3.6.2). Ekmek her hamburgerde sabittir ve sinyal gerektirmez (GDD 3.6.2) —
// bu yüzden listede yer almaz; liste protein + garnitür + sos malzemeleridir.
// (Eski BurgerRecipe'nin yerine geçti — aynı script GUID'i; tarif doğrulaması yok, GDD 6.7.3.)
[CreateAssetMenu(fileName = "BurgerVariant", menuName = "Cook No Evil/Burger Variant")]
public class BurgerVariant : ScriptableObject
{
    [Serializable]
    public struct Ingredient
    {
        public ItemType item;
        [Tooltip("Eksik malzeme 'komple randomize' kısayolu yalnızca işaretlileri alır (GDD 7.3.2; 30 Eyl kararı). " +
            "Protein genelde işaretlenmez — etsiz hamburger kapatılamaz.")]
        public bool removable;
    }

    [Tooltip("Yalnızca seviye tasarımı etiketi; oyuncuya gösterilmez.")]
    [SerializeField] private string designLabel;
    [Tooltip("Pop-up ve kitapçık resmi (GDD 3.6.2). Şimdilik boş olabilir.")]
    [SerializeField] private Sprite image;
    [SerializeField] private List<Ingredient> ingredients = new();

    public string DesignLabel => designLabel;
    public Sprite Image => image;
    public IReadOnlyList<Ingredient> Ingredients => ingredients;

    public bool Contains(ItemType item)
    {
        foreach (var ingredient in ingredients)
        {
            if (ingredient.item == item)
                return true;
        }

        return false;
    }

    public List<ItemType> GetRemovableItems()
    {
        var result = new List<ItemType>();
        foreach (var ingredient in ingredients)
        {
            if (ingredient.removable && ingredient.item != null)
                result.Add(ingredient.item);
        }

        return result;
    }
}
