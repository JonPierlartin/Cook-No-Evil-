using System;
using Unity.Netcode;
using UnityEngine;

// Server-authoritative rol atamasi. Atama mantigi IRoleAssignmentStrategy arkasina
// soyutlanmistir (GDD 3, Bilesen 1) — ileride bir lobi rol-secim ekrani eklenirse
// RoleManager'in kendisi degil sadece bu strateji degistirilir.
[RequireComponent(typeof(NetworkObject))]
public class RoleManager : NetworkBehaviour
{
    public static RoleManager Instance { get; private set; }

    public const int MaxPlayers = 3;
    private const string LobbyFullReason = "Lobi dolu.";

    public event Action<PlayerRole> OnLocalRoleAssigned;

    // GEÇİCİ yer tutucu: gerçek round/oyun döngüsü yönetimi Bileşen 2'deki GameLoopManager'a
    // ait olacak. O gelene kadar rol kısıtlamalarının (VoIPController) ne zaman devreye
    // girecegini belirlemek icin burada tutuluyor. StartRound() host'un "Oyunu Baslat"
    // butonuyla cagrilir; GameLoopManager gelince bu cagri oradaki gercek round-baslatma
    // mantigina devredilecek.
    public readonly NetworkVariable<bool> IsRoundActive =
        new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly NetworkList<ClientRoleEntry> _assignedRoles = new();
    private IRoleAssignmentStrategy _strategy;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _strategy = new SequentialRoleAssignmentStrategy();
    }

    private void Start()
    {
        // ConnectionApproval, StartHost()/StartServer() cagrilmadan ONCE etkinlestirilmis olmali
        // (NetworkTransportManager'in transport'u Start()'ta ayarlamasiyla ayni zamanlama kurali).
        // Client tarafinda bu ayarlarin bir etkisi olmuyor (NGO callback'i sadece server'da cagirir),
        // o yuzden IsServer kontrolu olmadan tum instance'larda kuruyoruz.
        var networkManager = NetworkManager.Singleton;
        if (networkManager == null)
        {
            Debug.LogError("[RoleManager] NetworkManager.Singleton bulunamadi.");
            return;
        }

        networkManager.NetworkConfig.ConnectionApproval = true;
        networkManager.ConnectionApprovalCallback += HandleConnectionApproval;
    }

    public override void OnNetworkSpawn()
    {
        _assignedRoles.OnListChanged += HandleAssignedRolesChanged;

        if (IsServer)
            NetworkManager.OnClientConnectedCallback += HandleClientConnected;

        // Gec katilan client icin: liste zaten dolu geldiyse kendi rolumuzu hemen bildir.
        var existing = GetRole(NetworkManager.LocalClientId);
        if (existing != PlayerRole.None)
            OnLocalRoleAssigned?.Invoke(existing);
    }

    public override void OnNetworkDespawn()
    {
        _assignedRoles.OnListChanged -= HandleAssignedRolesChanged;

        if (IsServer && NetworkManager != null)
            NetworkManager.OnClientConnectedCallback -= HandleClientConnected;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.ConnectionApprovalCallback -= HandleConnectionApproval;
    }

    // Lobi zaten MaxPlayers'a ulasmissa yeni baglantiyi acik bir sebeple reddeder — SteamLobbyManager
    // bunu NetworkManager.DisconnectReason uzerinden okuyup ayirt edici bir UI mesaji gosterir
    // (genel "Sunucu Baglantisi Koptu" ekraniyla karistirmadan).
    private void HandleConnectionApproval(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        response.CreatePlayerObject = false;

        if (_assignedRoles.Count >= MaxPlayers)
        {
            response.Approved = false;
            response.Reason = LobbyFullReason;
            return;
        }

        response.Approved = true;
    }

    private void HandleClientConnected(ulong clientId)
    {
        int joinOrderIndex = _assignedRoles.Count;
        var role = _strategy.AssignRole(clientId, joinOrderIndex);
        _assignedRoles.Add(new ClientRoleEntry(clientId, role));

        Debug.Log($"[RoleManager] Client {clientId} -> {role}");
    }

    // Host'un "Oyunu Baslat" butonuyla cagirdigi, server-authoritative round baslatma.
    // GameLoopManager (Bilesen 2) gelince bu metod oradaki gercek round-baslatma
    // akisina (5 dk sayac, strike sistemi vb.) devredilecek.
    public bool StartRound()
    {
        if (!IsServer)
            return false;

        if (IsRoundActive.Value)
            return true;

        if (_assignedRoles.Count < MaxPlayers)
        {
            Debug.LogWarning($"[RoleManager] Round baslatilamiyor, {_assignedRoles.Count}/{MaxPlayers} oyuncu var.");
            return false;
        }

        IsRoundActive.Value = true;
        return true;
    }

    private void HandleAssignedRolesChanged(NetworkListEvent<ClientRoleEntry> change)
    {
        if (change.Value.ClientId != NetworkManager.LocalClientId)
            return;

        OnLocalRoleAssigned?.Invoke(change.Value.Role);
    }

    public PlayerRole GetRole(ulong clientId)
    {
        foreach (var entry in _assignedRoles)
        {
            if (entry.ClientId == clientId)
                return entry.Role;
        }

        return PlayerRole.None;
    }

    public PlayerRole LocalRole => NetworkManager == null ? PlayerRole.None : GetRole(NetworkManager.LocalClientId);
}

public readonly struct ClientRoleEntry : IEquatable<ClientRoleEntry>, INetworkSerializeByMemcpy
{
    public readonly ulong ClientId;
    public readonly PlayerRole Role;

    public ClientRoleEntry(ulong clientId, PlayerRole role)
    {
        ClientId = clientId;
        Role = role;
    }

    public bool Equals(ClientRoleEntry other) => ClientId == other.ClientId && Role == other.Role;
    public override bool Equals(object obj) => obj is ClientRoleEntry other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(ClientId, Role);
}
