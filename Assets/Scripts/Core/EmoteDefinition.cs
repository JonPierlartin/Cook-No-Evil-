using UnityEngine;

// Genel emote carkindan (E) secilen tepkiler; siparis bilgisi TASIMAZ (o, sinyal carkinin isidir —
// bkz. SignalValue). Secim, secen oyuncunun karakteri uzerinde HERKESIN gorebilecegi kisa
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
    // Tepkinin süresi (GDD 3.6.0: hedef 1,2–1,5 sn). "Bitmeden yenisi başlatılamaz" kuralı ve görsel bunu okur;
    // final animasyon geldiğinde klip uzunluğuyla değiştirilir.
    [SerializeField, Min(0.1f)] private float durationSeconds = 1.3f;
    [Tooltip("Emote'un kodla üretilen el animasyonu. Boşsa yer tutucu tepki (zıplama) oynar.")]
    [SerializeField] private ProceduralHandAnimation proceduralAnimation;

    public string LocalizationKey => localizationKey;
    public Sprite Icon => icon;
    public Color ReactionColor => reactionColor;
    public string DisplayName => displayName;
    public string Description => description;
    public float Duration => durationSeconds;
    public ProceduralHandAnimation ProceduralAnimation => proceduralAnimation;
}
