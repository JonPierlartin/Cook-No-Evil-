using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Birleştirme tezgahındaki yığını (BurgerAssemblyStation.PlacedIngredients) her istemcide YEREL olarak
// çizer — geri bildirim kuralı: konan malzeme tezgahta görünür ve üst üste dizilir. Katman çizimi
// BurgerStackBuilder'da (hamburger öğesiyle ortak). Yığın büyüdükçe yerleştirme noktası (önizleme) da
// yığının ÜSTÜNE taşınır (GDD 4.1.2: yığılan hedeflerde önizleme birikmiş katmanın üstünde belirir).
[RequireComponent(typeof(BurgerAssemblyStation))]
public class BurgerStackVisual : NetworkBehaviour
{
    [Tooltip("Katmanların dizildiği taban (tezgah yüzeyi). Yerleştirme noktasından AYRI, sabit bir çocuk olmalı.")]
    [SerializeField] private Transform stackRoot;
    [Tooltip("PlacementTarget'ın bulunduğu nokta: yığının üstüne taşınır (önizleme buradan çıkar).")]
    [SerializeField] private Transform placementPoint;

    private BurgerAssemblyStation _station;
    private readonly List<GameObject> _layers = new();

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
        BurgerStackBuilder.Clear(_layers);
    }

    private void HandleListChanged(NetworkListEvent<BurgerLayerEntry> change) => Rebuild();

    // Liste küçük (birkaç katman) ve nadiren değişir: her değişimde baştan kurmak en basit doğru yol.
    private void Rebuild()
    {
        BurgerStackBuilder.Clear(_layers);
        float height = BurgerStackBuilder.Build(_station.PlacedIngredients, _station.Registry, stackRoot, _layers);

        if (placementPoint != null)
            placementPoint.position = stackRoot.position + stackRoot.up * height;
    }
}
