using System.Collections.Generic;

// Sipariş süresi hesabı (GDD 3.4.1). Saf fonksiyonlar: sahneye ve ağa dokunmaz, Edit modunda da çağrılabilir.
//
// Sinyal sayısı siparişin İÇERİĞİNDEN çıkar: siparişte bulunan ve bu bölümde AÇIK bir kanalda eşleşmesi olan her
// öğe bir sinyaldir (protein, her garnitür, sos, içecek... — hangi kanal olduğu önemli değildir), artı siparişi
// kapatan "Sipariş Bitti". Kodda kanal adı, kanal listesi veya sabit bir sayı yoktur: yeni bir kanal veride
// açılıp öğeleri eşleştirildiğinde o öğeler kendiliğinden sayılır. Eksik malzeme sinyal değildir (GDD 3.6.1) —
// sipariş yalnızca daha kısa olur.
public static class OrderTimeCalculator
{
    // Siparişte iletilmesi gereken öğeler: varyantın malzemeleri (eksikler hariç) + yan ürünler.
    public static List<ItemType> CollectContent(ResolvedLevel.Order order)
    {
        var content = new List<ItemType>();
        if (order.Variant != null)
        {
            foreach (var ingredient in order.Variant.Ingredients)
            {
                if (ingredient.item != null && !order.Missing.Contains(ingredient.item))
                    content.Add(ingredient.item);
            }
        }

        if (order.Drink != null) content.Add(order.Drink);
        if (order.IceCream != null) content.Add(order.IceCream);
        if (order.Side != null) content.Add(order.Side);
        return content;
    }

    // unmapped: siparişte olup hiçbir açık kanalda eşleşmesi bulunmayan öğeler (iletilemez; seviye verisi hatası).
    public static int CountSignals(ResolvedLevel.Order order, ResolvedLevel level, List<ItemType> unmapped = null)
    {
        int signals = 0;
        foreach (var item in CollectContent(order))
        {
            if (IsMapped(item, level))
                signals++;
            else
                unmapped?.Add(item);
        }

        // "Sipariş Bitti" jesti (GDD 3.4.1, 3.6.0).
        return signals + 1;
    }

    public static float ComputeSeconds(int signalCount, OrderTimeSettings settings, float levelMultiplier)
    {
        return (settings.BaseSeconds + signalCount * settings.SecondsPerSignal) * levelMultiplier;
    }

    private static bool IsMapped(ItemType item, ResolvedLevel level)
    {
        foreach (var channel in level.Channels)
        {
            // Eşleşme Items[i] <-> Values[i]: değeri olmayan fazla öğe eşleşmiş sayılmaz.
            int index = channel.Items.IndexOf(item);
            if (index >= 0 && index < channel.Values.Count)
                return true;
        }

        return false;
    }
}
