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

    [Tooltip("Sıralı seviye listesi (K8: aktif seviyenin tek seçim yeri). Oyun ilk seviyeden başlar; kazanınca host " +
        "'Sonraki seviye' ile bir sonrakine geçer.")]
    [SerializeField] private List<LevelConfig> levels = new();

    // Aktif seviyenin listedeki dizini. Yalnızca sunucu yazar; istemciler aynı listeyi taşıdığı için aynı
    // LevelConfig'e çözer (asset referansı ağdan gitmez).
    public readonly NetworkVariable<int> CurrentLevelIndex =
        new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private LevelConfig levelConfig =>
        CurrentLevelIndex.Value >= 0 && CurrentLevelIndex.Value < levels.Count ? levels[CurrentLevelIndex.Value] : null;

    public bool HasNextLevel => CurrentLevelIndex.Value + 1 < levels.Count;

    // Sunucu: bir sonraki seviyeyi seçer (round yeniden başlatılmadan ÖNCE çağrılır).
    public bool ServerAdvanceLevel()
    {
        if (!IsServer || !HasNextLevel)
            return false;

        CurrentLevelIndex.Value++;
        return true;
    }

    public readonly NetworkList<SignalRow> SignalRows = new();

    // Bu bölümde AÇIK varyantlar (tarif kitapçığı bunlardan üretilir), varyant diziniyle (bkz. GetVariantIndex).
    // Yalnızca sunucu yazar, round başında.
    public readonly NetworkList<int> OpenVariantIndices = new();

    // Yalnızca sunucuda dolu; round başında üretilir.
    public ResolvedLevel Current { get; private set; }

    public LevelConfig Config => levelConfig;

    private List<BurgerVariant> _variants;
    private LevelConfig _variantsOf;

    private List<BurgerVariant> Variants
    {
        get
        {
            // Seviye değişince varyant dizini de değişir.
            if (_variants == null || _variantsOf != levelConfig)
            {
                _variantsOf = levelConfig;
                _variants = levelConfig != null ? levelConfig.CollectVariants() : new List<BurgerVariant>();
            }

            return _variants;
        }
    }

    // Varyant <-> ağ dizini (LevelConfig.CollectVariants sırası; her makinede aynı). Yoksa -1 / false.
    public int GetVariantIndex(BurgerVariant variant)
    {
        return variant != null ? Variants.IndexOf(variant) : -1;
    }

    public bool TryGetVariant(int index, out BurgerVariant variant)
    {
        variant = index >= 0 && index < Variants.Count ? Variants[index] : null;
        return variant != null;
    }

    // Bu bölümde açık varyantlar, replike dizinlerden asset'e çözülmüş (her istemcide çalışır).
    public List<BurgerVariant> GetOpenVariants()
    {
        var result = new List<BurgerVariant>();
        for (int i = 0; i < OpenVariantIndices.Count; i++)
        {
            if (TryGetVariant(OpenVariantIndices[i], out var variant))
                result.Add(variant);
        }

        return result;
    }

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
            OpenVariantIndices.Clear();
            Current = null;
            CurrentLevelIndex.Value = 0;
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

        OpenVariantIndices.Clear();
        foreach (var variant in Current.OpenVariants)
        {
            int index = GetVariantIndex(variant);
            if (index >= 0)
                OpenVariantIndices.Add(index);
        }

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
