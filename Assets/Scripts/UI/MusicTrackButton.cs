using UnityEngine;
using UnityEngine.UI;

// Müzik parçası seçme düğmesi: tıklayınca sıradaki parçaya geçer, üstünde çalan parçanın adı yazar. Ana menüde ve
// ayarlar menüsünde aynı bileşen kullanılır; parçayı değiştiren ve saklayan MusicPlayer / GameSettings'tir.
[RequireComponent(typeof(Button))]
public class MusicTrackButton : MonoBehaviour
{
    [SerializeField] private Text label;
    [Tooltip("Etiket biçimi; {0} = parçanın adı.")]
    [SerializeField] private string labelFormat = "MÜZİK: {0}";

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(HandleClicked);
    }

    // MusicPlayer bu düğmeden sonra uyanmış olabilir: ilk karede bir kez daha yazılır.
    private void Start() => Refresh();

    private void OnEnable()
    {
        GameSettings.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        GameSettings.Changed -= Refresh;
    }

    private void HandleClicked()
    {
        if (MusicPlayer.Instance != null)
            MusicPlayer.Instance.SelectNextTrack();
    }

    private void Refresh()
    {
        label.text = string.Format(labelFormat, MusicPlayer.Instance != null ? MusicPlayer.Instance.CurrentTrackName : string.Empty);
    }
}
