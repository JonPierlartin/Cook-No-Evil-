using System;
using Unity.Netcode;

// Envanter slotunun ag kaydi (PlayerInventory.Slots). Slot bir ogenin NetworkObject kimligini tutar.
// Kimlik +1 KAYDIRILMIS saklanir, 0 = bos slot: default(NetworkObjectReference) bos DEGILDIR, id 0'i
// cozer (NGO kaynagindan dogrulandi) — bu yuzden bos slot icin ayri bir sifir degeri gerekir.
// Ogenin kendisi dunyada bir NetworkObject'tir; buradan agdan yalnizca kimligi gider.
public struct ItemSlotEntry : INetworkSerializeByMemcpy, IEquatable<ItemSlotEntry>
{
    public ulong ItemIdPlusOne;

    public readonly bool IsEmpty => ItemIdPlusOne == 0;

    // Yalnizca IsEmpty degilken anlamlidir.
    public readonly ulong NetworkObjectId => ItemIdPlusOne - 1;

    public static ItemSlotEntry For(NetworkObject item) => new() { ItemIdPlusOne = item.NetworkObjectId + 1 };

    public readonly bool Equals(ItemSlotEntry other) => ItemIdPlusOne == other.ItemIdPlusOne;

    public override readonly bool Equals(object obj) => obj is ItemSlotEntry other && Equals(other);

    public override readonly int GetHashCode() => ItemIdPlusOne.GetHashCode();
}
