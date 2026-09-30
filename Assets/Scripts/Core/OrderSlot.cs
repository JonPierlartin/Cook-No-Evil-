using System;
using UnityEngine;

// Bir müşterinin siparişinin tanımı (GDD 7.3.1). Her alan KENDİ modunu taşır (Selection): "sabit slot" = tüm alanlar
// Fixed; "rastgele slot" = alanlardan biri veya birkaçı Random. Slotlar birbirinden bağımsızdır. Kategori başına en
// fazla 1 ürün (GDD 6.7) — tek seçimli alanlar count 1 ile kullanılır; "yok" = boş sabit liste veya havuzda boş eleman.
[Serializable]
public class OrderSlot
{
    [Tooltip("Yalnızca seviye tasarımı notu.")]
    public string note;

    [Tooltip("Hamburger varyantı (tek seçim). Yok = bu siparişte hamburger yok.")]
    public Selection<BurgerVariant> variant = new();

    [Tooltip("Eksik malzemeler (GDD 7.3.2): sabit liste VEYA izinli havuz + adet aralığı. Varyantta olmayan malzeme yok sayılır.")]
    public Selection<ItemType> missing = new();

    [Tooltip("'Komple randomize' kısayolu (GDD 7.3.2): rastgele modda havuz yerine varyantın ÇIKARILABİLİR işaretli malzemelerinin tamamı kullanılır.")]
    public bool missingPoolIsVariantRemovables;

    [Tooltip("İçecek (tek seçim). Faz 0'da içecek yok — boş bırakılır.")]
    public Selection<ItemType> drink = new();

    [Tooltip("Dondurma (tek seçim). Faz 0'da yok — boş bırakılır.")]
    public Selection<ItemType> iceCream = new();

    [Tooltip("Patates / Ekstra (tek seçim). Faz 0'da yok — boş bırakılır.")]
    public Selection<ItemType> side = new();
}
