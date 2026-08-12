using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// GameSystems uzerinde yasar (RoleManager/VoIPController ile ayni sahne-ici kalici
// obje). RoleManager.OnServerRoleAssigned tetiklendiginde (rol atamasi KESINLIKLE
// tamamlandiktan sonra — event sirasi garantisi olmayan
// NetworkManager.OnClientConnectedCallback'e ayrica abone olmak yerine) server-side
// player objesini spawn eder. RoleManager.HandleConnectionApproval hala
// CreatePlayerObject=false ayarliyor (NGO'nun otomatik player-object spawn'i
// KULLANILMIYOR) — boylece rol bazli spawn konumu secilebiliyor. Lobi fazinda
// disconnect'te oyuncu objesinin temizlenmesi NGO'nun varsayilan davranisiyla
// (NetworkObject DontDestroyWithOwner=false) otomatik gerceklesir. Round SIRASINDA
// disconnect ise FARKLI: Player.prefab'in NetworkObject'i artik
// DontDestroyWithOwner=true, yani obje sunucu tarafindan YOK EDILMIYOR —ayni rolle
// geri baglanan client'a (RoleManager zaten SteamId eslesmesini dogruladiktan sonra)
// bu metod ayni objeyi ChangeOwnership ile geri veriyor, YENI bir prefab spawn
// ETMIYOR. Boylece pozisyon (NetworkTransform) ve envanter (PlayerInventory) elle bir
// snapshot/restore sistemine gerek kalmadan oldugu gibi korunuyor.
[RequireComponent(typeof(NetworkObject))]
public class PlayerSpawner : NetworkBehaviour
{
    [SerializeField] private NetworkObject playerPrefab;
    [Tooltip("Gercek seviye geometrisi henuz yok; her rol icin basit bir ofset kullanilir.")]
    [SerializeField] private float spawnSpacing = 2f;

    // Rol -> spawn edilmis Player.prefab NetworkObject'i. Round sirasinda dondurulmus
    // (sahibi kopmus ama obje yok edilmemis) objeleri reconnect'te bulmak icin kullanilir.
    private readonly Dictionary<PlayerRole, NetworkObject> _spawnedPlayerObjects = new();

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        // GameSystems kalici bir obje oldugu icin (bkz. RoleManager'daki ayni desen notu)
        // bir onceki hosting oturumundan kalma referanslar burada acikca temizlenir.
        _spawnedPlayerObjects.Clear();

        if (RoleManager.Instance != null)
            RoleManager.Instance.OnServerRoleAssigned += HandleServerRoleAssigned;
        else
            Debug.LogError("[PlayerSpawner] RoleManager.Instance bulunamadi.");
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && RoleManager.Instance != null)
            RoleManager.Instance.OnServerRoleAssigned -= HandleServerRoleAssigned;
    }

    private void HandleServerRoleAssigned(ulong clientId, PlayerRole role)
    {
        if (role == PlayerRole.None)
            return;

        if (_spawnedPlayerObjects.TryGetValue(role, out var existing) && existing != null)
        {
            existing.ChangeOwnership(clientId);
            Debug.Log($"[PlayerSpawner] Client {clientId} icin mevcut Player.prefab geri devredildi (rol={role}).");
            return;
        }

        if (playerPrefab == null)
        {
            Debug.LogError("[PlayerSpawner] playerPrefab atanmamis.");
            return;
        }

        var spawnPosition = new Vector3((int)role * spawnSpacing, 0f, 0f);
        var instance = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
        instance.SpawnAsPlayerObject(clientId);
        _spawnedPlayerObjects[role] = instance;

        // TESHIS: gercek cok-makineli testte "round basladiginda ekranda hicbir sey
        // degismiyor" raporu icin — Player.log'da bu satirin varligi spawn'in
        // gercekten gerceklestigini dogrular (bkz. PlayerController'daki kamera log'u).
        Debug.Log($"[PlayerSpawner] Client {clientId} icin Player.prefab spawn edildi (rol={role}).");
    }
}
