using Unity.Netcode;
using UnityEngine;

// Ogelerin spawn / despawn'inin TEK yeri (CLAUDE.md "Hedef mimari — Adim 4.0"). Yalnizca sunucuda
// calisir. Basarisizlik KOSULSUZ Debug.LogError basar: NGO'nun kendi basarisizlik loglari (orn.
// TrySetParent) varsayilan log seviyesinde sessizdir, teshisi imkansiz kilar. Adim 4.2'de parent
// islemleri (TrySetParent / TryRemoveParent) de buradadir — projede baska hicbir yerde cagrilmaz.
//
// NGO tuzagi: NetworkObject.TrySetParent, OnTransformParentChanged gecersiz bir parent'i sessizce GERI
// ALIRKEN yine de true donebilir (kaynaktan dogrulandi). Bu yuzden her parent islemi sonrasi gercek
// transform.parent da denetlenir; yalnizca donus degerine guvenilmez.
public static class ItemMover
{
    // Ogeyi ELDE dogar: Presence varsayilani Carried'dir, dunya gorseli kapali. Ayni karede parent
    // yapilmaz; oge once envantere yazilir (PlayerInventory.ServerTryAddItem). Basarisizsa null.
    public static Item SpawnCarried(ItemType type, Vector3 position)
    {
        if (!IsServerRunning("SpawnCarried"))
            return null;

        if (type == null || type.ItemPrefab == null)
        {
            Debug.LogError($"[ItemMover] Spawn edilemedi: '{(type != null ? type.name : "null")}' turunun itemPrefab'i yok.");
            return null;
        }

        var instance = Object.Instantiate(type.ItemPrefab, position, Quaternion.identity);
        if (!instance.TryGetComponent(out NetworkObject networkObject) || !instance.TryGetComponent(out Item item))
        {
            Debug.LogError($"[ItemMover] Spawn edilemedi: '{type.ItemPrefab.name}' prefab'inin kokunde NetworkObject + Item yok.", type.ItemPrefab);
            Object.Destroy(instance);
            return null;
        }

        networkObject.Spawn();
        if (!networkObject.IsSpawned)
        {
            Debug.LogError($"[ItemMover] '{type.ItemPrefab.name}' spawn edilemedi (NetworkObject.Spawn sonrasi IsSpawned=false).", type.ItemPrefab);
            Object.Destroy(instance);
            return null;
        }

        return item;
    }

    // Ogeyi ag uzerinden yok eder. Cagirmadan once oge yuvalardan (slot vb.) cikarilmis olmali.
    // Icinde baska ogeler varsa (paket) ONCE onlar acikca despawn edilir: ebeveyni despawn edilen ag nesnesinin
    // cocuklarina guvenilmez (sahipsiz kalabilirler). Sizinti denetimi: islemden once/sonra spawn edilmis oge sayisi.
    public static bool Despawn(Item item)
    {
        if (item != null && item.transform.childCount > 0)
        {
            var contents = new System.Collections.Generic.List<Item>();
            foreach (var child in item.GetComponentsInChildren<Item>(true))
            {
                if (child != item)
                    contents.Add(child);
            }

            if (contents.Count > 0)
            {
                int before = CountSpawnedItems();
                // Icteki ogeler once (en derindeki en once): GetComponentsInChildren ebeveynden cocuga siralar.
                for (int i = contents.Count - 1; i >= 0; i--)
                    DespawnSingle(contents[i]);

                bool result = DespawnSingle(item);
                int after = CountSpawnedItems();
                int expected = before - contents.Count - 1;
                if (after == expected)
                    Debug.Log($"[ItemMover] '{item.name}' icindeki {contents.Count} oge ile birlikte yok edildi (oge sayisi {before} -> {after}, sizinti yok).");
                else
                    Debug.LogError($"[ItemMover] '{item.name}' yok edilirken SIZINTI: oge sayisi {before} -> {after}, beklenen {expected}.");

                return result;
            }
        }

        return DespawnSingle(item);
    }

    private static int CountSpawnedItems()
    {
        int count = 0;
        foreach (var networkObject in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList)
        {
            if (networkObject != null && networkObject.TryGetComponent<Item>(out _))
                count++;
        }

        return count;
    }

    private static bool DespawnSingle(Item item)
    {
        if (!IsServerRunning("Despawn"))
            return false;

        if (item == null)
        {
            Debug.LogError("[ItemMover] Despawn edilemedi: oge null.");
            return false;
        }

        var networkObject = item.NetworkObject;
        if (networkObject == null || !networkObject.IsSpawned)
        {
            Debug.LogError($"[ItemMover] Despawn edilemedi: '{item.name}' spawn edilmis degil.", item);
            return false;
        }

        networkObject.Despawn();
        return true;
    }

