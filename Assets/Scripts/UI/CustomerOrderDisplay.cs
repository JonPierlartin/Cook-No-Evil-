using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

// Sipariş pop-up'ı (GDD 3.6.1): sipariş alınınca müşterinin üstünde, istenen ürünün FOTOĞRAFI (varyantın görseli —
// tarif kitapçığıyla aynı asset, BurgerVariant.Image) ve istenmeyen her malzeme için üstü çarpılı ikon. Müşteri
// teslim alana/ayrılana kadar kalır; ayrı bir fiş arayüzü yoktur. Yalnızca gösterir: içerik müşterinin replike
// siparişinden gelir (Customer.OrderVariantIndex, OrderMissingItemIds).
public class CustomerOrderDisplay : MonoBehaviour
{
    [SerializeField] private Customer customer;
    [SerializeField] private ItemRegistry registry;
    [Tooltip("Pop-up'ın kökü; sipariş alınmadan önce ve müşteri ayrılırken kapalıdır.")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private Image variantImage;
    [Tooltip("Eksik malzeme girişlerinin dizildiği kap.")]
    [SerializeField] private RectTransform missingContainer;
    [Tooltip("Eksik malzeme şablonu (kökünde malzeme ikonu, üstünde çarpı). Her eksik malzeme için kopyalanır.")]
    [SerializeField] private Image missingEntryTemplate;

    private readonly List<GameObject> _entries = new();
    private bool _dirty = true;

    private void OnEnable()
    {
        customer.OrderVariantIndex.OnValueChanged += HandleVariantChanged;
        customer.OrderMissingItemIds.OnListChanged += HandleMissingChanged;
        missingEntryTemplate.gameObject.SetActive(false);
        _dirty = true;
    }

    private void OnDisable()
    {
        customer.OrderVariantIndex.OnValueChanged -= HandleVariantChanged;
        customer.OrderMissingItemIds.OnListChanged -= HandleMissingChanged;
    }

    private void HandleVariantChanged(int previous, int current) => _dirty = true;
    private void HandleMissingChanged(NetworkListEvent<int> change) => _dirty = true;

    private void LateUpdate()
    {
        bool visible = customer.IsSpawned && customer.State.Value == CustomerState.Ordered;
        if (popupRoot.activeSelf != visible)
            popupRoot.SetActive(visible);

        if (!visible || !_dirty)
            return;

        _dirty = false;
        Rebuild();
    }

    private void Rebuild()
    {
        BurgerVariant variant = null;
        if (LevelDirector.Instance != null)
            LevelDirector.Instance.TryGetVariant(customer.OrderVariantIndex.Value, out variant);

        variantImage.sprite = variant != null ? variant.Image : null;
        variantImage.enabled = variantImage.sprite != null;

        foreach (var entry in _entries)
            Destroy(entry);
        _entries.Clear();

        var missing = customer.OrderMissingItemIds;
        for (int i = 0; i < missing.Count; i++)
        {
            var item = registry != null ? registry.Find(missing[i]) : null;
            if (item == null)
                continue;

            var entry = Instantiate(missingEntryTemplate, missingContainer);
            entry.sprite = item.Icon;
            entry.gameObject.SetActive(true);
            _entries.Add(entry.gameObject);
        }
    }
}
