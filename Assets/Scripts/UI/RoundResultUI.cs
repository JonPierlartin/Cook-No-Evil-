using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

// Bölüm sonu ekranı (GDD 3.4): round bitince üç oyuncuda da sonuç yazar. Yalnızca gösterir; sonuç sunucudadır
// (GameLoopManager.Outcome, replike). Düğmeler yalnızca host'ta görünür ve doğrudan sunucu işlemlerini çağırır
// (host = sunucu): "Tekrar oyna" aynı seviyeyi, "Sonraki seviye" (kazanıldıysa ve son seviye değilse) sıradaki
// seviyeyi temiz yeniden başlatır.
public class RoundResultUI : MonoBehaviour
{
    [Tooltip("Sonuç ekranının kökü; round bitmeden kapalıdır.")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Text title;
    [SerializeField] private Button replayButton;
    [SerializeField] private Button nextLevelButton;
    [Tooltip("Host olmayan oyunculara gösterilen bekleme yazısı.")]
    [SerializeField] private GameObject waitingForHost;

    [Header("Metinler (Localization tablosuna bağlanana kadar düz metin)")]
    [SerializeField] private string wonText = "KAZANDINIZ!";
    [SerializeField] private string lostText = "KAYBETTİNİZ";

    private void Start()
    {
        panel.SetActive(false);
        replayButton.onClick.AddListener(HandleReplayClicked);
        nextLevelButton.onClick.AddListener(HandleNextLevelClicked);
    }

    private void Update()
    {
        var loop = GameLoopManager.Instance;
        bool ended = loop != null && loop.IsSpawned && loop.CurrentRoundState.Value == RoundState.RoundEnded;
        if (panel.activeSelf != ended)
            panel.SetActive(ended);

        if (!ended)
            return;

        bool won = loop.Outcome.Value == RoundOutcome.Won;
        title.text = won ? wonText : lostText;

        bool isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
        bool hasNext = LevelDirector.Instance != null && LevelDirector.Instance.HasNextLevel;
        replayButton.gameObject.SetActive(isHost);
        nextLevelButton.gameObject.SetActive(isHost && won && hasNext);
        waitingForHost.SetActive(!isHost);
    }

    private void HandleReplayClicked()
    {
        if (GameLoopManager.Instance != null)
            GameLoopManager.Instance.ServerRestartRound();
    }

    private void HandleNextLevelClicked()
    {
        // Sıra: önce seviye seçilir, sonra round yeniden başlar (seviye round başında çözülür).
        if (LevelDirector.Instance != null && LevelDirector.Instance.ServerAdvanceLevel() && GameLoopManager.Instance != null)
            GameLoopManager.Instance.ServerRestartRound();
    }
}
