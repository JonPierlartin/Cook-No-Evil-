using System.Collections.Generic;
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

#if UNITY_EDITOR
    private bool _validationQueued;

    // Kayit defteri her degistiginde (ve script yenilemesinde) itemPrefab dogrulamasi calisir. AssetDatabase
    // erisimi OnValidate icinde riskli oldugu icin bir sonraki editor turuna ertelenir; birden cok tetik tek
    // dogrulamaya iner. Saglikli durumda hicbir sey basmaz.
    private void OnValidate()
    {
        if (_validationQueued)
            return;

        _validationQueued = true;
        UnityEditor.EditorApplication.delayCall += () =>
        {
            _validationQueued = false;
            if (this != null)
                ValidateItemPrefabs();
        };
    }

    // Her turun itemPrefab'i icin ag kabugu kurallarini denetler ve sorun basina bir UYARI basar.
    // Sorun sayisini dondurur. Inspector'da kayit defterinin sag-tik menusunden de calistirilabilir.
    [ContextMenu("Öğe prefab'larını doğrula")]
    public int ValidateItemPrefabs()
    {
        if (ingredients == null)
            return 0;

        int warnings = 0;
        var issues = new List<string>();
        foreach (var itemType in ingredients)
        {
            if (itemType == null)
                continue;

            issues.Clear();
            itemType.CollectItemPrefabIssues(issues);
            foreach (var issue in issues)
            {
                Debug.LogWarning($"[ItemRegistry] '{itemType.name}': {issue}", itemType);
                warnings++;
            }
        }

        return warnings;
    }
#endif
}
