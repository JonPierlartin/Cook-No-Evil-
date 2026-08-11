using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

// UGUI (Canvas/Button/Text) tabanli lobi arayuzu. Manuel lobi kodu girme ekrani YOK:
// katilma tamamen Steam'in kendi davet sistemi (overlay) uzerinden, otomatik olarak olur.
// Butonlarin sabit metinleri sahnede LocalizeStringEvent component'leri uzerinden geliyor
// (UIStrings tablosu); burada sadece DINAMIK (calisma zamaninda degisen) metinler
// Unity Localization String Database uzerinden cozuluyor — bkz. Localize().
public class LobbyUIController : MonoBehaviour
{
    private const string TableName = "UIStrings";

    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button inviteButton;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button leaveButton;
    [SerializeField] private Text statusText;
    [SerializeField] private GameObject connectionLostPanel;
    [SerializeField] private Button connectionLostOkButton;

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
        statusText.text = Localize("lobby.creating");
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
            statusText.text = Localize("lobby.start_failed", RoleManager.MaxPlayers);
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
        statusText.text = Localize("lobby.connecting_host");
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

        // Round basladiginda tam ekran lobi paneli (buton/status text) 3D oyun goruntusunu
        // ve GameplayCanvas'i (Hotbar/Emote carki) tamamen kapatiyordu — oyuncular Player.prefab
        // spawn olup kamerasi devreye girse bile ekranda hicbir degisiklik GORMUYORDU (gercek
        // 3 makineli testte bulundu). lobbyPanel'i gizleyip GameplayCanvas'i acmak ve fare
        // imlecini kilitlemek bu gecisi tamamliyor.
        lobbyPanel.SetActive(!current);
        SetGameplayCanvasVisible(current);

        // TESHIS: gercek cok-makineli testte "Rolun:" adinin bos kalma raporunu local testte
        // tekrar uretemedik. Bug hala gorulurse Player.log'daki bu satiri kontrol et — LocalRole
        // None ise sorun RoleManager senkronizasyonunda, dolu ama statusText'te gorunmuyorsa
        // sorun UI/Localization katmanindadir. Teshis netlesince bu log kaldirilmali.
        if (current)
        {
            var role = RoleManager.Instance != null ? RoleManager.Instance.LocalRole : PlayerRole.None;
            Debug.Log($"[LobbyUIController] Round baslama teshis: LocalRole={role} statusText=\"{statusText.text}\"");
        }
    }

    // GameplayCanvas, LobbyUIController'in yasadigi LobbyCanvas'tan ayri bir sahne
    // objesi oldugu icin (Inspector referansi yerine) isimle bulunuyor — bu tek,
    // durum-degisimi-basi bir cagri, performans sorunu yaratmaz. lobbyPanel'i
    // KENDI YONETMEZ — her cagiran taraf kendi lobbyPanel durumuna karar verir
    // (orn. host-disconnect'te lobbyPanel yerine connectionLostPanel gosterilir).
    private void SetGameplayCanvasVisible(bool visible)
    {
        var gameplayCanvas = GameObject.Find("GameplayCanvas");
        if (gameplayCanvas != null)
            gameplayCanvas.SetActive(visible);
        else
            Debug.LogError("[LobbyUIController] GameplayCanvas sahnede bulunamadi.");

        Cursor.lockState = visible ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !visible;
    }

    // Windows/Unity, pencere fokusu kaybedildiginde imlec kilidini/gizliligini
    // OTOMATIK olarak iptal eder (isletim sistemi bunun disina cikilmasina izin
    // vermez) — ama fokus GERI geldiginde bunu hicbir kod yeniden uygulamiyordu.
    // Gercek 3 kisilik testte "bir oyuncuda E'ye basinca da imlec cikiyor" olarak
    // bildirilen sorunun asil kok nedeni muhtemelen buydu (alt-tab/pencere disina
    // tiklama), rol-bazli bir sizinti degil — bkz. CLAUDE.md.
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            return;

        bool roundActive = RoleManager.Instance != null && RoleManager.Instance.IsRoundActive.Value;
        Cursor.lockState = roundActive ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !roundActive;
    }

    private void RefreshStatusText()
    {
        bool isHost = SteamLobbyManager.Instance != null && SteamLobbyManager.Instance.IsHost;
        bool roundActive = RoleManager.Instance != null && RoleManager.Instance.IsRoundActive.Value;

        // Onbellege alinmis bir alan yerine RoleManager'in NetworkList uzerinden senkronize
        // ettigi GUNCEL rolu her seferinde yeniden okuyoruz. Boylece bu metod hangi event'ten
        // tetiklenirse tetiklensin (rol atama VEYA round baslama), gosterilen rol adi hicbir
        // zaman eski/senkronize-olmamis bir onbellek degerine bagli kalmaz.
        var localRole = RoleManager.Instance != null ? RoleManager.Instance.LocalRole : PlayerRole.None;
        string roleName = localRole != PlayerRole.None ? LocalizeRole(localRole) : Localize("lobby.connecting_generic");

        if (roundActive)
        {
            statusText.text = Localize("lobby.round_started", roleName);
            return;
        }

        statusText.text = isHost
            ? Localize("lobby.connected_host", roleName)
            : Localize("lobby.connected_client", roleName);
    }

    // SteamLobbyManager/RoleManager, gosterim metni yerine sabit bir HATA ANAHTARI
    // gonderir (orn. "error.lobby_full") — boylece ag katmani UI'nin dilinden/metninden
    // tamamen habersiz kalir, cevirisi burada, tek yerde yapilir.
    private void HandleLobbyError(string errorKey)
    {
        statusText.text = Localize("lobby.error_prefix", Localize(errorKey));
        hostButton.interactable = true;
    }

    private void HandleHostDisconnected()
    {
        // Host round SIRASINDA koparsa GameplayCanvas acik/imlec kilitli kalmis olabilir —
        // "Sunucu Baglantisi Koptu" ekrani gorunur/tiklanabilir olsun diye geri alir.
        SetGameplayCanvasVisible(false);

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
    }

    private static string LocalizeRole(PlayerRole role)
    {
        string key = role switch
        {
            PlayerRole.Sef => "role.sef",
            PlayerRole.Yamak => "role.yamak",
            PlayerRole.Kasiyer => "role.kasiyer",
            _ => "role.none",
        };

        return Localize(key);
    }

    private static string Localize(string key, params object[] arguments)
    {
        return LocalizationSettings.StringDatabase.GetLocalizedString(TableName, key, null, FallbackBehavior.UseProjectSettings, arguments);
    }
}
