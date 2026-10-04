using UnityEngine;

// Arka plan müziği: 2B, döngülü, oyun dünyasına ait değil (mekânsal değil, oyun bilgisi taşımaz). Seviyesi oyuncunun
// yerel ayarından gelir (GameSettings.MusicVolume). Kaynakta "Bypass Listener Effects" açık tutulur: sağır rolün
// dinleyicisindeki boğukluk filtresi (DeafHearing) oyun DÜNYASININ sesleri içindir, müziğe uygulanmaz.
[RequireComponent(typeof(AudioSource))]
public class MusicPlayer : MonoBehaviour
{
    [Tooltip("Ayar %100'deyken müziğin kaynak seviyesi (müzik efektlerin önüne geçmesin).")]
    [SerializeField, Range(0f, 1f)] private float baseVolume = 0.35f;

    private AudioSource _source;

    private void Awake()
    {
        _source = GetComponent<AudioSource>();
        ApplyVolume();
    }

    private void OnEnable()
    {
        GameSettings.Changed += ApplyVolume;
        ApplyVolume();
        if (!_source.isPlaying)
            _source.Play();
    }

    private void OnDisable()
    {
        GameSettings.Changed -= ApplyVolume;
    }

    private void ApplyVolume()
    {
        _source.volume = baseVolume * GameSettings.MusicVolume;
    }
}
