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
    [Tooltip("BurgerAssemblyStation'da ilk yerlestirilmesi zorunlu olan malzeme turu (ekmek).")]
    [SerializeField] private bool isBread;

    public int Id => id;
    public string LocalizationKey => localizationKey;
    public Sprite Icon => icon;
    public GameObject VisualPrefab => visualPrefab;
    public bool IsBread => isBread;
}
