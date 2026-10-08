using CookNoEvil.UI;
using UnityEngine;

// Arayüz kitinin jukebox şeridi ile oyunun müzik çaları arasındaki köprü. Kit müzik çalmaz; şerit MusicPlayer'ı
// gösterir ve "sonraki parça" der. MusicPlayer'da önceki parça yolu olmadığı için geri oku gizli kalır.
// Parçanın lisansı atıf istiyorsa (CC-BY) atıf adın yanında gösterilir.
[CreateAssetMenu(menuName = "Cook No Evil/UI/Müzik Kaynağı", fileName = "MusicPlayerUISource")]
public sealed class MusicPlayerUISource : CNEMusicSource
{
    [Tooltip("Parça adı ile atıf arasına konan ayraç.")]
    [SerializeField] private string creditSeparator = " · ";

    public override int TrackCount => MusicPlayer.Instance != null ? MusicPlayer.Instance.TrackCount : 0;

    public override int CurrentIndex => MusicPlayer.Instance != null ? MusicPlayer.Instance.CurrentTrackIndex : -1;

    public override string CurrentTitle
    {
        get
        {
            var player = MusicPlayer.Instance;
            if (player == null)
                return string.Empty;

            string credit = player.CurrentTrackCredit;
            return string.IsNullOrEmpty(credit) ? player.CurrentTrackName : player.CurrentTrackName + creditSeparator + credit;
        }
    }

    public override bool IsPlaying => MusicPlayer.Instance != null && MusicPlayer.Instance.IsPlaying;

    public override void Next()
    {
        if (MusicPlayer.Instance != null)
            MusicPlayer.Instance.SelectNextTrack();
    }
}
