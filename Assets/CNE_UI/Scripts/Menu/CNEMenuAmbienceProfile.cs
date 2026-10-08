using UnityEngine;

namespace CookNoEvil.UI
{
    /// <summary>
    /// Ana menunun gece ortami icin veri: ortam isigini kisma, yansimayi azaltma, gokyuzu ve kamera arka plani.
    /// Sahneye ait baglantilar (kamera, kadraj noktasi, isik duzenegi) CNEMenuAmbience bilesenindedir.
    /// </summary>
    [CreateAssetMenu(menuName = "Cook No Evil/UI/Menu Gece Ortami Profili", fileName = "CNE_MenuNightProfile")]
    public sealed class CNEMenuAmbienceProfile : ScriptableObject
    {
        [Header("Ortam isigi (ambient probe)")]
        [Tooltip("Ortam isiginin carpani (1 = degismez). Gece icin 0,2-0,4.")]
        [Range(0f, 1.5f)] public float ambientMultiplier = 0.3f;
        [Tooltip("Ortam isigi bu renkle carpilir (ay mavisi).")]
        public Color ambientTint = new Color(0.62f, 0.70f, 1f, 1f);

        [Header("Yansima")]
        [Tooltip("Ortam yansimasinin carpani (1 = degismez). Gunduz gokyuzunun parlak yansimalarini bastirir.")]
        [Range(0f, 1.5f)] public float reflectionMultiplier = 0.35f;

        [Header("Gokyuzu ve kamera arka plani")]
        [Tooltip("Menude kullanilacak gece gokyuzu materyali. Bossa gokyuzune dokunulmaz.")]
        public Material nightSkybox;
        [Tooltip("Kamera gokyuzu yerine duz renk temizlesin (vitrinden gorunen disarisi).")]
        public bool overrideCameraBackground;
        public Color cameraBackground = new Color(0.055f, 0.086f, 0.149f, 1f); // #0E1626
    }
}
