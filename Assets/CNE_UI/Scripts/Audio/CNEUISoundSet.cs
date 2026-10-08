using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace CookNoEvil.UI
{
    /// <summary>
    /// UI ses seti: her olaya bir ya da birkac klip (varyasyon), ses seviyesi ve perde oynamasi.
    /// Paketteki asset: Assets/CNE_UI/Resources/CNE_UISoundSet.asset (CNEUIAudio onu otomatik yukler).
    /// </summary>
    [CreateAssetMenu(menuName = "Cook No Evil/UI/Ses Seti", fileName = "CNE_UISoundSet")]
    public class CNEUISoundSet : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public CNEUISound sound;
            [Tooltip("Birden fazla klip varsa sirayla tekrarlamadan rastgele secilir.")]
            public AudioClip[] clips = new AudioClip[0];
            [Range(0f, 1f)] public float volume = 1f;
            [Tooltip("Her calista perde +/- bu oranda oynar; tekrar yorgunlugunu azaltir.")]
            [Range(0f, 0.25f)] public float pitchJitter = 0.04f;
            [Tooltip("Ayni ses bu sureden (sn) daha sik calmaz (fareyi hizla gezdirince makineli tufek olmasin).")]
            public float minInterval = 0.03f;
        }

        [Tooltip("Bos birakilirsa sesler dogrudan cikisa gider. Projenizde AudioMixer varsa UI grubunu buraya verin.")]
        public AudioMixerGroup output;

        public Entry[] entries = new Entry[0];

        Dictionary<CNEUISound, Entry> lookup;

        public Entry Get(CNEUISound sound)
        {
            if (lookup == null) Build();
            Entry entry;
            return lookup.TryGetValue(sound, out entry) ? entry : null;
        }

        void OnEnable() { lookup = null; }

        void OnValidate() { lookup = null; }

        void Build()
        {
            lookup = new Dictionary<CNEUISound, Entry>();
            if (entries == null) return;
            for (int i = 0; i < entries.Length; i++)
            {
                var e = entries[i];
                if (e != null && !lookup.ContainsKey(e.sound)) lookup.Add(e.sound, e);
            }
        }
    }
}
