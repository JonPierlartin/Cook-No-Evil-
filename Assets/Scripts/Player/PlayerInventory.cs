using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Generic slotlu envanter — tum roller icin AYNI kavram. Slotlar GERCEK OGELERIN (Item, sunucu sahipli
// NetworkObject) kimligini tutar (ItemSlotEntry; bos slot = 0). Turu/durumu ogenin kendisi tasir; burada
// tur numarasi tutulmaz. Slot listesini yalnizca sunucu yazar; ActiveSlotIndex ise yalnizca sahibinin
// etkiledigi bir secim oldugu icin NetworkVariableWritePermission.Owner ile dogrudan client tarafindan
// yazilir. Ogenin dogumu/olumu burada DEGIL, ItemMover'dadir.
[RequireComponent(typeof(NetworkObject))]
public class PlayerInventory : NetworkBehaviour
{
    [Tooltip("Slot sayisi (GDD 4.1: 4). Hotbar UI'sinin slot sayisiyla eslesmeli.")]
    [SerializeField] private int slotCount = 4;

    public readonly NetworkList<ItemSlotEntry> Slots = new();

    public readonly NetworkVariable<int> ActiveSlotIndex =
        new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private static readonly List<PlayerInventory> Spawned = new();

    public int SlotCount => slotCount;

    public override void OnNetworkSpawn()
    {
        Spawned.Add(this);

        if (IsServer)
        {
            Slots.Clear();
            for (int i = 0; i < slotCount; i++)
                Slots.Add(default);
        }
    }

    public override void OnNetworkDespawn()
    {
        Spawned.Remove(this);

        // Oyuncu nesnesi GERCEKTEN despawn oluyorsa (oyuncu ayrildi ve nesnesi silindi) elindeki ogeler
        // sahipsiz kalmasin. Ag kapanirken (Shutdown) NGO tum nesneleri zaten despawn eder; orada
        // ItemMover'i cagirmak yarisa girer ve sahte hata basar — bu yuzden ShutdownInProgress ile ayrilir.
        var manager = NetworkManager.Singleton;
        if (!IsServer || manager == null || manager.ShutdownInProgress)
            return;

        for (int i = 0; i < Slots.Count; i++)
        {
            if (!TryGetItem(i, out var item))
                continue;

            Slots[i] = default;
            ItemMover.Despawn(item);
        }
    }

    // IInteractionGate uygulamalarinin sunucuda VE istemcide ayni cagriyla envanteri bulmasi icin.
    // Sunucu: ConnectedClients (mevcut yol). Istemci: yalnizca KENDI envanteri — IsOwner ile;
    // NetworkManager.LocalClient.PlayerObject ChangeOwnership'ten sonra (rejoin) guncellenmedigi
    // icin kullanilmaz (bkz. HotbarUI notu).
    public static PlayerInventory FindForClient(ulong clientId)
    {
        var networkManager = NetworkManager.Singleton;
        if (networkManager == null)
            return null;

        if (networkManager.IsServer)
        {
            if (!networkManager.ConnectedClients.TryGetValue(clientId, out var client) || client.PlayerObject == null)
                return null;

            return client.PlayerObject.GetComponent<PlayerInventory>();
        }

        if (clientId != networkManager.LocalClientId)
            return null;

        foreach (var inventory in Spawned)
        {
            if (inventory.IsOwner)
                return inventory;
        }

        return null;
    }

    public void SetActiveSlot(int slotIndex)
    {
        if (!IsOwner || slotIndex < 0 || slotIndex >= slotCount)
            return;

        ActiveSlotIndex.Value = slotIndex;
    }

    // Slottaki ogeyi SpawnedObjects'ten cozer. Sunucuda ve istemcide AYNI fonksiyon. Slot bos, oge bu
    // makinede (henuz) spawn olmamis veya Item bileseni yoksa false doner — istisna atmaz. Liste
    // ogeden once gelebildigi icin "false" istemcide gecici bir durum olabilir; cagiranlar bunu hata
    // saymaz, oge gelince yeniden sorar.
    public bool TryGetItem(int slot, out Item item)
    {
        item = null;

        if (slot < 0 || slot >= Slots.Count)
            return false;

        var entry = Slots[slot];
        if (entry.IsEmpty)
            return false;

        var manager = NetworkManager.Singleton;
        if (manager == null || manager.SpawnManager == null)
            return false;

        if (!manager.SpawnManager.SpawnedObjects.TryGetValue(entry.NetworkObjectId, out var networkObject) || networkObject == null)
            return false;

        return networkObject.TryGetComponent(out item);
    }

    public bool TryGetActiveItem(out Item item) => TryGetItem(ActiveSlotIndex.Value, out item);

    // Salt-okunur uygunluk kontrolu — ServerTryAddItem'in hedef slot aramasiyla AYNI fonksiyonu kullanir,
    // yani "bos slot var" dedigi her yerde ekleme gercekten basarir (crosshair yalan soylemez).
    public bool HasFreeSlot() => FindTargetSlot() >= 0;

    // GDD 4.1 "Alinan ogenin hangi slota girdigi": SECILI slot bossa oraya; doluysa secili slottan
    // SONRAKI ilk bos slota, sona gelince basa sararak. Secili slot degismez. Hic bos slot yoksa -1.
    private int FindTargetSlot()
    {
        int count = Slots.Count;
        if (count == 0)
            return -1;

        int selected = Mathf.Clamp(ActiveSlotIndex.Value, 0, count - 1);
        for (int step = 0; step < count; step++)
        {
            int index = (selected + step) % count;
            if (Slots[index].IsEmpty)
                return index;
        }

        return -1;
    }

    // Spawn edilmis, Carried bir ogeyi envantere yazar. Basarisizsa false — cagiran, spawn ettigi
    // ogeyi hemen ItemMover ile despawn etmekten sorumludur (sahipsiz oge kalmasin).
    public bool ServerTryAddItem(Item item)
    {
        if (!IsServer || item == null || !item.NetworkObject.IsSpawned)
            return false;

        int target = FindTargetSlot();
        if (target < 0)
            return false;

        Slots[target] = ItemSlotEntry.For(item.NetworkObject);
        return true;
    }

    // "Oyuncunun su an sectigi ogeyi kullan" (BurgerAssemblyStation / PackagingStation). Ogeyi
    // slottan CIKARIR ama despawn ETMEZ — ogenin akibetine cagiran karar verir (tuketim: ItemMover.Despawn).
    public bool ServerTryTakeActiveItem(out Item item)
    {
        item = null;

        if (!IsServer)
            return false;

        int index = ActiveSlotIndex.Value;
        if (!TryGetItem(index, out item))
        {
            item = null;
            return false;
        }

        Slots[index] = default;
        return true;
    }
}
