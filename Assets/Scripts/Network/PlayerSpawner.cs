using Unity.Netcode;
using UnityEngine;

// GameSystems uzerinde yasar (RoleManager/VoIPController ile ayni sahne-ici kalici
// obje). RoleManager.OnServerRoleAssigned tetiklendiginde (rol atamasi KESINLIKLE
// tamamlandiktan sonra — event sirasi garantisi olmayan
// NetworkManager.OnClientConnectedCallback'e ayrica abone olmak yerine) server-side
// player objesini spawn eder. RoleManager.HandleConnectionApproval hala
// CreatePlayerObject=false ayarliyor (NGO'nun otomatik player-object spawn'i
// KULLANILMIYOR) — boylece rol bazli spawn konumu secilebiliyor. Disconnect'te
// oyuncu objesinin temizlenmesi NGO'nun varsayilan davranisiyla (NetworkObject
// DontDestroyWithOwner=false) otomatik gerceklesir, ekstra kod gerekmiyor.
[RequireComponent(typeof(NetworkObject))]
public class PlayerSpawner : NetworkBehaviour
{
    [SerializeField] private NetworkObject playerPrefab;
    [Tooltip("Gercek seviye geometrisi henuz yok; her rol icin basit bir ofset kullanilir.")]
    [SerializeField] private float spawnSpacing = 2f;

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

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

        if (playerPrefab == null)
        {
            Debug.LogError("[PlayerSpawner] playerPrefab atanmamis.");
            return;
        }

        var spawnPosition = new Vector3((int)role * spawnSpacing, 0f, 0f);
        var instance = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
        instance.SpawnAsPlayerObject(clientId);

        // TESHIS: gercek cok-makineli testte "round basladiginda ekranda hicbir sey
        // degismiyor" raporu icin — Player.log'da bu satirin varligi spawn'in
        // gercekten gerceklestigini dogrular (bkz. PlayerController'daki kamera log'u).
        Debug.Log($"[PlayerSpawner] Client {clientId} icin Player.prefab spawn edildi (rol={role}).");
    }
}
