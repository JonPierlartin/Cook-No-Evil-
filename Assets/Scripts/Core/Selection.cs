using System;
using System.Collections.Generic;
using UnityEngine;

// GDD 7.3.4 / K8 seçim alanlarının TEK ortak veri tipi: "sabit seçim" VEYA "havuzdan rastgele".
//  - Fixed : fixedItems olduğu gibi (sıra korunur). Boş liste = "yok".
//  - Random: havuzdan, tekrarsız, rastgele sırayla 'count' kadar öğe. Havuzdaki BOŞ (null) eleman "yok"
//            anlamına gelir (içecek/dondurma/yan ürün "yok" çıkabilsin) ve sonuçtan düşülür.
//            takeAll açıksa havuzun tamamı rastgele sırayla gelir — eşleşme karıştırma (GDD 3.6.3) böyle kurulur;
//            havuza öğe eklenince adet güncellemek gerekmez.
// Tek seçimli alan (varyant, içecek) = count 1; çok seçimli alan (eksik malzeme) = count aralığı. Kod öğe sayısı
// varsaymaz.
[Serializable]
public class Selection<T> where T : UnityEngine.Object
{
    public ValueSource source;
    [Tooltip("Sabit seçim (source = Fixed). Boş = yok.")]
    public List<T> fixedItems = new();
    [Tooltip("Rastgele havuz (source = Random). Boş (None) eleman = 'yok' seçeneği.")]
    public List<T> pool = new();
    [Tooltip("Rastgele modda kaç öğe çekileceği (elle veya aralık). takeAll açıksa yok sayılır.")]
    public NumericValue count = NumericValue.Fixed(1);
    [Tooltip("Rastgele modda havuzun TAMAMI rastgele sırayla gelir (eşleşme karıştırma).")]
    public bool takeAll;

    // poolOverride: rastgele modda havuz yerine kullanılacak aday listesi (ör. varyantın çıkarılabilir
    // malzemeleri — 'komple randomize' kısayolu). null ise pool kullanılır.
    public List<T> Resolve(System.Random rng, IReadOnlyList<T> poolOverride = null)
    {
        var result = new List<T>();
        if (source == ValueSource.Fixed)
        {
            foreach (var item in fixedItems)
            {
                if (item != null)
                    result.Add(item);
            }

            return result;
        }

        var candidates = new List<T>(poolOverride ?? pool);
        int take = takeAll ? candidates.Count : Mathf.Clamp(count.ResolveInt(rng), 0, candidates.Count);

        // Kısmi Fisher–Yates: ilk 'take' eleman tekrarsız ve rastgele sıralı.
        for (int i = 0; i < take; i++)
        {
            int j = rng.Next(i, candidates.Count);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
            if (candidates[i] != null)
                result.Add(candidates[i]);
        }

        return result;
    }

    public override string ToString()
    {
        return source == ValueSource.Fixed
            ? $"sabit [{Join(fixedItems)}]"
            : $"rastgele havuz [{Join(pool)}] adet {(takeAll ? "tümü" : count.ToString())}";
    }

    private static string Join(List<T> items)
    {
        var names = new List<string>();
        foreach (var item in items)
            names.Add(item != null ? item.name : "yok");
        return string.Join(", ", names);
    }
}
