using Unity.Netcode;
using UnityEngine;

// Taşınabilir her ogenin AG KABUGU (CLAUDE.md "Hedef mimari — Adim 4.0"): kokte NetworkObject + Item,
// cocuk olarak ItemType.visualPrefab. Sunucu sahiplidir (K6): tur prefab'a gomulu oldugu icin agdan
// gonderilmez, degisen tek sey Presence'tir ve yalnizca sunucu yazar. Ogeye collider KONMAZ —
// yuvaya tiklanir, ogeye degil.
//
// Presence dunya gorselini (Renderer'lari) surer: Carried iken kapali, Placed iken acik. Varsayilan
// Carried'dir; Awake'te gorsel kapatilir, boylece spawn edilen oge baslangic konumunda bir kare bile
// gorunmez. Gec katilan/rejoin eden istemcide gercek deger OnNetworkSpawn'da uygulanir.
[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkObject))]
public class Item : NetworkBehaviour
{
    [Tooltip("Bu prefab'in ogesinin turu. Prefab'a gomulu; agdan gonderilmez. ItemType.itemPrefab bu prefab'i gostermeli.")]
    [SerializeField] private ItemType type;

    public readonly NetworkVariable<ItemPresence> Presence =
        new(ItemPresence.Carried, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Bir oge bu istemcide spawn/despawn oldugunda tetiklenir. Envanter listesi ogeden ONCE gelebilir
    // (iki mesaj bagimsiz islenir); elde tutulan ogeyi olay-surumlu cizen tuketiciler (HeldItemVisual)
    // gec gelen ogeyi buradan yakalar. Despawn olayi, oge hala SpawnedObjects'teyken tetiklenir.
    public static event System.Action<Item> NetworkSpawned;
    public static event System.Action<Item> NetworkDespawned;

    private Renderer[] _renderers;

    public ItemType Type => type;

    private void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
        SetWorldVisualVisible(false);
    }

    public override void OnNetworkSpawn()
    {
        ApplyPresence(Presence.Value);
        Presence.OnValueChanged += HandlePresenceChanged;
        NetworkSpawned?.Invoke(this);
    }

    public override void OnNetworkDespawn()
    {
        Presence.OnValueChanged -= HandlePresenceChanged;
        NetworkDespawned?.Invoke(this);
    }

    // Gorseli calisma zamaninda olusturulan/degisen ogeler (BurgerAssembly) renderer'lari yeniden
    // toplatir; boylece yeni renderer'lar da Presence'a (Carried iken gizli, Placed iken acik) uyar.
    public void RefreshWorldVisual()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
        ApplyPresence(Presence.Value);
    }

    private void HandlePresenceChanged(ItemPresence previous, ItemPresence current) => ApplyPresence(current);

    private void ApplyPresence(ItemPresence presence)
    {
        SetWorldVisualVisible(presence == ItemPresence.Placed);
    }

    private void SetWorldVisualVisible(bool visible)
    {
        foreach (var renderer in _renderers)
            renderer.enabled = visible;
    }
}
