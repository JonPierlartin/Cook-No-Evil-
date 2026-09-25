using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Bir katman listesini (BurgerLayerEntry) verilen kökün üstüne görünür bir yığın olarak dizen TEK
// yardımcı: birleştirme tezgahındaki yığın (BurgerStackVisual) ve tamamlanan hamburgerin görseli
// (BurgerAssembly) aynı kuralı kullanır. Katman için ağ nesnesi spawn edilmez, collider eklenmez.
// Her katman türün visualPrefab'ından üretilir; prefab kökü tabanda olduğu için (K2d) katmanın tabanı
// bir öncekinin ölçülen sınır kutusu yüksekliği toplanarak üstüne oturur. Köftenin pişmişlik rengi
// (faz) ItemPhaseColoring ile uygulanır.
public static class BurgerStackBuilder
{
    // Katmanları root'un altına dizer, oluşan nesneleri created'a ekler, toplam yüksekliği döndürür.
    public static float Build(NetworkList<BurgerLayerEntry> layers, ItemRegistry registry, Transform root, List<GameObject> created)
    {
        float height = 0f;
        for (int i = 0; i < layers.Count; i++)
        {
            var type = registry != null ? registry.Find(layers[i].TypeId) : null;
            if (type == null || type.VisualPrefab == null)
                continue;

            var layer = Object.Instantiate(type.VisualPrefab, root.position + root.up * height, root.rotation, root);
            var renderers = layer.GetComponentsInChildren<Renderer>(true);
            ItemPhaseColoring.Apply(renderers, type, layers[i].PhaseIndex);
            created.Add(layer);

            height += MeasureHeight(renderers);
        }

        return height;
    }

    public static void Clear(List<GameObject> created)
    {
        foreach (var layer in created)
        {
            if (layer == null)
                continue;

            // Destroy kare sonuna ertelenir; yeni yigin ayni karede kurulacagi icin eskiyi hemen gizle.
            layer.SetActive(false);
            Object.Destroy(layer);
        }

        created.Clear();
    }

    private static float MeasureHeight(Renderer[] renderers)
    {
        if (renderers.Length == 0)
            return 0f;

        var bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds.size.y;
    }
}
