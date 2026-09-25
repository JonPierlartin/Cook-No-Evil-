using System;
using Unity.Netcode;

// Birleştirme tezgahındaki yığının bir katmanı (BurgerAssemblyStation.PlacedIngredients). Katman için
// ağ nesnesi spawn edilmez (hamburger tek öğe olacak — CLAUDE.md hedef mimari); yığın bu replike
// listeden her istemcide yerel olarak çizilir. TypeId görseli, PhaseIndex pişmişlik rengini belirler
// (köfte yığında çiğ koyulduysa çiğ görünür — Komi bunu okuyabilmeli).
public struct BurgerLayerEntry : INetworkSerializeByMemcpy, IEquatable<BurgerLayerEntry>
{
    public int TypeId;
    public int PhaseIndex;

    public BurgerLayerEntry(int typeId, int phaseIndex)
    {
        TypeId = typeId;
        PhaseIndex = phaseIndex;
    }

    public readonly bool Equals(BurgerLayerEntry other) => TypeId == other.TypeId && PhaseIndex == other.PhaseIndex;

    public override readonly bool Equals(object obj) => obj is BurgerLayerEntry other && Equals(other);

    public override readonly int GetHashCode() => HashCode.Combine(TypeId, PhaseIndex);
}
