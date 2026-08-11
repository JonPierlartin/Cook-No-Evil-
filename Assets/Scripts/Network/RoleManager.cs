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

    // Raw metin degil, UIStrings tablosundaki bir anahtar: NetworkManager.DisconnectReason
    // ile agdan gectigi icin locale'den bagimsiz kalmali; ceviri SteamLobbyManager/
    // LobbyUIController tarafinda yapilir.
    private const string LobbyFullReasonKey = "error.lobby_full";

    public event Action<PlayerRole> OnLocalRoleAssigned;

    // Server-only hook: rol atamasi TAMAMLANDIKTAN sonra tetiklenir (PlayerSpawner
    // bunu dinleyip player objesini spawn eder — event sirasi garantisi olmayan
    // NetworkManager.OnClientConnectedCallback'e ayrica abone olmak yerine).
    public event Action<ulong, PlayerRole> OnServerRoleAssigned;

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
        if (IsServer)
        {
            // RoleManager, GameSystems sahne-ici kalici bir obje uzerinde yasiyor —
            // NetworkManager.Shutdown() bu component'i yok etmez, sadece network
            // spawn/despawn dongusunu kapatir. Yani _assignedRoles/IsRoundActive gibi
            // alanlar bir onceki oturumdan (host kimse) OLDUGU GIBI kalir. Server, her
            // yeni StartHost() cagrisinda (bu OnNetworkSpawn, o cagrinin bir parcasi
            // olarak, herhangi bir client baglanmadan ONCE calisir) bu durumu acikca
            // sifirlamazsa, yeni lobide eski oyuncu sayisi/round durumu sizar — tam da
            // bu bug'in sebebi buydu.
            _assignedRoles.Clear();
            IsRoundActive.Value = false;
        }

        _assignedRoles.OnListChanged += HandleAssignedRolesChanged;

        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.OnClientDisconnectCallback += HandleClientDisconnectedOnServer;
        }

        // Gec katilan client icin: liste zaten dolu geldiyse kendi rolumuzu hemen bildir.
        var existing = GetRole(NetworkManager.LocalClientId);
        if (existing != PlayerRole.None)
            OnLocalRoleAssigned?.Invoke(existing);
    }

    public override void OnNetworkDespawn()
    {
        _assignedRoles.OnListChanged -= HandleAssignedRolesChanged;

        if (IsServer && NetworkManager != null)
        {
            NetworkManager.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.OnClientDisconnectCallback -= HandleClientDisconnectedOnServer;
        }
    }

    // Bir client aynı (host'un yeniden host olmadigi, hala calisan) lobiden ayrilip
    // tekrar baglanmaya calisirsa, eski kaydi burada silinmezse _assignedRoles surekli
    // buyur: yeni baglanti MaxPlayers sinirina takilir VEYA joinOrderIndex araligin
    // disina cikip PlayerRole.None alir (Bilesen 1 test raporundaki "Round basladi!
    // Rolun:" bos gorunmesi bugu tam olarak buydu). NOT: bir oyuncu round SIRASINDA
    // ayrilirsa rolu bosa cikar ama round'un kendisi ne olacak (durur mu, bekler mi)
    // GDD'de tanimli degil — bu, GameLoopManager (Bilesen 2) ile birlikte netlestirilecek
    // ayri bir tasarim karari, burada sadece sayim/atama tutarliligi duzeltiliyor.
    private void HandleClientDisconnectedOnServer(ulong clientId)
    {
        for (int i = 0; i < _assignedRoles.Count; i++)
        {
            if (_assignedRoles[i].ClientId != clientId)
                continue;

            Debug.Log($"[RoleManager] Client {clientId} ayrildi, rol kaydi kaldirildi ({_assignedRoles[i].Role}).");
            _assignedRoles.RemoveAt(i);
            break;
        }
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
            response.Reason = LobbyFullReasonKey;
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
        OnServerRoleAssigned?.Invoke(clientId, role);
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
