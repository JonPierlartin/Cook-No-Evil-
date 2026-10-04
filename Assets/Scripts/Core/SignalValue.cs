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
    [Tooltip("Karakterin eliyle işaret edeceği yön (karakterin yerel uzayı: yukarı (0,1,0), kendi sağı (1,0,0)). " +
        "Sıfırdan farklıysa sinyal karakterin jestiyle oynar; sıfırsa aşağıdaki yer tutucu işaret gösterilir.")]
    [SerializeField] private Vector3 gestureDirection;
    [Tooltip("Karakterin parmaklarıyla göstereceği sayı (0 = yok). Sıfırdan büyükse sinyal o kadar parmak açılarak oynar.")]
    [SerializeField, Min(0)] private int gestureCount;
    [Tooltip("Yer tutucu işaret: sinyal oynarken Kasiyer'in üzerinde gösterilir. Collider taşımamalı. Animasyon gelince boşaltılır.")]
    [SerializeField] private GameObject visualPrefab;
    [Tooltip("Final animasyon klibi. Atanırsa süre klipten okunur.")]
    [SerializeField] private AnimationClip clip;
    [Tooltip("Klip yokken sinyalin süresi (sn). GDD üretim hedefi 1,2–1,5 sn.")]
    [SerializeField, Min(0.05f)] private float durationSeconds = 1.3f;

    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public bool UseWheelAngle => useWheelAngle;
    public float WheelAngle => wheelAngle;
    public GameObject VisualPrefab => visualPrefab;
    public Vector3 GestureDirection => gestureDirection;
    public int GestureCount => gestureCount;
    public AnimationClip Clip => clip;

    // Sinyalin oynama süresi: klip varsa klibin uzunluğu, yoksa verideki süre.
    public float Duration => clip != null ? clip.length : durationSeconds;
}
