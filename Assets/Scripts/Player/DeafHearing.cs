using UnityEngine;

// Sağır rolün (Komi) duyuşu — "sağır mı" kuralının TEK yeri. Yerel oyuncunun kamerasındaki AudioListener'da
// durur (kamera yalnızca sahip için açıktır, bkz. PlayerController). Sağır rol + round aktifken:
//  - Sesli sohbet: hiç duyulmaz (VoIPController paketi çözmez; GDD 11.2, Faz 0 kararı).
//  - Oyun efektleri (GDD 4.1.3): çok dar bir yarıçapta, boğuk ve kısık duyulur. Boğukluk dinleyicideki
//    low-pass ile (duyulan her şeye), dar yarıçap ve kısıklık efekt kaynağında (RoleAwareAudioRange).
// Hiçbiri ana ses seviyesine dayanmaz (K3): ayar menüsü AudioListener.volume'u değiştirse de sağırlık sürer.
[RequireComponent(typeof(AudioListener))]
[RequireComponent(typeof(AudioLowPassFilter))]
public class DeafHearing : MonoBehaviour
{
    [Tooltip("Sağır rol (GDD 4.1: Komi).")]
    [SerializeField] private PlayerRole deafRole = PlayerRole.Komi;
    [Tooltip("Sağır dinleyicide duyulan her şeye uygulanan low-pass kesim frekansı (Hz). Playtest parametresi.")]
    [SerializeField, Min(10f)] private float muffleCutoffHz = 500f;
    [Tooltip("Sağır dinleyicinin oyun efektlerini duyabildiği en uzak mesafe (m). GDD 4.1.3: 'çok dar'. Playtest parametresi.")]
    [SerializeField, Min(0.1f)] private float effectRadius = 2f;
    [Tooltip("Sağır dinleyicide oyun efektlerinin ses çarpanı (GDD 4.1.3: 'kısık'). Playtest parametresi.")]
    [SerializeField, Range(0f, 1f)] private float effectVolumeScale = 0.3f;

    // Etkin (kamerası açık) dinleyiciler. Kamera yalnızca sahipte açık kalır, ama başka bir oyuncunun karakteri
    // doğarken onun kamerası bir an etkinleşip kapanır (PlayerController.ApplyNonOwnerState). Tek bir statik
    // "_local" alanı tutulsaydı o geçici kamera kaydı ezip kapanırken silerdi ve yerel dinleyici hiç sağır
    // olmazdı (30 Eyl testi: Komi hâlâ Şef'i ve cızırtıyı duyuyordu). Liste bunu önler.
    private static readonly System.Collections.Generic.List<DeafHearing> Enabled = new();
    private AudioLowPassFilter _filter;

    private static DeafHearing Local => Enabled.Count > 0 ? Enabled[Enabled.Count - 1] : null;

    // Yerel dinleyici şu an sağır mı. Dinleyici yoksa (lobi, kamera kapalı) sağır değildir.
    public static bool IsLocalListenerDeaf => Local != null && Local.IsDeafNow;

    public static float EffectRadius => Local != null ? Local.effectRadius : 0f;

    public static float EffectVolumeScale => Local != null ? Local.effectVolumeScale : 1f;

    private bool IsDeafNow =>
        GameLoopManager.Instance != null && GameLoopManager.Instance.IsRoundActive &&
        RoleManager.Instance != null && RoleManager.Instance.LocalRole == deafRole;

    private void Awake()
    {
        _filter = GetComponent<AudioLowPassFilter>();
        _filter.enabled = false;
    }

    private void OnEnable()
    {
        if (!Enabled.Contains(this))
            Enabled.Add(this);
    }

    private void OnDisable()
    {
        Enabled.Remove(this);
        _filter.enabled = false;
    }

    private void Update()
    {
        bool deaf = IsDeafNow;
        if (deaf)
            _filter.cutoffFrequency = muffleCutoffHz;

        if (_filter.enabled != deaf)
            _filter.enabled = deaf;
    }
}
