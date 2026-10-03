using UnityEngine;

// Teslim geri bildirimi — sesler (GDD 7.1.1: tek olay, role göre ayrı sunum). Olay tektir: bir müşteri memnun
// ayrıldı (doğru teslim) ya da bölüme 1 Hata yazıldı (yanlış/geç teslim, sabır). Sunum MEKÂNSALDIR, role göre kod
// yoktur: sesler dünyadaki kaynaklardan çalar — Kasa'daki kaynağı Kasiyer (yazar kasa / hata), mutfaktaki kaynağı
// Şef duyar. Komi sağırdır: kaynaklar RoleAwareAudioRange taşır; onun görsel göstergesi duvar panelidir (ayrı adım).
// Her istemcide yerel çalışır; replike durumdan (Customer.Mood, GameLoopManager.ErrorCount) tetiklenir.
public class DeliveryFeedback : MonoBehaviour
{
    [Tooltip("Sesin çalacağı dünya kaynakları (ör. Kasa tezgahı, mutfak). Hepsinde aynı klip çalar.")]
    [SerializeField] private AudioSource[] sources;
    [Tooltip("Doğru teslim: yazar kasa 'ching'.")]
    [SerializeField] private AudioClip successClip;
    [Tooltip("1 Hata: belirgin hata sesi.")]
    [SerializeField] private AudioClip errorClip;

    private GameLoopManager _loop;

    private void Start()
    {
        Customer.MoodChanged += HandleMoodChanged;

        // Singleton'lara Awake'te erişilmez (CLAUDE.md NGO notu).
        _loop = GameLoopManager.Instance;
        if (_loop != null)
            _loop.ErrorCount.OnValueChanged += HandleErrorCountChanged;
    }

    private void OnDestroy()
    {
        Customer.MoodChanged -= HandleMoodChanged;
        if (_loop != null)
            _loop.ErrorCount.OnValueChanged -= HandleErrorCountChanged;
    }

    private void HandleMoodChanged(Customer customer, CustomerMood mood)
    {
        if (mood == CustomerMood.Happy)
            Play(successClip);
    }

    // Sayaç yalnızca arttığında (round başındaki sıfırlamada değil).
    private void HandleErrorCountChanged(int previous, int current)
    {
        if (current > previous)
            Play(errorClip);
    }

    private void Play(AudioClip clip)
    {
        if (clip == null)
            return;

        foreach (var source in sources)
        {
            if (source != null)
                source.PlayOneShot(clip);
        }
    }
}
