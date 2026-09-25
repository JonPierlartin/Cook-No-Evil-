// Görseli ItemType.visualPrefab'ından DEĞİL, öğenin kendi (replike) durumundan üretilen öğeler için
// (yarılanmış ekmek, katman listesinden çizilen hamburger). Elde tutulan görsel (HeldItemVisual) ve
// yerleştirme önizlemesi (PlacementPreview) bu arayüz varsa visualPrefab yerine bunu kullanır — böylece
// elde, yuvada ve önizlemede AYNI hâl görünür (GDD 4.1.2 ②). Üretim tamamen yerel; ağdan bir şey gitmez.
public interface IItemVisualSource
{
    // Öğenin o anki görselini üretir; sahipliği çağırana geçer (yok etmek çağıranın işi). parent null
    // olabilir (önizleme kopyası sahne köküne konur). Kök tabanda olmalı (K2d).
    UnityEngine.GameObject CreateVisual(UnityEngine.Transform parent);

    // Görsel durumu her değiştiğinde artar; önizleme gibi tüketiciler yeniden üretmek gerekip
    // gerekmediğini bununla anlar.
    int VisualVersion { get; }

    // Görsel durumu değişince tetiklenir (olay-sürümlü tüketiciler için).
    event System.Action VisualChanged;
}
