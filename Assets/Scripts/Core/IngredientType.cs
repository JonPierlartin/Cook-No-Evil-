using UnityEngine;

// Malzeme turlerini tanimlayan veri modeli. Int sayac degil SO tabanli: ileride
// birden fazla hamburger/malzeme turu eklenecegi icin (henuz GDD'de tanimli
// degil) yapinin bastan genisletilebilir olmasi gerekiyor.
[CreateAssetMenu(fileName = "IngredientType", menuName = "Cook No Evil/Ingredient Type")]
public class IngredientType : ScriptableObject
{
    [Tooltip("PlayerInventory/NetworkList gibi ag uzerinden senkronize edilen alanlarda kullanilan kararli kimlik.")]
    [SerializeField] private int id;
    [SerializeField] private string localizationKey;
    [SerializeField] private Sprite icon;
    [Tooltip("BurgerAssemblyStation'da ilk yerlestirilmesi zorunlu olan malzeme turu (ekmek).")]
    [SerializeField] private bool isBread;

    public int Id => id;
    public string LocalizationKey => localizationKey;
    public Sprite Icon => icon;
    public bool IsBread => isBread;
}
