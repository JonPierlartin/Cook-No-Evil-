using Unity.Netcode;
using UnityEngine;

// Sinyali gönderen oyuncunun üzerindeki işaret (GDD 3.6.0; final animasyon gelene kadar YER TUTUCU). Yayın herkese
// gider (kasıtlı), her istemcide yalnızca göndereninki olan oyuncu kopyası işareti gösterir. İşaretin kendisi ve
// süresi SignalValue'dan gelir — burada sabit süre veya sabit görsel yoktur; animasyon geldiğinde yalnızca veri
// değişir. Tamamen yerel sunum: karar (kim, ne zaman, ne kadar) sunucudadır (EmoteSystem).
public class PlayerSignalDisplay : NetworkBehaviour
{
    [Tooltip("İşaretin gösterileceği nokta (gövdenin önü; Komi pencereden karşıdan okur).")]
    [SerializeField] private Transform anchor;

    [Tooltip("Karakter modeli (jestli sinyaller modelin eliyle oynar).")]
    [SerializeField] private PlayerCharacterVisual character;

    private GameObject _instance;
    private float _hideAt;

    public override void OnNetworkSpawn()
    {
        if (EmoteSystem.Instance == null)
            return;

        EmoteSystem.Instance.OnSignalStarted += HandleSignalStarted;
        EmoteSystem.Instance.OnPlaybackCancelled += HandlePlaybackCancelled;
    }

    public override void OnNetworkDespawn()
    {
        if (EmoteSystem.Instance != null)
        {
            EmoteSystem.Instance.OnSignalStarted -= HandleSignalStarted;
            EmoteSystem.Instance.OnPlaybackCancelled -= HandlePlaybackCancelled;
        }

        Clear();
    }

    // Etkileşim sinyali kesti (GDD 3.6.0): işaret süresini beklemeden kalkar.
    private void HandlePlaybackCancelled(ulong clientId)
    {
        if (OwnerClientId != clientId)
            return;

        Clear();
        if (character != null && character.Animator != null)
            character.Animator.CancelGesture();
    }

    private void HandleSignalStarted(ulong senderId, SignalValue signal)
    {
        if (OwnerClientId != senderId)
            return;

        Clear();

        // Jest verisi olan sinyal (yön) karakterin eliyle oynar; olmayan yer tutucu işaretle gösterilir.
        if (signal.GestureDirection != Vector3.zero && character != null && character.Animator != null)
        {
            character.Animator.PlayGesture(signal.GestureDirection, signal.Duration);
            return;
        }

        if (signal.VisualPrefab == null || anchor == null)
            return;

        _instance = Instantiate(signal.VisualPrefab, anchor);
        _hideAt = Time.unscaledTime + signal.Duration;
    }

    private void Update()
    {
        if (_instance != null && Time.unscaledTime >= _hideAt)
            Clear();
    }

    private void Clear()
    {
        if (_instance != null)
            Destroy(_instance);

        _instance = null;
    }
}
