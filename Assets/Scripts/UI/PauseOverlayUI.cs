using UnityEngine;

// GDD 8.2: round sırasında bir oyuncu koparsa oyun durur ve ekranda "DURDURULDU" yazar. Duraklatma sunucu
// sahiplidir (GameLoopManager.IsGamePaused, replike); bu bileşen yalnızca GÖSTERİR, karar vermez.
// ESC menüsüyle ilgisi yoktur (K9) — o yereldir ve oyunu durdurmaz.
public class PauseOverlayUI : MonoBehaviour
{
    [Tooltip("Oyun duraklatılınca açılan katman (\"DURDURULDU\" yazısı).")]
    [SerializeField] private GameObject overlay;

    private void Update()
    {
        bool paused = GameLoopManager.Instance != null && GameLoopManager.Instance.IsGamePaused;
        if (overlay.activeSelf != paused)
            overlay.SetActive(paused);
    }
}
