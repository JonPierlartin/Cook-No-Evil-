using UnityEngine;

// GDD 10.5 Şef'in işitsel telafisi: ızgarada et pişerken SABİT bir cızırtı çalar; faz değişiminde
// (Çiğ→Pişmiş→Yanmış) ses değişmez ve ek ses çıkmaz — Şef "burada bir şey pişiyor"u duyar, "hazır oldu"yu
// duymaz. Ses mekânsaldır (K4, AudioSource 3B): Komi ve Kasiyer mesafeden dolayı duymaz (GDD 4.1.3).
//
// Tamamen yerel: "pişiyor mu" kararı Grill.IsCooking'den, sunucunun pişirmesiyle AYNI kuraldan okunur;
// ağdan ses durumu gönderilmez. Oyun duraklatılınca (kopma) pişme durduğu için ses de susar.
[RequireComponent(typeof(Grill))]
[RequireComponent(typeof(AudioSource))]
public class GrillSizzle : MonoBehaviour
{
    private Grill _grill;
    private AudioSource _source;

    private void Awake()
    {
        _grill = GetComponent<Grill>();
        _source = GetComponent<AudioSource>();
        _source.playOnAwake = false;
        _source.loop = true;
    }

    private void Update()
    {
        bool cooking = _grill.IsCooking;
        if (cooking == _source.isPlaying)
            return;

        if (cooking)
            _source.Play();
        else
            _source.Stop();
    }
}
