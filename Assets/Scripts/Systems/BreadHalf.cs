using System;
using Unity.Netcode;
using UnityEngine;

// GDD 6.7.3: "Ekmek iki parçadır, tek envanter öğesidir." Alt kısım konunca ekmek envanterden düşmez,
// elde YARIM kalır; yalnızca yarım ekmek üst olarak konabilir. Bu bileşen o durumu taşır (sunucu
// sahipli, herkese replike — K6). Görsel: yarım ekmek, bütün ekmeğin belirlenen oranı kadar yüksek
// bir disk (yer tutucu; kök tabanda olduğu için kökün Y ölçeği tabanı sabit tutar). Dünya görseli,
// elde görsel ve önizleme AYNI hâli gösterir: dünya görselini bu bileşen ölçekler; elde/önizleme
// kopyalarını IItemVisualSource ile kendisi üretir.
[RequireComponent(typeof(Item))]
public class BreadHalf : NetworkBehaviour, IItemVisualSource
{
    [Tooltip("Öğenin dünya görselinin kökü (Ekmek_Item'ın çocuğu olan görsel prefab). Yarılanınca Y ölçeği küçülür.")]
    [SerializeField] private Transform worldVisualRoot;
    [Tooltip("Yarım ekmeğin bütün ekmeğe göre yüksekliği (yer tutucu görsel: 0,5). Şef konturdan bütün/yarım farkını okuyabilmeli (GDD 4.1.1).")]
    [SerializeField, Range(0.1f, 0.9f)] private float halvedHeightScale = 0.5f;

    // Alt kısım kondu mu. Yalnızca sunucu yazar.
    public readonly NetworkVariable<bool> IsHalved =
        new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private Item _item;

    public event Action VisualChanged;

    public int VisualVersion => IsHalved.Value ? 1 : 0;

    private void Awake()
    {
        _item = GetComponent<Item>();
    }

    public override void OnNetworkSpawn()
    {
        IsHalved.OnValueChanged += HandleHalvedChanged;
        ApplyWorldScale();
    }

    public override void OnNetworkDespawn()
    {
        IsHalved.OnValueChanged -= HandleHalvedChanged;
    }

    public void ServerMarkHalved()
    {
        if (!IsServer)
            return;

        IsHalved.Value = true;
    }

    private void HandleHalvedChanged(bool previous, bool current)
    {
        ApplyWorldScale();
        VisualChanged?.Invoke();
    }

    private void ApplyWorldScale()
    {
        if (worldVisualRoot != null)
            worldVisualRoot.localScale = new Vector3(1f, IsHalved.Value ? halvedHeightScale : 1f, 1f);
    }

    public GameObject CreateVisual(Transform parent)
    {
        var prefab = _item.Type != null ? _item.Type.VisualPrefab : null;
        if (prefab == null)
            return null;

        var visual = Instantiate(prefab, parent);
        if (IsHalved.Value)
        {
            var scale = visual.transform.localScale;
            visual.transform.localScale = new Vector3(scale.x, scale.y * halvedHeightScale, scale.z);
        }

        return visual;
    }
}
