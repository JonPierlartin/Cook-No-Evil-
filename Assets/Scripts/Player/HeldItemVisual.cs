using Unity.Netcode;
using UnityEngine;

// GDD 4.1 "Elde tutulan nesne gorunur": her istemcide, HER oyuncu icin, o oyuncunun aktif
// slotundaki ogenin visualPrefab'ini bir tutma noktasinda gosterir. Sahip kendi elindekini
// birinci sahis noktasinda (kameranin cocugu), diger oyuncular ucuncu sahis noktasinda
// (govdenin cocugu) gorur — sahip icin ucuncu sahis noktasi kullanilmaz, yani cift gorunum yok.
//
// Tamamen yerel ve replike veriden calisir: PlayerInventory.Slots ve ActiveSlotIndex zaten
// herkese replike; buradan ag uzerinden hicbir sey gonderilmez. Gorsel yalnizca gosterilecek
// oge (veya tutma noktasi) DEGISINCE yeniden olusturulur, her karede degil.
[RequireComponent(typeof(PlayerInventory))]
public class HeldItemVisual : NetworkBehaviour
{
    [Tooltip("Id -> ItemType cozumlemesi icin TEK kayit defteri (tum tuketicilerle ortak asset).")]
    [SerializeField] private ItemRegistry registry;
    [Tooltip("Birinci sahis tutma noktasi (oyuncu kamerasinin cocugu). Yalnizca sahip gorur.")]
    [SerializeField] private Transform firstPersonHoldPoint;
    [Tooltip("Ucuncu sahis tutma noktasi (govde gorselinin cocugu, el hizasi). Yalnizca diger oyuncular gorur.")]
    [SerializeField] private Transform thirdPersonHoldPoint;

    private PlayerInventory _inventory;
    private bool _isLocalOwner;
    private GameObject _instance;
    private int _shownId = PlayerInventory.EmptySlot;
    private Transform _shownAnchor;

    public override void OnNetworkSpawn()
    {
        _inventory = GetComponent<PlayerInventory>();
        _isLocalOwner = IsOwner;

        _inventory.Slots.OnListChanged += HandleSlotsChanged;
        _inventory.ActiveSlotIndex.OnValueChanged += HandleActiveSlotChanged;

        Refresh();
    }

    public override void OnNetworkDespawn()
    {
        _inventory.Slots.OnListChanged -= HandleSlotsChanged;
        _inventory.ActiveSlotIndex.OnValueChanged -= HandleActiveSlotChanged;

        ClearInstance();
        _shownId = PlayerInventory.EmptySlot;
        _shownAnchor = null;
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

    private void HandleSlotsChanged(NetworkListEvent<int> change) => Refresh();

    private void HandleActiveSlotChanged(int previous, int current) => Refresh();

    private void Refresh()
    {
        var anchor = _isLocalOwner ? firstPersonHoldPoint : thirdPersonHoldPoint;
        int id = GetActiveItemId();

        if (id == _shownId && anchor == _shownAnchor)
            return;

        ClearInstance();
        _shownId = id;
        _shownAnchor = anchor;

        // Bos slot, kayitsiz id, gorseli olmayan oge veya atanmamis nokta: hata degil, el bos.
        var itemType = registry != null ? registry.Find(id) : null;
        if (itemType == null || itemType.VisualPrefab == null || anchor == null)
            return;

        _instance = Instantiate(itemType.VisualPrefab, anchor);
    }

    private int GetActiveItemId()
    {
        int index = _inventory.ActiveSlotIndex.Value;
        if (index < 0 || index >= _inventory.Slots.Count)
            return PlayerInventory.EmptySlot;

        return _inventory.Slots[index];
    }

    private void ClearInstance()
    {
        if (_instance != null)
            Destroy(_instance);

        _instance = null;
    }
}
