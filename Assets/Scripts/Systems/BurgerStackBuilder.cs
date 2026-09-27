using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Bir katman listesini (BurgerLayerEntry) verilen kökün üstüne görünür bir yığın olarak dizen TEK
// yardımcı: birleştirme tezgahındaki yığın (BurgerStackVisual) ve tamamlanan hamburgerin görseli
// (BurgerAssembly, hem dünya görseli hem elde/önizleme kopyası) aynı kuralı kullanır. Katman için ağ
// nesnesi spawn edilmez, collider eklenmez. Her katman türün visualPrefab'ından üretilir; prefab kökü
// tabanda olduğu için (K2d) katmanın tabanı bir öncekinin ölçülen yüksekliği toplanarak üstüne oturur.
// Köftenin pişmişlik rengi (faz) ItemPhaseColoring ile uygulanır.
//
// D5 (25 Eyl 2026): olcum ve yerlesim ROOT'UN KENDI YEREL uzayinda yapilir, world-space DEGIL. root
// hangi ebeveynin altinda kurulursa kurulsun (elde tutma noktasi, onizleme koku, yuva) — o ebeveyn
// donuk/olcekli olsa BILE (orn. birinci sahis tutma noktasi PlayerController'in uyguladigi kamera
// pitch'iyle her kare doner) katmanlar bosluksuz/ortusmesiz dizilir. Onceki surum world-space
// `root.up * height` ile yerlestirip world-eksenine-hizali `Renderer.bounds.size.y` ile olcuyordu; bu
// ikisi yalnizca root donusu tam olarak world Y ile hizaliyken tutarliydi — pitch'li ankorde bir
// katman kalinligi kadar (~0,10-0,16 birim) ortusme/bosluk uretiyordu (olculdu, bkz. commit govdesi).
public static class BurgerStackBuilder
{
    // Katmanları root'un altına dizer, oluşan nesneleri created'a ekler, toplam yüksekliği (root'un
    // yerel Y ekseninde) döndürür.
    public static float Build(NetworkList<BurgerLayerEntry> layers, ItemRegistry registry, Transform root, List<GameObject> created)
    {
        float top = 0f;
        for (int i = 0; i < layers.Count; i++)
        {
            var type = registry != null ? registry.Find(layers[i].TypeId) : null;
            if (type == null || type.VisualPrefab == null)
                continue;

            // Once ROOT'A GORE kimlik donusumde kur (world pozisyon/rotasyon degil) — root'un kendisi
            // donuk/olcekli olsa da katman root ile AYNI yerel eksende durur.
            var layer = Object.Instantiate(type.VisualPrefab, root);
            layer.transform.localPosition = Vector3.zero;
            layer.transform.localRotation = Quaternion.identity;

            var renderers = layer.GetComponentsInChildren<Renderer>(true);
            ItemPhaseColoring.Apply(renderers, type, layers[i].PhaseIndex);
            created.Add(layer);

            MeasureLocalRange(root, renderers, out float minY, out float maxY);
            // Katmanin tabani (minY, K2d'de ~0 ama varsayilmaz) tam "top"a otursun.
            layer.transform.localPosition = new Vector3(0f, top - minY, 0f);
            top += maxY - minY;
        }

        return top;
    }

    public static void Clear(List<GameObject> created)
    {
        foreach (var layer in created)
        {
            if (layer == null)
                continue;

            // Destroy kare sonuna ertelenir; yeni yigin ayni karede kurulacagi icin eskiyi hemen gizle ve
            // kokten ayir — ayni karede GetComponentsInChildren (Item.RefreshWorldVisual) silinmeyi bekleyen
            // renderer'lari toplarsa kare sonunda yok olmus referans tutar (MissingReferenceException).
            layer.SetActive(false);
            layer.transform.SetParent(null, false);
            Object.Destroy(layer);
        }

        created.Clear();
    }

    // Renderer'larin (world-eksenine-hizali) sinir kutusu koselerini root'un YEREL uzayina tasiyip
    // oradaki Y araligini olcer. root donuk olsa bile katmanin root'a gore GERCEK dikey kaplamini
    // verir; yalnizca world Y okumak (eski surum) root donukken yanlis sonuc uretiyordu.
    private static void MeasureLocalRange(Transform root, Renderer[] renderers, out float minY, out float maxY)
    {
        minY = float.PositiveInfinity;
        maxY = float.NegativeInfinity;

        foreach (var renderer in renderers)
        {
            var bounds = renderer.bounds;
            var center = bounds.center;
            var extents = bounds.extents;

            for (int xi = -1; xi <= 1; xi += 2)
            for (int yi = -1; yi <= 1; yi += 2)
            for (int zi = -1; zi <= 1; zi += 2)
            {
                var corner = center + new Vector3(extents.x * xi, extents.y * yi, extents.z * zi);
                float localY = root.InverseTransformPoint(corner).y;
                if (localY < minY) minY = localY;
                if (localY > maxY) maxY = localY;
            }
        }

        if (float.IsPositiveInfinity(minY))
        {
            minY = 0f;
            maxY = 0f;
        }
    }
}
