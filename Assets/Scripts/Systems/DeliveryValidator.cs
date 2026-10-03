using System.Collections.Generic;

// Teslim doğrulaması (GDD 5.3.1, 3.4): paketin GERÇEK içeriği müşterinin siparişiyle BİREBİR eşleşmeli — eksik de
// fazla da hatadır. Saf fonksiyon: ağa ve sahneye dokunmaz; YALNIZCA sunucu çağırır (K6), Edit modunda da
// çağrılabilir. Fotoğraf burada kullanılmaz; karar içeriğe göre verilir.
//
// Kural ürün türüne geneldir: sipariş "beklenen ürünler"e, paket "ürünler"e çevrilir ve bire bir eşleştirilir.
//  - Hamburger: malzemeleri (ekmek hariç) = varyantın malzemeleri − eksikler. Sıra önemsiz (GDD 6.7.3: kategori
//    içinde sıra serbest), ADETLER önemli. Fazı olan her malzeme (köfte) "servis edilebilir" fazda olmalı
//    (ProgressPhase.Servable — çiğ/yanık yanlıştır; hangi fazın geçerli olduğu veride).
//  - Diğer ürünler (içecek, dondurma, yan): türü aynı olan bir öğe. İleride doluluk vb. gerekirse yalnızca
//    buradaki karşılaştırma genişler.
public static class DeliveryValidator
{
    public struct Layer
    {
        public ItemType Type;
        public int Phase;

        public Layer(ItemType type, int phase)
        {
            Type = type;
            Phase = phase;
        }
    }

    // Paketteki bir ürün. Layers doluysa hamburgerdir; boşsa (null) düz bir öğedir.
    public class Product
    {
        public ItemType Type;
        public List<Layer> Layers;
    }

    public readonly struct Result
    {
        public readonly bool Correct;
        public readonly string Reason;

        public Result(bool correct, string reason)
        {
            Correct = correct;
            Reason = reason;
        }
    }

    public static Result Validate(ResolvedLevel.Order order, IReadOnlyList<Product> contents)
    {
        var remaining = new List<Product>(contents);

        if (order.Variant != null)
        {
            var expected = new List<ItemType>();
            foreach (var ingredient in order.Variant.Ingredients)
            {
                if (ingredient.item != null)
                    expected.Add(ingredient.item);
            }

            foreach (var missing in order.Missing)
                expected.Remove(missing);

            var burger = remaining.Find(product => product.Layers != null);
            if (burger == null)
                return new Result(false, "pakette hamburger yok");

            remaining.Remove(burger);
            var burgerResult = ValidateBurger(expected, burger);
            if (!burgerResult.Correct)
                return burgerResult;
        }

        foreach (var expectedItem in new[] { order.Drink, order.IceCream, order.Side })
        {
            if (expectedItem == null)
                continue;

            var match = remaining.Find(product => product.Layers == null && product.Type == expectedItem);
            if (match == null)
                return new Result(false, $"pakette '{expectedItem.name}' yok");

            remaining.Remove(match);
        }

        if (remaining.Count > 0)
            return new Result(false, $"pakette fazla öğe var ({remaining.Count} adet; ilki '{Name(remaining[0].Type)}')");

        return new Result(true, "sipariş birebir eşleşti");
    }

    private static Result ValidateBurger(List<ItemType> expected, Product burger)
    {
        var missing = new List<ItemType>(expected);
        foreach (var layer in burger.Layers)
        {
            // Ekmek her hamburgerde sabittir; varyantta ve siparişte yer almaz.
            if (layer.Type == null || layer.Type.Category == ItemCategory.Ekmek)
                continue;

            if (!missing.Remove(layer.Type))
                return new Result(false, $"hamburgerde fazla/yanlış malzeme: '{layer.Type.name}'");

            if (!IsServable(layer))
                return new Result(false, $"'{layer.Type.name}' servis edilebilir fazda değil ({PhaseName(layer)})");
        }

        if (missing.Count > 0)
            return new Result(false, $"hamburgerde eksik malzeme: '{missing[0].name}'" + (missing.Count > 1 ? $" (+{missing.Count - 1})" : ""));

        return new Result(true, null);
    }

    // Fazı olmayan tür (garnitür) her zaman servis edilebilir. Fazı olan türde karar profildedir.
    private static bool IsServable(Layer layer)
    {
        var profile = GetProfile(layer.Type);
        if (profile == null)
            return true;

        return layer.Phase >= 0 && layer.Phase < profile.Phases.Count && profile.Phases[layer.Phase].Servable;
    }

    private static string PhaseName(Layer layer)
    {
        var profile = GetProfile(layer.Type);
        return profile != null && layer.Phase >= 0 && layer.Phase < profile.Phases.Count ? profile.Phases[layer.Phase].Name : $"faz {layer.Phase}";
    }

    private static ProgressProfile GetProfile(ItemType type)
    {
        return type.ItemPrefab != null && type.ItemPrefab.TryGetComponent(out ServerProgress progress) ? progress.Profile : null;
    }

    private static string Name(ItemType type) => type != null ? type.name : "?";
}
