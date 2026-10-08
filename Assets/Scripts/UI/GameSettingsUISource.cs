using System.Collections.Generic;
using CookNoEvil.UI;
using UnityEngine;

// Arayüz kitinin Ayarlar kartı ile oyunun ayar deposu arasındaki köprü. Kart yalnızca görünümdür: değerleri
// buradan okur, buraya yazar; tek kaynak GameSettings (sürgüler) ve LookPreference'tır (görünüm). Köprü
// AudioListener'a ya da PlayerPrefs'e doğrudan yazmaz — GameSettings kendi uygulamasını ve bildirimini yapar.
// Sürgülerin aralığı ve yazılışı bu asset'in verisidir; varsayılan DEĞERLER burada tutulmaz (depodadır).
[CreateAssetMenu(menuName = "Cook No Evil/UI/Oyun Ayarları Kaynağı", fileName = "GameSettingsUISource")]
public sealed class GameSettingsUISource : CNESettingsSource
{
    [Header("Sürgülerin aralığı ve yazılışı")]
    [SerializeField] private CNESliderSpec masterVolume = new(0f, 1f, CNEValueFormat.Percent);
    [SerializeField] private CNESliderSpec musicVolume = new(0f, 1f, CNEValueFormat.Percent);
    [SerializeField] private CNESliderSpec voiceVolume = new(0f, 1f, CNEValueFormat.Percent);
    [Tooltip("Mikrofon kazancı: 1 = olduğu gibi (ekranda 100).")]
    [SerializeField] private CNESliderSpec micGain = new(0f, GameSettings.MaxMicGain, CNEValueFormat.Percent);
    [SerializeField] private CNESliderSpec mouseSensitivity =
        new(GameSettings.MinMouseSensitivity, GameSettings.MaxMouseSensitivity, CNEValueFormat.Decimal);

    [Header("Görünüm")]
    [Tooltip("Hiçbir görünüm seçili değilken düğmede yazan ad.")]
    [SerializeField] private string offLabel = "KAPALI";
    [Tooltip("Aynı adın arayüz kitinin metin tablosundaki anahtarı (varsa ad oradan gelir).")]
    [SerializeField] private string offLabelKey = "settings.view.off";

    public override CNESliderSpec GetSpec(CNEFloatSetting setting)
    {
        return setting switch
        {
            CNEFloatSetting.MasterVolume => masterVolume,
            CNEFloatSetting.MusicVolume => musicVolume,
            CNEFloatSetting.VoiceVolume => voiceVolume,
            CNEFloatSetting.MicGain => micGain,
            _ => mouseSensitivity,
        };
    }

    public override float GetFloat(CNEFloatSetting setting)
    {
        return setting switch
        {
            CNEFloatSetting.MasterVolume => GameSettings.MasterVolume,
            CNEFloatSetting.MusicVolume => GameSettings.MusicVolume,
            CNEFloatSetting.VoiceVolume => GameSettings.VoiceVolume,
            CNEFloatSetting.MicGain => GameSettings.MicGain,
            _ => GameSettings.MouseSensitivity,
        };
    }

    public override void SetFloat(CNEFloatSetting setting, float value)
    {
        switch (setting)
        {
            case CNEFloatSetting.MasterVolume: GameSettings.MasterVolume = value; break;
            case CNEFloatSetting.MusicVolume: GameSettings.MusicVolume = value; break;
            case CNEFloatSetting.VoiceVolume: GameSettings.VoiceVolume = value; break;
            case CNEFloatSetting.MicGain: GameSettings.MicGain = value; break;
            default: GameSettings.MouseSensitivity = value; break;
        }
    }

    // "Kapalı" + kayıtlı görünümler, kayıt sırasıyla. Bir görünüm kalkarsa (klasörü silinirse) listeden de kalkar.
    public override void GetLookOptions(List<CNEChoice> results)
    {
        results.Add(new CNEChoice(LookPreference.Off, offLabel, offLabelKey));
        foreach (var option in LookPreference.Options)
            results.Add(new CNEChoice(option.Id, option.Label));
    }

    public override string Look
    {
        get => LookPreference.Selected;
        set => LookPreference.Selected = value;
    }

    // BEKLEYEN KARAR (Ersel): GameSettings varsayılanlarını dışarı vermiyor ve sıfırlama yolu yok. Sayılar buraya
    // kopyalanmaz (iki yerde durup ayrışmasınlar); GameSettings'e bir sıfırlama metodu eklenince buradan çağrılacak.
    public override void ResetToDefaults()
    {
        Debug.LogWarning("[GameSettingsUISource] VARSAYILANLAR henüz bağlı değil: GameSettings'te sıfırlama yolu yok.");
    }

    // GameSettings her değişiklikte kendi yazar; ayrıca diske yazdırma kararı bekliyor (bugünkü davranış korunur).
    public override void Save()
    {
    }
}
