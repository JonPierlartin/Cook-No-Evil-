using Unity.Netcode;
using UnityEngine;

// Oyuncunun rolüne göre karakter modeli (GDD 9: Şef kör hamburger, Komi sağır ketçap, Kasiyer dilsiz yazar kasa).
// Tamamen yerel sunum: rol replike rol listesinden okunur (RoleManager), model her istemcide o role göre kurulur;
// ağdan ek bir şey gitmez. Rol → model eşleşmesi veridir (Inspector); modeli olmayan rol yer tutucu gövdeyle kalır.
// Animasyon modelin kendi bileşenindedir (ProceduralCharacterAnimator: hareketi karakterin gerçek hızından üretir).
//  - Sahibi kendi gövdesini ve ayaklarını görmez (birinci şahıs: kamera modelin içinde), gölgesi kalır. ELLERİ
//    görünür kalır: kendi yön jestini görebilsin diye.
public class PlayerCharacterVisual : NetworkBehaviour
{
    [System.Serializable]
    private struct RoleModel
    {
        public PlayerRole role;
        [Tooltip("Kökü ayak hizasında, +Z'ye bakan karakter prefab'ı. Collider taşımamalı.")]
        public GameObject prefab;
    }

    [SerializeField] private RoleModel[] models;
    [Tooltip("Modelin altına kurulacağı kök (oyuncunun Visual nesnesi).")]
    [SerializeField] private Transform visualRoot;
    [Tooltip("Model kurulunca kapatılan yer tutucu gövde.")]
    [SerializeField] private Renderer placeholderBody;
    [Tooltip("Ayak hizası, visualRoot'un yerel uzayında (oyuncu kökü kapsülün merkezindedir).")]
    [SerializeField] private Vector3 feetLocalPosition = new(0f, -1f, 0f);

    private GameObject _model;
    private PlayerRole _shownRole = PlayerRole.None;

    // Kurulu modelin animasyon bileşeni (jest oynatmak için); model yoksa null.
    public ProceduralCharacterAnimator Animator { get; private set; }

    public override void OnNetworkSpawn()
    {
        if (RoleManager.Instance != null)
            RoleManager.Instance.OnRolesChanged += Refresh;

        Refresh();
    }

    public override void OnNetworkDespawn()
    {
        if (RoleManager.Instance != null)
            RoleManager.Instance.OnRolesChanged -= Refresh;
    }

    // Sahiplik değişince (rejoin'de nesne geri devredilir) hem rol hem "kendi modelimi gizle" kuralı yenilenir.
    public override void OnGainedOwnership() => Rebuild();
    public override void OnLostOwnership() => Rebuild();

    private void Refresh()
    {
        var role = RoleManager.Instance != null ? RoleManager.Instance.GetRole(OwnerClientId) : PlayerRole.None;
        if (role != _shownRole)
            Rebuild();
    }

    private void Rebuild()
    {
        if (_model != null)
            Destroy(_model);

        _model = null;
        Animator = null;
        _shownRole = RoleManager.Instance != null ? RoleManager.Instance.GetRole(OwnerClientId) : PlayerRole.None;

        GameObject prefab = null;
        foreach (var entry in models)
        {
            if (entry.role == _shownRole)
                prefab = entry.prefab;
        }

        if (placeholderBody != null)
            placeholderBody.enabled = prefab == null;

        if (prefab == null)
            return;

        _model = Instantiate(prefab, visualRoot);
        _model.transform.localPosition = feetLocalPosition;
        _model.transform.localRotation = Quaternion.identity;
        Animator = _model.GetComponent<ProceduralCharacterAnimator>();

        if (!IsOwner)
            return;

        // Birinci şahıs: sahibi kendi gövdesini/ayaklarını görmez (yalnızca gölge); elleri görünür kalır.
        foreach (var renderer in _model.GetComponentsInChildren<Renderer>(true))
        {
            bool isHand = Animator != null
                && (renderer.transform.IsChildOf(Animator.LeftHand) || renderer.transform.IsChildOf(Animator.RightHand));
            if (!isHand)
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        }
    }
}
