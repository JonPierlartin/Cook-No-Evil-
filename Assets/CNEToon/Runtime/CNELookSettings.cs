using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// CNE Toon görünümünün ayarları (Resources/CNELookSettings). Değerler burada durur, koda gömülmez.
// Profiller "CNE → Post → Create Profiles" menüsüyle üretilir ve sonradan elle düzenlenebilir.
public class CNELookSettings : ScriptableObject
{
    private const string ResourceName = "CNELookSettings";

    [Header("Post-process (görünüm açıkken, kör olmayan oyuncu kameralarında)")]
    [Tooltip("Görünüm açıkken uygulanan global Volume profili (CNE_Global ya da karşılaştırma için CNE_Global_Neutral).")]
    public VolumeProfile globalProfile;
    [Tooltip("Global Volume'un önceliği. Sahnedeki diğer Volume'lardan büyük olmalı ki onları ezsin.")]
    public float globalPriority = 100f;
    public AntialiasingMode antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
    public AntialiasingQuality antialiasingQuality = AntialiasingQuality.High;

    [Header("Debug (yalnızca Editor ve Development Build)")]
    [Tooltip("Gri tonlama testi profili (Saturation -100).")]
    public VolumeProfile grayscaleProfile;
    [Tooltip("Debug Volume'unun önceliği; global profilden büyük olmalı.")]
    public float debugPriority = 1000f;
    public Key grayscaleKey = Key.F9;
    public Key outlineKey = Key.F10;

    [Header("Oyunda uygulama (görünüm seçiliyken, çalışırken; materyal dosyaları ve prefab'lar değişmez)")]
    [Tooltip("Ayarlar menüsündeki GÖRÜNÜM satırında bu görünümün adı.")]
    public string optionLabel = "CNE TOON";
    [Tooltip("Görünümü hangi roller görür. Kör rol (Şef) eklenmez: onun görüşü ayrı bir render'dır.")]
    public PlayerRole[] roles = { PlayerRole.Komi, PlayerRole.Kasiyer };
    [Tooltip("Bu shader'ı kullanan opak materyaller CNE/Toon kopyalarıyla değiştirilir.")]
    public string sourceShaderName = "Universal Render Pipeline/Lit";
    [Tooltip("Çevrilen materyallerin ayar şablonu (ışık, gölge...). Doku ve renk asıl materyalden alınır.")]
    public Material surfaceTemplate;
    [Tooltip("Karakter ve müşteri yüzeyleri için şablon (kenar ışığı açık).")]
    public Material characterTemplate;
    [Tooltip("Yeni doğan nesnelerin taranma aralığı (sn).")]
    [Min(0.05f)] public float scanInterval = 0.5f;

    [Header("Outline katmanı (çalışırken atanır; diğer bitler korunur)")]
    public RenderingLayerMask outlineLayer;
    [Tooltip("Oyuncu karakterleri ve müşteriler çizgi alsın.")]
    public bool outlineCharacters = true;
    [Tooltip("Taşınabilir öğeler (elde ve yuvada) çizgi alsın.")]
    public bool outlineItems = true;
    [Tooltip("Etkileşilen nesneler (kap, ızgara, tezgah, çöp, yuva...) çizgi alsın.")]
    public bool outlineInteractables = true;

    [Header("Işık (görünüm açıkken; kapanınca eski değerler geri gelir)")]
    [Tooltip("Kapalıysa sahnenin ışığına dokunulmaz. Açık ortam ışığı ve güçlü güneş bantları yok eder.")]
    public bool overrideLighting = true;
    public Color sunColor = new Color32(0xFF, 0xE9, 0xC7, 0xFF);
    [Min(0f)] public float sunIntensity = 1f;
    [Tooltip("Düz ortam ışığı rengi (koyu tutulur).")]
    public Color ambientColor = new(0.10f, 0.13f, 0.18f);

    [Header("Test sahnesi")]
    [Tooltip("Ana menüdeki test sahnesi düğmesinin yazısı.")]
    public string lookdevButtonLabel = "TOON TEST";

    private static CNELookSettings _instance;

    // Ayar varlığı yoksa null döner; çağıran taraf görünümü uygulamaz (hata fırlatmaz).
    public static CNELookSettings Load()
    {
        if (_instance == null)
            _instance = Resources.Load<CNELookSettings>(ResourceName);

        return _instance;
    }
}
