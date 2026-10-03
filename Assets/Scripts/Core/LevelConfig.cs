using System;
using System.Collections.Generic;
using UnityEngine;

// K8 / GDD 11.9 — seviyeden seviyeye değişen HER ŞEYİN tek kaynağı. Alan silme yasağı: Faz 0'da kullanılmayan
// alanlar (stok, fritöz/içecek/dondurma makineleri, sos konumları, Renk/Şekil/Vücut kanalları, hazırlık fazı,
// kontrol ipuçları) tanımlı ve boş/kapalıdır. Sabitleme yasağı: kod bu değerlerin hiçbirini varsaymaz.
// Değişken alanların hepsi iki ortak tipten türer (GDD 7.3.4): NumericValue (elle / aralık) ve Selection<T>
// (sabit / havuz). Bölüm başında sunucu bir kez somut değerlere çözer (LevelResolver, LevelDirector).
[CreateAssetMenu(fileName = "LevelConfig", menuName = "Cook No Evil/Level Config")]
public class LevelConfig : ScriptableObject
{
    // Menüdeki ürün kategorileri (GDD 6.7). Hamburger zinciri + Komi/Şef'in yan üretim kategorileri.
    [Flags]
    public enum ProductCategory
    {
        None = 0,
        Hamburger = 1 << 0,
        PatatesEkstra = 1 << 1,
        Icecek = 1 << 2,
        Dondurma = 1 << 3
    }

    // Bir iletişim kanalının bu seviyedeki hâli (GDD 3.6.0, 3.6.3). values[i] <-> items çözümünün i. öğesi eşleşir.
    // Rastgele modda eşleşme bölüm başında karıştırılır (items: Random + takeAll). Kanal/değer/öğe sayısı veriden gelir.
    [Serializable]
    public class ChannelConfig
    {
        public SignalChannel channel;
        [Tooltip("Kanal çarkta görünsün mü (GDD 6.7.5: kategori, tanıtıldığı bölümde belirir).")]
        public bool enabled;
        [Tooltip("Bu seviyede açık değerler, eşleşme sırasıyla (ör. Sayı: 1,2,3,4,5).")]
        public List<SignalValue> values = new();
        [Tooltip("Kanalla eşleşen öğeler (ör. garnitürler). Sabit: sıra elle; Rastgele + takeAll: bölüm başında karışır.")]
        public Selection<ItemType> items = new();
    }

    // GDD 5.4.1 stok bazlı tanım. Faz 0'da boş (stok sistemi Faz 1).
    [Serializable]
    public class StockEntry
    {
        public ItemType item;
        public bool unlimited = true;
        public NumericValue startPortions;
    }

    // GDD 5.4.1 tetik bazlı tanım: N. teslimattan sonra X biter. Faz 0'da boş.
    [Serializable]
    public class StockTrigger
    {
        public ItemType item;
        public NumericValue afterDeliveryCount;
    }

    [Header("Müşteri akışı (GDD 3.4, 3.4.2, 3.4.4)")]
    [Tooltip("Toplam müşteri sayısı; oyunculara gösterilmez (GDD 3.4).")]
    [SerializeField] private NumericValue customerCount = NumericValue.Fixed(1);
    [Tooltip("Müşteriler arası temel aralık (sn).")]
    [SerializeField] private NumericValue customerInterval = NumericValue.Range(10f, 15f);
    [Tooltip("Aralık kısalma eğrisi: x = müşteri sırası (0..1), y = temel aralık çarpanı.")]
    [SerializeField] private AnimationCurve intervalCurve = AnimationCurve.Constant(0f, 1f, 1f);
    [Tooltip("Sabır süresi (sn), sipariş alınana kadar (GDD 3.4.4). Aralık verilirse HER MÜŞTERİ için ayrı çekilir.")]
    [SerializeField] private NumericValue patience = NumericValue.Range(20f, 30f);
    [Tooltip("Yedek müşteri havuzu — yalnızca sabır hatasında gelir (GDD 3.4.4).")]
    [SerializeField] private NumericValue backupPoolSize = NumericValue.Fixed(2);

