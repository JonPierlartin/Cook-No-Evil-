using System.Collections.Generic;
using System.Text;

// LevelConfig'in bir bölüm için çözülmüş, somut hâli (LevelResolver üretir). Düz çalışma zamanı nesnesi:
// müşteri, pano, çark gibi tüketiciler sonraki adımlarda bunu okur. Replike edilmez (bu adımda).
public class ResolvedLevel
{
    public class Order
    {
        public BurgerVariant Variant;
        // Bu müşterinin sabır süresi (sn). Her müşteri için AYRI çekilir (Ersel, 3 Eki 2026) — aralık verildiyse
        // 1. müşteri 22, 2. müşteri 28 olabilir; bölüm başında tek değer çekilip herkese verilmez.
        public float Patience;
        public readonly List<ItemType> Missing = new();
        public ItemType Drink;
        public ItemType IceCream;
        public ItemType Side;
    }

    public class ChannelMapping
    {
        public SignalChannel Channel;
        public readonly List<SignalValue> Values = new();
        // Eşleşme: Items[i] <-> Values[i]. Değer sayısından fazla öğe varsa fazlası eşleşmez (uyarı).
        public readonly List<ItemType> Items = new();
    }

    public class Stock
    {
        public ItemType Item;
        public bool Unlimited;
        public int StartPortions;
    }

    public int Seed;
    public int CustomerCount;
    public float CustomerInterval;
    public int BackupPoolSize;
    public float TimeMultiplier;
    public readonly List<Order> Orders = new();
    public readonly List<Order> BackupOrders = new();
    public readonly List<BurgerVariant> OpenVariants = new();
    public LevelConfig.ProductCategory OpenProductCategories;
    public readonly List<ChannelMapping> Channels = new();
    public readonly List<StationId> ActiveStations = new();
    public readonly List<ItemType> SaucePumpOrder = new();
    public readonly List<Stock> Stocks = new();
    public bool PrepPhaseEnabled;
    public float PrepPhaseSeconds;
    public bool ShowControlHints;
    public readonly List<string> Warnings = new();

    public string Describe(string levelName)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[Seviye] '{levelName}' çözüldü (tohum {Seed})");
        sb.AppendLine($"  Müşteri sayısı: {CustomerCount} · müşteriler arası bekleme: {CustomerInterval:0.#} sn · yedek müşteri: {BackupPoolSize} · sipariş süresi çarpanı: {TimeMultiplier:0.##}");
        for (int i = 0; i < Orders.Count; i++)
            sb.AppendLine($"  Sipariş {i + 1}: {DescribeOrder(Orders[i])}");
        for (int i = 0; i < BackupOrders.Count; i++)
            sb.AppendLine($"  Yedek {i + 1}: {DescribeOrder(BackupOrders[i])}");
        sb.AppendLine($"  Açık varyantlar (kitapçık): {Names(OpenVariants)} · kategoriler: {OpenProductCategories}");
        foreach (var channel in Channels)
        {
            var pairs = new List<string>();
            for (int i = 0; i < channel.Items.Count; i++)
                pairs.Add($"{Name(channel.Items[i])}→{(i < channel.Values.Count ? Name(channel.Values[i]) : "(değer yok)")}");
            sb.AppendLine($"  Kanal {Name(channel.Channel)}: değerler [{Names(channel.Values)}] · eşleşme {string.Join(", ", pairs)}");
        }
        sb.AppendLine($"  Aktif makine/kaplar: {Names(ActiveStations)}");
        sb.AppendLine($"  Sos pompası dizilimi: {(SaucePumpOrder.Count > 0 ? Names(SaucePumpOrder) : "yok")} · stok: {(Stocks.Count > 0 ? Stocks.Count + " kayıt" : "sınırsız")}");
        sb.AppendLine($"  Hazırlık fazı: {(PrepPhaseEnabled ? PrepPhaseSeconds.ToString("0.#") + " sn" : "kapalı")} · kontrol ipuçları: {(ShowControlHints ? "açık" : "kapalı")}");
        foreach (var warning in Warnings)
            sb.AppendLine($"  UYARI: {warning}");
        return sb.ToString();
    }

    private static string DescribeOrder(Order order)
    {
        string burger = order.Variant != null
            ? $"{Name(order.Variant)}{(order.Missing.Count > 0 ? " — eksik: " + Names(order.Missing) : " — tam")}"
            : "hamburger yok";
        return $"sabır {order.Patience:0.#} sn · {burger} · içecek {Name(order.Drink)} · dondurma {Name(order.IceCream)} · yan {Name(order.Side)}";
    }

    private static string Name(UnityEngine.Object obj) => obj != null ? obj.name : "yok";

    private static string Names<T>(List<T> items) where T : UnityEngine.Object
    {
        var names = new List<string>();
        foreach (var item in items)
            names.Add(Name(item));
        return names.Count > 0 ? string.Join(", ", names) : "yok";
    }
}
