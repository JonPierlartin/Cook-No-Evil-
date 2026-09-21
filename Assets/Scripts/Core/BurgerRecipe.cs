using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct IngredientRequirement
{
    public ItemType Type;
    public int Quantity;
}

// Bir tarif + varyasyonunu (orn. "sogansiz") tanimlar. BurgerAssemblyStation bu
// veriye gore masadaki yerlestirmeleri dogrular. Musteri/siparis sistemi bu
// tarifi henuz SECMIYOR — test icin Inspector'dan sabit atanir.
[CreateAssetMenu(fileName = "BurgerRecipe", menuName = "Cook No Evil/Burger Recipe")]
public class BurgerRecipe : ScriptableObject
{
    [SerializeField] private string recipeName;
    [SerializeField] private List<IngredientRequirement> requiredIngredients = new();
    [Tooltip("Bu varyasyonda tarifte olsa bile kullanilmasi yasak olan malzemeler (orn. sogansiz varyasyon).")]
    [SerializeField] private List<ItemType> excludedIngredients = new();

    public string RecipeName => recipeName;
    public IReadOnlyList<IngredientRequirement> RequiredIngredients => requiredIngredients;
    public IReadOnlyList<ItemType> ExcludedIngredients => excludedIngredients;

    public bool AllowsIngredient(ItemType type)
    {
        if (excludedIngredients.Contains(type))
            return false;

        foreach (var requirement in requiredIngredients)
        {
            if (requirement.Type == type)
                return true;
        }

        return false;
    }
}
