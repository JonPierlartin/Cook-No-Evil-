using UnityEngine;

// Kasiyer'in emote carkindan sectigi iletisim token'lari. Belirli bir tarif/siparise
// bagli DEGIL (Order/Recipe sistemi kapsam disi birakildi) — sadece soyut bir
// isaret/ikon seti. Secim, Kasiyer'in karakteri uzerinde HERKESIN gorebilecegi kisa
// bir gorsel tepkiyi (renk parlamasi + egilme/ziplama) tetikler — reactionColor bu
// tepkinin rengini, cark/hotbar ikonuyla ayni paleti kullanacak sekilde belirler.
[CreateAssetMenu(fileName = "EmoteDefinition", menuName = "Cook No Evil/Emote Definition")]
public class EmoteDefinition : ScriptableObject
{
    [SerializeField] private string localizationKey;
    [SerializeField] private Sprite icon;
    [SerializeField] private Color reactionColor = Color.white;
    // Cark merkez panelinde (bkz. EmoteWheelUI) gosterilen ham metinler — henuz
    // Localization tablosuna baglanmadi, placeholder/duz metin (gercek metin/tasarim
    // sonra netlesecek, kullanici istegi).
    [SerializeField] private string displayName;
    [SerializeField, TextArea] private string description;

    public string LocalizationKey => localizationKey;
    public Sprite Icon => icon;
    public Color ReactionColor => reactionColor;
    public string DisplayName => displayName;
    public string Description => description;
}
