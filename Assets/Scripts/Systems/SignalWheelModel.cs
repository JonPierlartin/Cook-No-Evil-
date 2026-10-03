using System.Collections.Generic;
using UnityEngine;

// Sinyal çarkının içeriği (GDD 3.6.0): replike satırlardan (LevelDirector.SignalRows) üretilen iç içe seçenek ağacı.
// Kodda kategori veya değer listesi YOKTUR — N kategori × M değer tamamen veriden gelir; kapalı kanalın satırı
// olmadığı için çarkta da yoktur. Kategorilere ek olarak tek bir yaprak: "Sipariş Bitti". Saf fonksiyon: sahneye ve
// ağa dokunmaz, Edit modunda da çağrılabilir. İç içe yapı genel emote çarkı (E) için de kullanılabilir.
public static class SignalWheelModel
{
    // "Sipariş Bitti" bir kanala ait değildir; sunucuya bu kanal diziniyle gider.
    public const int OrderDoneChannel = -1;

    public class Option
    {
        public string Label;
        public Sprite Icon;
        public int ChannelIndex;
        public int ValueIndex;
        // Doluysa bu bir kategoridir (seçilince içine girilir); boşsa yapraktır (seçilince sinyal gönderilir).
        public List<Option> Children;

        public bool IsLeaf => Children == null;
    }

    public static List<Option> Build(LevelConfig config, IReadOnlyList<SignalRow> rows, SignalValue orderDone)
    {
        var top = new List<Option>();
        var byChannel = new Dictionary<int, Option>();
        if (config != null)
        {
            foreach (var row in rows)
            {
                if (row.ChannelIndex < 0 || row.ChannelIndex >= config.Channels.Count)
                    continue;

                var channelConfig = config.Channels[row.ChannelIndex];
                if (channelConfig.channel == null || row.ValueIndex < 0 || row.ValueIndex >= channelConfig.values.Count)
                    continue;

                var value = channelConfig.values[row.ValueIndex];
                if (value == null)
                    continue;

                if (!byChannel.TryGetValue(row.ChannelIndex, out var category))
                {
                    category = new Option
                    {
                        Label = channelConfig.channel.DisplayName,
                        ChannelIndex = row.ChannelIndex,
                        Children = new List<Option>()
                    };
                    byChannel.Add(row.ChannelIndex, category);
                    top.Add(category);
                }

                category.Children.Add(new Option
                {
                    Label = value.DisplayName,
                    Icon = value.Icon,
                    ChannelIndex = row.ChannelIndex,
                    ValueIndex = row.ValueIndex
                });
            }
        }

        if (orderDone != null)
            top.Add(new Option { Label = orderDone.DisplayName, Icon = orderDone.Icon, ChannelIndex = OrderDoneChannel });

        return top;
    }
}
