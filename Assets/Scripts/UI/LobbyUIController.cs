using UnityEngine;
using UnityEngine.UI;

// UGUI (Canvas/Button/Text) tabanli lobi arayuzu. Manuel lobi kodu girme ekrani YOK:
// katilma tamamen Steam'in kendi davet sistemi (overlay) uzerinden, otomatik olarak olur.
public class LobbyUIController : MonoBehaviour
{
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button inviteButton;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button leaveButton;
    [SerializeField] private Text statusText;
    [SerializeField] private GameObject connectionLostPanel;
    [SerializeField] private Button connectionLostOkButton;

    private PlayerRole _localRole = PlayerRole.None;

    private void Awake()
    {
        hostButton.onClick.AddListener(HandleHostClicked);
        inviteButton.onClick.AddListener(HandleInviteClicked);
        startGameButton.onClick.AddListener(HandleStartGameClicked);
        leaveButton.onClick.AddListener(HandleLeaveClicked);
        connectionLostOkButton.onClick.AddListener(HandleConnectionLostOkClicked);

        inviteButton.gameObject.SetActive(false);
        startGameButton.gameObject.SetActive(false);
        leaveButton.gameObject.SetActive(false);
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

        // RoleManager.Instance de kendi Awake'inde atanir, ayni sebeple burada abone oluyoruz.
        if (RoleManager.Instance != null)
        {
            RoleManager.Instance.OnLocalRoleAssigned += HandleLocalRoleAssigned;
            RoleManager.Instance.IsRoundActive.OnValueChanged += HandleRoundActiveChanged;
        }
        else
        {
            Debug.LogError("[LobbyUIController] RoleManager.Instance bulunamadi.");
        }
    }

    private void OnDestroy()
    {
        var lobby = SteamLobbyManager.Instance;
        if (lobby != null)
        {
            lobby.OnLobbyCreated -= HandleLobbyCreated;
            lobby.OnLobbyJoined -= HandleLobbyJoined;
            lobby.OnLobbyError -= HandleLobbyError;
            lobby.OnHostDisconnected -= HandleHostDisconnected;
        }

        if (RoleManager.Instance != null)
        {
            RoleManager.Instance.OnLocalRoleAssigned -= HandleLocalRoleAssigned;
            RoleManager.Instance.IsRoundActive.OnValueChanged -= HandleRoundActiveChanged;
        }
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

    private void HandleStartGameClicked()
    {
        bool started = RoleManager.Instance != null && RoleManager.Instance.StartRound();
        if (!started)
            statusText.text = $"Baslatilamadi: en az {RoleManager.MaxPlayers} oyuncu gerekli.";
    }

    private void HandleLeaveClicked()
    {
        SteamLobbyManager.Instance.LeaveLobby();
        ResetToInitialScreen();
    }

    private void HandleLobbyCreated()
    {
        inviteButton.gameObject.SetActive(true);
        startGameButton.gameObject.SetActive(true);
        leaveButton.gameObject.SetActive(true);
        RefreshStatusText();
    }

    private void HandleLobbyJoined()
    {
        statusText.text = "Host'a baglaniliyor...";
        hostButton.gameObject.SetActive(false);
    }

    // NGO baglantisi gercekten tamamlanip server rol atadiktan SONRA tetiklenir. Host icin bu,
    // StartHost() cagrisi sirasinda SENKRON olarak (yani OnLobbyCreated'dan ONCE) tetiklenir;
    // client icin ise gercek ag baglantisi kurulduktan SONRA (yani OnLobbyJoined'dan SONRA)
    // tetiklenir. Iki tarafta da olay sirasi farkli oldugu icin metinler burada sabit
    // yazilmiyor, RefreshStatusText mevcut duruma gore her seferinde yeniden hesapliyor.
    // NOT: Bundan sonraki asama (lobi listesi / oyun ekrani) henuz bu bilesende yok, GameLoopManager
    // ile birlikte (Bilesen 2) gelecek — su an icin sadece baglantinin basarili oldugunu gosteriyoruz.
    private void HandleLocalRoleAssigned(PlayerRole role)
    {
        _localRole = role;
        hostButton.gameObject.SetActive(false);
        leaveButton.gameObject.SetActive(true);
        RefreshStatusText();
    }

    private void HandleRoundActiveChanged(bool previous, bool current)
    {
        if (current)
        {
            startGameButton.gameObject.SetActive(false);
            inviteButton.gameObject.SetActive(false);
        }

        RefreshStatusText();
    }

    private void RefreshStatusText()
    {
        bool isHost = SteamLobbyManager.Instance != null && SteamLobbyManager.Instance.IsHost;
        bool roundActive = RoleManager.Instance != null && RoleManager.Instance.IsRoundActive.Value;
        string roleText = _localRole != PlayerRole.None ? $"Rolun: {_localRole}." : "Baglaniliyor...";

        if (roundActive)
        {
            statusText.text = $"Round basladi! {roleText}";
            return;
        }

        statusText.text = isHost
            ? $"Baglandi! {roleText} Arkadasini davet edebilir, hazir olunca oyunu baslatabilirsin."
            : $"Baglandi! {roleText}";
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
        ResetToInitialScreen();
    }

    private void ResetToInitialScreen()
    {
        hostButton.gameObject.SetActive(true);
        hostButton.interactable = true;
        inviteButton.gameObject.SetActive(false);
        startGameButton.gameObject.SetActive(false);
        leaveButton.gameObject.SetActive(false);
        statusText.text = "";
        _localRole = PlayerRole.None;
    }
}
