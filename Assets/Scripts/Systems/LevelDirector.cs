using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Aktif seviyenin TEK seçim noktası (Inspector) ve bölüm başındaki çözümleme. Round RoundActive'e geçtiğinde
// YALNIZCA SUNUCUDA, bir kez, LevelConfig somut değerlere çözülür (LevelResolver), Current'ta tutulur ve konsola
// okunur biçimde yazılır (geri bildirim). GameSystems üzerinde durur.
//
// Replikasyon (Adım 13): istemcilerin ihtiyaç duyduğu kısım — bu bölümde AÇIK sinyal kanalları, değerleri ve
// eşleşmeleri (garnitür→numara, protein→yön) — SignalRows ile herkese gider; yalnızca sunucu yazar. Sinyal çarkı
// ve duvar panosu yalnızca bu listeden kurulur (kodda kategori/değer listesi yok). Satırlardaki dizinler aktif
// LevelConfig'in listelerine işaret eder; config sahnede tek yerden seçildiği için herkeste aynıdır.
[RequireComponent(typeof(NetworkObject))]
public class LevelDirector : NetworkBehaviour
{
    public static LevelDirector Instance { get; private set; }

    [Tooltip("Bu sahnede oynanan seviye. Tek kaynak (K8).")]
    [SerializeField] private LevelConfig levelConfig;

    public readonly NetworkList<SignalRow> SignalRows = new();

    // Yalnızca sunucuda dolu; round başında üretilir.
    public ResolvedLevel Current { get; private set; }

    public LevelConfig Config => levelConfig;

    // Yalnızca sunucuda: seviye çözüldü (round başı). Müşteri akışı gibi sunucu tüketicileri buradan başlar.
    public event System.Action<ResolvedLevel> ServerLevelResolved;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Singleton'lara Awake'te erişilmez (CLAUDE.md NGO notu).
        if (GameLoopManager.Instance != null)
            GameLoopManager.Instance.CurrentRoundState.OnValueChanged += HandleRoundStateChanged;
        else
            Debug.LogError("[LevelDirector] GameLoopManager.Instance bulunamadı.");
    }

    public override void OnNetworkSpawn()
    {
        // GameSystems kalıcı bir sahne nesnesi: önceki oturumdan kalan durum sunucuda açıkça temizlenir.
        if (IsServer)
        {
            SignalRows.Clear();
            Current = null;
        }
    }

    public override void OnDestroy()
    {
        if (GameLoopManager.Instance != null)
            GameLoopManager.Instance.CurrentRoundState.OnValueChanged -= HandleRoundStateChanged;

        if (Instance == this)
            Instance = null;

        base.OnDestroy();
    }

    private void HandleRoundStateChanged(RoundState previous, RoundState current)
    {
        if (current != RoundState.RoundActive || !IsServer)
            return;

        if (levelConfig == null)
        {
            Debug.LogError("[LevelDirector] LevelConfig atanmamış; seviye çözülemedi.");
            return;
        }

        int seed = levelConfig.UseFixedSeed ? levelConfig.Seed : new System.Random().Next();
        Current = LevelResolver.Resolve(levelConfig, seed);

        SignalRows.Clear();
        foreach (var row in BuildSignalRows(Current))
            SignalRows.Add(row);

        Debug.Log(Current.Describe(levelConfig.name));
        ServerLevelResolved?.Invoke(Current);
    }

    // Çözülmüş kanal eşleşmelerinden replike satırları üretir. Saf: Edit modunda da çağrılabilir.
    public static List<SignalRow> BuildSignalRows(ResolvedLevel level)
    {
        var rows = new List<SignalRow>();
        foreach (var channel in level.Channels)
        {
            for (int i = 0; i < channel.Values.Count; i++)
            {
                int itemId = i < channel.Items.Count && channel.Items[i] != null ? channel.Items[i].Id : SignalRow.NoItem;
                rows.Add(new SignalRow(channel.ConfigIndex, channel.ValueIndices[i], itemId));
            }
        }

        return rows;
    }

    // Bu kanal değeri bu bölümde açık mı (sunucu doğrulaması ve istemci gösterimi AYNI sorguyu kullanır).
    public bool IsSignalOpen(int channelIndex, int valueIndex)
    {
        for (int i = 0; i < SignalRows.Count; i++)
        {
            if (SignalRows[i].ChannelIndex == channelIndex && SignalRows[i].ValueIndex == valueIndex)
                return true;
        }

        return false;
    }

    // Dizinleri aktif LevelConfig üzerinden asset'e çözer. Aralık dışıysa false.
    public bool TryGetSignalValue(int channelIndex, int valueIndex, out SignalValue value)
    {
        value = null;
        if (levelConfig == null || channelIndex < 0 || channelIndex >= levelConfig.Channels.Count)
            return false;

        var values = levelConfig.Channels[channelIndex].values;
        if (valueIndex < 0 || valueIndex >= values.Count)
            return false;

        value = values[valueIndex];
        return value != null;
    }
}
