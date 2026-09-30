using UnityEngine;

// Dünyadaki bir oyun efekti kaynağının menzili, dinleyicinin duyuşuna göre (GDD 4.1.3). Yerel dinleyici
// sağırsa (DeafHearing) kaynak yalnızca çok dar bir yarıçapta ve kısık duyulur; değilse Inspector'daki
// kendi menzili ve seviyesi geçerlidir. Makine/istasyon efektlerinin hepsine konur.
// Kaynak Linear rolloff kullanmalı: Logarithmic eğri maxDistance'ta sıfıra inmez.
[RequireComponent(typeof(AudioSource))]
public class RoleAwareAudioRange : MonoBehaviour
{
    private AudioSource _source;
    private float _defaultMaxDistance;
    private float _defaultVolume;
    private bool _wasDeaf;

    private void Awake()
    {
        _source = GetComponent<AudioSource>();
        _defaultMaxDistance = _source.maxDistance;
        _defaultVolume = _source.volume;

        if (_source.rolloffMode != AudioRolloffMode.Linear)
            Debug.LogWarning($"[RoleAwareAudioRange] '{name}': rolloff Linear değil; sağır dinleyicide menzil sınırı tam kesmez.", this);
    }

    private void Update()
    {
        bool deaf = DeafHearing.IsLocalListenerDeaf;
        if (deaf == _wasDeaf)
            return;

        _wasDeaf = deaf;
        _source.maxDistance = deaf ? Mathf.Min(DeafHearing.EffectRadius, _defaultMaxDistance) : _defaultMaxDistance;
        _source.volume = deaf ? _defaultVolume * DeafHearing.EffectVolumeScale : _defaultVolume;
    }
}
