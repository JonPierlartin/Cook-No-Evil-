using System;
using Unity.Netcode;
using UnityEngine;

// Kasiyer VE Komi'nin emote carkindan sectigi tepki (GDD 2.2 — Kasiyer'in Komi'yi
// yonlendirmesi; Komi'nin erisimi kisitli/placeholder — bkz. komiEmoteLimit). Eskiden
// sadece Komi'ye hedefli bir ClientRpc'ydi (ReceivedEmoteIcon adinda ayri bir UI ile);
// yeniden tasarlandi: artik HERKESE broadcast ediliyor ve secimi yapan oyuncunun kendi
// karakteri uzerinde (PlayerEmoteReactor) herkesin gorebilecegi kisa bir gorsel tepki
// tetikliyor. NetworkVariable degil bilerek ClientRpc kullaniliyor — ayni emote art arda
// iki kez secilirse bir NetworkVariable'da deger degismedigi icin OnValueChanged hic
// tetiklenmezdi (sessizce yutulurdu); RPC her cagriyi kosulsuz iletir.
[RequireComponent(typeof(NetworkObject))]
public class EmoteSystem : NetworkBehaviour
{
    public static EmoteSystem Instance { get; private set; }

    [SerializeField] private EmoteDefinition[] availableEmotes;

    // Komi da carka erisebilir ama Kasiyer'den daha kisitli bir secimle: sadece
    // availableEmotes dizisinin ILK N elemani. Placeholder/basit tutuluyor (kullanici
    // istegi) — gercek kisitli-liste icerigi (hangi emote'lar) ileride ayrica
    // tasarlanacak, simdilik sadece SAYI kisitlanmis durumda.
    [SerializeField] private int komiEmoteLimit = 1;

    // Ayni veya farkli emote farketmeksizin, son basarili secimden itibaren bu sure
    // gecmeden yeni bir secim reddedilir (spam/iletisim kirliligini onlemek icin,
    // kullanici istegi). GLOBAL bir cooldown — kimin sectigi onemli degil, herkes
    // icin ayni sayaci paylasir (komiEmoteLimit gibi basit tutuluyor, kisi-basi
    // ayrica takip edilmiyor).
    [SerializeField] private float selectionCooldown = 2.5f;

