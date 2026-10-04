using Unity.Netcode;
using UnityEngine;

// Oyuncunun rolüne göre karakter modeli (GDD 9: Şef kör hamburger, Komi sağır ketçap, Kasiyer dilsiz yazar kasa).
// Rol, karakter NESNESİNİN kendi replike alanıdır (CharacterRole; sunucu doğururken yazar) — sahibin kimliğinden
// TÜRETİLMEZ. Neden: round sırasında kopan oyuncunun nesnesi geçici olarak host'a devredilir (NGO); rol sahibe
// bakılarak okunursa o süre boyunca ve geri dönüşte karakter host'un rolünün modeline (hamburger) dönüşüyordu
// (4 Eki 2026: Kasiyer lobiye dönüp yeniden katılınca hamburger oldu). Rol → model eşleşmesi veridir (Inspector);
// model her istemcide yerel kurulur. Animasyon modelin kendi bileşenindedir (ProceduralCharacterAnimator).
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

    // Bu karakterin rolü. Yalnızca sunucu yazar (PlayerSpawner, doğururken); sahiplik değişse de değişmez.
    public readonly NetworkVariable<PlayerRole> CharacterRole =
        new(PlayerRole.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private GameObject _model;
    private bool _isLocalOwner;

    // Kurulu modelin animasyon bileşeni (jest oynatmak için); model yoksa null.
    public ProceduralCharacterAnimator Animator { get; private set; }

    public override void OnNetworkSpawn()
    {
        CharacterRole.OnValueChanged += HandleRoleChanged;
        _isLocalOwner = IsOwner;
        Rebuild();
    }

    public override void OnNetworkDespawn()
    {
        CharacterRole.OnValueChanged -= HandleRoleChanged;
    }

    // Sunucu: karakter doğduktan hemen sonra.
    public void ServerSetRole(PlayerRole role)
    {
        if (IsServer)
            CharacterRole.Value = role;
    }

    private void HandleRoleChanged(PlayerRole previous, PlayerRole current) => Rebuild();

    // Sahiplik değişince (rejoin'de nesne geri devredilir) model aynı kalır; yalnızca "kendi gövdemi gizle" kuralı
    // yeni sahibe göre yeniden uygulanır. Kopan oyuncunun nesnesi host'a devredilir ve host'ta IsOwner yanlışlıkla
    // true olur — o durumda nesne host'un "kendi" karakteri sayılmaz (HeldItemVisual ile aynı kural).
    protected override void OnOwnershipChanged(ulong previous, ulong current)
    {
        _isLocalOwner = current != NetworkManager.ServerClientId && IsOwner;
        Rebuild();
    }

    private void Rebuild()
    {
        if (_model != null)
            Destroy(_model);

        _model = null;
        Animator = null;

        GameObject prefab = null;
        foreach (var entry in models)
        {
            if (entry.role == CharacterRole.Value)
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

        if (!_isLocalOwner)
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
