using UnityEngine;
using UnityEngine.Rendering;

// GDD 4.1.2 (2) Yerlestirme Onizlemesi: crosshair bir PlacementTarget'a bakarken VE o hedef icin
// "kullanilabilir" iken, hedefin noktasinda elindeki nesnenin sekilde yesil yari saydam bir kopyasi
// gorunur. Hedef bir ItemSlot ise onizleme YALNIZCA yuva BOSKEN ve elindeki oge kabul ediliyorsa cikar:
// dolu yuvada crosshair "kullanilabilir" (alma) olabilir ama konacak bir sey yoktur. Yeni kural yazilmaz: hedef ve durum PlayerInteractor'in mevcut (tek nisan taramali) her
// kare hesabindan okunur — menzil ve CanInteract zaten "kullanilabilir"in icindedir. Sirtini
// donunce hedef degistigi icin onizleme kendiliginden kaybolur.
//
// Sekil, aktif slottaki ogenin turunun visualPrefab'idir (elde tutulanla AYNI gorsel); tur, aktif
// slottaki gercek ogeden (PlayerInventory.TryGetActiveItem -> Item.Type) okunur. Her karede sorulur, bu
// yuzden ogeden once gelen slot listesi oge spawn olunca kendiliginden tamamlanir. Kopya yalnizca
// aktif ogenin TURU DEGISINCE olusturulur; her karede yalnizca konum ve gorunurluk guncellenir. Kopyada collider tutulmaz (crosshair'in nisan taramasi kendi onizlemesine
// carpmasin). Tamamen yerel: yalnizca sahibin PlayerInteractor'i (PlayerInteractor.Local) icin calisir.
[RequireComponent(typeof(PlayerInteractor))]
[RequireComponent(typeof(PlayerInventory))]
public class PlacementPreview : MonoBehaviour
{
    [Tooltip("Onizleme kopyasinin tum renderer'larina uygulanan yesil yari saydam materyal.")]
    [SerializeField] private Material previewMaterial;
    [Tooltip("Onizleme kopyasinin duracagi katman. Etkilesim raycast maskesinde OLMAMALI (crosshair kendi onizlemesine carpmasin). Sef'in renderer'i (BlindVision_Renderer) bu katmani nabizli konturla ayri bir gecisle cizer; diger roller yesil yari saydami gorur.")]
    [SerializeField] private string previewLayerName;

    private PlayerInteractor _interactor;
    private PlayerInventory _inventory;
    private GameObject _instance;
    private ItemType _shownType;
    private Item _shownItem;
    private IItemVisualSource _shownSource;
    private int _shownVersion;
    private bool _visible;
    private HoldOrPressInteractable _lastTarget;
    private PlacementTarget _placement;
    private ItemSlot _slot;
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
        var item = _inventory.TryGetActiveItem(out var active) ? active : null;

        // Ogenin kendi gorsel kaynagi varsa (yarim ekmek, hamburger) onizleme O HALI gosterir (GDD 4.1.2 (2)):
        // oge degisince VEYA kaynagin surumu artinca yeniden uretilir.
        if (item != _shownItem)
        {
            _shownSource = item != null ? item.GetComponent<IItemVisualSource>() : null;
            _shownVersion = -1;
        }

        int version = _shownSource != null ? _shownSource.VisualVersion : 0;
        if (item == _shownItem && version == _shownVersion)
            return;

        DestroyInstance();
        _shownItem = item;
        _shownVersion = version;
        _shownType = item != null ? item.Type : null;

        // Bos slot, gorseli olmayan oge veya atanmamis materyal: hata degil, onizleme yok.
        if (item == null || previewMaterial == null)
            return;

        if (_shownSource != null)
            _instance = _shownSource.CreateVisual(null);
        else if (_shownType != null && _shownType.VisualPrefab != null)
            _instance = Instantiate(_shownType.VisualPrefab);

        if (_instance == null)
            return;

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
            _slot = null;
            if (target != null)
            {
                // Yuvasiz hedefte PlacementTarget onizleme noktasini gosteren bir cocuk nesnede durabilir.
                _placement = target.GetComponentInChildren<PlacementTarget>();
                target.TryGetComponent(out _slot);
            }
        }

        bool show = _instance != null && _placement != null && _interactor.Feedback == CrosshairState.Usable
            && (_slot == null || (_slot.IsEmpty && _slot.Accepts(_shownType)));

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

    private void Clear()
    {
        DestroyInstance();
        _shownType = null;
        _shownItem = null;
        _shownSource = null;
        _lastTarget = null;
        _placement = null;
        _slot = null;
    }

    private void DestroyInstance()
    {
        if (_instance != null)
            Destroy(_instance);

        _instance = null;
        _visible = false;
    }
}
