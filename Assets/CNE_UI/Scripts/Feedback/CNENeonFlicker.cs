using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CookNoEvil.UI
{
    /// <summary>
    /// Neon tabela davranisi: acilista citirdayip titreyerek yanar (ses ritmiyle ayni),
    /// bostayken arada bir kisa titrer. "nervousness" yuksekse daha sik ve cift titrer
    /// (logodaki "Evil!" kelimesi icin).
    /// </summary>
    [AddComponentMenu("Cook No Evil/UI/Neon Titremesi")]
    public class CNENeonFlicker : MonoBehaviour
    {
        [Tooltip("Neon tupleri (TMP yazi ya da Image).")] public Graphic[] tubes = new Graphic[0];
        [Tooltip("Arkadaki isik halesi (ui_glow).")] public Graphic[] glows = new Graphic[0];
        public Color onColor = Color.white;
        [Tooltip("Sonuk tup rengi. Alfa dusuk olmali: TMP parlamasi (glow) yazi renginin yalnizca alfasiyla kisilir, " +
                 "RGB'si parlamayi etkilemez; alfa 1 kalirsa tup sonerken parlama yanik kalir.")]
        public Color offColor = new Color(0.35f, 0.33f, 0.45f, 0.35f);
        [Range(0f, 1f)] public float glowAlpha = 0.55f;
        public bool igniteOnEnable = true;
        public float igniteDelay = 0f;
        public bool playSound = true;
        public bool idleFlicker = true;
        [Tooltip("Iki kisa titreme arasi sure (sn)")] public Vector2 idleInterval = new Vector2(5f, 12f);
        [Range(0f, 1f)] public float nervousness = 0f;

        // Ses dosyasindaki (ui_neon_on) kapi ritmiyle ayni: kapali/acik sureleri
        static readonly float[] IgnitePattern = { 0.03f, 0.04f, 0.03f, 0.03f, 0.05f };

        Coroutine routine;
        float nextIdle;

        void OnEnable()
        {
            if (igniteOnEnable && Application.isPlaying) routine = StartCoroutine(Ignite());
            else Set(1f);
            ScheduleIdle();
        }

        void OnDisable()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null;
        }

        /// <summary>Yeniden yakar (ornegin panel acilinca).</summary>
        public void Reignite()
        {
            if (!isActiveAndEnabled) return;
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(Ignite());
        }

        IEnumerator Ignite()
        {
            Set(0.06f);
            if (igniteDelay > 0f) yield return new WaitForSecondsRealtime(igniteDelay);
            if (playSound) CNEUIAudio.Play(CNEUISound.NeonOn);
            bool on = false;
            for (int i = 0; i < IgnitePattern.Length; i++)
            {
                Set(on ? 1f : 0.06f);
                yield return new WaitForSecondsRealtime(IgnitePattern[i]);
                on = !on;
            }
            Set(1f);
            routine = null;
            ScheduleIdle();
        }

        IEnumerator Dip()
        {
            Set(Random.Range(0.35f, 0.6f));
            yield return new WaitForSecondsRealtime(Random.Range(0.04f, 0.09f));
            Set(1f);
            if (Random.value < nervousness)
            {
                yield return new WaitForSecondsRealtime(Random.Range(0.05f, 0.12f));
                Set(0.2f);
                yield return new WaitForSecondsRealtime(0.05f);
                Set(1f);
            }
            routine = null;
        }

        void Update()
        {
            if (!idleFlicker || routine != null) return;
            if (Time.unscaledTime >= nextIdle)
            {
                routine = StartCoroutine(Dip());
                ScheduleIdle();
            }
        }

        void ScheduleIdle()
        {
            float wait = Random.Range(idleInterval.x, idleInterval.y) * (1f - nervousness * 0.6f);
            nextIdle = Time.unscaledTime + wait;
        }

        /// <summary>0 = sonuk, 1 = tam yanik.</summary>
        public void Set(float level)
        {
            var c = Color.LerpUnclamped(offColor, onColor, level);
            for (int i = 0; i < tubes.Length; i++)
                if (tubes[i] != null) tubes[i].color = c;
            for (int i = 0; i < glows.Length; i++)
            {
                if (glows[i] == null) continue;
                var g = glows[i].color;
                g.a = glowAlpha * Mathf.Clamp01(level);
                glows[i].color = g;
            }
        }
    }
}
