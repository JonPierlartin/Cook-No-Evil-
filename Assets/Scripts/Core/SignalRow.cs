using System;
using Unity.Netcode;

// Çözülmüş seviyenin istemcilere replike edilen sinyal satırı (LevelDirector.SignalRows): bu bölümde AÇIK olan bir
// kanal değeri ve onunla eşleşen öğe. Satır sırası eşleşme sırasıdır (GDD 3.6.3: duvar panosu da bu sırayı çizer).
// Kanal ve değer, herkeste aynı olan aktif LevelConfig'in listelerindeki dizinlerle taşınır; asset referansı ağdan
// gitmez. Kapalı kanalların satırı yoktur — çark ve sunucu doğrulaması yalnızca bu listeye bakar.
public struct SignalRow : INetworkSerializeByMemcpy, IEquatable<SignalRow>
{
    public const int NoItem = -1;

    // LevelConfig.Channels içindeki dizin.
    public int ChannelIndex;
    // O kanalın LevelConfig'teki 'values' listesindeki dizin.
    public int ValueIndex;
    // Bu değerle eşleşen öğenin ItemType.Id'si; eşleşen öğe yoksa NoItem.
    public int ItemId;

    public SignalRow(int channelIndex, int valueIndex, int itemId)
    {
        ChannelIndex = channelIndex;
        ValueIndex = valueIndex;
        ItemId = itemId;
    }

    public readonly bool Equals(SignalRow other) =>
        ChannelIndex == other.ChannelIndex && ValueIndex == other.ValueIndex && ItemId == other.ItemId;

    public override readonly bool Equals(object obj) => obj is SignalRow other && Equals(other);

    public override readonly int GetHashCode() => HashCode.Combine(ChannelIndex, ValueIndex, ItemId);
}
