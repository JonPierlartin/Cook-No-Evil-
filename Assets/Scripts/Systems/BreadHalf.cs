using System;
using Unity.Netcode;
using UnityEngine;

// GDD 6.7.3: "Ekmek iki parçadır, tek envanter öğesidir." Alt kısım konunca ekmek envanterden düşmez,
// elde YARIM kalır; yalnızca yarım ekmek üst olarak konabilir. Bu bileşen o durumu taşır (sunucu
// sahipli, herkese replike — K6). Görsel: bütünken alt + üst ekmek üst üste, yarılanınca YALNIZCA üst
// ekmek (GDD 6.7.3 "Ekmeğin görseli"); parçaları BreadVisualParts gösterir. Dünya görseli, elde görsel
// ve önizleme AYNI hâli gösterir: dünya görselini bu bileşen ayarlar; elde/önizleme kopyalarını
// IItemVisualSource ile kendisi üretir.
[RequireComponent(typeof(Item))]
public class BreadHalf : NetworkBehaviour, IItemVisualSource
{
    // Alt kısım kondu mu. Yalnızca sunucu yazar.
    public readonly NetworkVariable<bool> IsHalved =
        new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private Item _item;
    private BreadVisualParts _worldParts;

    public event Action VisualChanged;

    public int VisualVersion => IsHalved.Value ? 1 : 0;

    private BreadVisualParts.Part CurrentPart => IsHalved.Value ? BreadVisualParts.Part.Top : BreadVisualParts.Part.Whole;

    private void Awake()
    {
        _item = GetComponent<Item>();
        // Dünya görseli, öğe prefab'ının çocuğu olan Ekmek_Visual örneğidir (visualPrefab ile aynı kaynak).
        _worldParts = GetComponentInChildren<BreadVisualParts>(true);
        if (_worldParts == null)
            Debug.LogWarning($"[BreadHalf] '{name}': dünya görselinde BreadVisualParts yok; alt/üst ekmek ayrımı görünmeyecek.", this);
    }

    public override void OnNetworkSpawn()
    {
        IsHalved.OnValueChanged += HandleHalvedChanged;
        ApplyWorldVisual();
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
        ApplyWorldVisual();
        VisualChanged?.Invoke();
    }

    private void ApplyWorldVisual()
    {
        if (_worldParts != null)
            _worldParts.Show(CurrentPart);
    }

    public GameObject CreateVisual(Transform parent)
    {
        var prefab = _item.Type != null ? _item.Type.VisualPrefab : null;
        if (prefab == null)
            return null;

        var visual = Instantiate(prefab, parent);
        if (visual.TryGetComponent(out BreadVisualParts parts))
            parts.Show(CurrentPart);

        return visual;
    }
}
