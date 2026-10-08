using UnityEngine;
using UnityEngine.Rendering.Universal;

// GDD 4.1.1 kor gorus: yerel oyuncunun rolu kor rol (Sef) ise KENDI kamerasinin URP renderer'ini
// kontur renderer'ina cevirir; diger roller varsayilan renderer'i gorur. Kor gorus bir RENDER
// ASAMASIDIR (renderer'daki Full Screen Pass + kenar shader'i) — parlaklik/gamma/exposure ayari
// degildir (K3), yani oyuncu parlakligi acarak goremez.
//
// Neden per-camera renderer (URP): etki asset seviyesinde ac/kapa edilmez (asset'i degistirmez,
// diger kameralari etkilemez); her kamera hangi renderer'i kullanacagini kendi secer. Rol,
// RoleManager'in atadigi role bakilarak secilir — katilma sirasina degil.
//
// Bu bilesen Player kamerasinda durur; kamera yalnizca SAHIP icin etkinlestirilir
// (bkz. PlayerController), dolayisiyla yalnizca yerel kamerada anlamli calisir.
[RequireComponent(typeof(Camera))]
public class BlindVisionCamera : MonoBehaviour
{
    [Tooltip("Kor gorus renderer'inin URP asset'indeki Renderer List indeksi (PC_RPAsset).")]
    [SerializeField] private int blindVisionRendererIndex;
    [Tooltip("Bu rol kor gorusle gorur.")]
    [SerializeField] private PlayerRole blindRole = PlayerRole.Sef;
    [Tooltip("Kör görüşte ÇİZİLMEYEN katmanlar (Şef'in görmemesi gereken eşyalar).")]
    [SerializeField] private LayerMask hiddenWhenBlind;
    [Tooltip("YALNIZCA kör görüşte çizilen katmanlar (Şef'in görüşünü kapatan perdeler; diğer roller görmez).")]
    [SerializeField] private LayerMask visibleOnlyWhenBlind;

    // URP: SetRenderer(-1) = asset'in varsayilan renderer'i.
    private const int DefaultRendererIndex = -1;

    // Bu kamera şu an kör görüşle mi çiziyor. Kör görüşe uyması gereken arayüz (hotbar ikonları) kuralı buradan
    // okur; "kör rol hangisi" ikinci bir yerde yazılmaz.
    public bool IsBlind { get; private set; }

    private UniversalAdditionalCameraData _cameraData;
    private RoleManager _roleManager;
    private Camera _camera;
    private int _baseCullingMask;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        _cameraData = _camera.GetUniversalAdditionalCameraData();
        _baseCullingMask = _camera.cullingMask;
        ApplyCullingMask();
    }

    // Şef'in neyi görebildiği (GDD 4.1.1 + 8 Eki kararı): Şef yalnızca kendi mutfağını ve pencereden Komi ile Komi'nin
    // ODASINI görür — İstasyon'un eşyalarını, Kasa'yı, Kasiyer'i ve ötesini görmez. Bu bir çizim kuralıdır:
    // gizlenecek eşyalar bir katmanda, görüşü kesen perdeler (Kasa penceresi, mutfak kapısı) başka bir katmandadır.
    private void ApplyCullingMask()
    {
        _camera.cullingMask = IsBlind
            ? (_baseCullingMask & ~hiddenWhenBlind.value) | visibleOnlyWhenBlind.value
            : _baseCullingMask & ~visibleOnlyWhenBlind.value;
    }

    private void OnEnable()
    {
        _roleManager = RoleManager.Instance;
        if (_roleManager == null)
            return;

        // Rol, kamera etkinlesmeden once veya sonra gelebilir: hem simdiki hali uygula hem de
        // atama/degisim olayini dinle.
        _roleManager.OnLocalRoleAssigned += ApplyRole;
        ApplyRole(_roleManager.LocalRole);
    }

    private void OnDisable()
    {
        if (_roleManager != null)
            _roleManager.OnLocalRoleAssigned -= ApplyRole;

        _roleManager = null;
        IsBlind = false;
        _cameraData.SetRenderer(DefaultRendererIndex);
        ApplyCullingMask();
    }

    private void ApplyRole(PlayerRole role)
    {
        IsBlind = role == blindRole;
        _cameraData.SetRenderer(IsBlind ? blindVisionRendererIndex : DefaultRendererIndex);
        ApplyCullingMask();

        // Kör görüşte post-process ve kenar yumuşatma YOKTUR: tonemapping / bloom / AA beyaz kontur çizgilerini
        // grileştirir ya da bulandırır. Başka bir sistem bu kamerada post-process açmış olsa da burada kapanır.
        if (IsBlind)
        {
            _cameraData.renderPostProcessing = false;
            _cameraData.antialiasing = AntialiasingMode.None;
        }
    }
}
