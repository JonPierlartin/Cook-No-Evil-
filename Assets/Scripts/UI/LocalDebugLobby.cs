using UnityEngine;

// Steam'siz Local Debug host/join giris noktasi — YALNIZCA gelistirme (Multiplayer Play Mode ile
// tek makinede 3 oyunculu test). Steam yolundan (SteamLobbyManager) tamamen ayridir: ona hic
// dokunmaz, onun icinden cagrilmaz. Butonlar ve ag baslatma kodu yalnizca UNITY_EDITOR /
// DEVELOPMENT_BUILD altinda derlenir; release build'de bu bilesen bos bir kabuktur ve hicbir
// buton olusturulmaz.
public class LocalDebugLobby : MonoBehaviour
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [Tooltip("Local butonlarinin kopyalanacagi sablon (mevcut Host butonu). Kopyalar ayni kapsayiciya, sablonun hemen ardina eklenir; sablonun kendisine dokunulmaz.")]
    [SerializeField] private UnityEngine.UI.Button buttonTemplate;
    [Tooltip("Local Host basarili olunca gosterilecek 'Oyunu Baslat' butonu (LobbyUIController'in kendi butonu; davranisi degistirilmez).")]
    [SerializeField] private UnityEngine.UI.Button startGameButton;
    [SerializeField] private string localHostLabel = "Local Host";
    [SerializeField] private string localJoinLabel = "Local Join";

    private UnityEngine.UI.Button _localHostButton;
    private UnityEngine.UI.Button _localJoinButton;
    private Unity.Netcode.NetworkManager _networkManager;

    private void Awake()
    {
        if (buttonTemplate == null)
        {
            Debug.LogError("[LocalDebugLobby] buttonTemplate atanmamis, Local butonlari olusturulamadi.");
            return;
        }

        _localHostButton = CreateButton(localHostLabel, HandleLocalHostClicked, 1);
        _localJoinButton = CreateButton(localJoinLabel, HandleLocalJoinClicked, 2);
    }

    private void Start()
    {
        // NetworkManager.Singleton'a Awake'te erisilmez (sira garantisi yok).
        _networkManager = Unity.Netcode.NetworkManager.Singleton;
        if (_networkManager != null)
            _networkManager.OnClientDisconnectCallback += HandleClientDisconnect;
    }

    private void OnDestroy()
    {
        if (_networkManager != null)
            _networkManager.OnClientDisconnectCallback -= HandleClientDisconnect;
    }

    private UnityEngine.UI.Button CreateButton(string label, UnityEngine.Events.UnityAction onClick, int siblingOffset)
    {
        var button = Instantiate(buttonTemplate, buttonTemplate.transform.parent);
        button.name = $"{label.Replace(" ", string.Empty)}Button";

        // Sablonun etiketi yerellestirme bileseniyle ("Host") surulur; kopyada kalirsa bizim
        // metnimizi ezer.
        foreach (var localized in button.GetComponentsInChildren<UnityEngine.Localization.Components.LocalizeStringEvent>(true))
            DestroyImmediate(localized);

        var text = button.GetComponentInChildren<UnityEngine.UI.Text>(true);
        if (text != null)
            text.text = label;

        button.transform.SetSiblingIndex(buttonTemplate.transform.GetSiblingIndex() + siblingOffset);
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(onClick);
        button.interactable = true;
        button.gameObject.SetActive(true);
        return button;
    }

    private void HandleLocalHostClicked() => StartLocalSession(asHost: true);

    private void HandleLocalJoinClicked() => StartLocalSession(asHost: false);

    private void StartLocalSession(bool asHost)
    {
        var networkManager = Unity.Netcode.NetworkManager.Singleton;
        var transportManager = NetworkTransportManager.Instance;
        if (networkManager == null || transportManager == null)
        {
            Debug.LogError("[LocalDebugLobby] NetworkManager veya NetworkTransportManager bulunamadi.");
            return;
        }

        // Onceki oturumun kapanmasi (NetworkManager.Shutdown asenkron) surerken yeni bir
        // StartHost/StartClient eski oturumun yarim durumuna carpar.
        if (networkManager.IsListening || networkManager.ShutdownInProgress)
        {
            Debug.LogWarning("[LocalDebugLobby] Ag zaten calisiyor veya onceki oturum kapaniyor, biraz sonra tekrar dene.");
            return;
        }

        // Transport, StartHost/StartClient'tan ONCE atanmis olmali.
        transportManager.ConfigureTransport(TransportMode.LocalUdp);

        // RoleManager.HandleConnectionApproval payload'tan 8 baytlik bir kimlik cozer (Steam yolunda
        // SteamId). Local Debug'da SteamId yok; islem kimligi her pencere (Editor/MPPM sanal oyuncu/
        // build ornegi) icin ayri, pencere acik oldugu surece sabittir ve gercek bir SteamId ile
        // asla cakismaz (SteamId'ler 2^32'den cok buyuk). 0 "kimlik yok" demektir, islem kimligi > 0.
        ulong debugId = (ulong)System.Diagnostics.Process.GetCurrentProcess().Id;
        networkManager.NetworkConfig.ConnectionData = System.BitConverter.GetBytes(debugId);

        bool started = asHost ? networkManager.StartHost() : networkManager.StartClient();
        if (!started)
        {
            Debug.LogError($"[LocalDebugLobby] {(asHost ? "StartHost" : "StartClient")} basarisiz.");
            return;
        }

        Debug.Log($"[LocalDebugLobby] Local {(asHost ? "Host" : "Join")} baslatildi (debugId={debugId}).");

        SetLocalButtonsVisible(false);
        if (asHost && startGameButton != null)
            startGameButton.gameObject.SetActive(true);
    }

    // Local Join basarisiz olursa (host yok, baglanti reddedildi) veya baglanti kopunca butonlar
    // tekrar gorunur; SteamLobbyManager bu durumu yalnizca bir Steam lobisi varken isler.
    private void HandleClientDisconnect(ulong clientId)
    {
        if (_networkManager == null || _networkManager.IsServer)
            return;

        SetLocalButtonsVisible(true);
    }

    private void SetLocalButtonsVisible(bool visible)
    {
        if (_localHostButton != null)
            _localHostButton.gameObject.SetActive(visible);
        if (_localJoinButton != null)
            _localJoinButton.gameObject.SetActive(visible);
    }
#endif
}
