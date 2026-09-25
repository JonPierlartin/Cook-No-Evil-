using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Tamamlanan hamburger ÖĞESİNİN katman listesi (GDD 6.7.3, CLAUDE.md hedef mimari: hamburger tek
// öğedir, katmanları kendi listesinde tutar; katman başına ağ nesnesi YOK). Her katman malzeme türünü
// ve pişmişlik fazını taşır (çiğ köfte hamburgere konabilir — GDD 5.2.1; teslimde birebir kontrol için
// içerik bilgisi korunur). Liste yalnızca sunucuda, doğumdan hemen sonra bir kez yazılır; hamburger
// yeniden sökülmez, içeriği değişmez.
//
// Görsel katman listesinden YEREL çizilir (BurgerStackBuilder — tezgahtaki yığınla ortak kural): dünya
// görseli bu bileşenin altında, elde/önizleme kopyaları IItemVisualSource.CreateVisual ile.
[RequireComponent(typeof(Item))]
public class BurgerAssembly : NetworkBehaviour, IItemVisualSource
{
    [Tooltip("Katman türlerinin id -> ItemType çözümü (görsel ve faz rengi için).")]
    [SerializeField] private ItemRegistry registry;

    public readonly NetworkList<BurgerLayerEntry> Layers = new();

    private Item _item;
    private readonly List<GameObject> _worldLayers = new();
    private Transform _worldRoot;
    private int _version;

    public event Action VisualChanged;

    public int VisualVersion => _version;

    private void Awake()
    {
        _item = GetComponent<Item>();

        var rootObject = new GameObject("HamburgerLayers");
        _worldRoot = rootObject.transform;
        _worldRoot.SetParent(transform, false);
    }

    public override void OnNetworkSpawn()
    {
        Layers.OnListChanged += HandleLayersChanged;
        RebuildWorldVisual();
    }

    public override void OnNetworkDespawn()
    {
        Layers.OnListChanged -= HandleLayersChanged;
        BurgerStackBuilder.Clear(_worldLayers);
    }

    // Sunucu: doğumdan sonra, envantere yazılmadan ÖNCE bir kez çağrılır.
    public void ServerSetLayers(IReadOnlyList<BurgerLayerEntry> layers)
    {
        if (!IsServer)
            return;

        Layers.Clear();
        foreach (var layer in layers)
            Layers.Add(layer);
    }

    private void HandleLayersChanged(NetworkListEvent<BurgerLayerEntry> change)
    {
        _version++;
        RebuildWorldVisual();
        VisualChanged?.Invoke();
    }

    private void RebuildWorldVisual()
    {
        BurgerStackBuilder.Clear(_worldLayers);
        BurgerStackBuilder.Build(Layers, registry, _worldRoot, _worldLayers);

        // Dinamik olusan renderer'lar Item'in Presence mantigina (Carried iken gizli, Placed iken acik) katilsin.
        _item.RefreshWorldVisual();
    }

    public GameObject CreateVisual(Transform parent)
    {
        var root = new GameObject("HamburgerVisual");
        root.transform.SetParent(parent, false);

        var created = new List<GameObject>();
        BurgerStackBuilder.Build(Layers, registry, root.transform, created);
        return root;
    }
}