    [Header("Siparişler (GDD 7.3)")]
    [Tooltip("Müşteri sırasına göre sipariş slotları. i. müşteri i. slotu kullanır.")]
    [SerializeField] private List<OrderSlot> orderSlots = new();
    [Tooltip("Slot listesinden fazla gelen müşteriler ve yedek havuz müşterileri bunu kullanır (30 Eyl kararı).")]
    [SerializeField] private OrderSlot defaultOrderSlot = new();

    [Header("Menü (GDD 6.7.5, 3.6.2)")]
    [Tooltip("Açık hamburger varyantları. Tarif kitapçığının açılımları bu listeden türetilir (ayrı alan yok).")]
    [SerializeField] private Selection<BurgerVariant> openVariants = new();
    [Tooltip("Açık ürün kategorileri.")]
    [SerializeField] private ProductCategory openProductCategories = ProductCategory.Hamburger;

    [Header("İletişim (GDD 3.6.0, 3.6.3)")]
    [Tooltip("Sinyal çarkındaki kanallar; açık olanlar ve değerleri. Garnitür→numara, protein→yön eşleşmeleri burada.")]
    [SerializeField] private List<ChannelConfig> channels = new();

    [Header("Mutfak (GDD 6.7.5, 5.6, 5.4)")]
    [Tooltip("Bu bölümde aktif makineler ve malzeme kapları (asset kimlikleri). Açma/kapama davranışı henüz yok.")]
    [SerializeField] private Selection<StationId> activeStations = new();
    [Tooltip("Sos pompalarının soldan sağa dizilimi (GDD 5.6; Rastgele + takeAll = bölüm başında karışır). Faz 0'da boş.")]
    [SerializeField] private Selection<ItemType> saucePumpOrder = new();
    [Tooltip("Malzeme başlangıç stokları (GDD 5.4.1). Faz 0'da boş = sınırsız.")]
    [SerializeField] private List<StockEntry> stocks = new();
    [Tooltip("Tetik bazlı stok tükenmesi (GDD 5.4.1). Faz 0'da boş.")]
    [SerializeField] private List<StockTrigger> stockTriggers = new();

    [Header("Süre (GDD 3.4.1)")]
    [Tooltip("Sipariş süresi çarpanı (erken seviyelerde cömert, geç seviyelerde sıkı).")]
    [SerializeField] private NumericValue timeMultiplier = NumericValue.Fixed(1f);

    [Header("Akış (GDD 3.4.3, 6.7.5)")]
    [Tooltip("İlk müşteriden önce hazırlık penceresi. Faz 0'da kapalı (henüz uygulanmadı).")]
    [SerializeField] private bool prepPhaseEnabled;
    [SerializeField] private NumericValue prepPhaseSeconds = NumericValue.Fixed(15f);
    [Tooltip("Yalnızca 1. bölümde bağlama duyarlı kontrol ipuçları. Faz 0'da kapalı (henüz uygulanmadı).")]
    [SerializeField] private bool showControlHints;

    [Header("Tohum")]
    [Tooltip("Açıkken aynı tohum her bölümde aynı çözümü verir (test/tekrar üretim).")]
    [SerializeField] private bool useFixedSeed;
    [SerializeField] private int seed;

    public NumericValue CustomerCount => customerCount;
    public NumericValue CustomerInterval => customerInterval;
    public AnimationCurve IntervalCurve => intervalCurve;
    public NumericValue Patience => patience;
    public NumericValue BackupPoolSize => backupPoolSize;
    public IReadOnlyList<OrderSlot> OrderSlots => orderSlots;
    public OrderSlot DefaultOrderSlot => defaultOrderSlot;
    public Selection<BurgerVariant> OpenVariants => openVariants;
    public ProductCategory OpenProductCategories => openProductCategories;
    public IReadOnlyList<ChannelConfig> Channels => channels;
    public Selection<StationId> ActiveStations => activeStations;
    public Selection<ItemType> SaucePumpOrder => saucePumpOrder;
    public IReadOnlyList<StockEntry> Stocks => stocks;
    public IReadOnlyList<StockTrigger> StockTriggers => stockTriggers;
    public NumericValue TimeMultiplier => timeMultiplier;
    public bool PrepPhaseEnabled => prepPhaseEnabled;
    public NumericValue PrepPhaseSeconds => prepPhaseSeconds;
    public bool ShowControlHints => showControlHints;
    public bool UseFixedSeed => useFixedSeed;
    public int Seed => seed;
}
