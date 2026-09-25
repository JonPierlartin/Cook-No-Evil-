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
// Ses/görsel geri bildirim EKLENMEZ: renk zaten faza göre ItemPhaseVisual/HeldItemVisual tarafından
// değişiyor; faz değişiminde ses çıkmaz (Şef pişmişliği duymamalı, GDD 4.1.1).
[RequireComponent(typeof(NetworkObject))]
public class Grill : NetworkBehaviour
{
    [Tooltip("Bu ızgaranın yuvaları. Sayı ve düzen tamamen buradan gelir.")]
    [SerializeField] private ItemSlot[] slots;

    // ServerProgress taşımayan bir öğe için uyarı öğe başına BİR kez basılır (NetworkObjectId hiç
    // yeniden kullanılmaz).
    private readonly HashSet<ulong> _warnedItems = new();

    private void Update()
    {
        if (!IsServer || slots == null)
            return;

        // Round dışında ve oyun duraklatılmışken (bir oyuncu koptu) pişme DURUR — GDD 8.2; aksi halde
        // kopan oyuncunun dönüşünü beklerken et yanardı.
        var loop = GameLoopManager.Instance;
        if (loop == null || !loop.IsRoundActive || loop.IsGamePaused)
            return;

        float deltaTime = Time.deltaTime;
        foreach (var slot in slots)
        {
            if (slot == null || !slot.TryGetOccupant(out var item))
                continue;

            if (!item.TryGetComponent(out ServerProgress progress))
            {
                if (_warnedItems.Add(item.NetworkObjectId))
                    Debug.LogWarning($"[Grill] '{name}': yuvadaki '{item.name}' öğesinde ServerProgress yok, pişirilemiyor.", item);

                continue;
            }

            progress.ServerAdvance(deltaTime);
        }
    }
}
