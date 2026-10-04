using System;
using UnityEngine;

// Oyuncunun YEREL ayarları (ESC menüsü). Bu makinede kalır (PlayerPrefs), ağdan gitmez, oyun kuralı değildir.
// Ayarın tek kaynağı burasıdır: tüketiciler (müzik, sesli sohbet, bakış) değeri buradan okur ve Changed'i dinler.
// Ana ses doğrudan AudioListener.volume'a uygulanır. Sağırlık (DeafHearing) bunların hiçbirine dayanmaz (K3):
// sesi açmak sağır rolün duyamadığını duyurmaz.
public static class GameSettings
{
    private const string MasterKey = "settings.masterVolume";
    private const string MusicKey = "settings.musicVolume";
    private const string VoiceKey = "settings.voiceVolume";
    private const string MicKey = "settings.micGain";
    private const string SensitivityKey = "settings.mouseSensitivity";

    public const float MaxMicGain = 2f;
    public const float MinMouseSensitivity = 0.2f;
    public const float MaxMouseSensitivity = 3f;

    private static bool _loaded;
    private static float _master = 1f;
    private static float _music = 0.5f;
    private static float _voice = 1f;
    private static float _mic = 1f;
    private static float _sensitivity = 1f;

    public static event Action Changed;

    // Duyulan her şey (0–1).
    public static float MasterVolume
    {
        get { Load(); return _master; }
        set => Set(ref _master, Mathf.Clamp01(value), MasterKey);
    }

    // Müzik (0–1), ana sesin içinde.
    public static float MusicVolume
    {
        get { Load(); return _music; }
        set => Set(ref _music, Mathf.Clamp01(value), MusicKey);
    }

    // Sesli sohbetten GELEN ses (0–1).
    public static float VoiceVolume
    {
        get { Load(); return _voice; }
        set => Set(ref _voice, Mathf.Clamp01(value), VoiceKey);
    }

    // Mikrofon giriş seviyesi (0–MaxMicGain; 1 = olduğu gibi). Diğer oyuncuların beni ne kadar yüksek duyacağı.
    public static float MicGain
    {
        get { Load(); return _mic; }
        set => Set(ref _mic, Mathf.Clamp(value, 0f, MaxMicGain), MicKey);
    }

    // Fare hassasiyeti çarpanı (1 = varsayılan).
    public static float MouseSensitivity
    {
        get { Load(); return _sensitivity; }
        set => Set(ref _sensitivity, Mathf.Clamp(value, MinMouseSensitivity, MaxMouseSensitivity), SensitivityKey);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        _loaded = false;
        Load();
        AudioListener.volume = _master;
    }

    private static void Load()
    {
        if (_loaded)
            return;

        _loaded = true;
        _master = PlayerPrefs.GetFloat(MasterKey, _master);
        _music = PlayerPrefs.GetFloat(MusicKey, _music);
        _voice = PlayerPrefs.GetFloat(VoiceKey, _voice);
        _mic = PlayerPrefs.GetFloat(MicKey, _mic);
        _sensitivity = PlayerPrefs.GetFloat(SensitivityKey, _sensitivity);
    }

    private static void Set(ref float field, float value, string key)
    {
        Load();
        if (Mathf.Approximately(field, value))
            return;

        field = value;
        PlayerPrefs.SetFloat(key, value);
        AudioListener.volume = _master;
        Changed?.Invoke();
    }
}
