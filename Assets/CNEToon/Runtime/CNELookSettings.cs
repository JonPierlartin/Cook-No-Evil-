using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// CNE Toon görünümünün ayarları (Resources/CNELookSettings). Değerler burada durur, koda gömülmez.
// Profiller "CNE → Post → Create Profiles" menüsüyle üretilir ve sonradan elle düzenlenebilir.
public class CNELookSettings : ScriptableObject
{
    private const string ResourceName = "CNELookSettings";

    [Header("Post-process (görünüm açıkken, kör olmayan oyuncu kameralarında)")]
    [Tooltip("Görünüm açıkken uygulanan global Volume profili (CNE_Global ya da karşılaştırma için CNE_Global_Neutral).")]
    public VolumeProfile globalProfile;
    [Tooltip("Global Volume'un önceliği. Sahnedeki diğer Volume'lardan büyük olmalı ki onları ezsin.")]
    public float globalPriority = 100f;
    public AntialiasingMode antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
    public AntialiasingQuality antialiasingQuality = AntialiasingQuality.High;

    [Header("Debug (yalnızca Editor ve Development Build)")]
    [Tooltip("Gri tonlama testi profili (Saturation -100).")]
    public VolumeProfile grayscaleProfile;
    [Tooltip("Debug Volume'unun önceliği; global profilden büyük olmalı.")]
    public float debugPriority = 1000f;
    public Key grayscaleKey = Key.F9;
    public Key outlineKey = Key.F10;

    private static CNELookSettings _instance;

    // Ayar varlığı yoksa null döner; çağıran taraf görünümü uygulamaz (hata fırlatmaz).
    public static CNELookSettings Load()
    {
        if (_instance == null)
            _instance = Resources.Load<CNELookSettings>(ResourceName);

        return _instance;
    }
}
