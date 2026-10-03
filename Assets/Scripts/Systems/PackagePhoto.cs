using System.Collections.Generic;

// Paket fotoğrafı (GDD 5.3.1): paketin GERÇEK içeriğinden türetilir — sipariş verisi burada hiç okunmaz; Şef yanlış
// yaptıysa fotoğraf da yanlış görünür. Saf fonksiyonlar: ağa ve sahneye dokunmaz, Edit modunda da çağrılabilir.
//
// Hamburger için kural (varyant listesi aktif LevelConfig'ten gelir):
//  - Malzemeleri bir varyanta eksiksiz uyuyor      -> o varyantın görseli (pop-up ile aynı asset).
//  - Bir varyantın malzemelerinin alt kümesi        -> o varyantın görseli + eksik malzemelerin X'i.
//    Birden fazla varyant uyuyorsa en az eksikli olan seçilir; eşitlikte listedeki ilki (Ambiguous işaretlenir).
//  - Hiçbir varyanta uymuyor (fazla/yabancı malzeme) -> varyant görseli YOK, malzemelerin ikon listesi.
// Ekmek sayılmaz (her hamburgerde sabit; varyant listesinde yer almaz). Pişmişlik fotoğrafa girmez (Faz 0 kararı).
public static class PackagePhoto
{
    public class Entry
    {
        // Hamburger bir varyanta uyduysa dolu.
        public BurgerVariant Variant;
        // Varyanta göre eksik malzemeler (X'li gösterilir).
        public readonly List<ItemType> Missing = new();
        // Varyant yoksa gösterilecek ikonlar: uymayan hamburgerin malzemeleri ya da hamburger olmayan ürünün kendisi.
        public readonly List<ItemType> Loose = new();
        // Aynı sayıda eksikle birden fazla varyant uydu (hangisinin seçildiği liste sırasına bağlı).
        public bool Ambiguous;
    }

    // ingredients: hamburgerin katmanlarındaki türler (ekmek dahil gelebilir; burada ayıklanır).
    public static Entry EvaluateBurger(IReadOnlyList<ItemType> ingredients, IReadOnlyList<BurgerVariant> variants)
    {
        var content = new List<ItemType>();
        foreach (var item in ingredients)
        {
            if (item != null && item.Category != ItemCategory.Ekmek)
                content.Add(item);
        }

        var entry = new Entry();
        int bestMissing = int.MaxValue;
        foreach (var variant in variants)
        {
            if (variant == null || !TryGetMissing(content, variant, out var missing))
                continue;

            if (missing.Count < bestMissing)
            {
                bestMissing = missing.Count;
                entry.Variant = variant;
                entry.Missing.Clear();
                entry.Missing.AddRange(missing);
                entry.Ambiguous = false;
            }
            else if (missing.Count == bestMissing)
            {
                entry.Ambiguous = true;
            }
        }

        if (entry.Variant == null)
            entry.Loose.AddRange(content);

        return entry;
    }

    public static Entry EvaluatePlain(ItemType item)
    {
        var entry = new Entry();
        entry.Loose.Add(item);
        return entry;
    }

    // content, varyantın malzemelerinin (çoklu) alt kümesi mi? Öyleyse eksikleri döndürür. Varyantta olmayan tek
    // bir malzeme bile varsa uymaz.
    private static bool TryGetMissing(List<ItemType> content, BurgerVariant variant, out List<ItemType> missing)
    {
        missing = new List<ItemType>();
        foreach (var ingredient in variant.Ingredients)
        {
            if (ingredient.item != null)
                missing.Add(ingredient.item);
        }

        foreach (var item in content)
        {
            if (!missing.Remove(item))
                return false;
        }

        return true;
    }
}
