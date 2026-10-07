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

    [Header("Artist klibi (varsa kodla üretilen animasyonun ve yer tutucunun önüne geçer)")]
    [Tooltip("Sağ elin klibi.")]
    [SerializeField] private AnimationClip clip;
    [Tooltip("Sol elin klibi (iki elli emote). Tek elliyse boş.")]
    [SerializeField] private AnimationClip leftClip;
    [Tooltip("Klibin oynatma hızı. Süre = en uzun klibin uzunluğu / hız.")]
    [SerializeField, Min(0.1f)] private float clipSpeed = 1f;
    [Tooltip("Eller klipte buluşuyor (ovuşturma gibi): eller buluşma noktasına göre yerleşir, gövdesi geniş karakterde " +
        "de birbirine değer. Eller ayrı hareket ediyorsa kapalı.")]
    [SerializeField] private bool handsMeet;

    public string LocalizationKey => localizationKey;
    public Sprite Icon => icon;
    public Color ReactionColor => reactionColor;
    public string DisplayName => displayName;
    public string Description => description;
    // Klip varsa süre klipten gelir ("bitmeden yenisi başlatılamaz" kuralı gerçek animasyon süresini izler).
    public float Duration => HasClip
        ? Mathf.Max(clip != null ? clip.length : 0f, leftClip != null ? leftClip.length : 0f) / clipSpeed
        : durationSeconds;
    public ProceduralHandAnimation ProceduralAnimation => proceduralAnimation;
    public bool HasClip => clip != null || leftClip != null;
    public AnimationClip Clip => clip;
    public AnimationClip LeftClip => leftClip;
    public bool HandsMeet => handsMeet;
}
