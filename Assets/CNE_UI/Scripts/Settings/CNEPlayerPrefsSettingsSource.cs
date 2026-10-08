using System;
using System.Collections.Generic;
using UnityEngine;

namespace CookNoEvil.UI
{
    /// <summary>
    /// Kitin demo ayar deposu: degerleri kendi PlayerPrefs anahtarlarinda tutar.
    /// Yalnizca demo sahnesi ve oyunun ayar deposu olmayan projeler icindir. Cook No Evil'da kart,
    /// GameSettings'e baglanan proje kaynagini kullanir; bu sinifi oraya atamayin (iki depo olur).
    /// Acilista hicbir seye yazmaz; ana ses yalnizca kullanici surguyu oynatinca AudioListener'a uygulanir.
    /// </summary>
    [CreateAssetMenu(menuName = "Cook No Evil/UI/Ayar Kaynagi (demo, PlayerPrefs)", fileName = "CNE_DemoSettingsSource")]
    public sealed class CNEPlayerPrefsSettingsSource : CNESettingsSource
    {
        [Serializable]
        public class FloatEntry
        {
            public CNEFloatSetting setting;
            public CNESliderSpec spec;
            public float defaultValue;

            public FloatEntry()
            {
            }

            public FloatEntry(CNEFloatSetting setting, float min, float max, float defaultValue, CNEValueFormat format)
            {
                this.setting = setting;
                spec = new CNESliderSpec(min, max, format);
                this.defaultValue = defaultValue;
            }
        }

        [Tooltip("PlayerPrefs anahtar oneki")] public string keyPrefix = "cne.demo.";
        [Tooltip("Surguler: aralik, gosterim, varsayilan")] public List<FloatEntry> floats = DefaultFloats();
        [Tooltip("Gorunum secenekleri (demo)")] public List<CNEChoice> looks = DefaultLooks();
        public string defaultLook = "cne";
        [Tooltip("Ana ses surgusu AudioListener.volume'a yazsin (yalnizca demo).")]
        public bool applyMasterToListener = true;

        static List<FloatEntry> DefaultFloats()
        {
            // Araliklar ve varsayilanlar oyunun GameSettings'iyle ayni tutuldu.
            return new List<FloatEntry>
            {
                new FloatEntry(CNEFloatSetting.MasterVolume, 0f, 1f, 1f, CNEValueFormat.Percent),
                new FloatEntry(CNEFloatSetting.MusicVolume, 0f, 1f, 0.5f, CNEValueFormat.Percent),
                new FloatEntry(CNEFloatSetting.VoiceVolume, 0f, 1f, 1f, CNEValueFormat.Percent),
                new FloatEntry(CNEFloatSetting.MicGain, 0f, 2f, 1f, CNEValueFormat.Percent),
                new FloatEntry(CNEFloatSetting.MouseSensitivity, 0.1f, 3f, 1f, CNEValueFormat.Decimal)
            };
        }

        static List<CNEChoice> DefaultLooks()
        {
            return new List<CNEChoice>
            {
                new CNEChoice("", "KAPALI", "settings.view.off"),
                new CNEChoice("stylized", "STİLİZE", "settings.view.stylized"),
                new CNEChoice("cne", "CNE TOON", "settings.view.toon")
            };
        }

        FloatEntry Find(CNEFloatSetting setting)
        {
            if (floats == null) return null;
            for (int i = 0; i < floats.Count; i++)
                if (floats[i] != null && floats[i].setting == setting) return floats[i];
            return null;
        }

        public override bool Supports(CNEFloatSetting setting)
        {
            return Find(setting) != null;
        }

        public override CNESliderSpec GetSpec(CNEFloatSetting setting)
        {
            var e = Find(setting);
            return e != null ? e.spec : new CNESliderSpec(0f, 1f, CNEValueFormat.Percent);
        }

        public override float GetFloat(CNEFloatSetting setting)
        {
            var e = Find(setting);
            if (e == null) return 0f;
            return Mathf.Clamp(PlayerPrefs.GetFloat(keyPrefix + setting, e.defaultValue), e.spec.min, e.spec.max);
        }

        public override void SetFloat(CNEFloatSetting setting, float value)
        {
            var e = Find(setting);
            if (e == null) return;
            value = Mathf.Clamp(value, e.spec.min, e.spec.max);
            PlayerPrefs.SetFloat(keyPrefix + setting, value);
            if (setting == CNEFloatSetting.MasterVolume && applyMasterToListener) AudioListener.volume = value;
        }

        public override void GetLookOptions(List<CNEChoice> results)
        {
            results.Clear();
            if (looks != null) results.AddRange(looks);
        }

        public override string Look
        {
            get { return PlayerPrefs.GetString(keyPrefix + "look", defaultLook); }
            set { PlayerPrefs.SetString(keyPrefix + "look", value ?? string.Empty); }
        }

        public override void ResetToDefaults()
        {
            if (floats == null) return;
            for (int i = 0; i < floats.Count; i++)
                if (floats[i] != null) SetFloat(floats[i].setting, floats[i].defaultValue);
            // Gorunum bilerek sifirlanmaz (oyunun deposuyla ayni davranis).
        }
    }
}
