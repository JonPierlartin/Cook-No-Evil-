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

    private Lobby? _currentLobby;

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
    }

    private void OnDisable()
    {
        SteamMatchmaking.OnLobbyMemberLeave -= HandleLobbyMemberLeave;
        SteamFriends.OnGameLobbyJoinRequested -= HandleGameLobbyJoinRequested;
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

        NetworkManager.Singleton.StartHost();
        Debug.Log($"[SteamLobbyManager] Lobi olusturuldu: {_currentLobby.Value.Id}");
        OnLobbyCreated?.Invoke();
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

    private async void JoinLobby(SteamId lobbyId)
    {
        if (!SteamClient.IsValid)
        {
            OnLobbyError?.Invoke("Steam istemcisi hazir degil.");
            return;
        }

        var result = await SteamMatchmaking.JoinLobbyAsync(lobbyId);
        if (!result.HasValue)
        {
            OnLobbyError?.Invoke("Lobiye katilinamadi.");
            return;
        }

        _currentLobby = result.Value;
        IsHost = _currentLobby.Value.Owner.Id == SteamClient.SteamId;

        if (IsHost)
        {
            // Host kendi davetini/lobisini tekrar actiginda burasi tetiklenebilir; StartHost
            // zaten HostLobby() icinde cagrildigi icin burada tekrar baslatmiyoruz.
            return;
        }

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
    }
}
