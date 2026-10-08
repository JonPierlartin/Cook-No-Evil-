using UnityEngine;

namespace CookNoEvil.UI
{
    /// <summary>
    /// Jukebox seridinin gosterdigi muzik calar. Kit muzik calmaz: projede oyunun MusicPlayer'ina
    /// baglanan tek bir alt sinif yazilir. Serit degisiklikleri kendisi fark eder (her kare CurrentIndex ve
    /// CurrentTitle'a bakar), bu yuzden kaynagin olay yayinlamasi gerekmez.
    /// </summary>
    public abstract class CNEMusicSource : ScriptableObject
    {
        public abstract int TrackCount { get; }

        /// <summary>Calan parcanin sirasi (0'dan); parca yoksa -1.</summary>
        public abstract int CurrentIndex { get; }

        /// <summary>Seritte gorunen ad (lisans geregi atif gerekiyorsa buraya eklenebilir).</summary>
        public abstract string CurrentTitle { get; }

        /// <summary>Plak donsun mu?</summary>
        public abstract bool IsPlaying { get; }

        /// <summary>Onceki parca destekleniyor mu? Desteklenmiyorsa seritteki geri oku gizlenir.</summary>
        public virtual bool CanGoPrevious
        {
            get { return false; }
        }

        public abstract void Next();

        public virtual void Previous()
        {
        }
    }
}
