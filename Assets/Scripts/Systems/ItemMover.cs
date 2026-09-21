using Unity.Netcode;
using UnityEngine;

// Ogelerin spawn / despawn'inin TEK yeri (CLAUDE.md "Hedef mimari — Adim 4.0"). Yalnizca sunucuda
// calisir. Basarisizlik KOSULSUZ Debug.LogError basar: NGO'nun kendi basarisizlik loglari (orn.
// TrySetParent) varsayilan log seviyesinde sessizdir, teshisi imkansiz kilar. Adim 4.2'de parent
// islemleri (TrySetParent / TryRemoveParent) de buraya eklenecek — baska hicbir yerde cagrilmaz.
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
    public static bool Despawn(Item item)
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

    private static bool IsServerRunning(string operation)
    {
        var manager = NetworkManager.Singleton;
        if (manager != null && manager.IsServer)
            return true;

        Debug.LogError($"[ItemMover] {operation} yalnizca sunucuda calisir.");
        return false;
    }
}
