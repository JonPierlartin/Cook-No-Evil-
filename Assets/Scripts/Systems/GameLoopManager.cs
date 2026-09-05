using Unity.Netcode;
using UnityEngine;

// Round State mimarisinin TEK OTORITESI (kullanici istegiyle konsolide edildi —
// bkz. CLAUDE.md). Eskiden RoleManager.IsRoundActive (round acik/kapali) ve
// GameLoopManager.IsGamePaused (disconnect/reconnect kilidi) birbirinden HABERSIZ,
// bagimsiz iki NetworkVariable'di — Bilesen 2 (siparis timer'i, 3 hak kurali) bunun
// uzerine insa edilseydi, disconnect sirasinda timer'in islemeye devam edip donen
// oyuncuya haksiz "1 Hata" yazdirmasi VEYA pause sirasinda round bitisinin
// tetiklenebilmesi gibi hatalara acikti. Artik CurrentRoundState (Lobby/RoundActive/
// RoundEnded) TEK NetworkVariable, IsPaused ise SADECE RoundActive iken anlamli olan
// bir overlay bayrak (Lobby/RoundEnded'da IsGamePaused her zaman false dondurur, bkz.
// asagidaki computed property). Bilesen 2 (5 dk sayac, 3 strike, skor hedefi) bu
// sinifin uzerine insa edilecek, TASINMASI gerekmeyecek sekilde onceden acildi.
// BILESEN 2 ICIN NOT: RoundEnded'a gecis tetikleyecek gelecekteki kod, IsPaused=true
// iken bu gecisi YAPMAMALI (kullanici istegi — "pause sirasinda round bitisi
// tetiklenemesin").
[RequireComponent(typeof(NetworkObject))]
public class GameLoopManager : NetworkBehaviour
{
    public static GameLoopManager Instance { get; private set; }

    public readonly NetworkVariable<RoundState> CurrentRoundState =
        new(RoundState.Lobby, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Sadece RoundActive iken anlamli bir overlay — Lobby/RoundEnded'da _isPaused.Value
    // ne olursa olsun IsGamePaused (asagida) hep false doner, cagiran kod bunu ayrica
    // kontrol etmek ZORUNDA DEGIL.
    private readonly NetworkVariable<bool> _isPaused =
        new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public bool IsRoundActive => CurrentRoundState.Value == RoundState.RoundActive;

    // Tum eski "GameLoopManager.Instance.IsGamePaused.Value" cagri yerleri artik bu
    // computed property'i (artik bir NetworkVariable DEGIL, .Value EKLENMEMELI) okuyor.
    public bool IsGamePaused => IsRoundActive && _isPaused.Value;

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
        {
            // GameSystems kalici bir obje oldugu icin (bkz. RoleManager'daki ayni desen
            // notu) bir onceki hosting oturumundan kalma durum burada acikca sifirlanir.
            CurrentRoundState.Value = RoundState.Lobby;
            _isPaused.Value = false;
        }
    }

    // Host'un "Oyunu Baslat" butonuyla cagirdigi, server-authoritative round baslatma —
    // eskiden RoleManager.StartRound() idi, tek otoriteye konsolide edildi. Rol sayisi
    // kontrolu icin RoleManager.AssignedRoleCount'a (public, sadece okuma) bakiyor;
    // rol ATAMA mantigina KARISMIYOR, o RoleManager'da kaliyor.
    public bool StartRound()
    {
        if (!IsServer)
            return false;

        if (CurrentRoundState.Value == RoundState.RoundActive)
            return true;

        if (RoleManager.Instance == null || RoleManager.Instance.AssignedRoleCount < RoleManager.MaxPlayers)
        {
            int count = RoleManager.Instance != null ? RoleManager.Instance.AssignedRoleCount : 0;
            Debug.LogWarning($"[GameLoopManager] Round baslatilamiyor, {count}/{RoleManager.MaxPlayers} oyuncu var.");
            return false;
        }

        CurrentRoundState.Value = RoundState.RoundActive;
        _isPaused.Value = false;
        return true;
    }

    // Lobby/RoundEnded'da pause anlamsiz/no-op (kullanici istegi) — RoundActive
    // disinda hicbir sey yapmiyor.
    public void ServerPauseForDisconnect()
    {
        if (!IsServer || !IsRoundActive)
            return;

        _isPaused.Value = true;
    }

    public void ServerResumeAfterReconnect()
    {
        if (!IsServer || !IsRoundActive)
            return;

        _isPaused.Value = false;
    }
}
