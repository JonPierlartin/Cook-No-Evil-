using Unity.Netcode;
using UnityEngine;

// Birleştirme tezgahındaki yığını (BurgerAssemblyStation.PlacedIngredients) her istemcide YEREL olarak
// çizer — geri bildirim kuralı: konan malzeme tezgahta görünür ve üst üste dizilir. Katman için ağ
// nesnesi spawn edilmez ve collider eklenmez (hamburger tek öğe olacak; nişan ışını yığına takılmaz).
//
// Yükseklik: her katman türün visualPrefab'ından üretilir; prefab kökü tabanda olduğu için (K2d) her
// katmanın tabanı bir öncekinin üstüne, ölçülen sınır kutusu yüksekliği toplanarak oturur — yüzeye
// gömülmez. Yığın büyüdükçe yerleştirme noktası (önizleme) da yığının ÜSTÜNE taşınır (GDD 4.1.2:
// yığılan hedeflerde önizleme birikmiş katmanın üstünde belirir).
[RequireComponent(typeof(BurgerAssemblyStation))]
public class BurgerStackVisual : NetworkBehaviour
{
    [Tooltip("Katmanların dizildiği taban (tezgah yüzeyi). Yerleştirme noktasından AYRI, sabit bir çocuk olmalı.")]
    [SerializeField] private Transform stackRoot;
    [Tooltip("PlacementTarget'ın bulunduğu nokta: yığının üstüne taşınır (önizleme buradan çıkar).")]
    [SerializeField] private Transform placementPoint;

    private BurgerAssemblyStation _station;
    private GameObject[] _layers = System.Array.Empty<GameObject>();

    private void Awake()
    {
        _station = GetComponent<BurgerAssemblyStation>();
    }

    public override void OnNetworkSpawn()
    {
        _station.PlacedIngredients.OnListChanged += HandleListChanged;
        Rebuild();
    }

    public override void OnNetworkDespawn()
    {
        _station.PlacedIngredients.OnListChanged -= HandleListChanged;
        ClearLayers();
    }

    private void HandleListChanged(NetworkListEvent<BurgerLayerEntry> change) => Rebuild();

    // Liste küçük (birkaç katman) ve nadiren değişir: her değişimde baştan kurmak en basit doğru yol.
    private void Rebuild()
    {
        ClearLayers();

        var registry = _station.Registry;
        var list = _station.PlacedIngredients;
        _layers = new GameObject[list.Count];

        float height = 0f;
        for (int i = 0; i < list.Count; i++)
        {
            var type = registry != null ? registry.Find(list[i].TypeId) : null;
            if (type == null || type.VisualPrefab == null)
                continue;

            var layer = Instantiate(type.VisualPrefab, stackRoot.position + stackRoot.up * height, stackRoot.rotation, stackRoot);
            var renderers = layer.GetComponentsInChildren<Renderer>(true);
            ItemPhaseColoring.Apply(renderers, type, list[i].PhaseIndex);
            _layers[i] = layer;

            height += MeasureHeight(renderers);
        }

        if (placementPoint != null)
            placementPoint.position = stackRoot.position + stackRoot.up * height;
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

    private void ClearLayers()
    {
        foreach (var layer in _layers)
        {
            if (layer == null)
                continue;

            // Destroy kare sonuna ertelenir; yeni yigin ayni karede kurulacagi icin eskiyi hemen gizle.
            layer.SetActive(false);
            Destroy(layer);
        }

        _layers = System.Array.Empty<GameObject>();
    }
}
