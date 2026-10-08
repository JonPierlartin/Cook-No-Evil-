using System;
using UnityEngine;

// Arka plan müziği: 2B, döngülü, oyun dünyasına ait değil (mekânsal değil, oyun bilgisi taşımaz). Hangi parçanın
// çalacağı ve seviyesi oyuncunun yerel ayarından gelir (GameSettings.MusicTrack / MusicVolume); parça listesi veridir.
// Kaynakta "Bypass Listener Effects" açık tutulur: sağır rolün dinleyicisindeki boğukluk filtresi (DeafHearing) oyun
// DÜNYASININ sesleri içindir, müziğe uygulanmaz.
[RequireComponent(typeof(AudioSource))]
public class MusicPlayer : MonoBehaviour
{
    [Serializable]
    private struct Track
    {
        [Tooltip("Menüde görünen ad.")]
        public string displayName;
        public AudioClip clip;
        [Tooltip("Lisansın istediği atıf (ör. CC-BY): parça adının yanında gösterilir. Gerekmiyorsa boş.")]
        public string credit;
    }

    public static MusicPlayer Instance { get; private set; }

    [Tooltip("Seçilebilir parçalar. Ayardaki dizin listeden büyükse ilk parça çalar.")]
    [SerializeField] private Track[] tracks;
    [Tooltip("Ayar %100'deyken müziğin kaynak seviyesi (müzik efektlerin önüne geçmesin).")]
    [SerializeField, Range(0f, 1f)] private float baseVolume = 0.35f;

    private AudioSource _source;

    public int TrackCount => tracks != null ? tracks.Length : 0;

    // Ayardaki dizinin listeye sığdırılmış hâli (liste boşsa -1).
    public int CurrentTrackIndex => TrackCount == 0 ? -1 : Mathf.Clamp(GameSettings.MusicTrack, 0, TrackCount - 1);

    public string CurrentTrackName => CurrentTrackIndex >= 0 ? tracks[CurrentTrackIndex].displayName : string.Empty;

    // Çalan parçanın atfı (yoksa boş).
    public string CurrentTrackCredit => CurrentTrackIndex >= 0 ? tracks[CurrentTrackIndex].credit ?? string.Empty : string.Empty;

    // Müzik şu an çalıyor mu (arayüzdeki plağın dönmesi için; salt okunur).
    public bool IsPlaying => _source != null && _source.isPlaying;

    private void Awake()
    {
        Instance = this;
        _source = GetComponent<AudioSource>();
        _source.loop = true;
    }

    private void OnEnable()
    {
        GameSettings.Changed += Apply;
        Apply();
    }

    private void OnDisable()
    {
        GameSettings.Changed -= Apply;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // Sıradaki parçaya geçer (sondan sonra başa döner).
    public void SelectNextTrack()
    {
        if (TrackCount > 0)
            GameSettings.MusicTrack = (CurrentTrackIndex + 1) % TrackCount;
    }

    private void Apply()
    {
        _source.volume = baseVolume * GameSettings.MusicVolume;

        int index = CurrentTrackIndex;
        if (index < 0)
            return;

        var clip = tracks[index].clip;
        if (_source.clip != clip)
        {
            _source.clip = clip;
            _source.Play();
        }
        else if (!_source.isPlaying)
        {
            _source.Play();
        }
    }
}
