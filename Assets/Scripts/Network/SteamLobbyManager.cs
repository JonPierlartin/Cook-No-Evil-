using System;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using UnityEngine;

// Lobi kurma, Steam'in kendi davet sistemi (overlay) uzerinden katilma ve host
// disconnect yonetimi. GDD: manuel lobi kodu girme ekrani YOK — davet Steam
// overlay'i ile gonderilir, kabul edilince SteamFriends.OnGameLobbyJoinRequested
// otomatik tetiklenir ve oyuncu dogrudan lobiye/host'a baglanir.
public class SteamLobbyManager : MonoBehaviour
{
    public static SteamLobbyManager Instance { get; private set; }

    public event Action OnLobbyCreated;
    public event Action OnLobbyJoined;
    public event Action<string> OnLobbyError;
    public event Action OnHostDisconnected;

    [SerializeField] private NetworkTransportManager transportManager;
    [SerializeField] private int maxLobbyMembers = 3;

    // Steam Friends listesindeki "Oyuna Davet Et" / "Katil" segmentinin calismasi icin
    // Rich Presence "connect" anahtari bu formatta yayinlanir; ayni format
    // HandleRichPresenceJoinRequested tarafindan geri okunur.
    private const string ConnectPrefix = "+connect_lobby ";

    private Lobby? _currentLobby;
    private bool _joinInProgress;

    public bool IsInLobby => _currentLobby.HasValue;
    public bool IsHost { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (transportManager == null)
            transportManager = GetComponent<NetworkTransportManager>();
    }

    private void OnEnable()
    {
        SteamMatchmaking.OnLobbyMemberLeave += HandleLobbyMemberLeave;
        SteamFriends.OnGameLobbyJoinRequested += HandleGameLobbyJoinRequested;
        SteamFriends.OnGameRichPresenceJoinRequested += HandleRichPresenceJoinRequested;
    }

    private void OnDisable()
    {
        SteamMatchmaking.OnLobbyMemberLeave -= HandleLobbyMemberLeave;
        SteamFriends.OnGameLobbyJoinRequested -= HandleGameLobbyJoinRequested;
        SteamFriends.OnGameRichPresenceJoinRequested -= HandleRichPresenceJoinRequested;
    }

    private void Start()
    {
        var networkManager = NetworkManager.Singleton;
        if (networkManager == null)
        {
            Debug.LogError("[SteamLobbyManager] NetworkManager.Singleton bulunamadi.");
            return;
        }

        networkManager.OnClientDisconnectCallback += HandleClientDisconnect;
        networkManager.OnTransportFailure += HandleTransportFailure;
    }

    private void OnDestroy()
    {
        var networkManager = NetworkManager.Singleton;
        if (networkManager == null)
            return;

        networkManager.OnClientDisconnectCallback -= HandleClientDisconnect;
        networkManager.OnTransportFailure -= HandleTransportFailure;
    }

    public async void HostLobby()
    {
        if (!SteamClient.IsValid)
        {
            OnLobbyError?.Invoke("Steam istemcisi hazir degil.");
            return;
        }

        transportManager.ConfigureTransport(TransportMode.Steam);

        var result = await SteamMatchmaking.CreateLobbyAsync(maxLobbyMembers);
        if (!result.HasValue)
        {
            OnLobbyError?.Invoke("Lobi olusturulamadi.");
            return;
        }

        _currentLobby = result.Value;
        IsHost = true;

        // CreateLobbyAsync varsayilan olarak GORUNMEZ bir lobi olusturur; arkadaslar
        // gorebilsin/davet edilebilsin diye acikca FriendsOnly yapiyoruz.
        _currentLobby.Value.SetFriendsOnly();
        AdvertiseLobbyPresence(_currentLobby.Value.Id);

        NetworkManager.Singleton.StartHost();
        Debug.Log($"[SteamLobbyManager] Lobi olusturuldu: {_currentLobby.Value.Id}");
        OnLobbyCreated?.Invoke();
    }

    // Steam Friends listesinde "Oyuna Davet Et" / "Katil" seceneklerinin gorunmesi VE
    // arkadasin Steam UI'inden dogrudan katilabilmesi icin Rich Presence "connect"
    // anahtari zorunludur — bu olmadan overlay disi davet/katilma akislari calismaz.
    private void AdvertiseLobbyPresence(SteamId lobbyId)
    {
        SteamFriends.SetRichPresence("status", "Lobide bekliyor");
        SteamFriends.SetRichPresence("connect", $"{ConnectPrefix}{lobbyId}");
    }