    // Server-authoritative zaman damgasi (NetworkManager.ServerTime.Time, sunucuda
    // yazilir) — client'lar IsOnCooldown uzerinden canli okuyup carki acmadan/secim
    // yapmadan once kendi taraflarinda da kontrol edebilir (komiEmoteLimit'teki gibi
    // hem client hem server tarafinda).
    private readonly NetworkVariable<double> _lastSelectionServerTime =
        new(-1000d, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // (kasiyerClientId, emoteIndex) — PlayerEmoteReactor kendi OwnerClientId'siyle
    // karsilastirip sadece dogru objede tepki oynatir. Isim tarihsel: artik Komi da
    // tetikleyebiliyor, ama alan/parametre adi degistirilmedi (RPC/UI'da hala
    // "hangi client tetikledi" anlaminda kullaniliyor).
    public event Action<ulong, int> OnEmoteTriggered;

    // ---- Sinyal (R çarkı, GDD 3.6.0) ----
    [Header("Sinyal (GDD 3.6.0)")]
    [Tooltip("Sinyal çarkını kullanabilen roller (GDD: yalnızca Kasiyer).")]
    [SerializeField] private PlayerRole[] signalRoles = { PlayerRole.Kasiyer };
    [Tooltip("'Sipariş Bitti' jesti: kategorilere ek, tek başına bir çark seçeneği. Görseli ve süresi bu asset'te.")]
    [SerializeField] private SignalValue orderDoneSignal;

    // (gönderen client, sinyal) — PlayerSignalDisplay kendi OwnerClientId'siyle karşılaştırıp işareti gösterir.
    public event Action<ulong, SignalValue> OnSignalStarted;

    // Sunucu: her oyuncunun oynayan sinyalinin/emote'unun biteceği an (Time.unscaledTime). "Bir emote bitmeden
    // yenisi başlatılamaz" (GDD 3.6.0) kuralının tek kaydı; cooldown değildir, süre veriden gelir.
    private readonly System.Collections.Generic.Dictionary<ulong, float> _serverBusyUntil = new();
    // İstemci: yerel oyuncunun oynayan sinyalinin biteceği an. Yayın geldiği anda başlar, yani sunucudan DAHA GEÇ
    // biter — istemci daha katıdır; "engelli değil" dediği anda sunucu da kabul eder.
    private float _localBusyUntil;

    public SignalValue OrderDoneSignal => orderDoneSignal;

    public bool IsLocalBusy => Time.unscaledTime < _localBusyUntil;

    public bool CanRoleSignal(PlayerRole role) => signalRoles != null && Array.IndexOf(signalRoles, role) >= 0;

    // Kanal/değer dizinlerini sinyale çözer. Yalnızca bu bölümde AÇIK olanlar geçerlidir (replike satırlar).
    public bool TryResolveSignal(int channelIndex, int valueIndex, out SignalValue value)
    {
        if (channelIndex == SignalWheelModel.OrderDoneChannel)
        {
            value = orderDoneSignal;
            return value != null;
        }

        value = null;
        var director = LevelDirector.Instance;
        return director != null && director.IsSignalOpen(channelIndex, valueIndex)
            && director.TryGetSignalValue(channelIndex, valueIndex, out value);
    }

    public override void OnNetworkSpawn()
    {
        // GameSystems kalıcı bir sahne nesnesi: önceki oturumun kaydı taşınmasın.
        _serverBusyUntil.Clear();
        _localBusyUntil = 0f;
    }

    private bool ServerIsBusy(ulong clientId) =>
        _serverBusyUntil.TryGetValue(clientId, out float until) && Time.unscaledTime < until;

    // İstemci yalnızca NİYET gönderir ("şu kanal, şu değer"); geçerliliğe sunucu karar verir (K6) ve herkese yayar.
    // GameSystems sunucunun nesnesidir; her istemci çağırabilmeli (InvokePermission.Everyone). Gönderen kimliği
    // RpcParams'tan okunur, istemciden gelen bir alandan değil.
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestSignalServerRpc(int channelIndex, int valueIndex, RpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        if (RoleManager.Instance == null || !CanRoleSignal(RoleManager.Instance.GetRole(senderId)))
        {
            Debug.LogWarning($"[EmoteSystem] Sinyal reddedildi (client={senderId}): rol izinli değil.");
            return;
        }

        if (!GameLoopManager.CanPlayersAct)
        {
            Debug.LogWarning($"[EmoteSystem] Sinyal reddedildi (client={senderId}): round aktif değil veya oyun durduruldu.");
            return;
        }

        if (!TryResolveSignal(channelIndex, valueIndex, out var signal))
        {
            Debug.LogWarning($"[EmoteSystem] Sinyal reddedildi (client={senderId}): kanal {channelIndex} / değer {valueIndex} bu bölümde açık değil.");
            return;
        }

        if (ServerIsBusy(senderId))
        {
            Debug.LogWarning($"[EmoteSystem] Sinyal reddedildi (client={senderId}): oynayan sinyal bitmedi.");
            return;
        }

        _serverBusyUntil[senderId] = Time.unscaledTime + signal.Duration;
        SignalStartedClientRpc(senderId, channelIndex, valueIndex);
    }

    // Herkese gider — kasıtlı (GDD 3.6.0: jestleri herkes görür; CLAUDE.md emote broadcast istisnası).
    [ClientRpc]
    private void SignalStartedClientRpc(ulong senderId, int channelIndex, int valueIndex)
    {
        // Açıklık sunucuda doğrulandı; burada yalnızca asset'e çözülür (satırlar yayından sonra gelebilir).
        SignalValue signal = null;
        if (channelIndex == SignalWheelModel.OrderDoneChannel)
            signal = orderDoneSignal;
        else if (LevelDirector.Instance != null)
            LevelDirector.Instance.TryGetSignalValue(channelIndex, valueIndex, out signal);

        if (signal == null)
            return;

        if (senderId == NetworkManager.LocalClientId)
            _localBusyUntil = Time.unscaledTime + signal.Duration;

        OnSignalStarted?.Invoke(senderId, signal);
    }

    public EmoteDefinition[] AvailableEmotes => availableEmotes;
    public int KomiEmoteLimit => komiEmoteLimit;
    public float SelectionCooldown => selectionCooldown;

    public bool IsOnCooldown =>
        NetworkManager != null &&
        NetworkManager.ServerTime.Time - _lastSelectionServerTime.Value < selectionCooldown;

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

    [ServerRpc(RequireOwnership = false)]
    public void SelectEmoteServerRpc(int emoteIndex, ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        if (availableEmotes == null || emoteIndex < 0 || emoteIndex >= availableEmotes.Length)
            return;

        if (RoleManager.Instance == null)
            return;

        var role = RoleManager.Instance.GetRole(senderId);
        if (role != PlayerRole.Kasiyer && role != PlayerRole.Komi)
            return;

        // Komi sadece kisitli (ilk N) emote'a erisebilir; Kasiyer tam listeyi kullanir.
        if (role == PlayerRole.Komi && emoteIndex >= komiEmoteLimit)
            return;

        if (GameLoopManager.Instance == null || !GameLoopManager.Instance.IsRoundActive)
            return;

        // "Oyun durduruldu" (bkz. PlayerController/PlayerInteractor/EmoteWheelUI ayni
        // kontrol) server-authoritative olarak burada da doğrulanıyor — client tarafi
        // (EmoteWheelUI) carki acmayi zaten engelliyor, bu sadece bypass'a karsi savunma.
        if (GameLoopManager.Instance.IsGamePaused)
            return;

        // Ayni/farkli emote farketmeksizin, cooldown suresi dolmadan yeni secim
        // server-authoritative olarak reddedilir — client tarafi (EmoteWheelUI) zaten
        // ayni kontrolu yapip carki acmiyor/secim yollamiyor, bu bypass'a karsi savunma.
        if (IsOnCooldown)
            return;

        _lastSelectionServerTime.Value = NetworkManager.ServerTime.Time;

        EmoteTriggeredClientRpc(senderId, emoteIndex);
    }

    [ClientRpc]
    private void EmoteTriggeredClientRpc(ulong kasiyerClientId, int emoteIndex)
    {
        OnEmoteTriggered?.Invoke(kasiyerClientId, emoteIndex);
    }
}
