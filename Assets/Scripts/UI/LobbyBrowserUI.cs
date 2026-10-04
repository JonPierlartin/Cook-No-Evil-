using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Ana menünün pencereleri: lobi oluşturma (ad + isteğe bağlı şifre), açık lobilerin listesi ve şifre sorma.
// Yalnızca arayüz: lobiyi kuran / listeleyen / katılan SteamLobbyManager'dır, şifreyi doğrulayan sunucudur
// (RoleManager bağlantı onayı). Bağlanınca ya da hata olunca pencereler kapanır; durum metnini LobbyUIController
// gösterir.
public class LobbyBrowserUI : MonoBehaviour
{
    [Header("Lobi oluştur")]
    [SerializeField] private GameObject createPanel;
    [SerializeField] private InputField lobbyNameInput;
    [SerializeField] private Toggle passwordToggle;
    [SerializeField] private InputField createPasswordInput;
    [SerializeField] private Button createConfirmButton;
    [SerializeField] private Button createCancelButton;
    [SerializeField] private Text createHintText;

    [Header("Lobi listesi")]
    [SerializeField] private GameObject listPanel;
    [Tooltip("Satırların ekleneceği kapsayıcı (ScrollRect içeriği).")]
    [SerializeField] private Transform listContent;
    [Tooltip("Satır şablonu; her lobi için kopyalanır.")]
    [SerializeField] private LobbyListRow rowTemplate;
    [SerializeField] private Text listStatusText;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button listBackButton;

    [Header("Şifre gir")]
    [SerializeField] private GameObject passwordPanel;
    [SerializeField] private InputField joinPasswordInput;
    [SerializeField] private Button joinConfirmButton;
    [SerializeField] private Button joinCancelButton;

    [Header("Metinler (Localization tablosuna bağlanana kadar düz metin)")]
    [SerializeField] private string defaultLobbyNameFormat = "{0} lobisi";
    [SerializeField] private string fallbackPlayerName = "Oyuncu";
    [SerializeField] private string searchingText = "Lobiler aranıyor...";
    [SerializeField] private string noLobbiesText = "Açık lobi bulunamadı. Yenile'ye bas ya da kendi lobini oluştur.";
    [SerializeField] private string passwordNeededText = "Şifreli lobi için bir şifre yaz.";

    private readonly List<LobbyListRow> _rows = new();
    private SteamLobbyManager.LobbyInfo _pendingLobby;
    private bool _refreshing;

    private void Awake()
    {
        createConfirmButton.onClick.AddListener(HandleCreateConfirmed);
        createCancelButton.onClick.AddListener(CloseAll);
        passwordToggle.onValueChanged.AddListener(_ => RefreshCreateDialog());
        refreshButton.onClick.AddListener(RefreshList);
        listBackButton.onClick.AddListener(CloseAll);
        joinConfirmButton.onClick.AddListener(HandleJoinPasswordConfirmed);
        joinCancelButton.onClick.AddListener(() => passwordPanel.SetActive(false));

        rowTemplate.gameObject.SetActive(false);
        CloseAll();
    }

    private void Start()
    {
        var lobby = SteamLobbyManager.Instance;
        if (lobby == null)
            return;

        lobby.OnLobbyCreated += CloseAll;
        lobby.OnLobbyJoined += CloseAll;
        lobby.OnLobbyError += HandleLobbyError;
        lobby.OnPasswordRequired += HandlePasswordRequired;
    }

    private void OnDestroy()
    {
        var lobby = SteamLobbyManager.Instance;
        if (lobby == null)
            return;

        lobby.OnLobbyCreated -= CloseAll;
        lobby.OnLobbyJoined -= CloseAll;
        lobby.OnLobbyError -= HandleLobbyError;
        lobby.OnPasswordRequired -= HandlePasswordRequired;
    }

    public void OpenCreateDialog()
    {
        CloseAll();
        string playerName = SteamLobbyManager.Instance != null ? SteamLobbyManager.Instance.LocalPlayerName : null;
        lobbyNameInput.text = string.Format(defaultLobbyNameFormat, string.IsNullOrEmpty(playerName) ? fallbackPlayerName : playerName);
        passwordToggle.SetIsOnWithoutNotify(false);
        createPasswordInput.text = string.Empty;
        createPanel.SetActive(true);
        RefreshCreateDialog();
    }

    public void OpenList()
    {
        CloseAll();
        listPanel.SetActive(true);
        RefreshList();
    }

    public void CloseAll()
    {
        createPanel.SetActive(false);
        listPanel.SetActive(false);
        passwordPanel.SetActive(false);
    }

    private void RefreshCreateDialog()
    {
        createPasswordInput.gameObject.SetActive(passwordToggle.isOn);
        createHintText.text = string.Empty;
    }

    private void HandleCreateConfirmed()
    {
        string password = passwordToggle.isOn ? createPasswordInput.text : string.Empty;
        if (passwordToggle.isOn && string.IsNullOrEmpty(password))
        {
            createHintText.text = passwordNeededText;
            return;
        }

        CloseAll();
        LobbyUIController.Instance.BeginHost(lobbyNameInput.text, password);
    }

    private async void RefreshList()
    {
        if (_refreshing || SteamLobbyManager.Instance == null)
            return;

        _refreshing = true;
        refreshButton.interactable = false;
        ClearRows();
        listStatusText.text = searchingText;

        var lobbies = await SteamLobbyManager.Instance.RequestLobbyListAsync();

        // Beklerken sahne kapanmış olabilir.
        if (this == null)
            return;

        _refreshing = false;
        refreshButton.interactable = true;
        ClearRows();
        foreach (var info in lobbies)
        {
            var row = Instantiate(rowTemplate, listContent);
            row.gameObject.SetActive(true);
            row.Bind(info, HandleJoinClicked);
            _rows.Add(row);
        }

        listStatusText.text = lobbies.Count == 0 ? noLobbiesText : string.Empty;
    }

    private void ClearRows()
    {
        foreach (var row in _rows)
            Destroy(row.gameObject);

        _rows.Clear();
    }

    private void HandleJoinClicked(SteamLobbyManager.LobbyInfo info)
    {
        if (info.Locked)
        {
            AskPassword(info);
            return;
        }

        LobbyUIController.Instance.BeginJoin(info.Id, null);
    }

    // Davetle / arkadaş listesinden gelinen lobi şifreliyse SteamLobbyManager şifre ister.
    private void HandlePasswordRequired(SteamLobbyManager.LobbyInfo info) => AskPassword(info);

    private void AskPassword(SteamLobbyManager.LobbyInfo info)
    {
        _pendingLobby = info;
        joinPasswordInput.text = string.Empty;
        passwordPanel.SetActive(true);
    }

    private void HandleJoinPasswordConfirmed()
    {
        if (string.IsNullOrEmpty(joinPasswordInput.text))
            return;

        passwordPanel.SetActive(false);
        LobbyUIController.Instance.BeginJoin(_pendingLobby.Id, joinPasswordInput.text);
    }

    private void HandleLobbyError(string errorKey) => CloseAll();
}
