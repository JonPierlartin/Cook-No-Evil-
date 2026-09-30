using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Bir katman listesini (BurgerLayerEntry) verilen kökün üstüne görünür bir yığın olarak dizen TEK
// yardımcı: birleştirme tezgahındaki yığın (BurgerStackVisual) ve tamamlanan hamburgerin görseli
// (BurgerAssembly, hem dünya görseli hem elde/önizleme kopyası) aynı kuralı kullanır. Katman için ağ
// nesnesi spawn edilmez, collider eklenmez. Her katman türün visualPrefab'ından üretilir; prefab kökü
// tabanda olduğu için (K2d) katmanın tabanı bir öncekinin ölçülen yüksekliği toplanarak üstüne oturur.
// Köftenin pişmişlik rengi (faz) ItemPhaseColoring ile uygulanır. Ekmek katmanı alt/üst parçasını
// yığındaki konumundan seçer (BreadVisualParts).
//
// D5 (25 Eyl 2026): olcum ve yerlesim ROOT'UN KENDI YEREL uzayinda yapilir, world-space DEGIL. root
// hangi ebeveynin altinda kurulursa kurulsun (elde tutma noktasi, onizleme koku, yuva) — o ebeveyn
// donuk/olcekli olsa BILE (orn. birinci sahis tutma noktasi PlayerController'in uyguladigi kamera
// pitch'iyle her kare doner) katmanlar bosluksuz/ortusmesiz dizilir.
//
// D6 (30 Eyl 2026): D5'in MeasureLocalRange'i world-AXIS-ALIGNED `Renderer.bounds`'un koselerini
// root'un yerel uzayina tasiyordu — bu koseler zaten EKSENE HIZALAMA ile SISMISTI (donuk bir mesh'in
// dunya AABB'si gercek kalinligindan buyuktur), donusum bunu GERI ALAMAZ. D5'in kendi "sonra: 0"
// ölçümü GECERSIZDI cunku dogrulama ayni (yanlis) fonksiyonu kullaniyordu — kendi kendini dogrulayan
// bir olcum. GERCEK oyun testinde (b11e568 sonrasi) katman boslugu KUCULMEDI, BUYUDU (bakis acisi
// dikeldikce sisme artiyor). Bagimsiz dogrulama (gercek mesh vertex'leriyle, hicbir bounds kisayolu
// olmadan) bunu dogruladi: 30 derece pitch'te bosluk 0,29 birim (bir katmanin ~2 kati). Duzeltme:
// `Renderer.bounds` (dunya AABB) hicbir yerde CAGRILMAZ; onun yerine `Renderer.localBounds` (mesh'in
// KENDI yerel uzayinda, eksene-hizalama kaybı OLMAYAN kutusu) kullanilir, koseleri
// `renderer.transform.TransformPoint` (dunyaya) sonra `root.InverseTransformPoint` (root'un yereline)
// ile tasinir. Bu bilesim matematiksel olarak yalnizca renderer'in ROOT'A GORE SABIT yerel donusumunu
// uygular (root.worldToLocal * renderer.localToWorld ifadesinde root'un KENDI dunya donusumu iptal
// olur) — root ne kadar donuk/olcekli olursa olsun ayni, DOGRU sonucu verir.
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

            // Ekmek (GDD 6.7.3): yığının ilk katmanı ALT, sonraki (hamburgeri kapatan) katman ÜST ekmektir.
            // Hangisi olduğu ağdan gelmez, katmanın yığındaki konumundan türetilir.
            if (layer.TryGetComponent(out BreadVisualParts bread))
                bread.Show(i == 0 ? BreadVisualParts.Part.Bottom : BreadVisualParts.Part.Top);

            // Yalnızca görünen parçalar ölçülür ve boyanır (gizlenen ekmek parçası yüksekliğe katılmaz).
            var renderers = CollectShownRenderers(layer.transform);
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

    // Katmanın içinde KAPATILMAMIŞ parçaların renderer'ları. GetComponentsInChildren(false) kullanılmaz:
    // o, ebeveyn zincirinin tamamına bakar — katman etkin olmayan bir ebeveynin altında kurulursa hiçbir
    // renderer döndürmez ve yığın yüksekliği sıfırlanır. Burada yalnızca katmanın KENDİ içindeki
    // activeSelf zinciri önemlidir.
    private static Renderer[] CollectShownRenderers(Transform layer)
    {
        var all = layer.GetComponentsInChildren<Renderer>(true);
        var shown = new List<Renderer>(all.Length);
        foreach (var renderer in all)
        {
            bool active = true;
            for (var t = renderer.transform; t != null && t != layer.parent; t = t.parent)
            {
                if (!t.gameObject.activeSelf)
                {
                    active = false;
                    break;
                }
            }

            if (active)
                shown.Add(renderer);
        }

        return shown.ToArray();
    }

    // Renderer'in KENDI yerel sinir kutusunu (mesh-net, eksene-hizalama kaybı OLMAYAN) alip
    // koselerini root'un yerel uzayina tasir. Renderer.bounds (dunya AABB) KULLANILMAZ — D6'da
    // bulundu: donuk bir mesh'in dunya-eksenine-hizali kutusu gercek kalinligindan buyuk cikar, bu
    // sismeyi sonraki donusum GERI ALAMAZ (bkz. sinif basi yorum).
    private static void MeasureLocalRange(Transform root, Renderer[] renderers, out float minY, out float maxY)
    {
        minY = float.PositiveInfinity;
        maxY = float.NegativeInfinity;

        foreach (var renderer in renderers)
        {
            var localBounds = renderer.localBounds;
            var center = localBounds.center;
            var extents = localBounds.extents;

            for (int xi = -1; xi <= 1; xi += 2)
            for (int yi = -1; yi <= 1; yi += 2)
            for (int zi = -1; zi <= 1; zi += 2)
            {
                var localCorner = center + new Vector3(extents.x * xi, extents.y * yi, extents.z * zi);
                var worldCorner = renderer.transform.TransformPoint(localCorner);
                float localY = root.InverseTransformPoint(worldCorner).y;
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
