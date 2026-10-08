using UnityEngine;

namespace CookNoEvil.UI
{
    /// <summary>
    /// Ayarlardaki mikrofon testinin (VU ibresi) seviye kaynagi. Atanmazsa test satiri gizlenir.
    /// Kitte Unity'nin Microphone API'siyle calisan CNEUnityMicrophoneSource var; ses sohbeti baska bir
    /// yoldan (ornegin Steam Voice) geliyorsa o yoldan okuyan bir alt sinif yazilmalidir.
    /// </summary>
    public abstract class CNEMicLevelSource : ScriptableObject
    {
        /// <summary>Kullanilabilir bir mikrofon var mi?</summary>
        public abstract bool IsAvailable { get; }

        /// <summary>Dinlemeye baslar. Basaramazsa false.</summary>
        public abstract bool Begin();

        public abstract void End();

        /// <summary>Son kisa pencerenin RMS seviyesi (0..1), kazanc uygulanmamis.</summary>
        public abstract float ReadRms();
    }
}
