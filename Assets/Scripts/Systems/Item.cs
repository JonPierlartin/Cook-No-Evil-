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
    }

    public override void OnNetworkDespawn()
    {
        Presence.OnValueChanged -= HandlePresenceChanged;
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
