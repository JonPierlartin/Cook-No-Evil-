using System;
using UnityEngine;

// GDD 7.3.4 / K8 "tek ortak veri tipi": her alan ya ELLE girilir ya aralıktan/havuzdan RASTGELE çekilir.
// Mod her alanın kendi üstündedir; alan başına ayrı "sabit mi rastgele mi" bayrağı yazılmaz.
public enum ValueSource
{
    Fixed,
    Random
}

// Sayısal seviye parametresi (GDD 7.3.3): elle değer VEYA min–max aralık. Aralık verildiğinde değer bölüm başında
// çekilir. Tam sayı alanları (müşteri sayısı, yedek havuz, stok, adet) aynı tipi ResolveInt ile okur.
[Serializable]
public struct NumericValue
{
    public ValueSource source;
    [Tooltip("Elle değer (source = Fixed).")]
    public float value;
    [Tooltip("Aralığın alt sınırı (source = Random).")]
    public float min;
    [Tooltip("Aralığın üst sınırı (source = Random). Tam sayı alanlarında dahildir.")]
    public float max;

    public static NumericValue Fixed(float value) => new() { source = ValueSource.Fixed, value = value };

    public static NumericValue Range(float min, float max) => new() { source = ValueSource.Random, min = min, max = max };

    public float ResolveFloat(System.Random rng)
    {
        if (source == ValueSource.Fixed)
            return value;

        float lo = Mathf.Min(min, max), hi = Mathf.Max(min, max);
        return lo + (float)rng.NextDouble() * (hi - lo);
    }

    public int ResolveInt(System.Random rng)
    {
        if (source == ValueSource.Fixed)
            return Mathf.RoundToInt(value);

        int lo = Mathf.RoundToInt(Mathf.Min(min, max)), hi = Mathf.RoundToInt(Mathf.Max(min, max));
        return rng.Next(lo, hi + 1);
    }

    public override string ToString() => source == ValueSource.Fixed ? $"{value:0.##} (elle)" : $"{min:0.##}–{max:0.##} (aralık)";
}
