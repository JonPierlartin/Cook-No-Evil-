using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

// Client-authoritative hareket: sahibi (owner) kendi pozisyonunu CharacterController
// ile hesaplar, Unity.Netcode.Components.NetworkTransform (prefab uzerinde,
// owner-authoritative modda) bunu diger client'lara senkronize eder — elle
// pozisyon senkron kodu YAZILMAZ. Oyun MANTIGI (skor, pisirme, strike) hala
// tamamen server'da; bu sadece pozisyon/kamera icin gecerli bir istisna.
[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private Transform cameraPivot;
    [Tooltip("Bu prefab'in kendi kamerasi (MainCamera etiketi TASIMAMALI — sahnenin statik kamerasiyla Camera.main belirsizligini onlemek icin).")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float mouseSensitivity = 0.12f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;
    [SerializeField] private float gravity = -20f;

    private CharacterController _characterController;
    private InputAction _moveAction;
    private InputAction _lookAction;
    private float _pitch;
    private float _verticalVelocity;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
    }

    public override void OnNetworkSpawn()
    {
        ApplyOwnershipState();
    }

    // BULUNAN HATA: round-ici rejoin'de PlayerSpawner ayni objeyi yeniden Spawn ETMIYOR,
    // sadece ChangeOwnership cagiriyor (bkz. PlayerSpawner) — OnNetworkSpawn ise sadece
    // objenin YASAM DONGUSUNDE BIR KEZ calisir, ownership sonradan degistiginde NGO
    // bunu TEKRAR cagirmaz. Eski kod bu yuzden "IsOwner=false" durumunu OnNetworkSpawn
    // aninda (henuz eski/dondurulmus sahiple senkron oldugunda) tek seferlik goruyor,
    // enabled=false yapiyor ve BIR DAHA ASLA true'ya donmuyordu — reconnect eden oyuncu
    // gercekten IsOwner=true olsa bile hareket edemiyordu (IsGamePaused'dan bagimsiz,
    // ayri bir "donma" kaynagi). Duzeltme: kurulum mantigi ApplyOwnershipState'e alindi,
    // hem OnNetworkSpawn'da HEM NGO'nun ownership-degisikligi icin saglanan
    // OnOwnershipChanged callback'inde cagriliyor.
    //
    // IKINCI BULUNAN HATA (gercek testte, yukaridaki fix'in yan etkisi olarak ortaya
    // cikti): NGO, DontDestroyWithOwner=true bir objenin sahibi kopunca OTOMATIK olarak
    // (NetworkConnectionManager.cs, bizim cagirdigimiz bir sey DEGIL) sahipligi
    // NetworkManager.ServerClientId'ye devrediyor (ownedObject.RemoveOwnership()).
    // Host ayni zamanda HER ZAMAN client 0 = ServerClientId oldugu icin, bu otomatik
    // devir host'ta IsOwner'i YANLISLIKLA true yapiyor — host boylece KOPAN OYUNCUNUN
    // karakterini "kendisininmis gibi" gorup kamerasini/kontrolunu aktif ediyordu (host
    // ekrani ayrilan client'in kamerasina geciyordu). Host'un KENDI objesinde ownership
    // spawn'dan beri hic degismedigi icin bu callback o objede zaten hic tetiklenmez —
    // yani current == ServerClientId gelmesi HER ZAMAN bu otomatik "sahipsiz kaldi"
    // temizligidir, gercek bir "bu benim karakterim" atamasi degildir. Bu durumda
    // IsOwner'in kendisine guvenilmeyip acikca donuk/sahipsiz durum zorlaniyor.
    protected override void OnOwnershipChanged(ulong previous, ulong current)
    {
        if (current == NetworkManager.ServerClientId)
        {
            ApplyNonOwnerState();
            return;
        }

        ApplyOwnershipState();
    }

    private void ApplyOwnershipState()
    {
        if (!IsOwner)
        {
            ApplyNonOwnerState();
            return;
        }

        // Sahnenin statik lobi kamerasini (Camera.main, henuz player kamerasi
        // aktif/etiketli olmadigi icin bu asamada tek/belirgin MainCamera) kapatip
        // kendi kameramizi devreye sokuyoruz — sahne kamerasi silinmiyor, sadece
        // devre disi birakiliyor. Reconnect'te bu tekrar calisirsa (sahne kamerasi zaten
        // kapaliysa) SetActive(false) idempotent, zararsizdir.
        var sceneCamera = Camera.main;
        if (sceneCamera != null)
            sceneCamera.gameObject.SetActive(false);

        if (playerCamera != null)
            playerCamera.gameObject.SetActive(true);

        var playerMap = inputActions.FindActionMap("Player");
        playerMap.Enable();
        _moveAction = playerMap.FindAction("Move");
        _lookAction = playerMap.FindAction("Look");

        enabled = true;

        // TESHIS: gercek cok-makineli testte "round basladiginda ekranda hicbir sey
        // degismiyor" raporu icin — Player.log'da bu satirin varligi owner kamerasinin
        // gercekten devreye girdigini dogrular (bkz. PlayerSpawner'daki spawn log'u).
        Debug.Log($"[PlayerController] Owner kamerasi aktif, sahne kamerasi kapatildi (clientId={NetworkManager.LocalClientId}).");
    }

    private void ApplyNonOwnerState()
    {
        // Uzak oyuncular icin bu bilesen hic calismaz — pozisyonlari NetworkTransform
        // uzerinden gelir, kendi CharacterController'lari sadece fiziksel collider olarak durur.
        if (playerCamera != null)
            playerCamera.gameObject.SetActive(false);

        enabled = false;
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        // "Oyun durduruldu" (round sirasinda bir oyuncu koptugunda, bkz. GameLoopManager)
        // TUM oyuncular icin gecerli — sadece kopan oyuncunun kendi objesi degil. Kopan
        // oyuncunun objesi zaten IsOwner hicbir client'ta true olmadigi icin kendiliginden
        // donuyordu (bkz. RoleManager notu); burada BAGLI KALAN oyuncular icin ayni
        // dondurma acikca uygulaniyor.
        if (GameLoopManager.Instance != null && GameLoopManager.Instance.IsGamePaused.Value)
            return;

        ApplyLook();
        ApplyMove();
    }

    private void ApplyLook()
    {
        Vector2 lookDelta = _lookAction.ReadValue<Vector2>() * mouseSensitivity;
        transform.Rotate(Vector3.up, lookDelta.x);

        _pitch = Mathf.Clamp(_pitch - lookDelta.y, minPitch, maxPitch);
        if (cameraPivot != null)
            cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    private void ApplyMove()
    {
        Vector2 moveInput = _moveAction.ReadValue<Vector2>();
        Vector3 move = (transform.right * moveInput.x + transform.forward * moveInput.y) * moveSpeed;

        if (_characterController.isGrounded && _verticalVelocity < 0f)
            _verticalVelocity = -1f;
        _verticalVelocity += gravity * Time.deltaTime;
        move.y = _verticalVelocity;

        _characterController.Move(move * Time.deltaTime);
    }
}