    // Host tarafinda: Steam overlay'inden arkadas davet penceresini acar.
    public void OpenInviteOverlay()
    {
        if (!_currentLobby.HasValue)
        {
            OnLobbyError?.Invoke("Davet gonderebilmek icin once bir lobi olusturmalisiniz.");
            return;
        }

        SteamFriends.OpenGameInviteOverlay(_currentLobby.Value.Id);
    }

    // Davet edilen oyuncu Steam overlay'inde "Kabul Et" / "Katil" dediginde tetiklenir
    // (oyun zaten acikken de, davetle yeni baslatildiginda da Facepunch.Steamworks bu
    // callback'i otomatik tetikler).
    private void HandleGameLobbyJoinRequested(Lobby lobby, SteamId friendId)
    {
        JoinLobby(lobby.Id);
    }

    // Arkadasin Steam Friends listesinden "Katil" dedigi, Rich Presence "connect"
    // anahtari uzerinden gelen istek (overlay disi akis; bkz. AdvertiseLobbyPresence).
    private void HandleRichPresenceJoinRequested(Friend friend, string connectString)
    {
        if (string.IsNullOrEmpty(connectString) || !connectString.StartsWith(ConnectPrefix))
            return;

        var idPart = connectString.Substring(ConnectPrefix.Length).Trim();
        if (ulong.TryParse(idPart, out var lobbyId))
            JoinLobby(lobbyId);
        else
            Debug.LogWarning($"[SteamLobbyManager] Gecersiz connect string: {connectString}");
    }

    private async void JoinLobby(SteamId lobbyId)
    {
        // Tek bir davet kabulu, hem OnGameLobbyJoinRequested hem OnGameRichPresenceJoinRequested'i
        // tetikleyebiliyor (Steam davetin arkasinda hem lobi hem rich-presence "connect" mekanizmasini
        // kullanabiliyor). Koruma olmadan JoinLobbyAsync/StartClient iki kez calisir; ikinci calisma
        // "ag zaten baslatildi" hatasina ve baglantinin hic tamamlanmamasina yol aciyordu.
        if (_joinInProgress || _currentLobby.HasValue)
        {
            Debug.Log($"[SteamLobbyManager] Katilma istegi yoksayildi, zaten devam eden/tamamlanmis bir katilma var (lobbyId={lobbyId}).");
            return;
        }

        _joinInProgress = true;

        if (!SteamClient.IsValid)
        {
            _joinInProgress = false;
            OnLobbyError?.Invoke("Steam istemcisi hazir degil.");
            return;
        }

        var result = await SteamMatchmaking.JoinLobbyAsync(lobbyId);
        if (!result.HasValue)
        {
            _joinInProgress = false;
            OnLobbyError?.Invoke("Lobiye katilinamadi.");
            return;
        }

        _currentLobby = result.Value;
        IsHost = _currentLobby.Value.Owner.Id == SteamClient.SteamId;

        if (IsHost)
        {
            // Host kendi davetini/lobisini tekrar actiginda burasi tetiklenebilir; StartHost
            // zaten HostLobby() icinde cagrildigi icin burada tekrar baslatmiyoruz.
            _joinInProgress = false;
            return;
        }

        AdvertiseLobbyPresence(_currentLobby.Value.Id);
        transportManager.ConfigureTransport(TransportMode.Steam);
        transportManager.SetSteamHostTarget(_currentLobby.Value.Owner.Id);
        NetworkManager.Singleton.StartClient();

        Debug.Log($"[SteamLobbyManager] Lobiye katilindi, host: {_currentLobby.Value.Owner.Id}");
        OnLobbyJoined?.Invoke();
    }

    private void HandleLobbyMemberLeave(Lobby lobby, Friend friend)
    {
        if (!_currentLobby.HasValue || IsHost)
            return;

        if (friend.Id == lobby.Owner.Id)
            HandleHostLost();
    }

    private void HandleClientDisconnect(ulong clientId)
    {
        if (IsHost || !_currentLobby.HasValue)
            return;

        HandleHostLost();
    }

    private void HandleTransportFailure()
    {
        if (IsHost || !_currentLobby.HasValue)
            return;

        HandleHostLost();
    }

    private void HandleHostLost()
    {
        Debug.LogWarning("[SteamLobbyManager] Host baglantisi koptu.");
        LeaveLobby();
        OnHostDisconnected?.Invoke();
    }

    public void LeaveLobby()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();

        _currentLobby?.Leave();
        _currentLobby = null;
        IsHost = false;
        _joinInProgress = false;
        SteamFriends.ClearRichPresence();
    }
}
