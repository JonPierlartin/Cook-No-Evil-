using UnityEngine;
using UnityEngine.UI;

// UGUI (Canvas/Button/Text) tabanli lobi arayuzu. Manuel lobi kodu girme ekrani YOK:
// katilma tamamen Steam'in kendi davet sistemi (overlay) uzerinden, otomatik olarak olur.
public class LobbyUIController : MonoBehaviour
{
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button inviteButton;
    [SerializeField] private Text statusText;
    [SerializeField] private GameObject connectionLostPanel;
    [SerializeField] private Button connectionLostOkButton;

    private void Awake()
    {
        hostButton.onClick.AddListener(HandleHostClicked);
        inviteButton.onClick.AddListener(HandleInviteClicked);
        connectionLostOkButton.onClick.AddListener(HandleConnectionLostOkClicked);

        inviteButton.gameObject.SetActive(false);
        connectionLostPanel.SetActive(false);
    }

    private void Start()
    {
        // SteamLobbyManager.Instance kendi Awake'inde atanir; sira garantisi olmadigi icin
        // aboneligi Start'a erteliyoruz (butun Awake'ler tamamlandiktan sonra calisir).
        var lobby = SteamLobbyManager.Instance;
        if (lobby == null)
        {
            Debug.LogError("[LobbyUIController] SteamLobbyManager.Instance bulunamadi.");
            return;
        }

        lobby.OnLobbyCreated += HandleLobbyCreated;
        lobby.OnLobbyJoined += HandleLobbyJoined;
        lobby.OnLobbyError += HandleLobbyError;
        lobby.OnHostDisconnected += HandleHostDisconnected;
    }

    private void OnDestroy()
    {
        var lobby = SteamLobbyManager.Instance;
        if (lobby == null)
            return;

        lobby.OnLobbyCreated -= HandleLobbyCreated;
        lobby.OnLobbyJoined -= HandleLobbyJoined;
        lobby.OnLobbyError -= HandleLobbyError;
        lobby.OnHostDisconnected -= HandleHostDisconnected;
    }

    private void HandleHostClicked()
    {
        statusText.text = "Lobi olusturuluyor...";
        hostButton.interactable = false;
        SteamLobbyManager.Instance.HostLobby();
    }

    private void HandleInviteClicked()
    {
        SteamLobbyManager.Instance.OpenInviteOverlay();
    }

    private void HandleLobbyCreated()
    {
        statusText.text = "Lobi hazir. Arkadasini davet edebilirsin.";
        inviteButton.gameObject.SetActive(true);
    }

    private void HandleLobbyJoined()
    {
        statusText.text = "Host'a baglaniliyor...";
        hostButton.gameObject.SetActive(false);
    }

    private void HandleLobbyError(string message)
    {
        statusText.text = $"Hata: {message}";
        hostButton.interactable = true;
    }

    private void HandleHostDisconnected()
    {
        statusText.text = "";
        connectionLostPanel.SetActive(true);
        lobbyPanel.SetActive(false);
    }

    private void HandleConnectionLostOkClicked()
    {
        connectionLostPanel.SetActive(false);
        lobbyPanel.SetActive(true);
        hostButton.gameObject.SetActive(true);
        hostButton.interactable = true;
        inviteButton.gameObject.SetActive(false);
        statusText.text = "";
    }
}
