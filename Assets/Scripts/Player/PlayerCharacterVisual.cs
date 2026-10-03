using Unity.Netcode;
using UnityEngine;

// Oyuncunun rolüne göre karakter modeli (GDD 9: Şef kör hamburger, Komi sağır ketçap, Kasiyer dilsiz yazar kasa).
// Tamamen yerel sunum: rol replike rol listesinden okunur (RoleManager), model her istemcide o role göre kurulur;
// ağdan ek bir şey gitmez. Rol → model eşleşmesi veridir (Inspector); modeli olmayan rol yer tutucu gövdeyle kalır.
//  - Sahibi kendi modelini görmez (birinci şahıs: kamera modelin içinde); gölgesi kalır.
//  - Yürüme animasyonu: modelde Animator varsa "moving" parametresi karakterin yataydaki gerçek hızından sürülür
//    (uzak oyuncularda NetworkTransform'un taşıdığı konumdan) — animasyon için ayrı bir ağ verisi yoktur.
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
    [Tooltip("Animator'daki yürüme parametresi (bool).")]
    [SerializeField] private string movingParameter = "Moving";
    [Tooltip("Bu hızın (m/sn) üstünde karakter yürüyor sayılır.")]
    [SerializeField, Min(0f)] private float movingSpeedThreshold = 0.15f;

    private GameObject _model;
    private Animator _animator;
    private PlayerRole _shownRole = PlayerRole.None;
    private Vector3 _lastPosition;
    private int _movingHash;
    private float _smoothedSpeed;

    public override void OnNetworkSpawn()
    {
        _movingHash = Animator.StringToHash(movingParameter);
        _lastPosition = transform.position;

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
        _animator = null;
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
        _animator = _model.GetComponentInChildren<Animator>();

        // Birinci şahıs: sahibi kendi modelini görmez, yalnızca gölgesini.
        if (IsOwner)
        {
            foreach (var renderer in _model.GetComponentsInChildren<Renderer>(true))
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        }
    }

    private void Update()
    {
        if (_animator == null)
            return;

        var position = transform.position;
        var delta = position - _lastPosition;
        _lastPosition = position;
        delta.y = 0f;

        // Uzak oyuncunun konumu ağ enterpolasyonuyla gelir; anlık hız titrer. Yumuşatılmış hız kullanılır.
        float speed = Time.deltaTime > 0f ? delta.magnitude / Time.deltaTime : 0f;
        _smoothedSpeed = Mathf.Lerp(_smoothedSpeed, speed, Mathf.Clamp01(10f * Time.deltaTime));
        _animator.SetBool(_movingHash, _smoothedSpeed > movingSpeedThreshold);
    }
}
