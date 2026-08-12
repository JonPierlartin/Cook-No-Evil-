using Unity.Netcode;
using UnityEngine;

// Bilesen 2'nin (henuz kurulmadi) round/strike/win-fail sorumluluklarinin gelecekte
// yasayacagi yer — simdilik BILINCLI olarak minimal tutuluyor: sadece "oyun durduruldu"
// bayragi (round sirasinda bir oyuncu kopunca RoleManager tarafindan set/clear edilir,
// bkz. RoleManager.HandleClientDisconnectedOnServer/HandleClientConnected). Bekleme
// suresi SINIRSIZ — otomatik strike/timeout GDD'de tanimli degil, Bilesen 2 tam
// kurulunca (5 dk sayac, 3 strike, skor hedefi) bu sinif buyuyecek, TASINMASI
// gerekmeyecek sekilde onceden burada acildi.
[RequireComponent(typeof(NetworkObject))]
public class GameLoopManager : NetworkBehaviour
{
    public static GameLoopManager Instance { get; private set; }

    public readonly NetworkVariable<bool> IsGamePaused =
        new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            IsGamePaused.Value = false;
    }

    public void ServerPauseForDisconnect()
    {
        if (!IsServer)
            return;

        IsGamePaused.Value = true;
    }

    public void ServerResumeAfterReconnect()
    {
        if (!IsServer)
            return;

        IsGamePaused.Value = false;
    }
}
