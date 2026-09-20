using System;
using System.Collections.Generic;
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

    // Round SIRASINDA gelen ve dondurulmus (round aktifken kopmus) hicbir SteamId ile
    // eslesmeyen bir baglanti reddedilir — GDD'nin sabit 3 rol varsayimiyla tutarli,
    // round ortasinda yabanci biri giremez.
    private const string RoundInProgressReasonKey = "error.round_in_progress";

    public event Action<PlayerRole> OnLocalRoleAssigned;

    // Server-only hook: rol atamasi TAMAMLANDIKTAN sonra tetiklenir (PlayerSpawner
    // bunu dinleyip player objesini spawn eder — event sirasi garantisi olmayan
    // NetworkManager.OnClientConnectedCallback'e ayrica abone olmak yerine).
    public event Action<ulong, PlayerRole> OnServerRoleAssigned;

    // Round State mimarisi TEK OTORITEYE (GameLoopManager.CurrentRoundState) konsolide
    // edildi — RoleManager artik round acik/kapali durumunu TUTMUYOR, sadece rol
    // atamasindan sorumlu. Round durumuna ihtiyac duyan kod GameLoopManager.Instance.
    // IsRoundActive okumali (bkz. asagidaki HandleClientConnected/HandleConnectionApproval/
    // HandleClientDisconnectedOnServer).
    private readonly NetworkList<ClientRoleEntry> _assignedRoles = new();
    private IRoleAssignmentStrategy _strategy;

    // HandleConnectionApproval'da payload'tan cozulen SteamId, NGO'nun clientId'yi
    // atadigi HandleClientConnected cagrisina kadar gecici olarak burada tutulur (bu iki
    // callback arasinda SteamId'yi tasiyacak baska bir NGO mekanizmasi yok). Sadece
    // server-ici, ag uzerinden senkronize edilmez.
    private readonly Dictionary<ulong, ulong> _pendingSteamIdByClientId = new();

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
            _pendingSteamIdByClientId.Clear();
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

    // Lobi fazinda: bir client ayrilip tekrar baglanmaya calisirsa, eski kaydi burada
    // silinmezse _assignedRoles surekli buyur: yeni baglanti MaxPlayers sinirina takilir
    // VEYA joinOrderIndex araligin disina cikip PlayerRole.None alir (Bilesen 1 test
    // raporundaki "Round basladi! Rolun:" bos gorunmesi bugu tam olarak buydu).
    //
    // Round SIRASINDA: kayit ARTIK SILINMIYOR — "oyun durduruldu" rejoin mekanizmasinin
    // parcasi olarak dondurulmus (IsFrozen=true) isaretlenip SteamId ile birlikte
    // saklaniyor. Player.prefab'in NetworkObject'i DontDestroyWithOwner=true oldugu icin
    // obje (pozisyon/envanter dahil) sunucu tarafindan yok edilmiyor, PlayerSpawner ayni
    // rolle geri baglanan client'a objeyi (ChangeOwnership ile) aynen geri veriyor —
    // ayrica bir snapshot/restore sistemine gerek yok. Bekleme suresi SINIRSIZ (round
    // bitene kadar) — otomatik strike/timeout GameLoopManager (Bilesen 2) tam kurulunca
    // netlesecek, GDD'de simdilik tanimli degil.
    private void HandleClientDisconnectedOnServer(ulong clientId)
    {
        for (int i = 0; i < _assignedRoles.Count; i++)
        {
            if (_assignedRoles[i].ClientId != clientId)
                continue;

            var entry = _assignedRoles[i];

            bool roundActive = GameLoopManager.Instance != null && GameLoopManager.Instance.IsRoundActive;
            if (roundActive)
            {
                _assignedRoles[i] = new ClientRoleEntry(entry.ClientId, entry.Role, entry.SteamId, isFrozen: true);
                Debug.Log($"[RoleManager] Client {clientId} round sirasinda koptu, rol donduruldu ({entry.Role}, SteamId={entry.SteamId}).");
                GameLoopManager.Instance?.ServerPauseForDisconnect();
            }
            else
            {
                Debug.Log($"[RoleManager] Client {clientId} ayrildi, rol kaydi kaldirildi ({entry.Role}).");
                _assignedRoles.RemoveAt(i);
            }

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
    // (genel "Sunucu Baglantisi Koptu" ekraniyla karistirmadan). Round SIRASINDA gelen
    // baglantilar icin ayri bir kural gecerli: sadece dondurulmus (round aktifken kopmus)
    // bir SteamId ile eslesirse kabul edilir — yabanci biri round ortasinda giremez.
    private void HandleConnectionApproval(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        response.CreatePlayerObject = false;

        ulong steamId = DecodeSteamId(request.Payload);
        _pendingSteamIdByClientId[request.ClientNetworkId] = steamId;

        bool roundActive = GameLoopManager.Instance != null && GameLoopManager.Instance.IsRoundActive;
        if (roundActive)
        {
            bool isKnownReconnect = FindFrozenEntryIndex(steamId) >= 0;
            response.Approved = isKnownReconnect;
            if (!isKnownReconnect)
                response.Reason = RoundInProgressReasonKey;
            return;
        }

        if (_assignedRoles.Count >= MaxPlayers)
        {
            response.Approved = false;
            response.Reason = LobbyFullReasonKey;
            return;
        }

        response.Approved = true;
    }

    // ConnectionData'ya (bkz. SteamLobbyManager.HostLobby/JoinLobby) client'in kendi
    // SteamClient.SteamId'si 8 baytlik ulong olarak yaziliyor; burada geri cozuluyor.
    // Payload eksik/bozuksa 0 dondurulur (eslesme aranmaz, sadece lobi-fazi atamasi
    // etkilenmez — SteamId sadece round-ici rejoin eslestirmesi icin kullanilir).
    private static ulong DecodeSteamId(byte[] payload)
    {
        if (payload == null || payload.Length < sizeof(ulong))
            return 0;

        return BitConverter.ToUInt64(payload, 0);
    }

    private int FindFrozenEntryIndex(ulong steamId)
    {
        if (steamId == 0)
            return -1;

        for (int i = 0; i < _assignedRoles.Count; i++)
        {
            if (_assignedRoles[i].IsFrozen && _assignedRoles[i].SteamId == steamId)
                return i;
        }

        return -1;
    }

    // BULUNAN HATA: joinOrderIndex dogrudan _assignedRoles.Count'tan turetiliyordu.
    // Lobi fazinda (round baslamadan once) bir oyuncu ayrilip _assignedRoles'tan
    // kaydi silinince (bkz. HandleClientDisconnectedOnServer) sayac geriye duserdi —
    // ornegin Sef (index 0) ayrilirsa kalanlar Komi+Kasiyer (count=2), sonra YENI
    // bir oyuncu katilinca joinOrderIndex=2 -> JoinOrder[2]=Kasiyer atanirdi: artik
    // IKI oyuncu Kasiyer olur ve Sef rolu HIC KIMSEYE atanmamis kalirdi (count yine
    // MaxPlayers'a ulastigi icin StartRound() bunu fark etmeden gecerdi). Duzeltme:
    // strateji arayuzu/JoinOrder DEGISTIRILMEDEN, sadece HALA BOS olan ilk role
    // denk gelen index araniyor — boylece hem eski "index sinirin disina cikip
    // PlayerRole.None kalma" bugu (bkz. HandleClientDisconnectedOnServer notu) HEM
    // bu yeni "iki oyuncuya ayni rol" bugu ayni anda cozulmus oluyor.
    private void HandleClientConnected(ulong clientId)
    {
        ulong steamId = _pendingSteamIdByClientId.TryGetValue(clientId, out var pending) ? pending : 0;
        _pendingSteamIdByClientId.Remove(clientId);

        // Round SIRASINDA: HandleConnectionApproval bu SteamId'yi zaten dondurulmus bir
        // kayitla eslestirip onaylamis olmali (aksi halde buraya hic ulasilmazdi) — ayni
        // kaydi yeni clientId ile guncelleyip "donma"yi kaldiriyoruz. YENI bir rol atamasi
        // YAPILMIYOR, PlayerSpawner de ayni objeyi (ChangeOwnership ile) geri veriyor.
        bool roundActive = GameLoopManager.Instance != null && GameLoopManager.Instance.IsRoundActive;
        if (roundActive)
        {
            int frozenIndex = FindFrozenEntryIndex(steamId);
            if (frozenIndex >= 0)
            {
                var frozen = _assignedRoles[frozenIndex];
                _assignedRoles[frozenIndex] = new ClientRoleEntry(clientId, frozen.Role, steamId, isFrozen: false);

                Debug.Log($"[RoleManager] Client {clientId} (SteamId={steamId}) round sirasinda tekrar baglandi -> {frozen.Role}");
                GameLoopManager.Instance?.ServerResumeAfterReconnect();
                OnServerRoleAssigned?.Invoke(clientId, frozen.Role);
                return;
            }

            // Buraya normalde hic ulasilmamali (HandleConnectionApproval zaten reddetmis
            // olmali) — savunma amacli, sessizce hicbir rol atanmaz.
            Debug.LogWarning($"[RoleManager] Client {clientId} round sirasinda baglandi ama dondurulmus bir kayitla eslesmedi.");
            return;
        }

        var takenRoles = new HashSet<PlayerRole>();
        foreach (var entry in _assignedRoles)
            takenRoles.Add(entry.Role);

        var role = PlayerRole.None;
        for (int i = 0; i < MaxPlayers; i++)
        {
            var candidate = _strategy.AssignRole(clientId, i);
            if (candidate != PlayerRole.None && !takenRoles.Contains(candidate))
            {
                role = candidate;
                break;
            }
        }

        _assignedRoles.Add(new ClientRoleEntry(clientId, role, steamId, isFrozen: false));

        Debug.Log($"[RoleManager] Client {clientId} -> {role}");
        OnServerRoleAssigned?.Invoke(clientId, role);
    }

    // Round baslatma mantigi GameLoopManager.StartRound()'a tasindi (Round State tek
    // otoriteye konsolide edildi) — o metod rol sayisini kontrol etmek icin bu property'i
    // okuyor, RoleManager rol ATAMA mantigina karismiyor.
    public int AssignedRoleCount => _assignedRoles.Count;

    private void HandleAssignedRolesChanged(NetworkListEvent<ClientRoleEntry> change)
    {
        if (change.Value.ClientId != NetworkManager.LocalClientId)
            return;

        OnLocalRoleAssigned?.Invoke(change.Value.Role);
    }

    // Indeksli dongu: crosshair bunu her karede (IInteractionGate uzerinden) cagirabiliyor;
    // foreach NetworkList enumerator'unu her cagrida heap'e kutulardi.
    public PlayerRole GetRole(ulong clientId)
    {
        for (int i = 0; i < _assignedRoles.Count; i++)
        {
            if (_assignedRoles[i].ClientId == clientId)
                return _assignedRoles[i].Role;
        }

        return PlayerRole.None;
    }

    public PlayerRole LocalRole => NetworkManager == null ? PlayerRole.None : GetRole(NetworkManager.LocalClientId);
}

public readonly struct ClientRoleEntry : IEquatable<ClientRoleEntry>, INetworkSerializeByMemcpy
{
    public readonly ulong ClientId;
    public readonly PlayerRole Role;
    public readonly ulong SteamId;
    // Round SIRASINDA kopan oyuncunun kaydi (bkz. HandleClientDisconnectedOnServer) —
    // rol bosa cikmaz, sadece bu bayrak set edilir; ayni SteamId ile geri baglanan
    // oyuncu (bkz. HandleClientConnected) kaydi guncelleyip bayragi false yapar.
    public readonly bool IsFrozen;

    public ClientRoleEntry(ulong clientId, PlayerRole role, ulong steamId, bool isFrozen)
    {
        ClientId = clientId;
        Role = role;
        SteamId = steamId;
        IsFrozen = isFrozen;
    }

    public bool Equals(ClientRoleEntry other) =>
        ClientId == other.ClientId && Role == other.Role && SteamId == other.SteamId && IsFrozen == other.IsFrozen;
    public override bool Equals(object obj) => obj is ClientRoleEntry other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(ClientId, Role, SteamId, IsFrozen);
}
