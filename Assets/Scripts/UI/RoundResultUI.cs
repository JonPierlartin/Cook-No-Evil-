using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

// Bölüm sonu ekranı (GDD 3.4): round bitince üç oyuncuda da sonuç yazar. Yalnızca gösterir; sonuç sunucudadır
// (GameLoopManager.Outcome, replike). Tek düğme yalnızca host'ta görünür: herkesi lobiye döndürür (host = sunucu,
// doğrudan sunucu işlemini çağırır). Lobide rol ve bölüm seçilip yeniden başlatılır.
public class RoundResultUI : MonoBehaviour
{
    [Tooltip("Sonuç ekranının kökü; round bitmeden kapalıdır.")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Text title;
    [Tooltip("Host'un 'Lobiye dön' düğmesi.")]
    [UnityEngine.Serialization.FormerlySerializedAs("replayButton")]
    [SerializeField] private Button lobbyButton;
    [Tooltip("Host olmayan oyunculara gösterilen bekleme yazısı.")]
    [SerializeField] private GameObject waitingForHost;

    [Header("Metinler (Localization tablosuna bağlanana kadar düz metin)")]
    [SerializeField] private string wonText = "KAZANDINIZ!";
    [SerializeField] private string lostText = "KAYBETTİNİZ";

    private void Start()
    {
        panel.SetActive(false);
        lobbyButton.onClick.AddListener(HandleLobbyClicked);
    }

    private void Update()
    {
        var loop = GameLoopManager.Instance;
        bool ended = loop != null && loop.IsSpawned && loop.CurrentRoundState.Value == RoundState.RoundEnded;
        if (panel.activeSelf != ended)
            panel.SetActive(ended);

        if (!ended)
            return;

        title.text = loop.Outcome.Value == RoundOutcome.Won ? wonText : lostText;

        bool isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
        lobbyButton.gameObject.SetActive(isHost);
        waitingForHost.SetActive(!isHost);
    }

    private void HandleLobbyClicked()
    {
        if (GameLoopManager.Instance != null)
            GameLoopManager.Instance.ServerReturnToLobby();
    }
}
