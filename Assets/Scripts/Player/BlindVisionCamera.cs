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

    // URP: SetRenderer(-1) = asset'in varsayilan renderer'i.
    private const int DefaultRendererIndex = -1;

    private UniversalAdditionalCameraData _cameraData;
    private RoleManager _roleManager;

    private void Awake()
    {
        _cameraData = GetComponent<Camera>().GetUniversalAdditionalCameraData();
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
        _cameraData.SetRenderer(DefaultRendererIndex);
    }

    private void ApplyRole(PlayerRole role)
    {
        _cameraData.SetRenderer(role == blindRole ? blindVisionRendererIndex : DefaultRendererIndex);
    }
}
