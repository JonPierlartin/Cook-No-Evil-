using System.Collections.Generic;

// LevelConfig -> ResolvedLevel. Saf fonksiyon: aynı config + aynı tohum her zaman aynı sonucu verir (System.Random,
// sabit çağrı sırası). Sahneye, ağa, UnityEngine.Random'a dokunmaz — Edit modunda da çağrılabilir. Hiçbir öğe, kanal
// veya değer sayısı varsaymaz; her şey config'teki listelerden gelir.
public static class LevelResolver
{
    public static ResolvedLevel Resolve(LevelConfig config, int seed)
    {
        var rng = new System.Random(seed);
        var result = new ResolvedLevel { Seed = seed };

        result.CustomerCount = System.Math.Max(0, config.CustomerCount.ResolveInt(rng));
        result.CustomerInterval = config.CustomerInterval.ResolveFloat(rng);
        result.BackupPoolSize = System.Math.Max(0, config.BackupPoolSize.ResolveInt(rng));
        result.TimeMultiplier = config.TimeMultiplier.ResolveFloat(rng);

        // Sipariş slotları: i. müşteri i. slotu, fazlası ve yedek havuz varsayılan slotu kullanır (30 Eyl kararı).
        // Sabır HER MÜŞTERİ için ayrı çekilir (3 Eki kararı) — aralık verildiyse müşteriden müşteriye değişir.
        for (int i = 0; i < result.CustomerCount; i++)
        {
            var order = ResolveOrder(i < config.OrderSlots.Count ? config.OrderSlots[i] : config.DefaultOrderSlot, rng, result.Warnings, $"sipariş {i + 1}");
            order.Patience = config.Patience.ResolveFloat(rng);
            result.Orders.Add(order);
        }

        if (config.OrderSlots.Count > result.CustomerCount)
            result.Warnings.Add($"{config.OrderSlots.Count} slot tanımlı ama müşteri sayısı {result.CustomerCount}; fazla slotlar kullanılmadı.");

        for (int i = 0; i < result.BackupPoolSize; i++)
        {
            var order = ResolveOrder(config.DefaultOrderSlot, rng, result.Warnings, $"yedek {i + 1}");
            order.Patience = config.Patience.ResolveFloat(rng);
            result.BackupOrders.Add(order);
        }

        result.OpenVariants.AddRange(config.OpenVariants.Resolve(rng));
        result.OpenProductCategories = config.OpenProductCategories;

        foreach (var channelConfig in config.Channels)
        {
            if (channelConfig == null || !channelConfig.enabled || channelConfig.channel == null)
                continue;

            var mapping = new ResolvedLevel.ChannelMapping { Channel = channelConfig.channel };
            foreach (var value in channelConfig.values)
            {
                if (value != null)
                    mapping.Values.Add(value);
            }

            mapping.Items.AddRange(channelConfig.items.Resolve(rng));
            if (mapping.Items.Count > mapping.Values.Count)
                result.Warnings.Add($"Kanal {channelConfig.channel.name}: {mapping.Items.Count} öğe, {mapping.Values.Count} değer — fazla öğeler eşleşmedi.");
            result.Channels.Add(mapping);
        }

        result.ActiveStations.AddRange(config.ActiveStations.Resolve(rng));
        result.SaucePumpOrder.AddRange(config.SaucePumpOrder.Resolve(rng));

        foreach (var stock in config.Stocks)
        {
            if (stock == null || stock.item == null)
                continue;

            result.Stocks.Add(new ResolvedLevel.Stock
            {
                Item = stock.item,
                Unlimited = stock.unlimited,
                StartPortions = stock.unlimited ? 0 : System.Math.Max(0, stock.startPortions.ResolveInt(rng))
            });
        }

        result.PrepPhaseEnabled = config.PrepPhaseEnabled;
        result.PrepPhaseSeconds = config.PrepPhaseEnabled ? config.PrepPhaseSeconds.ResolveFloat(rng) : 0f;
        result.ShowControlHints = config.ShowControlHints;
        return result;
    }

    private static ResolvedLevel.Order ResolveOrder(OrderSlot slot, System.Random rng, List<string> warnings, string label)
    {
        var order = new ResolvedLevel.Order();
        if (slot == null)
            return order;

        var variants = slot.variant.Resolve(rng);
        order.Variant = variants.Count > 0 ? variants[0] : null;
        if (variants.Count > 1)
            warnings.Add($"{label}: {variants.Count} varyant çözüldü, ilki kullanıldı (kategori başına en fazla 1 ürün, GDD 6.7).");

        if (order.Variant != null)
        {
            // 'Komple randomize' kısayolu: havuz = varyantın çıkarılabilir işaretli malzemeleri (GDD 7.3.2).
            var poolOverride = slot.missingPoolIsVariantRemovables ? order.Variant.GetRemovableItems() : null;
            foreach (var item in slot.missing.Resolve(rng, poolOverride))
            {
                if (order.Variant.Contains(item))
                    order.Missing.Add(item);
                else
                    warnings.Add($"{label}: eksik '{item.name}' varyant '{order.Variant.name}' içinde yok, yok sayıldı.");
            }
        }

        order.Drink = First(slot.drink.Resolve(rng));
        order.IceCream = First(slot.iceCream.Resolve(rng));
        order.Side = First(slot.side.Resolve(rng));
        return order;
    }

    private static T First<T>(List<T> items) where T : UnityEngine.Object => items.Count > 0 ? items[0] : null;
}
