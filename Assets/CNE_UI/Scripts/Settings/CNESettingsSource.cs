using System;
using System.Collections.Generic;
using UnityEngine;

namespace CookNoEvil.UI
{
    /// <summary>Ayar kartindaki surgulu ayarlar.</summary>
    public enum CNEFloatSetting
    {
        MasterVolume,
        MusicVolume,
        VoiceVolume,
        MicGain,
        MouseSensitivity
    }

    /// <summary>Surgunun yanindaki rakam ekraninda degerin yazilisi.</summary>
    public enum CNEValueFormat
    {
        /// <summary>Deger x 100, tam sayi: ses 0,8 -> "80", mikrofon kazanci 1,0 -> "100".</summary>
        Percent,
        /// <summary>Iki ondalik: hassasiyet 1,25 -> "1.25".</summary>
        Decimal
    }

    /// <summary>Bir surgunun araligi ve gosterimi. Varsayilan deger ayar deposundadir, burada degil.</summary>
    [Serializable]
    public struct CNESliderSpec
    {
        public float min;
        public float max;
        public CNEValueFormat format;

        public CNESliderSpec(float min, float max, CNEValueFormat format)
        {
            this.min = min;
            this.max = max;
            this.format = format;
        }
    }

    /// <summary>Doner secicideki bir secenek (ornegin bir gorunum).</summary>
    [Serializable]
    public struct CNEChoice
    {
        [Tooltip("Kayit kimligi; bos olabilir (KAPALI gibi).")] public string id;
        [Tooltip("Ekranda gorunen ad.")] public string label;
        [Tooltip("Doluysa ad, CNE_UI tablosundaki bu anahtardan gelir (label yerine).")] public string labelKey;

        public CNEChoice(string id, string label, string labelKey = null)
        {
            this.id = id;
            this.label = label;
            this.labelKey = labelKey;
        }
    }

    /// <summary>
    /// Ayar kartinin konustugu ayar deposu. Kart yalnizca gorunumdur: degerleri buradan okur, buraya yazar.
    /// Projede oyunun kendi ayar deposuna (GameSettings + LookPreference) baglanan tek bir alt sinif yazilir;
    /// kitteki CNEPlayerPrefsSettingsSource yalnizca demo icindir.
    /// Tam ekran bu depoda degildir: kart onu dogrudan Screen.fullScreenMode ile yonetir.
    /// </summary>
    public abstract class CNESettingsSource : ScriptableObject
    {
        /// <summary>Bu satir kartta gosterilsin mi? Desteklenmeyen satir gizlenir.</summary>
        public virtual bool Supports(CNEFloatSetting setting)
        {
            return true;
        }

        public abstract CNESliderSpec GetSpec(CNEFloatSetting setting);
        public abstract float GetFloat(CNEFloatSetting setting);
        public abstract void SetFloat(CNEFloatSetting setting, float value);

        /// <summary>Gorunum secenekleri, sirayla. Ikiden az secenek varsa gorunum satiri gizlenir.</summary>
        public abstract void GetLookOptions(List<CNEChoice> results);

        /// <summary>Secili gorunumun kimligi.</summary>
        public abstract string Look { get; set; }

        /// <summary>VARSAYILANLAR dugmesi. Neyin sifirlanacagina depo karar verir.</summary>
        public abstract void ResetToDefaults();

        /// <summary>Kart kapaninca cagrilir (diske yazma).</summary>
        public virtual void Save()
        {
            PlayerPrefs.Save();
        }
    }
}
