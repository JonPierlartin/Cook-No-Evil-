using Unity.Netcode;
using UnityEngine;

// Aktif seviyenin TEK seçim noktası (Inspector) ve bölüm başındaki çözümleme. Round RoundActive'e geçtiğinde
// YALNIZCA SUNUCUDA, bir kez, LevelConfig somut değerlere çözülür (LevelResolver) ve Current'ta tutulur; konsola
// okunur biçimde yazılır (geri bildirim). Tüketiciler (müşteri, pano, çark) sonraki adımlarda Current'ı okur.
// Replikasyon bu adımda yok. GameSystems üzerinde durur.
public class LevelDirector : MonoBehaviour
{
    public static LevelDirector Instance { get; private set; }

    [Tooltip("Bu sahnede oynanan seviye. Tek kaynak (K8).")]
    [SerializeField] private LevelConfig levelConfig;

    // Yalnızca sunucuda dolu; round başında üretilir.
    public ResolvedLevel Current { get; private set; }

    public LevelConfig Config => levelConfig;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Singleton'lara Awake'te erişilmez (CLAUDE.md NGO notu).
        if (GameLoopManager.Instance != null)
            GameLoopManager.Instance.CurrentRoundState.OnValueChanged += HandleRoundStateChanged;
        else
            Debug.LogError("[LevelDirector] GameLoopManager.Instance bulunamadı.");
    }

    private void OnDestroy()
    {
        if (GameLoopManager.Instance != null)
            GameLoopManager.Instance.CurrentRoundState.OnValueChanged -= HandleRoundStateChanged;

        if (Instance == this)
            Instance = null;
    }

    private void HandleRoundStateChanged(RoundState previous, RoundState current)
    {
        var manager = NetworkManager.Singleton;
        if (current != RoundState.RoundActive || manager == null || !manager.IsServer)
            return;

        if (levelConfig == null)
        {
            Debug.LogError("[LevelDirector] LevelConfig atanmamış; seviye çözülemedi.");
            return;
        }

        int seed = levelConfig.UseFixedSeed ? levelConfig.Seed : new System.Random().Next();
        Current = LevelResolver.Resolve(levelConfig, seed);
        Debug.Log(Current.Describe(levelConfig.name));
    }
}
