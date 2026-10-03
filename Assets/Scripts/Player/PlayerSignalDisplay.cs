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

    private GameObject _instance;
    private float _hideAt;

    public override void OnNetworkSpawn()
    {
        if (EmoteSystem.Instance != null)
            EmoteSystem.Instance.OnSignalStarted += HandleSignalStarted;
    }

    public override void OnNetworkDespawn()
    {
        if (EmoteSystem.Instance != null)
            EmoteSystem.Instance.OnSignalStarted -= HandleSignalStarted;

        Clear();
    }

    private void HandleSignalStarted(ulong senderId, SignalValue signal)
    {
        if (OwnerClientId != senderId)
            return;

        Clear();
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
