using UnityEngine;

// GDD 4.1.2 (2): elde uygun nesne tasiyan oyuncu bu hedefin placementRadius'una girince hedefin
// ustunde bir isaret (marker mesh) belirir. YAKINLIK tetiklidir — bakis yonunden bagimsizdir,
// nisan/isin taramasi yapilmaz. Tamamen yereldir: her oyuncu yalnizca kendi isaretini gorur,
// hicbir sey agdan gecmez.
//
// Isaret kosulu yalnizca "yaricap icinde" VE "HoldOrPressInteractable.CanInteract(yerel client)":
// rol, elin dolulugu ve elindekinin konulabilirligi istasyonun gate'inde yasar (IInteractionGate),
// burada tekrar yazilmaz. Isaret bir mesh'tir (siluet degisimi); istasyonun materyali/rengi
// degismez — GDD 4.1.1: Sef icin sekille anlatilan gorunur, renkle anlatilan gorunmez.
//
// Hedefe nisan alinabilmesi icin (GDD 4.1.2: hedef secimi nisanla yapilir) bu bilesen bir
// HoldOrPressInteractable ile ayni nesnede durur; birden fazla yuvali istasyonlarda her yuva
// kendi PlacementTarget + HoldOrPressInteractable'ini tasir.
[DisallowMultipleComponent]
[RequireComponent(typeof(HoldOrPressInteractable))]
public class PlacementTarget : MonoBehaviour
{
    [Tooltip("Isaretin gorunmesi icin oyuncunun bu nesneye olan azami mesafesi (m). PlayerInteractor'in interactRange'inden AYRI bir playtest parametresidir.")]
    [SerializeField] private float placementRadius;
    [Tooltip("Belirecek isaret objesi (mesh). Varsayilan olarak kapali olmalidir; bu bilesen acip kapatir.")]
    [SerializeField] private GameObject marker;

    private HoldOrPressInteractable _interactable;
    private bool _markerVisible;

    private void Awake()
    {
        _interactable = GetComponent<HoldOrPressInteractable>();

        if (marker == null)
            Debug.LogWarning($"[PlacementTarget] '{name}': marker atanmamis, isaret gosterilemeyecek.");
    }

    private void OnEnable()
    {
        SetMarkerVisible(false);
    }

    private void OnDisable()
    {
        SetMarkerVisible(false);
    }

    private void Update()
    {
        bool shouldShow = ShouldShowMarker();
        if (shouldShow != _markerVisible)
            SetMarkerVisible(shouldShow);
    }

    private bool ShouldShowMarker()
    {
        // Yalnizca yerel (owner) oyuncunun etkilesimcisi; yoksa (lobi, baglanti kopmasi) isaret yok.
        var local = PlayerInteractor.Local;
        if (local == null)
            return false;

        Vector3 offset = local.transform.position - transform.position;
        if (offset.sqrMagnitude > placementRadius * placementRadius)
            return false;

        return _interactable.CanInteract(local.OwnerClientId, out _);
    }

    private void SetMarkerVisible(bool visible)
    {
        _markerVisible = visible;

        if (marker != null)
            marker.SetActive(visible);
    }
}
