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
    public static LobbyUIController Instance { get; private set; }

    private const string TableName = "UIStrings";

    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button inviteButton;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button leaveButton;
    [SerializeField] private Text statusText;
    [SerializeField] private GameObject connectionLostPanel;
    [SerializeField] private Button connectionLostOkButton;
    [Tooltip("GameObject.Find KULLANILMAZ: GameplayCanvas round bitince/disconnect'te inactive olabiliyor, Find inactive objelerde null doner (bulunan gercek bug — bkz. asagidaki not).")]
    [SerializeField] private GameObject gameplayCanvas;

    // Round aktifken imlecin kilitli/gizli olmasi gerektigini YEREL olarak izler.
    // RoleManager.IsRoundActive'e (bir NetworkVariable) GUVENILMEZ: host disconnect
    // sonrasi client'in kendi kopyasinda bu deger HICBIR YERDE false'a resetlenmiyor
    // (RoleManager sadece IsServer iken OnNetworkSpawn'da resetliyor) — son senkronize
    // "true" degeri bellekte donuk kalir. Bu yuzden disconnect sonrasi bir
    // OnApplicationFocus tetiklenirse (alt-tab, pencere disina tiklama vb.) stale
    // deger imleci YANLISLIKLA tekrar kilitliyordu — client'in Host butonuna
    // tiklayamamasina (imlec gorunmez/kilitli kaldigi icin) yol acan kritik bir bug.
    // Bu bayrak SADECE bizim kendi UI gecislerimizde (round basladi/bitti, host
    // disconnect) guncellenir, hicbir zaman ag durumundan yeniden turetilmez.
    [Header("Rol seçimi (GDD 8.1)")]
    [Tooltip("Rol düğmelerinin ve oyuncu listesinin kökü; yalnızca lobide ve bağlıyken görünür.")]
    [SerializeField] private GameObject rolePanel;
    [Tooltip("Rol düğmeleri; roleButtonRoles ile aynı sırada.")]
    [SerializeField] private Button[] roleButtons;
    [SerializeField] private PlayerRole[] roleButtonRoles;
    [Tooltip("Bağlı oyuncuların rollerini listeleyen yazı.")]
    [SerializeField] private Text rosterText;
    [SerializeField] private Color roleNormalColor = Color.white;
    [SerializeField] private Color roleSelectedColor = new(0.55f, 0.9f, 0.55f);
    [Header("Metinler (Localization tablosuna bağlanana kadar düz metin)")]
    [SerializeField] private string rosterLineFormat = "Oyuncu {0}: {1}{2}";
    [SerializeField] private string rosterSelfSuffix = "  (sen)";
    [SerializeField] private string rolesNotReadyText = "Başlamak için 3 oyuncu ve her rolden birer tane gerekli.";

    public bool ShouldLockCursor { get; private set; }

    private void Awake()
    {
        Instance = this;

        hostButton.onClick.AddListener(HandleHostClicked);
        inviteButton.onClick.AddListener(HandleInviteClicked);
        startGameButton.onClick.AddListener(HandleStartGameClicked);
        leaveButton.onClick.AddListener(HandleLeaveClicked);
        connectionLostOkButton.onClick.AddListener(HandleConnectionLostOkClicked);

        inviteButton.gameObject.SetActive(false);
        startGameButton.gameObject.SetActive(false);
        leaveButton.gameObject.SetActive(false);
        connectionLostPanel.SetActive(false);

        for (int i = 0; i < roleButtons.Length && i < roleButtonRoles.Length; i++)
        {
            var role = roleButtonRoles[i];
            roleButtons[i].onClick.AddListener(() => HandleRoleClicked(role));
        }

        if (rolePanel != null)
            rolePanel.SetActive(false);
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
            RoleManager.Instance.OnRolesChanged += RefreshRolePanel;
        }
        else
        {
            Debug.LogError("[LobbyUIController] RoleManager.Instance bulunamadi.");
        }

        // Round State artik tek otorite: GameLoopManager.CurrentRoundState (eskiden
        // RoleManager.IsRoundActive idi, Round State konsolidasyonu ile tasindi).
        if (GameLoopManager.Instance != null)
        {
            GameLoopManager.Instance.CurrentRoundState.OnValueChanged += HandleRoundStateChanged;
            GameLoopManager.Instance.OnServerSessionEnded += HandleServerSessionEnded;
        }
        else
        {
            Debug.LogError("[LobbyUIController] GameLoopManager.Instance bulunamadi.");
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

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
            RoleManager.Instance.OnRolesChanged -= RefreshRolePanel;
        }

        if (GameLoopManager.Instance != null)
        {
            GameLoopManager.Instance.CurrentRoundState.OnValueChanged -= HandleRoundStateChanged;
            GameLoopManager.Instance.OnServerSessionEnded -= HandleServerSessionEnded;
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
        bool started = GameLoopManager.Instance != null && GameLoopManager.Instance.StartRound();
        if (!started)
            statusText.text = Localize("lobby.start_failed", RoleManager.MaxPlayers);
    }

    private void HandleLeaveClicked()
    {
        SteamLobbyManager.Instance.LeaveLobby();
        ResetToInitialScreen();
    }

    // Oyun içinden (ESC menüsü) ilk ekrana dönüş: oturumdan ayrılır, oyun arayüzünü kapatır, lobi ekranını açar.
    // Uygulamayı kapatıp açmadan yeniden host/join yapılabilsin diye.
    public void LeaveToInitialScreen()
    {
        SteamLobbyManager.Instance.LeaveLobby();
        SetGameplayCanvasVisible(false);
        connectionLostPanel.SetActive(false);
        lobbyPanel.SetActive(true);
        ResetToInitialScreen();
    }

    // İstemci yalnızca niyet gönderir; rolü sunucu yazar (RoleManager).
    private void HandleRoleClicked(PlayerRole role)
    {
        if (RoleManager.Instance != null && RoleManager.Instance.IsSpawned)
            RoleManager.Instance.RequestRoleServerRpc(role);
    }

    // Rol paneli: yalnızca lobide ve bu oyuncu bağlıyken. Oyuncu listesi replike rol listesinden yazılır; host'un
    // "Başlat" düğmesi her rolden bir tane olana kadar kapalıdır (asıl kontrol sunucuda: GameLoopManager.StartRound).
    private void RefreshRolePanel()
    {
        if (rolePanel == null)
            return;

        var roles = RoleManager.Instance;
        var network = Unity.Netcode.NetworkManager.Singleton;
        bool connected = roles != null && roles.IsSpawned && network != null && network.IsListening && roles.LocalRole != PlayerRole.None;
        bool inLobby = GameLoopManager.Instance != null && GameLoopManager.Instance.CurrentRoundState.Value == RoundState.Lobby;
        bool show = connected && inLobby;
        rolePanel.SetActive(show);
        if (!show)
            return;

        var localRole = roles.LocalRole;
        for (int i = 0; i < roleButtons.Length && i < roleButtonRoles.Length; i++)
        {
            if (roleButtons[i].targetGraphic != null)
                roleButtons[i].targetGraphic.color = roleButtonRoles[i] == localRole ? roleSelectedColor : roleNormalColor;
        }

        var lines = new System.Text.StringBuilder();
        for (int i = 0; i < roles.AssignedRoleCount; i++)
        {
            if (!roles.TryGetAssignedRoleAt(i, out var clientId, out var role))
                continue;

            lines.AppendLine(string.Format(rosterLineFormat, i + 1, LocalizeRole(role), clientId == network.LocalClientId ? rosterSelfSuffix : ""));
        }

        bool ready = roles.AreRolesReady;
        if (!ready)
            lines.AppendLine().Append(rolesNotReadyText);

        rosterText.text = lines.ToString();
        startGameButton.interactable = ready;
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

        // BULUNAN HATA (rejoin senaryosu): NetworkVariable.OnValueChanged SADECE canli bir
        // deger degisikliginde tetiklenir. Round zaten aktifken (yeniden) baglanan bir
        // client icin NGO bu NetworkVariable'in ilk senkronizasyonunu bir "degisiklik"
        // olarak raporlamaz — deger dogrudan RoundActive olarak gelir, HandleRoundStateChanged
        // hic tetiklenmez. Ekran sonsuza dek "Baglandi... Rolun:" yazisinda takili kaliyordu.
        // OnLocalRoleAssigned ise HEM ilk katilimda HEM rejoin'de guvenilir sekilde
        // tetiklendigi icin (RoleManager her baglantida rolu yeniden atar), buraya GUNCEL
        // round durumunu acikca uygulayan ayni cagriyi ekliyoruz — boylece hangi event
        // once/sonra gelirse gelsin ekran dogru durumu yakaliyor.
        bool roundActive = GameLoopManager.Instance != null && GameLoopManager.Instance.IsRoundActive;
        ApplyRoundActiveState(roundActive);
        RefreshRolePanel();
    }

    private void HandleRoundStateChanged(RoundState previous, RoundState current)
    {
        bool isActive = current == RoundState.RoundActive;

        if (isActive)
        {
            startGameButton.gameObject.SetActive(false);
            inviteButton.gameObject.SetActive(false);
        }

        RefreshStatusText();
        ApplyRoundActiveState(isActive);
        RefreshRolePanel();

        // TESHIS: gercek cok-makineli testte "Rolun:" adinin bos kalma raporunu local testte
        // tekrar uretemedik. Bug hala gorulurse Player.log'daki bu satiri kontrol et — LocalRole
        // None ise sorun RoleManager senkronizasyonunda, dolu ama statusText'te gorunmuyorsa
        // sorun UI/Localization katmanindadir. Teshis netlesince bu log kaldirilmali.
        if (isActive)
        {
            var role = RoleManager.Instance != null ? RoleManager.Instance.LocalRole : PlayerRole.None;
            Debug.Log($"[LobbyUIController] Round baslama teshis: LocalRole={role} statusText=\"{statusText.text}\"");
        }
    }

    // Round basladiginda/rejoin'de tam ekran lobi paneli (buton/status text) 3D oyun
    // goruntusunu ve GameplayCanvas'i (Hotbar/Emote carki) tamamen kapatiyordu — oyuncular
    // Player.prefab spawn olup kamerasi devreye girse bile ekranda hicbir degisiklik
    // GORMUYORDU (gercek 3 makineli testte bulundu). lobbyPanel'i gizleyip GameplayCanvas'i
    // acmak ve fare imlecini kilitlemek bu gecisi tamamliyor. HEM HandleRoundStateChanged
    // (canli gecis) HEM HandleLocalRoleAssigned (baglanti/rejoin anindaki durum yakalama)
    // tarafindan cagrilir — ikisi de gerekli, yukarida detayli aciklama var.
    private void ApplyRoundActiveState(bool active)
    {
        // Bolum bittiginde (RoundEnded) lobiye donulmez: oyun arayuzu acik kalir (sonuc ekrani orada), imlec
        // dugmeler icin serbest birakilir. Replike durum yalnizca ag acikken gecerlidir (kopmadan sonra bayat kalir).
        bool ended = !active
            && Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsListening
            && GameLoopManager.Instance != null && GameLoopManager.Instance.CurrentRoundState.Value == RoundState.RoundEnded;

        lobbyPanel.SetActive(!active && !ended);
        SetGameplayCanvasVisible(active || ended, active);
    }

    // BULUNAN HATA: burada eskiden GameObject.Find("GameplayCanvas") kullaniliyordu.
    // GameObject.Find INACTIVE objelerde null doner — GameplayCanvas bir onceki
    // disconnect/round-bitisinde inactive kaldiysa Find basarisiz oluyordu, null-guard
    // yuzunden SetActive hic cagrilmiyordu ve GameplayCanvas bir daha ASLA aktif
    // olamiyordu (kendini kendi hatasiyla kilitleyen bir durum). Gercek testte "%2/3
    // denemede GameplayCanvas sahnede bulunamadi hatasi + Emote carki acilmiyor" olarak
    // gorulen bug buydu (cark, inactive GameplayCanvas'in child'i oldugu icin calismiyordu).
    // Duzeltme: Inspector'dan sabit atanan referans kullaniliyor, aktif/inaktif durumundan
    // bagimsiz her zaman bulunuyor.
    private void SetGameplayCanvasVisible(bool visible) => SetGameplayCanvasVisible(visible, visible);

    private void SetGameplayCanvasVisible(bool visible, bool lockCursor)
    {
        if (gameplayCanvas != null)
            gameplayCanvas.SetActive(visible);
        else
            Debug.LogError("[LobbyUIController] gameplayCanvas Inspector referansi atanmamis.");

        ShouldLockCursor = lockCursor;
        Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !lockCursor;
    }

    // Windows/Unity, pencere fokusu kaybedildiginde imlec kilidini/gizliligini
    // OTOMATIK olarak iptal eder (isletim sistemi bunun disina cikilmasina izin
    // vermez) — ama fokus GERI geldiginde bunu hicbir kod yeniden uygulamiyordu.
    // Gercek 3 kisilik testte "bir oyuncuda E'ye basinca da imlec cikiyor" olarak
    // bildirilen sorunun asil kok nedeni muhtemelen buydu (alt-tab/pencere disina
    // tiklama), rol-bazli bir sizinti degil — bkz. CLAUDE.md. ShouldLockCursor
    // (YEREL bayrak) kullanilir, RoleManager.IsRoundActive DEGIL — o bir
    // NetworkVariable, host disconnect sonrasi client'ta stale kaliyor (hicbir
    // yerde false'a resetlenmiyor) ve bu da ayri, kritik bir bug'a yol aciyordu:
    // disconnect sonrasi imlec tekrar kilitlenip client Host butonuna tiklayamiyordu.
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            return;

        Cursor.lockState = ShouldLockCursor ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !ShouldLockCursor;
    }

    private void RefreshStatusText()
    {
        bool isHost = SteamLobbyManager.Instance != null && SteamLobbyManager.Instance.IsHost;
        bool roundActive = GameLoopManager.Instance != null && GameLoopManager.Instance.IsRoundActive;

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
        // Round sirasinda gelen hata = oturumdan sebeple koparildik (orn. GDD 8.2 zaman asimi): oyun arayuzu
        // acik ve imlec kilitli kalmasin, ilk ekrana donulsun.
        if (ShouldLockCursor)
        {
            ShowErrorScreen(errorKey);
            return;
        }

        statusText.text = Localize("lobby.error_prefix", Localize(errorKey));
        hostButton.interactable = true;
    }

    // Oturum bir sebeple bitti: oyun arayuzu kapanir, ilk ekran ve hata metni gosterilir. Steam yolu
    // (HandleLobbyError), Local Debug istemcisi (LocalDebugLobby) ve host'un zaman asimi
    // (HandleServerSessionEnded) ayni ekrani kullanir.
    public void ShowErrorScreen(string errorKey)
    {
        SetGameplayCanvasVisible(false);
        connectionLostPanel.SetActive(false);
        lobbyPanel.SetActive(true);
        ResetToInitialScreen();
        statusText.text = Localize("lobby.error_prefix", Localize(errorKey));
    }

    // Yalnizca host: GameLoopManager istemcileri koparip oturumu bitirdi; ag burada kapatilir.
    private void HandleServerSessionEnded(string reasonKey)
    {
        SteamLobbyManager.Instance.LeaveLobby();
        ShowErrorScreen(reasonKey);
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
        startGameButton.interactable = true;
        if (rolePanel != null)
            rolePanel.SetActive(false);
    }

    private static string LocalizeRole(PlayerRole role)
    {
        string key = role switch
        {
            PlayerRole.Sef => "role.sef",
            PlayerRole.Komi => "role.komi",
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
