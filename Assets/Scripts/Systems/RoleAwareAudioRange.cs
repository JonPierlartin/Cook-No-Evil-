using UnityEngine;

// Dünyadaki bir oyun efekti kaynağının duyuluşu, dinleyiciye göre:
//  - Sağırlık (GDD 4.1.3): yerel dinleyici sağırsa (DeafHearing) kaynak yalnızca çok dar bir yarıçapta ve kısık
//    duyulur; değilse Inspector'daki kendi menzili ve seviyesi geçerlidir.
//  - Akustik (GDD 10.4, K4): ses duvardan ve kapalı kapıdan geçmez, pencerelerden dolaşır (AcousticSpace). Unity'nin
//    kendi zayıflaması düz mesafeyle çalıştığı için, seviye "sesin gerçekten kat ettiği yol"a göre düzeltilir.
// Makine / istasyon / adım efektlerinin hepsine konur. Kaynak Linear rolloff kullanmalı: Logarithmic eğri
// maxDistance'ta sıfıra inmez ve aşağıdaki düzeltme doğrusal eğriye göre hesaplanır.
[RequireComponent(typeof(AudioSource))]
public class RoleAwareAudioRange : MonoBehaviour
{
    // Akustik yol her karede değil, bu aralıkla yeniden hesaplanır (kaynak ve dinleyici yavaş hareket eder).
    private const float AcousticRefreshInterval = 0.1f;

    private AudioSource _source;
    private float _defaultMaxDistance;
    private float _defaultVolume;
    private float _nextAcousticRefresh;
    private float _acousticFactor = 1f;

    private void Awake()
    {
        _source = GetComponent<AudioSource>();
        _defaultMaxDistance = _source.maxDistance;
        _defaultVolume = _source.volume;

        if (_source.rolloffMode != AudioRolloffMode.Linear)
            Debug.LogWarning($"[RoleAwareAudioRange] '{name}': rolloff Linear değil; menzil sınırı ve akustik düzeltme tam çalışmaz.", this);
    }

    private void Update()
    {
        bool deaf = DeafHearing.IsLocalListenerDeaf;
        float maxDistance = deaf ? Mathf.Min(DeafHearing.EffectRadius, _defaultMaxDistance) : _defaultMaxDistance;

        if (Time.unscaledTime >= _nextAcousticRefresh)
        {
            _nextAcousticRefresh = Time.unscaledTime + AcousticRefreshInterval;
            _acousticFactor = ComputeAcousticFactor(maxDistance);
        }

        _source.maxDistance = maxDistance;
        _source.volume = _defaultVolume * (deaf ? DeafHearing.EffectVolumeScale : 1f) * _acousticFactor;
    }

    // Unity kaynağı düz mesafeye göre zayıflatır; olması gereken, ses yolunun etkin mesafesine göre zayıflamadır.
    // Çarpan = olması gereken / Unity'nin uyguladığı. Aynı odada 1; yol yoksa 0.
    private float ComputeAcousticFactor(float maxDistance)
    {
        var space = AcousticSpace.Instance;
        var listener = AcousticSpace.Listener;
        if (space == null || listener == null)
            return 1f;

        float direct = Vector3.Distance(transform.position, listener.position);
        float effective = space.EffectiveDistance(transform.position, listener.position);
        if (effective <= direct + 0.01f)
            return 1f;

        float applied = LinearRolloff(direct, maxDistance);
        return applied <= 0f ? 0f : Mathf.Clamp01(LinearRolloff(effective, maxDistance) / applied);
    }

    private float LinearRolloff(float distance, float maxDistance)
    {
        float min = _source.minDistance;
        if (float.IsInfinity(distance))
            return 0f;

        return Mathf.Clamp01(1f - (distance - min) / Mathf.Max(maxDistance - min, 0.01f));
    }
}
