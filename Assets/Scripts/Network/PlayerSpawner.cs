using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// GameSystems uzerinde yasar (RoleManager/VoIPController ile ayni sahne-ici kalici obje).
// GDD 8.1 "Lobide karakter yoktur": oyuncu nesneleri BAGLANTI ANINDA DEGIL, round
// RoundActive'e GECTIGINDE doger (bkz. HandleRoundStateChanged). RoleManager rol atamasini
// hala baglanti aninda (lobide) yapar — sadece FIZIKSEL karakterin dogumu erteleniyor, rol
// kaydi degil. RoleManager.HandleConnectionApproval hala CreatePlayerObject=false ayarliyor
// (NGO'nun otomatik player-object spawn'i KULLANILMIYOR) — boylece rol bazli spawn konumu
// secilebiliyor; NetworkManager'in kendi NetworkConfig.PlayerPrefab alani da bos (sahnede
// dogrulandi) — otomatik spawn'a giden IKI yol da kapali.
//
// Round SIRASINDA disconnect FARKLI: Player.prefab'in NetworkObject'i DontDestroyWithOwner=
// true, yani obje sunucu tarafindan YOK EDILMIYOR — ayni rolle geri baglanan client'a
// (RoleManager zaten SteamId eslesmesini dogruladiktan sonra, RoundActive iken
// OnServerRoleAssigned'i tekrar tetikler) bu obje ChangeOwnership ile geri verilir, YENI bir
// prefab spawn EDILMEZ. Boylece pozisyon (NetworkTransform) ve envanter (PlayerInventory)
// elle bir snapshot/restore sistemine gerek kalmadan oldugu gibi korunuyor.
//
// Round'un RoundActive'den LOBIYE donup karakterleri despawn edecegi bir kod yolu YOK
// (GameLoopManager.StartRound() yalnizca Lobby -> RoundActive gecisini yapar, tersi
// tanimli degil) — bu yuzden burada boyle bir temizlik YAZILMADI; GDD/GameLoopManager'a
// eklenirse burasi da guncellenmeli.
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

        if (GameLoopManager.Instance != null)
            GameLoopManager.Instance.CurrentRoundState.OnValueChanged += HandleRoundStateChanged;
        else
            Debug.LogError("[PlayerSpawner] GameLoopManager.Instance bulunamadi.");
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer)
            return;

        if (RoleManager.Instance != null)
            RoleManager.Instance.OnServerRoleAssigned -= HandleServerRoleAssigned;

        if (GameLoopManager.Instance != null)
            GameLoopManager.Instance.CurrentRoundState.OnValueChanged -= HandleRoundStateChanged;
    }

    // Lobi -> RoundActive gecisinde (host "Oyunu Baslat"a bastiginda) o ana kadar SADECE
    // atanmis (rol kaydi var ama karakteri henuz yok) her rolu doger. GDD 8.1'in tek
    // dogum noktasi burasidir; asagidaki HandleServerRoleAssigned (baglanti aninda tetiklenen
    // event) round aktif DEGILKEN kasitli olarak hicbir sey yapmaz (bkz. o metodun govdesi).
    private void HandleRoundStateChanged(RoundState previous, RoundState current)
    {
        if (current != RoundState.RoundActive)
            return;

        if (RoleManager.Instance == null)
        {
            Debug.LogError("[PlayerSpawner] RoleManager.Instance bulunamadi, karakterler doğurulamadı.");
            return;
        }

        for (int i = 0; i < RoleManager.Instance.AssignedRoleCount; i++)
        {
            if (RoleManager.Instance.TryGetAssignedRoleAt(i, out var clientId, out var role))
                SpawnOrReassign(clientId, role);
        }
    }

    // RoleManager.OnServerRoleAssigned iki farkli anda tetiklenir: (1) LOBIDE bir client
    // baglandiginda (rol atanir ama round henuz aktif degildir — GDD 8.1 geregi burada
    // KARAKTER DOGMAZ, sadece rol kaydedilir, asil dogum HandleRoundStateChanged'dedir);
    // (2) ROUND SIRASINDA donmus bir SteamId ile geri baglanildiginda (round zaten aktiftir,
    // bu durumda ASAGIDAKI mevcut reassign/spawn mantigi hemen calisir — rejoin bugunku gibi
    // isler).
    private void HandleServerRoleAssigned(ulong clientId, PlayerRole role)
    {
        bool roundActive = GameLoopManager.Instance != null && GameLoopManager.Instance.IsRoundActive;
        if (!roundActive)
            return;

        SpawnOrReassign(clientId, role);
    }

    private void SpawnOrReassign(ulong clientId, PlayerRole role)
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
