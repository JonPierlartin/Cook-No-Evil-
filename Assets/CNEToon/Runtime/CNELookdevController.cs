using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// ToonLookdev test sahnesinin denetimi: sahne açılınca CNE Toon görünümünü açar, tuşlarla ışık rengini, Şef
// kamerasını ve tonemapping profilini değiştirir, kamerayı hedefin çevresinde döndürür. Yalnızca test içindir.
public class CNELookdevController : MonoBehaviour
{
    [Header("Işık")]
    [SerializeField] private Light mainLight;
    [SerializeField] private Color warmLightColor = new Color32(0xFF, 0xE9, 0xC7, 0xFF);
    [SerializeField] private Color whiteLightColor = Color.white;

    [Header("Kameralar")]
    [SerializeField] private Camera playerCamera;
    [Tooltip("Kör görüş renderer'ıyla çizen kamera (kontur görüşünün değişmediğini göstermek için).")]
    [SerializeField] private Camera chefCamera;
    [SerializeField] private Transform orbitTarget;
    [SerializeField] private float orbitSpeed = 60f;
    [SerializeField] private float zoomSpeed = 4f;
    [SerializeField] private Vector2 distanceRange = new(3f, 14f);

    [Header("Profiller")]
    [SerializeField] private VolumeProfile toneNoneProfile;
    [SerializeField] private VolumeProfile toneNeutralProfile;

    [Header("Tuşlar")]
    [SerializeField] private Key lightKey = Key.Digit1;
    [SerializeField] private Key chefKey = Key.Digit2;
    [SerializeField] private Key tonemapKey = Key.Digit3;
    [SerializeField] private Key exitKey = Key.Escape;

    private bool _warm = true;
    private bool _chef;
    private bool _neutral;
    private VolumeProfile _originalProfile;
    private CNELookSettings _settings;
    private CursorLockMode _previousLock;
    private bool _previousCursorVisible;

    private void OnEnable()
    {
        _settings = CNELookSettings.Load();
        if (_settings != null)
            _originalProfile = _settings.globalProfile;

        _previousLock = Cursor.lockState;
        _previousCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        ApplyLight();
        ApplyCamera();
        CNELook.Active = true;
    }

    private void OnDisable()
    {
        CNELook.Active = false;
        // Ayar varlığı paylaşılan bir dosyadır: testte seçilen profil kalıcı olmasın.
        if (_settings != null)
            _settings.globalProfile = _originalProfile;

        Cursor.lockState = _previousLock;
        Cursor.visible = _previousCursorVisible;
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (Pressed(keyboard, lightKey))
        {
            _warm = !_warm;
            ApplyLight();
        }

        if (Pressed(keyboard, chefKey))
        {
            _chef = !_chef;
            ApplyCamera();
        }

        if (Pressed(keyboard, tonemapKey) && _settings != null)
        {
            _neutral = !_neutral;
            _settings.globalProfile = _neutral ? toneNeutralProfile : toneNoneProfile;
            // Denetleyici profili görünüm değişiminde okur.
            CNELook.Active = false;
            CNELook.Active = true;
        }

        if (Pressed(keyboard, exitKey) && CNELookdev.IsOpen)
        {
            CNELookdev.Exit();
            return;
        }

        Orbit(keyboard);
    }

    private void Orbit(Keyboard keyboard)
    {
        if (orbitTarget == null || playerCamera == null)
            return;

        float turn = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
        float zoom = (keyboard.sKey.isPressed ? 1f : 0f) - (keyboard.wKey.isPressed ? 1f : 0f);
        var rig = playerCamera.transform;

        if (turn != 0f)
            rig.RotateAround(orbitTarget.position, Vector3.up, -turn * orbitSpeed * Time.unscaledDeltaTime);

        if (zoom != 0f)
        {
            Vector3 offset = rig.position - orbitTarget.position;
            float distance = Mathf.Clamp(offset.magnitude + zoom * zoomSpeed * Time.unscaledDeltaTime, distanceRange.x, distanceRange.y);
            rig.position = orbitTarget.position + offset.normalized * distance;
        }

        rig.LookAt(orbitTarget.position);
    }

    private void ApplyLight()
    {
        if (mainLight != null)
            mainLight.color = _warm ? warmLightColor : whiteLightColor;
    }

    // Şef kamerası oyuncu kamerasının çocuğudur (aynı bakış); yalnızca hangisinin çizdiği değişir.
    private void ApplyCamera()
    {
        if (chefCamera != null)
            chefCamera.enabled = _chef;
    }

    private static bool Pressed(Keyboard keyboard, Key key)
    {
        return key != Key.None && keyboard[key].wasPressedThisFrame;
    }

    private void OnGUI()
    {
        string tone = _neutral ? "Neutral" : "None";
        GUI.Label(new Rect(16, 12, 900, 24),
            $"[{Label(lightKey)}] Işık: {(_warm ? "sıcak" : "beyaz")}    [{Label(chefKey)}] Şef kamerası: {(_chef ? "açık" : "kapalı")}    " +
            $"[{Label(tonemapKey)}] Tonemapping: {tone}    [A/D] döndür  [W/S] yaklaş    " +
            (_settings != null ? $"[{_settings.grayscaleKey}] gri test  [{_settings.outlineKey}] outline    " : string.Empty) +
            (CNELookdev.IsOpen ? $"[{Label(exitKey)}] ana menü" : string.Empty));
    }

    private static string Label(Key key) => key.ToString().Replace("Digit", string.Empty);
}
