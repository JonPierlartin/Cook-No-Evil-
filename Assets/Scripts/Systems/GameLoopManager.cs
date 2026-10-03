using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Round State mimarisinin TEK OTORITESI (kullanici istegiyle konsolide edildi —
// bkz. CLAUDE.md). Eskiden RoleManager.IsRoundActive (round acik/kapali) ve
// GameLoopManager.IsGamePaused (disconnect/reconnect kilidi) birbirinden HABERSIZ,
// bagimsiz iki NetworkVariable'di — Bilesen 2 (siparis timer'i, 3 hak kurali) bunun
// uzerine insa edilseydi, disconnect sirasinda timer'in islemeye devam edip donen
// oyuncuya haksiz "1 Hata" yazdirmasi VEYA pause sirasinda round bitisinin
// tetiklenebilmesi gibi hatalara acikti. Artik CurrentRoundState (Lobby/RoundActive/
// RoundEnded) TEK NetworkVariable, IsPaused ise SADECE RoundActive iken anlamli olan
// bir overlay bayrak (Lobby/RoundEnded'da IsGamePaused her zaman false dondurur, bkz.
// asagidaki computed property). Bilesen 2 (5 dk sayac, 3 strike, skor hedefi) bu
// sinifin uzerine insa edilecek, TASINMASI gerekmeyecek sekilde onceden acildi.
// BILESEN 2 ICIN NOT: RoundEnded'a gecis tetikleyecek gelecekteki kod, IsPaused=true
// iken bu gecisi YAPMAMALI (kullanici istegi — "pause sirasinda round bitisi
// tetiklenemesin").
[RequireComponent(typeof(NetworkObject))]
public class GameLoopManager : NetworkBehaviour
{
    public static GameLoopManager Instance { get; private set; }

    // GDD 8.2: round sirasinda kopan oyuncu bu sure icinde donmezse oturum kapanir, bolum basarisiz sayilir.
    // Duraklatma boyunca GERCEK zamanla sayilir; oyun devam edince (donus) sifirlanir.
    [Tooltip("Kopan oyuncunun donmesi icin beklenen sure (saniye). GDD 8.2: 300 (5 dk). Test icin kisaltilabilir.")]
    [SerializeField, Min(1f)] private float disconnectTimeoutSeconds = 300f;

    // UIStrings tablosundaki anahtar (DisconnectReason ile agdan gider; ceviri UI'da yapilir).
    public const string SessionTimeoutReasonKey = "error.session_timeout";

    // Sebepli DisconnectClient istemciyi bir sonraki guncellemede koparir (NGO: sebep mesaji once kuyruga
    // girer). Host, istemciler kopana kadar (en fazla bu kadar) bekler ki sebep mesaji gitsin; sonra kapanir.
    private const float SessionEndFlushSeconds = 2f;

    // Yalnizca sunucuda (host) tetiklenir: oturum zaman asimiyla bitti, istemciler koparildi. Host'un
    // arayuzu agi kapatip sebebi gosterir (bkz. LobbyUIController). Parametre: UIStrings anahtari.
    public event Action<string> OnServerSessionEnded;

    private float _pausedSeconds;
    private bool _sessionEnding;

    public readonly NetworkVariable<RoundState> CurrentRoundState =
        new(RoundState.Lobby, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Sadece RoundActive iken anlamli bir overlay — Lobby/RoundEnded'da _isPaused.Value
    // ne olursa olsun IsGamePaused (asagida) hep false doner, cagiran kod bunu ayrica
    // kontrol etmek ZORUNDA DEGIL.
    private readonly NetworkVariable<bool> _isPaused =
        new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Bölümün hata sayacı (GDD 3.4: 3 Hata'da seviye kaybedilir). TEK kayıt: sabır hatası, geç ve yanlış teslim
    // buraya yazar. Yalnızca sunucu yazar; duvar göstergesi ve kaybetme kararı ayrı adımlarda bunu okur.
    public readonly NetworkVariable<int> ErrorCount =
        new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Tooltip("Kaybetme eşiği: bu kadar hatada seviye kaybedilir (GDD 3.4: 3 — global sabit, seviye parametresi DEĞİL). " +
        "Duvar panelindeki X sayısı da budur. TEK yer.")]
    [SerializeField, Min(1)] private int maxErrors = 3;

    // Biten bölümün sonucu; RoundEnded'da anlamlıdır. Yalnızca sunucu yazar.
    public readonly NetworkVariable<RoundOutcome> Outcome =
        new(RoundOutcome.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Yalnızca sunucuda: yeniden başlatmadan hemen önce. Dünyada durum tutan her sistem (müşteriler, öğeler,
    // tezgah yığınları, oyuncu nesneleri) kendini burada temizler; GameLoopManager onları tanımaz.
    public event Action ServerRoundResetting;

    public int MaxErrors => maxErrors;

    public bool IsRoundActive => CurrentRoundState.Value == RoundState.RoundActive;

    // Tum eski "GameLoopManager.Instance.IsGamePaused.Value" cagri yerleri artik bu
    // computed property'i (artik bir NetworkVariable DEGIL, .Value EKLENMEMELI) okuyor.
    public bool IsGamePaused => IsRoundActive && _isPaused.Value;

    // "Oyuncular şu an bir şey yapabilir mi": round aktif VE oyun duraklatılmamış. Etkileşim, sinyal ve emote
    // çarkları, hem istemcide (gösterim) hem sunucuda (karar) AYNI koşulu buradan okur — ayrı ayrı yazılmaz.
    public bool IsPlayable => IsRoundActive && !_isPaused.Value;

    // Instance yokken de güvenli (lobi, sahne kapanışı).
    public static bool CanPlayersAct => Instance != null && Instance.IsPlayable;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // GameSystems kalici bir obje oldugu icin (bkz. RoleManager'daki ayni desen
            // notu) bir onceki hosting oturumundan kalma durum burada acikca sifirlanir.
            CurrentRoundState.Value = RoundState.Lobby;
            _isPaused.Value = false;
            ErrorCount.Value = 0;
            Outcome.Value = RoundOutcome.None;
        }

        _pausedSeconds = 0f;
        _sessionEnding = false;
    }

    private void Update()
    {
        if (!IsServer || _sessionEnding)
            return;

        if (!IsGamePaused)
        {
            _pausedSeconds = 0f;
            return;
        }

        _pausedSeconds += Time.unscaledDeltaTime;
        if (_pausedSeconds >= disconnectTimeoutSeconds)
            StartCoroutine(EndSessionForTimeout());
    }

    // GDD 8.2 "oturum kapatilir, bolum basarisiz sayilir": bagli istemciler sebeple koparilir (ekranlarinda
    // sebep yazar), ardindan host'un kendi arayuzu agi kapatir. Faz 0'da ayri lobi sahnesi olmadigi icin
    // "lobiye donus" = herkesin ilk ekrana donmesi; yeniden host edilerek tekrar oynanir.
    private IEnumerator EndSessionForTimeout()
    {
        _sessionEnding = true;
        Debug.LogWarning($"[GameLoopManager] Kopan oyuncu {disconnectTimeoutSeconds:F0} sn icinde donmedi; oturum kapatiliyor (bolum basarisiz).");

        foreach (var clientId in new List<ulong>(NetworkManager.ConnectedClientsIds))
        {
            if (clientId != NetworkManager.ServerClientId)
                NetworkManager.DisconnectClient(clientId, SessionTimeoutReasonKey);
        }

        float deadline = Time.realtimeSinceStartup + SessionEndFlushSeconds;
        while (NetworkManager != null && NetworkManager.ConnectedClientsIds.Count > 1 && Time.realtimeSinceStartup < deadline)
            yield return null;

        OnServerSessionEnded?.Invoke(SessionTimeoutReasonKey);
    }

    // Host'un "Oyunu Baslat" butonuyla cagirdigi, server-authoritative round baslatma —
    // eskiden RoleManager.StartRound() idi, tek otoriteye konsolide edildi. Rol sayisi
    // kontrolu icin RoleManager.AssignedRoleCount'a (public, sadece okuma) bakiyor;
    // rol ATAMA mantigina KARISMIYOR, o RoleManager'da kaliyor.
    public bool StartRound()
    {
        if (!IsServer)
            return false;

        if (CurrentRoundState.Value == RoundState.RoundActive)
            return true;

        if (RoleManager.Instance == null || RoleManager.Instance.AssignedRoleCount < RoleManager.MaxPlayers)
        {
            int count = RoleManager.Instance != null ? RoleManager.Instance.AssignedRoleCount : 0;
            Debug.LogWarning($"[GameLoopManager] Round baslatilamiyor, {count}/{RoleManager.MaxPlayers} oyuncu var.");
            return false;
        }

        ErrorCount.Value = 0;
        Outcome.Value = RoundOutcome.None;
        CurrentRoundState.Value = RoundState.RoundActive;
        _isPaused.Value = false;
        return true;
    }

    // Sunucu: bölüme 1 Hata yazar. Sebep yalnızca log içindir.
    public void ServerAddError(string reason)
    {
        if (!IsServer || !IsRoundActive)
            return;

        ErrorCount.Value++;
        Debug.LogWarning($"[GameLoopManager] 1 HATA: {reason}. Toplam hata: {ErrorCount.Value}/{maxErrors}.");

        // GDD 3.4: eşiğe ulaşılınca seviye kaybedilir.
        if (ErrorCount.Value >= maxErrors)
            ServerEndRound(RoundOutcome.Lost);
    }

    // Sunucu: bölümü bitirir. Round geri sayımı YOKTUR (K1) — kayıp hata eşiğinden, kazanç tüm müşterilerin
    // bitmesinden (CustomerDirector) gelir. Yalnızca RoundActive iken geçerlidir; ilk gelen sonuç kalır (aynı karede
    // hem son hata hem son müşteri olursa kayıp önce yazılır).
    public void ServerEndRound(RoundOutcome outcome)
    {
        if (!IsServer || !IsRoundActive || outcome == RoundOutcome.None)
            return;

        Outcome.Value = outcome;
        _isPaused.Value = false;
        CurrentRoundState.Value = RoundState.RoundEnded;
        Debug.Log($"[GameLoopManager] Bölüm bitti: {(outcome == RoundOutcome.Won ? "KAZANILDI" : "KAYBEDİLDİ")} (hata {ErrorCount.Value}/{maxErrors}).");
    }

    // Sunucu (host'un sonuç ekranı): bölümü TEMİZ yeniden başlatır. Önce herkes kendini temizler
    // (ServerRoundResetting), sonra round yeniden başlar ve seviye yeniden çözülür. Sızıntı denetimi: temizlikten
    // sonra ağda yalnızca sahneye yerleştirilmiş nesneler kalmalıdır.
    public bool ServerRestartRound()
    {
        if (!IsServer || CurrentRoundState.Value != RoundState.RoundEnded)
            return false;

        int before = NetworkManager.SpawnManager.SpawnedObjectsList.Count;
        ServerRoundResetting?.Invoke();

        int after = 0, dynamic = 0;
        foreach (var networkObject in NetworkManager.SpawnManager.SpawnedObjectsList)
        {
            after++;
            if (!networkObject.InScenePlaced)
                dynamic++;
        }

        if (dynamic == 0)
            Debug.Log($"[GameLoopManager] Yeniden başlatma temizliği: ağ nesnesi {before} -> {after} (hepsi sahne nesnesi, sızıntı yok).");
        else
            Debug.LogError($"[GameLoopManager] Yeniden başlatma temizliğinde SIZINTI: ağ nesnesi {before} -> {after}, {dynamic} dinamik nesne kaldı.");

        return StartRound();
    }

    // Lobby/RoundEnded'da pause anlamsiz/no-op (kullanici istegi) — RoundActive
    // disinda hicbir sey yapmiyor.
    public void ServerPauseForDisconnect()
    {
        if (!IsServer || !IsRoundActive)
            return;

        _isPaused.Value = true;
    }

    public void ServerResumeAfterReconnect()
    {
        if (!IsServer || !IsRoundActive)
            return;

        _isPaused.Value = false;
    }
}
