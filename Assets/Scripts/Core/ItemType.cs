using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Envanterde tasinabilen her ogenin turunu tanimlayan veri modeli: malzeme, bardak, dondurma kabi,
// kese kagidi, hamburger... Int sayac degil SO tabanli: yeni tur eklemek yalnizca yeni bir asset'tir.
[CreateAssetMenu(fileName = "ItemType", menuName = "Cook No Evil/Item Type")]
public class ItemType : ScriptableObject
{
    [Tooltip("PlayerInventory/NetworkList gibi ag uzerinden senkronize edilen alanlarda kullanilan kararli kimlik.")]
    [SerializeField] private int id;
    [SerializeField] private string localizationKey;
    [SerializeField] private Sprite icon;
    [Tooltip("Hem elde tutulurken (GDD 4.1) hem yerlestirme onizlemesinde (GDD 4.1.2) kullanilan TEK gorsel. Collider tasimamali. Bos birakilirsa hicbir sey gosterilmez.")]
    [SerializeField] private GameObject visualPrefab;
    [Tooltip("Bu ogeyi dunyada temsil eden AG KABUGU: kokte NetworkObject + Item (type = bu tur), cocuk olarak visualPrefab. Sunucu bunu spawn eder; NetworkManager'in prefab listesinde kayitli olmali. Collider ve ic ice NetworkObject tasimamali. Bos birakilirsa bu tur spawn edilemez.")]
    [SerializeField] private GameObject itemPrefab;
    [Tooltip("Hamburger birlestirmedeki kategori (GDD 6.7.3): siralama kurali buna dayanir. Birlestirmeye girmeyen turler Yok.")]
    [SerializeField] private ItemCategory category;
    [Tooltip("Yalnizca itemPrefab'inda ServerProgress olan turler icin anlamlidir: ServerProgress." +
        "Profile ile AYNI SIRADA, ayni uzunlukta faz basina renk (GDD 5.2.1: Cig/Pismis/Yanmis icin " +
        "yer tutucu renkler). ServerProgress tasimayan turlerde bos birakilir, kullanilmaz.")]
    [SerializeField] private Color[] phaseColors;

    public int Id => id;
    public string LocalizationKey => localizationKey;
    public Sprite Icon => icon;
    public GameObject VisualPrefab => visualPrefab;
    public GameObject ItemPrefab => itemPrefab;
    public ItemCategory Category => category;
    public Color[] PhaseColors => phaseColors;

#if UNITY_EDITOR
    // Editor dogrulamasi (bkz. ItemRegistry): itemPrefab doluysa ag kabugu kurallarini denetler, her
    // sorunu anlasilir bir metinle 'issues'a ekler. Bos itemPrefab hata degildir (tur henuz spawn edilmez).
    public void CollectItemPrefabIssues(List<string> issues)
    {
        if (itemPrefab == null)
            return;

        if (itemPrefab.GetComponent<NetworkObject>() == null)
            issues.Add($"itemPrefab '{itemPrefab.name}' kokunde NetworkObject yok.");

        var item = itemPrefab.GetComponent<Item>();
        if (item == null)
            issues.Add($"itemPrefab '{itemPrefab.name}' kokunde Item bileşeni yok.");
        else if (item.Type != this)
            issues.Add($"itemPrefab '{itemPrefab.name}' kokundaki Item.type '{(item.Type != null ? item.Type.name : "bos")}' — bu tur ('{name}') ile ayni olmali.");

        foreach (var networkObject in itemPrefab.GetComponentsInChildren<NetworkObject>(true))
        {
            if (networkObject.gameObject != itemPrefab)
                issues.Add($"itemPrefab '{itemPrefab.name}' icinde ic ice NetworkObject var: '{networkObject.name}'.");
        }

        foreach (var collider in itemPrefab.GetComponentsInChildren<Collider>(true))
            issues.Add($"itemPrefab '{itemPrefab.name}' hiyerarsisinde collider var: '{collider.name}' ({collider.GetType().Name}). Yuvaya tiklanir, ogeye degil.");

        if (!IsInAnyNetworkPrefabsList(itemPrefab))
            issues.Add($"itemPrefab '{itemPrefab.name}' hicbir NetworkPrefabsList'te (orn. DefaultNetworkPrefabs) kayitli degil; istemcide spawn olmaz.");
    }

    private static bool IsInAnyNetworkPrefabsList(GameObject prefab)
    {
        foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:NetworkPrefabsList"))
        {
            var list = UnityEditor.AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
            if (list != null && list.Contains(prefab))
                return true;
        }

        return false;
    }
#endif
}
