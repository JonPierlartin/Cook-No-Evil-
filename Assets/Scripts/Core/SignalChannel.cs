using System.Collections.Generic;
using UnityEngine;

// İletişim kanalı (GDD 3.6: Yön, Sayı, Renk, Şekil, Vücut Bölgesi) ve değer kataloğu. Kanal sayısı ve kanal başına
// değer sayısı koda gömülmez: yeni kanal veya değer yalnızca asset'tir. Hangi kanalların/değerlerin bir seviyede
// açık olduğunu LevelConfig belirler (GDD 3.6.0, 6.7.5). Animasyon bağlantısı çark adımında eklenir.
[CreateAssetMenu(fileName = "SignalChannel", menuName = "Cook No Evil/Signal Channel")]
public class SignalChannel : ScriptableObject
{
    [Tooltip("Çarkta görünen kategori adı (yerelleştirme çark adımında).")]
    [SerializeField] private string displayName;
    [Tooltip("Bu kanalın tüm değerleri (katalog). Seviyede açık olanları LevelConfig seçer.")]
    [SerializeField] private List<SignalValue> values = new();

    public string DisplayName => displayName;
    public IReadOnlyList<SignalValue> Values => values;
}
