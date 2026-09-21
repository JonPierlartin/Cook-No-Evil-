using Unity.Netcode;
using UnityEngine;

// GDD 4.1 "Elde tutulan nesne gorunur": her istemcide, HER oyuncu icin, o oyuncunun aktif
// slotundaki ogenin visualPrefab'ini bir tutma noktasinda gosterir. Sahip kendi elindekini
// birinci sahis noktasinda (kameranin cocugu), diger oyuncular ucuncu sahis noktasinda
// (govdenin cocugu) gorur — sahip icin ucuncu sahis noktasi kullanilmaz, yani cift gorunum yok.
//
// Tamamen yerel ve replike veriden calisir: PlayerInventory.Slots, ActiveSlotIndex ve spawn edilmis
// ogeler zaten herkese replike; buradan ag uzerinden hicbir sey gonderilmez. Gorsel yalnizca
// gosterilecek ogenin turu (veya tutma noktasi) DEGISINCE yeniden olusturulur, her karede degil.
// Slot listesi ogeden ONCE gelebilir: oge bu istemcide spawn/despawn olunca da yeniden bakilir.
[RequireComponent(typeof(PlayerInventory))]
public class HeldItemVisual : NetworkBehaviour
{
    [Tooltip("Birinci sahis tutma noktasi (oyuncu kamerasinin cocugu). Yalnizca sahip gorur.")]
    [SerializeField] private Transform firstPersonHoldPoint;
    [Tooltip("Ucuncu sahis tutma noktasi (govde gorselinin cocugu, el hizasi). Yalnizca diger oyuncular gorur.")]
    [SerializeField] private Transform thirdPersonHoldPoint;

    private PlayerInventory _inventory;
    private bool _isLocalOwner;
    private bool _subscribed;
    private GameObject _instance;
    private ItemType _shownType;
    private Transform _shownAnchor;

    public override void OnNetworkSpawn()
    {
        _inventory = GetComponent<PlayerInventory>();
        _isLocalOwner = IsOwner;

        Subscribe();
        Refresh();
    }

    public override void OnNetworkDespawn()
    {
        Unsubscribe();

        ClearInstance();
        _shownType = null;
        _shownAnchor = null;
    }

    // Item.NetworkSpawned/NetworkDespawned STATIK: bu bilesen yok olurken OnNetworkDespawn garanti
    // cagrilmaz (NGO kapanisi/yok etme sirasi), abonelik kalirsa sonraki her oge olayi yok olmus
    // bileseni cagirir. Bu yuzden OnDestroy da (idempotent) aboneligi birakir.
    public override void OnDestroy()
    {
        Unsubscribe();
        base.OnDestroy();
    }

    private void Subscribe()
    {
        if (_subscribed)
            return;

        _subscribed = true;
        _inventory.Slots.OnListChanged += HandleSlotsChanged;
        _inventory.ActiveSlotIndex.OnValueChanged += HandleActiveSlotChanged;
        Item.NetworkSpawned += HandleItemSpawned;
        Item.NetworkDespawned += HandleItemDespawned;
    }

    private void Unsubscribe()
    {
        if (!_subscribed)
            return;

        _subscribed = false;
        if (_inventory != null)
        {
            _inventory.Slots.OnListChanged -= HandleSlotsChanged;
            _inventory.ActiveSlotIndex.OnValueChanged -= HandleActiveSlotChanged;
        }

        Item.NetworkSpawned -= HandleItemSpawned;
        Item.NetworkDespawned -= HandleItemDespawned;
    }

    // Round-ici rejoin'de obje yeniden spawn edilmez, yalnizca sahiplik degisir (bkz.
    // PlayerController). NGO, DontDestroyWithOwner objenin sahibi kopunca sahipligi
    // ServerClientId'ye OTOMATIK devreder ve host'ta IsOwner'i yanlislikla true yapar —
    // bu "sahipsiz kaldi"dir, yerel sahiplik degildir; o obje host'ta ucuncu sahis gorunmeli.
    protected override void OnOwnershipChanged(ulong previous, ulong current)
    {
        _isLocalOwner = current != NetworkManager.ServerClientId && IsOwner;
        Refresh();
    }

    private void HandleSlotsChanged(NetworkListEvent<ItemSlotEntry> change) => Refresh();

    private void HandleActiveSlotChanged(int previous, int current) => Refresh();

    // Liste ogeden once gelmisse (aktif slot dolu ama oge henuz yok) el simdilik bos gorunur; oge
    // spawn olunca burasi tamamlar. Baska ogeler icin de tetiklenir — Refresh degisiklik yoksa hicbir
    // sey yapmaz, bu yuzden ucuzdur.
    private void HandleItemSpawned(Item item) => Refresh();

    // Despawn olayi oge HALA SpawnedObjects'teyken tetiklenir; bu yuzden yok edilen oge cozumden dislanir.
    private void HandleItemDespawned(Item item) => Refresh(ignoredItem: item);

    private void Refresh(Item ignoredItem = null)
    {
        var anchor = _isLocalOwner ? firstPersonHoldPoint : thirdPersonHoldPoint;
        var itemType = _inventory.TryGetActiveItem(out var item) && item != ignoredItem ? item.Type : null;

        if (itemType == _shownType && anchor == _shownAnchor)
            return;

        ClearInstance();
        _shownType = itemType;
        _shownAnchor = anchor;

        // Bos slot, gorseli olmayan oge veya atanmamis nokta: hata degil, el bos.
        if (itemType == null || itemType.VisualPrefab == null || anchor == null)
            return;

        _instance = Instantiate(itemType.VisualPrefab, anchor);
    }

    private void ClearInstance()
    {
        if (_instance != null)
            Destroy(_instance);

        _instance = null;
    }
}
