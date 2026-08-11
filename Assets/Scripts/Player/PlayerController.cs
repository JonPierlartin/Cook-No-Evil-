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
    [SerializeField] private float sprintMultiplier = 1.6f;
    [SerializeField] private float mouseSensitivity = 0.12f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;
    [SerializeField] private float gravity = -20f;

    private CharacterController _characterController;
    private InputAction _moveAction;
    private InputAction _lookAction;
    private InputAction _sprintAction;
    private float _pitch;
    private float _verticalVelocity;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            // Uzak oyuncular icin bu bilesen hic calismaz — pozisyonlari NetworkTransform
            // uzerinden gelir, kendi CharacterController'lari sadece fiziksel collider olarak durur.
            if (playerCamera != null)
                playerCamera.gameObject.SetActive(false);

            enabled = false;
            return;
        }

        // Sahnenin statik lobi kamerasini (Camera.main, henuz player kamerasi
        // aktif/etiketli olmadigi icin bu asamada tek/belirgin MainCamera) kapatip
        // kendi kameramizi devreye sokuyoruz — sahne kamerasi silinmiyor, sadece
        // devre disi birakiliyor.
        var sceneCamera = Camera.main;
        if (sceneCamera != null)
            sceneCamera.gameObject.SetActive(false);

        if (playerCamera != null)
            playerCamera.gameObject.SetActive(true);

        var playerMap = inputActions.FindActionMap("Player");
        playerMap.Enable();
        _moveAction = playerMap.FindAction("Move");
        _lookAction = playerMap.FindAction("Look");
        _sprintAction = playerMap.FindAction("Sprint");

        // TESHIS: gercek cok-makineli testte "round basladiginda ekranda hicbir sey
        // degismiyor" raporu icin — Player.log'da bu satirin varligi owner kamerasinin
        // gercekten devreye girdigini dogrular (bkz. PlayerSpawner'daki spawn log'u).
        Debug.Log($"[PlayerController] Owner kamerasi aktif, sahne kamerasi kapatildi (clientId={NetworkManager.LocalClientId}).");
    }

    private void Update()
    {
        if (!IsOwner)
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
        bool sprinting = _sprintAction != null && _sprintAction.IsPressed();
        float speed = moveSpeed * (sprinting ? sprintMultiplier : 1f);

        Vector3 move = (transform.right * moveInput.x + transform.forward * moveInput.y) * speed;

        if (_characterController.isGrounded && _verticalVelocity < 0f)
            _verticalVelocity = -1f;
        _verticalVelocity += gravity * Time.deltaTime;
        move.y = _verticalVelocity;

        _characterController.Move(move * Time.deltaTime);
    }
}
