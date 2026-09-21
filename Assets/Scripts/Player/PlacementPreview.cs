using UnityEngine;
using UnityEngine.Rendering;

// GDD 4.1.2 (2) Yerlestirme Onizlemesi: crosshair bir PlacementTarget'a bakarken VE o hedef icin
// "kullanilabilir" iken, hedefin noktasinda elindeki nesnenin sekilde yesil yari saydam bir kopyasi
// gorunur. Yeni kural yazilmaz: hedef ve durum PlayerInteractor'in mevcut (tek nisan taramali) her
// kare hesabindan okunur — menzil ve CanInteract zaten "kullanilabilir"in icindedir. Sirtini
// donunce hedef degistigi icin onizleme kendiliginden kaybolur.
//
// Sekil, aktif slottaki ogenin visualPrefab'idir (elde tutulanla AYNI gorsel); ItemRegistry
// uzerinden bulunur. Kopya yalnizca aktif oge DEGISINCE olusturulur; her karede yalnizca konum ve
// gorunurluk guncellenir. Kopyada collider tutulmaz (crosshair'in nisan taramasi kendi onizlemesine
// carpmasin). Tamamen yerel: yalnizca sahibin PlayerInteractor'i (PlayerInteractor.Local) icin calisir.
[RequireComponent(typeof(PlayerInteractor))]
[RequireComponent(typeof(PlayerInventory))]
public class PlacementPreview : MonoBehaviour
{
    [Tooltip("Id -> ItemType cozumlemesi icin TEK kayit defteri (tum tuketicilerle ortak asset).")]
    [SerializeField] private ItemRegistry registry;
    [Tooltip("Onizleme kopyasinin tum renderer'larina uygulanan yesil yari saydam materyal.")]
    [SerializeField] private Material previewMaterial;
    [Tooltip("Onizleme kopyasinin duracagi katman. Etkilesim raycast maskesinde OLMAMALI (crosshair kendi onizlemesine carpmasin). Sef'in renderer'i (BlindVision_Renderer) bu katmani nabizli konturla ayri bir gecisle cizer; diger roller yesil yari saydami gorur.")]
    [SerializeField] private string previewLayerName;

    private PlayerInteractor _interactor;
    private PlayerInventory _inventory;
    private GameObject _instance;
    private int _shownId = PlayerInventory.EmptySlot;
    private bool _visible;
    private HoldOrPressInteractable _lastTarget;
    private PlacementTarget _placement;
    private int _previewLayer = -1;

    private void Awake()
    {
        _interactor = GetComponent<PlayerInteractor>();
        _inventory = GetComponent<PlayerInventory>();

        _previewLayer = LayerMask.NameToLayer(previewLayerName);
        if (_previewLayer < 0)
            Debug.LogWarning($"[PlacementPreview] '{previewLayerName}' katmani bulunamadi; onizleme varsayilan katmanda kalacak (Sef'te nabizli kontur cizilmez).");
    }

    private void OnDisable()
    {
        Clear();
    }

    private void Update()
    {
        // PlayerInteractor.Local yalnizca yerel sahip icin set edilir (rejoin'deki ServerClientId
        // otomatik devri dahil) — diger oyuncularin kopyalari hicbir sey yapmaz.
        if (PlayerInteractor.Local != _interactor)
        {
            Clear();
            return;
        }

        SyncShownItem();
        UpdatePlacement();
    }

    private void SyncShownItem()
    {
        int id = GetActiveItemId();
        if (id == _shownId)
            return;

        DestroyInstance();
        _shownId = id;

        // Bos slot, kayitsiz id, gorseli olmayan oge veya atanmamis materyal: hata degil, onizleme yok.
        var itemType = registry != null ? registry.Find(id) : null;
        if (itemType == null || itemType.VisualPrefab == null || previewMaterial == null)
            return;

        _instance = Instantiate(itemType.VisualPrefab);
        _instance.SetActive(false);
        PrepareGhost(_instance);
    }

    private void PrepareGhost(GameObject ghost)
    {
        if (_previewLayer >= 0)
        {
            foreach (var child in ghost.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = _previewLayer;
        }

        foreach (var collider in ghost.GetComponentsInChildren<Collider>(true))
            Destroy(collider);

        foreach (var renderer in ghost.GetComponentsInChildren<Renderer>(true))
        {
            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
                materials[i] = previewMaterial;

            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    private void UpdatePlacement()
    {
        var target = _interactor.CurrentTarget;
        if (target != _lastTarget)
        {
            _lastTarget = target;
            _placement = null;
            if (target != null)
                target.TryGetComponent(out _placement);
        }

        bool show = _instance != null && _placement != null && _interactor.Feedback == CrosshairState.Usable;

        if (show)
        {
            _placement.PlacementPoint.GetPositionAndRotation(out var position, out var rotation);
            _instance.transform.SetPositionAndRotation(position, rotation);
        }

        if (show == _visible)
            return;

        _visible = show;
        _instance.SetActive(show);
    }

    private int GetActiveItemId()
    {
        int index = _inventory.ActiveSlotIndex.Value;
        if (index < 0 || index >= _inventory.Slots.Count)
            return PlayerInventory.EmptySlot;

        return _inventory.Slots[index];
    }

    private void Clear()
    {
        DestroyInstance();
        _shownId = PlayerInventory.EmptySlot;
        _lastTarget = null;
        _placement = null;
    }

    private void DestroyInstance()
    {
        if (_instance != null)
            Destroy(_instance);

        _instance = null;
        _visible = false;
    }
}
