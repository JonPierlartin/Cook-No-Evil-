using UnityEngine;

// ProgressProfile'in tek bir fazi: adi (yalnizca Inspector/debug icin, kodda okunmaz) ve suresi
// (saniye). GDD 5.2.1: "Her faz gecisi 3-10 sn araliginda, urun bazinda ayarlanabilir olmalidir" —
// bu SURE degeri kodda SABITLENMEZ, her urunun kendi ProgressProfile asset'inde secilir; bu struct
// yalnizca VERI SEKLINI tanimlar, hicbir sayiyi varsaymaz.
[System.Serializable]
public struct ProgressPhase
{
    [SerializeField] private string name;
    [Tooltip("Bu fazda kalinacak sure (saniye). GDD 5.2.1 yol gostericisi: urun basina 3-10 sn " +
        "arasinda secilir; bu bir kod kisiti degildir, tasarim onerisidir.")]
    [SerializeField, Min(0.01f)] private float duration;

    public readonly string Name => name;
    public readonly float Duration => duration;
}
