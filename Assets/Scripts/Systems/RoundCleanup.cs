using UnityEngine;

// Yeniden başlatma temizliği — öğeler (GDD 3.4 bölüm sonu): GameLoopManager.ServerRoundResetting geldiğinde
// dünyadaki TÜM öğeleri (elde, yuvada, ızgarada, paketlerin içinde) ItemMover üzerinden yok eder. Yalnızca
// sunucuda çalışır. Diğer sistemler kendi durumlarını aynı olayla kendileri temizler (müşteriler: CustomerDirector,
// tezgah yığınları: BurgerAssemblyStation, karakterler ve envanterler: PlayerSpawner).
public class RoundCleanup : MonoBehaviour
{
    private GameLoopManager _loop;

    private void Start()
    {
        // Singleton'lara Awake'te erişilmez (CLAUDE.md NGO notu).
        _loop = GameLoopManager.Instance;
        if (_loop != null)
            _loop.ServerRoundResetting += HandleRoundResetting;
    }

    private void OnDestroy()
    {
        if (_loop != null)
            _loop.ServerRoundResetting -= HandleRoundResetting;
    }

    private void HandleRoundResetting()
    {
        int removed = ItemMover.DespawnAll();
        Debug.Log($"[RoundCleanup] Yeniden başlatma: {removed} öğe yok edildi.");
    }
}
