using UnityEngine;

// DENEME — stilize görünümün ayarları (Assets/StylizedTest/Resources/StylizedLookSettings.asset). Görünümün tamamı
// bu klasördedir ve çalışma zamanında uygulanır; hiçbir sahne, materyal ya da proje ayarı kalıcı olarak değişmez.
// Denemeyi kaldırmak için Assets/StylizedTest klasörünü silmek yeterlidir.
[CreateAssetMenu(fileName = "StylizedLookSettings", menuName = "Cook No Evil/Stilize Deneme/Ayarlar")]
public class StylizedLookSettings : ScriptableObject
{
    [Header("Kim görür")]
    [Tooltip("Stilize görünüm yalnızca bu rollerdeki yerel oyuncuya uygulanır (Şef'in kör görüşü ayrı bir render'dır).")]
    public PlayerRole[] roles = { PlayerRole.Komi, PlayerRole.Kasiyer };

    [Header("Toon shader")]
    public Shader toonShader;
    public Texture2D ramp;
    [Tooltip("Bu shader'ı kullanan opak materyaller toon'a çevrilir.")]
    public string sourceShaderName = "Universal Render Pipeline/Lit";
    public Color shadowColor = new(0.45f, 0.40f, 0.62f);
    [Range(0f, 2f)] public float ambientStrength = 0.6f;
    public Color rimColor = new(1f, 0.93f, 0.78f);
    [Range(0f, 2f)] public float rimIntensity = 0.35f;
    [Range(0.5f, 8f)] public float rimPower = 3f;

    [Header("Kontur çizgileri (geometrinin kenarlarına ince koyu çizgi)")]
    public bool outlineEnabled = true;
    public Shader outlineShader;
    public Color outlineColor = new(0.05f, 0.04f, 0.06f, 1f);
    [Tooltip("Çizgi kalınlığı (piksel).")]
    [Min(1f)] public float outlineThickness = 2f;
    [Tooltip("Derinlik farkı eşiği (göz derinliğinin oranı). Küçük = daha çok çizgi.")]
    public float outlineDepthThreshold = 0.05f;
    [Tooltip("Yüzey yönü farkı eşiği. Küçük = yumuşak kıvrımlar da çizilir.")]
    public float outlineNormalThreshold = 0.4f;

    [Header("Ortam (görünüm açıkken; kapanınca eski değerler geri gelir)")]
    public bool overrideEnvironment = true;
    public Color skyColor = new(0.62f, 0.74f, 0.95f);
    public Color equatorColor = new(0.95f, 0.80f, 0.66f);
    public Color groundColor = new(0.42f, 0.36f, 0.44f);
    public Color fogColor = new(0.86f, 0.74f, 0.80f);
    [Tooltip("Sisin başladığı ve tamamen kapattığı mesafe (m).")]
    public float fogStart = 7f;
    public float fogEnd = 45f;
    [Tooltip("Ana ışığın rengi bununla çarpılır (sıcak ton).")]
    public Color sunTint = new(1f, 0.93f, 0.80f);

    [Header("Tarama")]
    [Tooltip("Yeni doğan nesneler (öğeler, müşteriler, karakterler) bu aralıkla taranıp çevrilir (sn).")]
    [Min(0.1f)] public float scanInterval = 0.5f;
}