    // Etkilesim baglamindaki (slotIndex) slottaki ogeyi yuvaya koyar: slottan al -> yuvanin pozuna
    // yerlestir -> TrySetParent(yuva) -> Presence = Placed. Oge yuvaya parent edilir (K7: yuva bir
    // NetworkObject'tir), yani doluluk artik parent iliskisidir (ItemSlot.TryGetOccupant). Kural
    // denetimi (rol, tur, doluluk) ItemSlot'un gate'indedir; burada yalnizca mekanik ve savunmaci
    // kontroller vardir. slotIndex CAGIRANDAN (InteractionContext.SlotIndex) gelir — ActiveSlotIndex
    // NetworkVariable'i BURADA OKUNMAZ (sahibin yazdigi deger ile bu cagri arasinda sira garantisi
    // yok, bkz. InteractionContext.cs). Basarisizlikta oge AYNI slota geri konur, Presence = Carried,
    // koşulsuz LogError.
    public static bool PlaceInSlot(ItemSlot slot, PlayerInventory inventory, int slotIndex)
    {
        if (!IsServerRunning("PlaceInSlot"))
            return false;

        if (slot == null || inventory == null)
        {
            Debug.LogError("[ItemMover] PlaceInSlot: yuva veya envanter null.");
            return false;
        }

        if (slot.TryGetOccupant(out _))
        {
            Debug.LogError($"[ItemMover] '{slot.name}' dolu; oge konamaz.", slot);
            return false;
        }

        if (!inventory.ServerTryTakeItemAt(slotIndex, out var item))
        {
            Debug.LogError($"[ItemMover] '{slot.name}': baglam slotunda (index {slotIndex}) oge yok.", slot);
            return false;
        }

        item.transform.SetPositionAndRotation(slot.transform.position, slot.transform.rotation);
        if (!TryAttach(item, slot.NetworkObject))
        {
            Debug.LogError($"[ItemMover] '{item.name}' '{slot.name}' yuvasina parent edilemedi; oge {slotIndex}. slota geri konuyor.", slot);
            item.Presence.Value = ItemPresence.Carried;

            if (!inventory.ServerTrySetItemAt(slotIndex, item))
            {
                Debug.LogError($"[ItemMover] '{item.name}' {slotIndex}. slota geri konamadi; sahipsiz kalmamasi icin despawn ediliyor.", slot);
                Despawn(item);
            }

            return false;
        }

        item.Presence.Value = ItemPresence.Placed;
        return true;
    }

    // Yuvadaki ogeyi oyuncunun envanterine alir: envanterde yer var mi -> TryRemoveParent -> Presence =
    // Carried -> slot kuraliyla (PlayerInventory.ServerTryAddItem) envantere ekle. Basarisizlikta oge
    // yuvada KALIR (gerekiyorsa geri parent edilir), koşulsuz LogError. slotIndex CAGIRANDAN
    // (InteractionContext.SlotIndex) gelir — GDD 4.1 slot kurali oradan baslar.
    public static bool TakeFromSlot(ItemSlot slot, PlayerInventory inventory, int slotIndex)
    {
        if (!IsServerRunning("TakeFromSlot"))
            return false;

        if (slot == null || inventory == null)
        {
            Debug.LogError("[ItemMover] TakeFromSlot: yuva veya envanter null.");
            return false;
        }

        if (!slot.TryGetOccupant(out var item))
        {
            Debug.LogError($"[ItemMover] '{slot.name}' bos; alinacak oge yok.", slot);
            return false;
        }

        if (!inventory.HasFreeSlot(slotIndex))
        {
            Debug.LogError($"[ItemMover] '{slot.name}': envanterde bos slot yok; oge yuvada kaldi.", slot);
            return false;
        }

        if (!item.NetworkObject.TryRemoveParent(true) || item.transform.parent != null)
        {
            Debug.LogError($"[ItemMover] '{item.name}' '{slot.name}' yuvasindan ayrilamadi; oge yuvada kaldi.", slot);
            return false;
        }

        item.Presence.Value = ItemPresence.Carried;

        if (!inventory.ServerTryAddItem(item, slotIndex))
        {
            Debug.LogError($"[ItemMover] '{item.name}' envantere yazilamadi; '{slot.name}' yuvasina geri konuyor.", slot);

            item.transform.SetPositionAndRotation(slot.transform.position, slot.transform.rotation);
            if (TryAttach(item, slot.NetworkObject))
                item.Presence.Value = ItemPresence.Placed;
            else
                Debug.LogError($"[ItemMover] '{item.name}' yuvaya da geri konamadi; oge sahipsiz.", slot);

            return false;
        }

        return true;
    }

    // Etkilesim baglamindaki (slotIndex) slottaki urunu paketin ICINE koyar (GDD 5.3.1): slottan al -> pakete
    // TrySetParent -> Presence = Contained. Urun yok edilmez; paketin cocugu olarak kendi durumunu (katmanlar,
    // faz) tasir. Kural denetimi PackingArea'nin gate'indedir. Basarisizlikta urun AYNI slota geri konur.
    public static bool PackInto(Package package, PlayerInventory inventory, int slotIndex)
    {
        if (!IsServerRunning("PackInto"))
            return false;

        if (package == null || inventory == null)
        {
            Debug.LogError("[ItemMover] PackInto: paket veya envanter null.");
            return false;
        }

        if (!inventory.ServerTryTakeItemAt(slotIndex, out var item))
        {
            Debug.LogError($"[ItemMover] '{package.name}': baglam slotunda (index {slotIndex}) urun yok.", package);
            return false;
        }

        item.transform.SetPositionAndRotation(package.transform.position, package.transform.rotation);
        if (!TryAttach(item, package.NetworkObject))
        {
            Debug.LogError($"[ItemMover] '{item.name}' '{package.name}' paketine konamadi; urun {slotIndex}. slota geri konuyor.", package);
            item.Presence.Value = ItemPresence.Carried;

            if (!inventory.ServerTrySetItemAt(slotIndex, item))
            {
                Debug.LogError($"[ItemMover] '{item.name}' {slotIndex}. slota geri konamadi; sahipsiz kalmamasi icin despawn ediliyor.", package);
                Despawn(item);
            }

            return false;
        }

        item.Presence.Value = ItemPresence.Contained;
        return true;
    }

    private static bool TryAttach(Item item, NetworkObject parent)
    {
        return item.NetworkObject.TrySetParent(parent, true) && item.transform.parent == parent.transform;
    }

    private static bool IsServerRunning(string operation)
    {
        var manager = NetworkManager.Singleton;
        if (manager != null && manager.IsServer)
            return true;

        Debug.LogError($"[ItemMover] {operation} yalnizca sunucuda calisir.");
        return false;
    }
}
