using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// GDD 5.2.1 / 5.2.1.1 ızgara: yuvalarındaki öğelerin ilerlemesini SUNUCU işletir (K5, K6). İlerleme
// mantığının kendisi (fazlar, süreler, terminal faz) burada DEĞİL, öğenin üstündeki ServerProgress'te
// ve profil asset'inde durur — ızgara yalnızca "yuvada duran öğeyi ilerlet" der (ServerAdvance).
// Bu yüzden ilerleme öğeyle birlikte taşınır: yarıda alınıp başka yuvaya (veya elde bekleyip geri)
// konan köfte kaldığı yerden devam eder; ızgara hiçbir öğe için durum tutmaz.
//
// Yuva sayısı koda gömülü DEĞİL: Inspector'daki referans dizisinden gelir. Yuvalar ItemSlot'tur
// (4.2 deseni; kendi NetworkObject'leri, sahne kökünde) — izinli rol ve kabul edilen tür yuva
// üzerinde ayarlanır, ızgara bunlara bakmaz. Faz 0'da yanmış öğe için özel kural yok (yangın yok).
//
// Görsel geri bildirim: renk faza göre ItemPhaseVisual/HeldItemVisual tarafından değişir. İşitsel: ızgarada
// pişen öğe varken sabit bir cızırtı çalar (GrillSizzle, GDD 10.5) — faz değişiminde ses DEĞİŞMEZ (Şef
// bir şeyin piştiğini duyar, pişmişliği duymaz, GDD 4.1.1).
[RequireComponent(typeof(NetworkObject))]
public class Grill : NetworkBehaviour
{
    [Tooltip("Bu ızgaranın yuvaları. Sayı ve düzen tamamen buradan gelir.")]
    [SerializeField] private ItemSlot[] slots;

    // ServerProgress taşımayan bir öğe için uyarı öğe başına BİR kez basılır (NetworkObjectId hiç
    // yeniden kullanılmaz).
    private readonly HashSet<ulong> _warnedItems = new();

    // Izgarada şu an pişen bir öğe var mı. Sunucunun pişirmesi (Update) ve istemcinin cızırtısı (GrillSizzle)
    // AYNI kuraldan okur: yuva doluluğu parent ilişkisinden, round/duraklatma durumu replike
    // NetworkVariable'lardan geldiği için her makinede yerel olarak hesaplanabilir.
    public bool IsCooking
    {
        get
        {
            if (!IsCookingAllowed || slots == null)
                return false;

            foreach (var slot in slots)
            {
                if (TryGetCookable(slot, out _, out _))
                    return true;
            }

            return false;
        }
    }

    // Round dışında ve oyun duraklatılmışken (bir oyuncu koptu) pişme DURUR — GDD 8.2; aksi halde kopan
    // oyuncunun dönüşünü beklerken et yanardı.
    private static bool IsCookingAllowed
    {
        get
        {
            var loop = GameLoopManager.Instance;
            return loop != null && loop.IsRoundActive && !loop.IsGamePaused;
        }
    }

    // Yuvada ServerProgress taşıyan bir öğe var mı. item: yuvadaki öğe (ilerlemesi olmasa da), uyarı için.
    private static bool TryGetCookable(ItemSlot slot, out Item item, out ServerProgress progress)
    {
        progress = null;
        item = null;
        return slot != null && slot.TryGetOccupant(out item) && item.TryGetComponent(out progress);
    }

    private void Update()
    {
        if (!IsServer || slots == null || !IsCookingAllowed)
            return;

        float deltaTime = Time.deltaTime;
        foreach (var slot in slots)
        {
            if (TryGetCookable(slot, out var item, out var progress))
            {
                progress.ServerAdvance(deltaTime);
                continue;
            }

            if (item != null && _warnedItems.Add(item.NetworkObjectId))
                Debug.LogWarning($"[Grill] '{name}': yuvadaki '{item.name}' öğesinde ServerProgress yok, pişirilemiyor.", item);
        }
    }
}
