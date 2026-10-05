using UnityEngine;

// Bir kanalın tek değeri (ör. Yön/Yukarı, Sayı/3) ya da "Sipariş Bitti" jesti: çarkta gösterilen ve Kasiyer'in
// üzerinde oynatılan birim. Görsel ve SÜRE veridir (GDD 3.6.0: "kodda süre animasyon klibinden okunur, sabit
// yazılmaz"). Final animasyon geldiğinde yalnızca bu asset değişir: klip atanır, yer tutucu görsel kaldırılır.
[CreateAssetMenu(fileName = "SignalValue", menuName = "Cook No Evil/Signal Value")]
public class SignalValue : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField] private Sprite icon;

    [Header("Çark")]
    [Tooltip("İşaretliyse bu değer çarkta sabit bir açıda durur (yön değerleri: Sağ sağda, Aşağı aşağıda). " +
        "Bir kattaki TÜM seçenekler işaretliyse açılar kullanılır; değilse seçenekler eşit aralıkla dizilir.")]
    [SerializeField] private bool useWheelAngle;
    [Tooltip("Derece: 0 = sağ, 90 = yukarı, 180 = sol, 270 = aşağı.")]
    [SerializeField, Range(0f, 360f)] private float wheelAngle;

    [Header("Oynatma (GDD 3.6.0)")]
    [Tooltip("Yer tutucu işaret: sinyal oynarken Kasiyer'in üzerinde gösterilir. Collider taşımamalı. Animasyon gelince boşaltılır.")]
    [SerializeField] private GameObject visualPrefab;
    [Tooltip("Artist'in el animasyonu klibi. Atanırsa sinyal bununla oynar ve süre klipten okunur.")]
    [SerializeField] private AnimationClip clip;
    [Tooltip("Klibin oynatma hızı (1 = olduğu gibi). Süre = klip uzunluğu / hız.")]
    [SerializeField, Min(0.1f)] private float clipSpeed = 1f;
    [Tooltip("Klip sağ el için yapılmıştır; işaretliyse aynalanıp SOL elle oynatılır (ör. Sol klibinden Sağ).")]
    [SerializeField] private bool clipMirrored;
    [Tooltip("Klip yoksa oynayan, kodla üretilen el animasyonu. O da yoksa yer tutucu işaret gösterilir.")]
    [SerializeField] private ProceduralHandAnimation proceduralAnimation;
    [Tooltip("Klip yokken sinyalin süresi (sn). GDD üretim hedefi 1,2–1,5 sn.")]
    [SerializeField, Min(0.05f)] private float durationSeconds = 1.3f;

    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public bool UseWheelAngle => useWheelAngle;
    public float WheelAngle => wheelAngle;
    public GameObject VisualPrefab => visualPrefab;
    public AnimationClip Clip => clip;
    public bool ClipMirrored => clipMirrored;
    public ProceduralHandAnimation ProceduralAnimation => proceduralAnimation;

    // Sinyalin oynama süresi: klip varsa klibin (hıza bölünmüş) uzunluğu, yoksa verideki süre.
    public float Duration => clip != null ? clip.length / clipSpeed : durationSeconds;
}
