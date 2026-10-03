using System;
using Unity.Netcode;
using UnityEngine;

// Jestlerin sunucu otoritesi (GDD 3.6.0): Kasiyer'in sinyal çarkı (R) ve genel emote'lar (E) aynı altyapıyı
// kullanır. İstemci yalnızca niyet gönderir; sunucu doğrular ve HERKESE yayar (kasıtlı — jestleri herkes görür).
// NetworkVariable değil bilerek RPC: aynı jest art arda seçilirse bir NetworkVariable'da değer değişmediği için
// OnValueChanged tetiklenmezdi. Cooldown YOKTUR; tek kural "oynayan bitmeden yenisi başlatılamaz" ve bir
// etkileşim oynayanı iptal eder. Süre veriden gelir (SignalValue / EmoteDefinition).
[RequireComponent(typeof(NetworkObject))]
public class EmoteSystem : NetworkBehaviour
{
    public static EmoteSystem Instance { get; private set; }

    [SerializeField] private EmoteDefinition[] availableEmotes;

    // (gönderen client, emote dizini) — PlayerEmoteReactor kendi OwnerClientId'siyle karşılaştırıp tepkiyi oynatır.
    public event Action<ulong, int> OnEmoteTriggered;

    // (client) — o oyuncunun oynayan jesti bir etkileşimle kesildi; görseller hemen kalkar.
    public event Action<ulong> OnPlaybackCancelled;

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

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public override void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        base.OnDestroy();
    }

    // Genel emote (E). Rol kısıtı yoktur (GDD 3.6.0: tüm roller); sinyalle AYNI kurallar: round oynanabilir olmalı,
    // oynayan jest bitmiş olmalı.
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SelectEmoteServerRpc(int emoteIndex, RpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        if (availableEmotes == null || emoteIndex < 0 || emoteIndex >= availableEmotes.Length || availableEmotes[emoteIndex] == null)
            return;

        if (!GameLoopManager.CanPlayersAct || ServerIsBusy(senderId))
            return;

        _serverBusyUntil[senderId] = Time.unscaledTime + availableEmotes[emoteIndex].Duration;
        EmoteTriggeredClientRpc(senderId, emoteIndex);
    }

    [ClientRpc]
    private void EmoteTriggeredClientRpc(ulong senderId, int emoteIndex)
    {
        if (availableEmotes == null || emoteIndex < 0 || emoteIndex >= availableEmotes.Length || availableEmotes[emoteIndex] == null)
            return;

        if (senderId == NetworkManager.LocalClientId)
            _localBusyUntil = Time.unscaledTime + availableEmotes[emoteIndex].Duration;

        OnEmoteTriggered?.Invoke(senderId, emoteIndex);
    }

    // GDD 3.6.0: jest oynarken oyuncu bir nesneyle etkileşirse jest anında iptal olur. Sunucu, etkileşimi KABUL
    // ettiği anda çağırır (PlayerInteractor); oynayan bir şey yoksa hiçbir şey yayınlanmaz.
    public void ServerCancelPlayback(ulong clientId)
    {
        if (!IsServer || !ServerIsBusy(clientId))
            return;

        _serverBusyUntil.Remove(clientId);
        PlaybackCancelledClientRpc(clientId);
    }

    // Herkese gider — iptal de jestin kendisi gibi herkese yansır.
    [ClientRpc]
    private void PlaybackCancelledClientRpc(ulong clientId)
    {
        if (clientId == NetworkManager.LocalClientId)
            _localBusyUntil = 0f;

        OnPlaybackCancelled?.Invoke(clientId);
    }
}
