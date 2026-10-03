using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Kese kağıdı / paket (GDD 5.3.1). İçindekiler GERÇEK öğe olarak paketin içinde kalır: paketin ağ nesnesinin
// çocuğudurlar (ItemMover.PackInto) ve kendi durumlarını (hamburgerin katmanları, faz) taşırlar — "yok edip listeye
// yaz" yolu kullanılmaz. Bu yüzden içerik için ayrı bir ağ alanı YOKTUR: içerik = paketin çocuğu olan Item'lar,
// sunucuda da istemcide de (replike hiyerarşi) aynı yerden okunur. Paket N öğe alır; hamburgere özel değildir.
//
// Görsel hâl ve fotoğraf içerikten YEREL türetilir (ağdan bir şey gitmez):
//  - paketleme alanında (ebeveyni PackingArea) -> ağzı açık
//  - alan dışında ve içi dolu                   -> ağzı kapalı
//  - alan dışında ve boş                        -> katlı
// Fotoğraf: PackagePhoto (sipariş verisini okumaz).
[RequireComponent(typeof(Item))]
public class Package : NetworkBehaviour, IItemVisualSource
{
    [Tooltip("Katman türlerinin id -> ItemType çözümü (fotoğraf için).")]
    [SerializeField] private ItemRegistry registry;

    private Item _item;
    private PackageVisualParts _worldParts;
    private int _version;
    private bool _dirty;

    public event Action VisualChanged;

    public int VisualVersion => _version;

    private void Awake()
    {
        _item = GetComponent<Item>();
        // Dünya görseli, öğe prefab'ının çocuğu olan KeseKagidi_Visual örneğidir (visualPrefab ile aynı kaynak).
        _worldParts = GetComponentInChildren<PackageVisualParts>(true);
    }

    public override void OnNetworkSpawn()
    {
        _item.Presence.OnValueChanged += HandlePresenceChanged;
        // İçerideki öğe pakete bağlandıktan SONRA spawn olabilir (geç katılan istemci); o da fotoğrafı değiştirir.
        Item.NetworkSpawned += HandleAnyItemSpawned;
        _dirty = true;
    }

    public override void OnNetworkDespawn()
    {
        _item.Presence.OnValueChanged -= HandlePresenceChanged;
        Item.NetworkSpawned -= HandleAnyItemSpawned;
    }

    private void HandlePresenceChanged(ItemPresence previous, ItemPresence current) => _dirty = true;
    private void HandleAnyItemSpawned(Item item) => _dirty = true;

    // Ebeveyn (alana kondu / alındı) ve çocuklar (ürün eklendi) hem sunucuda hem istemcide buradan yakalanır.
    private void OnTransformParentChanged() => _dirty = true;
    private void OnTransformChildrenChanged() => _dirty = true;

    private void LateUpdate()
    {
        if (!_dirty)
            return;

        _dirty = false;
        _version++;
        ApplyWorldVisual();
        VisualChanged?.Invoke();
    }

    // Paketin içindeki öğeler (doğrudan çocuk olan Item'lar).
    public void GetContents(List<Item> results)
    {
        results.Clear();
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).TryGetComponent(out Item item))
                results.Add(item);
        }
    }

    public bool IsEmpty
    {
        get
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i).TryGetComponent<Item>(out _))
                    return false;
            }

            return true;
        }
    }

    // Ağzı açık mı: yalnızca paketleme alanındayken ürün eklenebilir (GDD 5.3.1).
    public bool IsOpen => transform.parent != null && transform.parent.TryGetComponent<PackingArea>(out _);

    private PackageVisualParts.State CurrentState =>
        IsOpen ? PackageVisualParts.State.Open : (IsEmpty ? PackageVisualParts.State.Folded : PackageVisualParts.State.Closed);

    // Fotoğraf: paketin GERÇEK içeriği + bu seviyenin varyantları. Sipariş verisi okunmaz.
    public List<PackagePhoto.Entry> BuildPhoto()
    {
        var entries = new List<PackagePhoto.Entry>();
        var variants = LevelDirector.Instance != null && LevelDirector.Instance.Config != null
            ? LevelDirector.Instance.Config.CollectVariants()
            : new List<BurgerVariant>();

        var contents = new List<Item>();
        GetContents(contents);
        foreach (var content in contents)
        {
            if (content.TryGetComponent(out BurgerAssembly burger))
            {
                var ingredients = new List<ItemType>();
                for (int i = 0; i < burger.Layers.Count; i++)
                    ingredients.Add(registry != null ? registry.Find(burger.Layers[i].TypeId) : null);
                entries.Add(PackagePhoto.EvaluateBurger(ingredients, variants));
            }
            else if (content.Type != null)
            {
                entries.Add(PackagePhoto.EvaluatePlain(content.Type));
            }
        }

        return entries;
    }

    private void ApplyWorldVisual()
    {
        if (_worldParts == null)
            return;

        // Fotoğraf bir Canvas'tır (Renderer değil), Item'ın Presence mantığına katılmaz: elde taşınırken dünya
        // görselinin tamamı kapatılır (elde görünen, HeldItemVisual'ın CreateVisual kopyasıdır).
        bool placed = _item.Presence.Value == ItemPresence.Placed;
        _worldParts.gameObject.SetActive(placed);
        if (!placed)
            return;

        _worldParts.Show(CurrentState);
        _worldParts.ShowPhoto(BuildPhoto());
    }

    public GameObject CreateVisual(Transform parent)
    {
        var prefab = _item.Type != null ? _item.Type.VisualPrefab : null;
        if (prefab == null)
            return null;

        var visual = Instantiate(prefab, parent);
        visual.SetActive(true);
        if (visual.TryGetComponent(out PackageVisualParts parts))
        {
            parts.Show(CurrentState);
            parts.ShowPhoto(BuildPhoto());
        }

        return visual;
    }
}
