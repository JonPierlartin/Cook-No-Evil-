using UnityEngine;

// id -> IngredientType cozumlemesinin TEK yasadigi yer. PlayerInventory yalnizca int id tasir
// (ag uzerinden); id'den oge turunu bulmasi gereken her tuketici (HotbarUI, BurgerAssemblyStation,
// HeldItemVisual, PlacementPreview) Inspector'dan bu tek asset'e referans verir — kendi dizisini
// tutmaz. Yeni bir IngredientType eklendiginde yalnizca burada kayit edilir.
[CreateAssetMenu(fileName = "IngredientRegistry", menuName = "Cook No Evil/Ingredient Registry")]
public class IngredientRegistry : ScriptableObject
{
    [Tooltip("Oyundaki tum malzeme turleri. Id'ler benzersiz olmali; -1 bos slot icin ayrilmistir.")]
    [SerializeField] private IngredientType[] ingredients;

    // Kayitli degilse (veya id bos slot ise) null doner.
    public IngredientType Find(int id)
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
