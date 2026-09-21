// Bir ogenin dunyadaki sunumu (Item.Presence). Yalnizca sunucu yazar.
// Su an iki deger var; baska durum (orn. paketin icinde) ilgili adim geldiginde eklenir.
public enum ItemPresence : byte
{
    // Birinin envanterinde: dunya gorseli KAPALI. Elde gorunum yerel HeldItemVisual'dir.
    Carried,

    // Bir yuvada (parent'li): dunya gorseli ACIK.
    Placed
}
