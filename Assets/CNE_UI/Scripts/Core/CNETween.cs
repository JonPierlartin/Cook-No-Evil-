using System;
using System.Collections.Generic;
using UnityEngine;

namespace CookNoEvil.UI
{
    /// <summary>
    /// Bagimliliksiz kucuk tween motoru (DOTween gerekmez).
    /// - Time.timeScale'e bakmaz: oyun dururken (ESC) menuler yine canlidir.
    /// - Ayni sahip + kanal icin yeni tween eskisini durdurur; animasyonlar cakismaz.
    /// - Sahip (component) yok edilirse tween kendiliginden biter.
    /// - Edit Mode'da anlik olarak son degeri uygular.
    /// </summary>
    public static class CNETween
    {
        internal sealed class Tween
        {
            public UnityEngine.Object owner;
            public string channel;
            public float delay;
            public float duration;
            public float elapsed;
            public Func<float, float> ease;
            public Action<float> onUpdate;
            public Action onComplete;
            public bool dead;
        }

        static readonly List<Tween> active = new List<Tween>();
        static readonly List<Tween> pending = new List<Tween>();
        static CNETweenRunner runner;
        static bool updating;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            active.Clear();
            pending.Clear();
            runner = null;
            updating = false;
        }

        /// <summary>
        /// owner + channel icin 0..1 arasinda ilerleyen bir tween baslatir.
        /// onUpdate, ease uygulanmis degeri alir (OutBack'te 1'i asabilir; LerpUnclamped kullanin).
        /// </summary>
        public static void To(UnityEngine.Object owner, string channel, float duration, Action<float> onUpdate,
            Func<float, float> ease = null, Action onComplete = null, float delay = 0f)
        {
            if (owner == null || onUpdate == null) return;
            Func<float, float> curve = ease ?? CNEEase.Linear;
            Kill(owner, channel);

            if (!Application.isPlaying)
            {
                onUpdate(curve(1f));
                if (onComplete != null) onComplete();
                return;
            }

            EnsureRunner();
            var tween = new Tween
            {
                owner = owner,
                channel = channel ?? string.Empty,
                delay = Mathf.Max(0f, delay),
                duration = Mathf.Max(0.0001f, duration),
                ease = curve,
                onUpdate = onUpdate,
                onComplete = onComplete
            };
            if (updating) pending.Add(tween);
            else active.Add(tween);

            if (tween.delay <= 0f) onUpdate(curve(0f)); // ilk kareyi hemen uygula, titreme olmasin
        }

        /// <summary>owner'in tweenlerini durdurur. channel null ise hepsi. complete: son degeri uygula.</summary>
        public static void Kill(UnityEngine.Object owner, string channel = null, bool complete = false)
        {
            if (ReferenceEquals(owner, null)) return;
            KillIn(active, owner, channel, complete);
            KillIn(pending, owner, channel, complete);
        }

        public static bool IsRunning(UnityEngine.Object owner, string channel)
        {
            if (ReferenceEquals(owner, null)) return false;
            for (int i = 0; i < active.Count; i++)
            {
                var t = active[i];
                if (!t.dead && ReferenceEquals(t.owner, owner) && (channel == null || t.channel == channel)) return true;
            }
            for (int i = 0; i < pending.Count; i++)
            {
                var t = pending[i];
                if (!t.dead && ReferenceEquals(t.owner, owner) && (channel == null || t.channel == channel)) return true;
            }
            return false;
        }

        // Sahip esitligi referansla: GetInstanceID 6.4'ten beri eskimis (EntityId), referans her surumde ayni.
        static void KillIn(List<Tween> list, UnityEngine.Object owner, string channel, bool complete)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var t = list[i];
                if (t.dead || !ReferenceEquals(t.owner, owner)) continue;
                if (channel != null && t.channel != channel) continue;
                t.dead = true;
                if (complete && t.owner != null)
                {
                    SafeInvoke(t, 1f);
                    if (t.onComplete != null) SafeComplete(t);
                }
            }
        }

        static void EnsureRunner()
        {
            if (runner != null) return;
            var go = new GameObject("[CNE Tween]");
            go.hideFlags = HideFlags.HideInHierarchy;
            UnityEngine.Object.DontDestroyOnLoad(go);
            runner = go.AddComponent<CNETweenRunner>();
        }

        internal static void Step(float dt)
        {
            updating = true;
            for (int i = 0; i < active.Count; i++)
            {
                var t = active[i];
                if (t.dead) continue;
                if (t.owner == null) { t.dead = true; continue; }

                if (t.delay > 0f)
                {
                    t.delay -= dt;
                    if (t.delay > 0f) continue;
                    t.elapsed = -t.delay;
                    t.delay = 0f;
                }
                else
                {
                    t.elapsed += dt;
                }

                float p = Mathf.Clamp01(t.elapsed / t.duration);
                if (!SafeInvoke(t, p)) { t.dead = true; continue; }
                if (p >= 1f)
                {
                    t.dead = true;
                    if (t.onComplete != null) SafeComplete(t);
                }
            }
            updating = false;
            active.RemoveAll(x => x.dead);
            if (pending.Count > 0)
            {
                active.AddRange(pending);
                pending.Clear();
            }
        }

        static bool SafeInvoke(Tween t, float p)
        {
            try
            {
                t.onUpdate(t.ease(p));
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
        }

        static void SafeComplete(Tween t)
        {
            try { t.onComplete(); }
            catch (Exception e) { Debug.LogException(e); }
        }
    }
}
