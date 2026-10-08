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
    private const string MusicTrackKey = "settings.musicTrack";

    public const float MaxMicGain = 2f;
    public const float MinMouseSensitivity = 0.2f;
    public const float MaxMouseSensitivity = 3f;

    // Fabrika değerleri: hiç ayar yapılmamış oyunun açıldığı ve "Varsayılanlar"ın döndüğü değerler (tek yer).
    public const float DefaultMasterVolume = 1f;
    public const float DefaultMusicVolume = 0.5f;
    public const float DefaultVoiceVolume = 1f;
    public const float DefaultMicGain = 1f;
    public const float DefaultMouseSensitivity = 1f;

    private static bool _loaded;
    private static float _master = DefaultMasterVolume;
    private static float _music = DefaultMusicVolume;
    private static float _voice = DefaultVoiceVolume;
    private static float _mic = DefaultMicGain;
    private static float _sensitivity = DefaultMouseSensitivity;
    private static int _musicTrack;

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

    // Çalan müzik parçasının dizini (liste MusicPlayer'dadır).
    public static int MusicTrack
    {
        get { Load(); return _musicTrack; }
        set
        {
            Load();
            int clamped = Mathf.Max(0, value);
            if (_musicTrack == clamped)
                return;

            _musicTrack = clamped;
            PlayerPrefs.SetInt(MusicTrackKey, clamped);
            Changed?.Invoke();
        }
    }

    // Ses seviyelerini ve fare hassasiyetini fabrika değerine döndürür. Seçili müzik parçası bir seviye değil,
    // tercihtir; ona dokunmaz.
    public static void ResetToDefaults()
    {
        MasterVolume = DefaultMasterVolume;
        MusicVolume = DefaultMusicVolume;
        VoiceVolume = DefaultVoiceVolume;
        MicGain = DefaultMicGain;
        MouseSensitivity = DefaultMouseSensitivity;
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
        _musicTrack = PlayerPrefs.GetInt(MusicTrackKey, _musicTrack);
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
