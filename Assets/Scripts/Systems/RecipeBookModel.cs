using System.Collections.Generic;

// Tarif kitapçığının içeriği (GDD 3.6.2): YALNIZCA bu bölümde açık olan varyantlardan üretilir. Kodda sayfa sayısı,
// varyant listesi ya da kategori listesi yoktur — varyant eklenince/çıkarılınca kitap kendini yeniden üretir.
//  - İlk açılım içindekilerdir: varyantların kategorileri.
//  - Her varyant bir açılımdır; aynı kategorinin varyantları art arda gelir.
// Kategori ayrı bir listeden gelmez: varyantın VERİSİNDEN okunur — varyantın proteinidir (Et / Tavuk / Balık /
// Veji burgerleri, GDD 3.6.2). Yeni bir protein türü eklenince yeni kategori kendiliğinden oluşur.
// Saf fonksiyon: sahneye ve ağa dokunmaz, Edit modunda da çağrılabilir.
public static class RecipeBookModel
{
    public class Category
    {
        // Kategoriyi tanımlayan protein (ikonu ve adı gösterilir). Proteini olmayan varyantlar için null.
        public ItemType Protein;
        // Bu kategorinin ilk açılımının dizini (Book.Spreads içinde).
        public int FirstSpread;
    }

    public class Book
    {
        // Spreads[0] içindekilerdir (null); sonrakiler varyant açılımlarıdır.
        public readonly List<BurgerVariant> Spreads = new();
        public readonly List<Category> Categories = new();
    }

    public static Book Build(IReadOnlyList<BurgerVariant> openVariants)
    {
        // Kategoriler ilk göründükleri sırayla; varyantlar kategorisinin içinde verideki sırayla.
        var order = new List<ItemType>();
        var groups = new List<List<BurgerVariant>>();
        foreach (var variant in openVariants)
        {
            if (variant == null)
                continue;

            var protein = FindProtein(variant);
            int index = order.IndexOf(protein);
            if (index < 0)
            {
                order.Add(protein);
                groups.Add(new List<BurgerVariant>());
                index = order.Count - 1;
            }

            if (!groups[index].Contains(variant))
                groups[index].Add(variant);
        }

        var book = new Book();
        book.Spreads.Add(null);
        for (int i = 0; i < order.Count; i++)
        {
            book.Categories.Add(new Category { Protein = order[i], FirstSpread = book.Spreads.Count });
            book.Spreads.AddRange(groups[i]);
        }

        return book;
    }

    private static ItemType FindProtein(BurgerVariant variant)
    {
        foreach (var ingredient in variant.Ingredients)
        {
            if (ingredient.item != null && ingredient.item.Category == ItemCategory.Protein)
                return ingredient.item;
        }

        return null;
    }
}
