using UnityEngine;

namespace CookNoEvil.UI
{
    /// <summary>Animasyon egrileri. Hepsi 0..1 alir; OutBack 1'i bir an asar (asma / overshoot).</summary>
    public static class CNEEase
    {
        public static float Linear(float t) { return t; }

        public static float InQuad(float t) { return t * t; }

        public static float OutQuad(float t) { return 1f - (1f - t) * (1f - t); }

        public static float InCubic(float t) { return t * t * t; }

        public static float OutCubic(float t)
        {
            float u = 1f - t;
            return 1f - u * u * u;
        }

        public static float InOutCubic(float t)
        {
            return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;
        }

        public static float OutBack(float t) { return OutBack(t, 1.70158f); }

        /// <summary>overshoot buyudukce asma artar (1.7 klasik, 2.5+ "boing" degil ama tok bir oturma).</summary>
        public static float OutBack(float t, float overshoot)
        {
            float u = t - 1f;
            return 1f + (overshoot + 1f) * u * u * u + overshoot * u * u;
        }

        /// <summary>Sonumlu salinim: asili tabelanin oturmasi. 0'da ve 1'de sifirdir.</summary>
        public static float Swing(float t, float cycles, float damping)
        {
            return Mathf.Sin(t * cycles * Mathf.PI * 2f) * Mathf.Exp(-damping * t) * (1f - t);
        }
    }
}
