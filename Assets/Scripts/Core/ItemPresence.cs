// Bir ogenin dunyadaki sunumu (Item.Presence). Yalnizca sunucu yazar.
public enum ItemPresence : byte
{
    // Birinin envanterinde: dunya gorseli KAPALI. Elde gorunum yerel HeldItemVisual'dir.
    Carried,

    // Bir yuvada (parent'li): dunya gorseli ACIK.
    Placed,

    // Bir paketin icinde (pakete parent'li): dunya gorseli KAPALI. Oge kendi durumunu tasimaya devam eder.
    Contained
}
