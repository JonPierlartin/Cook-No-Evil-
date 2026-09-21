using UnityEngine;

// id -> ItemType cozumlemesinin TEK yasadigi yer. PlayerInventory yalnizca int id tasir
// (ag uzerinden); id'den oge turunu bulmasi gereken her tuketici (HotbarUI, BurgerAssemblyStation,
// HeldItemVisual, PlacementPreview) Inspector'dan bu tek asset'e referans verir — kendi dizisini
// tutmaz. Yeni bir ItemType eklendiginde yalnizca burada kayit edilir.
[CreateAssetMenu(fileName = "ItemRegistry", menuName = "Cook No Evil/Item Registry")]
public class ItemRegistry : ScriptableObject
{
    [Tooltip("Oyundaki tum oge turleri. Id'ler benzersiz olmali; -1 bos slot icin ayrilmistir.")]
    [SerializeField] private ItemType[] ingredients;

    // Kayitli degilse (veya id bos slot ise) null doner.
    public ItemType Find(int id)
    {
        if (ingredients == null)
            return null;

        foreach (var ingredient in ingredients)
        {
            if (ingredient != null && ingredient.Id == id)
                return ingredient;
        }

        return null;
    }
}
