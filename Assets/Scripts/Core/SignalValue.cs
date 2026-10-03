using UnityEngine;

// Bir kanalın tek değeri (ör. Yön/Yukarı, Sayı/3) ya da "Sipariş Bitti" jesti: çarkta gösterilen ve Kasiyer'in
// üzerinde oynatılan birim. Görsel ve SÜRE veridir (GDD 3.6.0: "kodda süre animasyon klibinden okunur, sabit
// yazılmaz"). Final animasyon geldiğinde yalnızca bu asset değişir: klip atanır, yer tutucu görsel kaldırılır.
[CreateAssetMenu(fileName = "SignalValue", menuName = "Cook No Evil/Signal Value")]
public class SignalValue : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField] private Sprite icon;

    [Header("Oynatma (GDD 3.6.0)")]
    [Tooltip("Yer tutucu işaret: sinyal oynarken Kasiyer'in üzerinde gösterilir. Collider taşımamalı. Animasyon gelince boşaltılır.")]
    [SerializeField] private GameObject visualPrefab;
    [Tooltip("Final animasyon klibi. Atanırsa süre klipten okunur.")]
    [SerializeField] private AnimationClip clip;
    [Tooltip("Klip yokken sinyalin süresi (sn). GDD üretim hedefi 1,2–1,5 sn.")]
    [SerializeField, Min(0.05f)] private float durationSeconds = 1.3f;

    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public GameObject VisualPrefab => visualPrefab;
    public AnimationClip Clip => clip;

    // Sinyalin oynama süresi: klip varsa klibin uzunluğu, yoksa verideki süre.
    public float Duration => clip != null ? clip.length : durationSeconds;
}
