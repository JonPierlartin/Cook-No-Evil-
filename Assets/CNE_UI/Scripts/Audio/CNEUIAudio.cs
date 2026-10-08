using System.Collections.Generic;
using UnityEngine;

namespace CookNoEvil.UI
{
    /// <summary>
    /// UI seslerini calan servis. Sahneye eklemeniz gerekmez: ilk CNEUIAudio.Play cagrisinda
    /// kendini olusturur, sahneler arasi yasar ve Resources/CNE_UISoundSet'i yukler.
    /// Kaynaklar 2B'dir ve dinleyici efektlerini atlamaz: ana ses (AudioListener.volume, oyunun ayar deposu yazar)
    /// ve dinleyicideki filtreler (ornegin Komi'nin sagirligi) arayuz seslerine de uygulanir.
    /// </summary>
    [AddComponentMenu("")]
    public sealed class CNEUIAudio : MonoBehaviour
    {
        const string ResourcePath = "CNE_UISoundSet";
        const int PoolSize = 10;

        static CNEUIAudio instance;
        static bool warned;

        CNEUISoundSet soundSet;
        AudioSource[] pool;
        int nextSource;
        readonly Dictionary<CNEUISound, float> lastPlayTime = new Dictionary<CNEUISound, float>();
        readonly Dictionary<CNEUISound, int> lastClip = new Dictionary<CNEUISound, int>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            instance = null;
            warned = false;
        }

        /// <summary>Calismakta olan ses seti. Farkli bir set vermek icin atayin (ornegin oyun ici).</summary>
        public static CNEUISoundSet SoundSet
        {
            get { var a = Instance; return a != null ? a.soundSet : null; }
            set { var a = Instance; if (a != null) a.soundSet = value; }
        }

        static CNEUIAudio Instance
        {
            get
            {
                if (instance != null) return instance;
                if (!Application.isPlaying) return null;
                var go = new GameObject("[CNE UI Audio]");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<CNEUIAudio>();
                return instance;
            }
        }

        /// <summary>Bir UI sesi calar. volumeScale ve pitchScale ses setindeki degerlerle carpilir.</summary>
        public static void Play(CNEUISound sound, float volumeScale = 1f, float pitchScale = 1f)
        {
            if (sound == CNEUISound.None) return;
            var a = Instance;
            if (a != null) a.PlayInternal(sound, volumeScale, pitchScale);
        }

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            soundSet = Resources.Load<CNEUISoundSet>(ResourcePath);
            pool = new AudioSource[PoolSize];
            for (int i = 0; i < PoolSize; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.loop = false;
                src.spatialBlend = 0f;
                src.ignoreListenerPause = true; // oyun duraklatilsa da UI sesi calar
                src.priority = 32;
                pool[i] = src;
            }
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        void PlayInternal(CNEUISound sound, float volumeScale, float pitchScale)
        {
            if (soundSet == null)
            {
                if (!warned)
                {
                    Debug.LogWarning("[CNE UI] Resources/CNE_UISoundSet bulunamadi; UI sesleri calmayacak.");
                    warned = true;
                }
                return;
            }

            var entry = soundSet.Get(sound);
            if (entry == null || entry.clips == null || entry.clips.Length == 0) return;

            float now = Time.unscaledTime;
            float last;
            if (lastPlayTime.TryGetValue(sound, out last) && now - last < entry.minInterval) return;
            lastPlayTime[sound] = now;

            var clip = entry.clips[PickClip(sound, entry.clips.Length)];
            if (clip == null) return;

            var src = pool[nextSource];
            nextSource = (nextSource + 1) % pool.Length;
            src.Stop();
            src.outputAudioMixerGroup = soundSet.output;
            src.clip = clip;
            src.volume = Mathf.Clamp01(entry.volume * volumeScale);
            src.pitch = pitchScale * (1f + Random.Range(-entry.pitchJitter, entry.pitchJitter));
            src.Play();
        }

        int PickClip(CNEUISound sound, int count)
        {
            if (count <= 1) return 0;
            int previous;
            if (!lastClip.TryGetValue(sound, out previous)) previous = -1;
            int pick = Random.Range(0, previous < 0 ? count : count - 1);
            if (previous >= 0 && pick >= previous) pick++;
            lastClip[sound] = pick;
            return pick;
        }
    }
}
