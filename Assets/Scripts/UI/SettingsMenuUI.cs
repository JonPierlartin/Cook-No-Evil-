using System;
using UnityEngine;
using UnityEngine.UI;

// ESC menüsündeki ayarlar: kaydırıcıları ve kutucukları GameSettings'e bağlar. Yalnızca bağlar — ayarın değeri,
// saklanması ve uygulanması GameSettings'tedir; menünün açılıp kapanması PauseMenuUI'dadır. Ayarlar yereldir
// (K9: menü oyunu durdurmaz, başkasını etkilemez).
public class SettingsMenuUI : MonoBehaviour
{
    [Serializable]
    private struct SliderRow
    {
        public Slider slider;
        [Tooltip("Değerin yüzde olarak yazıldığı etiket.")]
        public Text valueLabel;
    }

    [SerializeField] private SliderRow masterVolume;
    [SerializeField] private SliderRow musicVolume;
    [SerializeField] private SliderRow voiceVolume;
    [SerializeField] private SliderRow micGain;
    [SerializeField] private SliderRow mouseSensitivity;
    [SerializeField] private Toggle fullscreen;

    private void Awake()
    {
        Bind(masterVolume, 0f, 1f, value => GameSettings.MasterVolume = value);
        Bind(musicVolume, 0f, 1f, value => GameSettings.MusicVolume = value);
        Bind(voiceVolume, 0f, 1f, value => GameSettings.VoiceVolume = value);
        Bind(micGain, 0f, GameSettings.MaxMicGain, value => GameSettings.MicGain = value);
        Bind(mouseSensitivity, GameSettings.MinMouseSensitivity, GameSettings.MaxMouseSensitivity, value => GameSettings.MouseSensitivity = value);

        fullscreen.onValueChanged.AddListener(on =>
            Screen.fullScreenMode = on ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
    }

    // Menü her açıldığında güncel değerleri gösterir.
    private void OnEnable()
    {
        Show(masterVolume, GameSettings.MasterVolume);
        Show(musicVolume, GameSettings.MusicVolume);
        Show(voiceVolume, GameSettings.VoiceVolume);
        Show(micGain, GameSettings.MicGain);
        Show(mouseSensitivity, GameSettings.MouseSensitivity);
        fullscreen.SetIsOnWithoutNotify(Screen.fullScreen);
    }

    private static void Bind(SliderRow row, float min, float max, Action<float> apply)
    {
        row.slider.minValue = min;
        row.slider.maxValue = max;
        row.slider.onValueChanged.AddListener(value =>
        {
            apply(value);
            row.valueLabel.text = FormatPercent(value);
        });
    }

    private static void Show(SliderRow row, float value)
    {
        row.slider.SetValueWithoutNotify(value);
        row.valueLabel.text = FormatPercent(value);
    }

    private static string FormatPercent(float value) => $"%{Mathf.RoundToInt(value * 100f)}";
}
