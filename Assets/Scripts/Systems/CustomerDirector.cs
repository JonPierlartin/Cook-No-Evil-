using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Müşteri akışı (GDD 3.4.2, 3.4.4): bölümün müşterilerini YALNIZCA SUNUCUDA sırayla içeri alır. Kaç müşteri, kaçar
// saniye arayla, hangi sabırla ve kaç yedekle geleceği çözümlenmiş seviyeden (ResolvedLevel) gelir; burada hiçbiri
// yazılı değildir. Tek sabit, GDD'nin "global sabit" dediği eşzamanlı üst sınırdır (maxConcurrentCustomers).
// Round geri sayımı YOKTUR (K1): bölüm, müşteriler bitince biter. Ağ durumu taşımaz — müşteriler kendi ağ
// nesneleridir, hata sayacı GameLoopManager'dadır.
public class CustomerDirector : MonoBehaviour
{
    public static CustomerDirector Instance { get; private set; }

    [SerializeField] private Customer customerPrefab;
    [Tooltip("Sipariş süresi formülünün taban ve katsayısı (GDD 3.4.1). Seviye çarpanı LevelConfig'ten gelir.")]
    [SerializeField] private OrderTimeSettings orderTimeSettings;
    [Tooltip("Restoranda aynı anda bulunabilecek en fazla müşteri. GDD 3.4.2: global sabit (3) — seviye parametresi DEĞİL.")]
    [SerializeField, Min(1)] private int maxConcurrentCustomers = 3;

    [Header("Noktalar (sahne)")]
    [Tooltip("Müşterinin doğduğu ve ayrılırken yürüdüğü nokta.")]
    [SerializeField] private Transform entrancePoint;
    [Tooltip("Sipariş Penceresi'ndeki SIRA: ilk eleman pencerenin önü (siparişi alınan yer), sonrakiler arka arkaya. En az eşzamanlı sınır kadar olmalı.")]
    [SerializeField] private Transform[] orderSpots;
    [Tooltip("Teslim Penceresi'ndeki yerler (yan yana; kuyruk değil — GDD 5.3). En az eşzamanlı sınır kadar olmalı.")]
    [SerializeField] private Transform[] deliverySpots;
    [Tooltip("Sipariş penceresinden teslim penceresine yürürken sırayla geçilen noktalar (duvarın DIŞINDAN dolaşmak için).")]
    [SerializeField] private Transform[] orderToDeliveryWaypoints;

    // Yalnızca sunucuda: bölümün tüm müşterileri (yedekler dahil) teslim alıp veya ayrılıp bitti.
    public event Action ServerAllCustomersFinished;
    public bool AllCustomersFinished { get; private set; }

    private readonly List<Customer> _active = new();
    // Sipariş sırası (Ersel, 3 Eki 2026): müşteriler küçük pencerede ARKA ARKAYA dizilir; yalnızca baştakinin
    // siparişi alınır. Sipariş alınınca ya da sabır dolunca sıra bir öne kayar.
    private readonly List<Customer> _queue = new();
    private Customer[] _deliveryOccupants;
    private ResolvedLevel _level;
    private LevelDirector _levelDirector;
    private int _nextOrderIndex;
    private int _backupsUsed;
    private int _backupsPending;
    private float _arrivalTimer;
    private int _spawnedCount;

    private static bool IsServer => NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;

    private void Awake()
    {
        Instance = this;
        _deliveryOccupants = new Customer[deliverySpots.Length];
    }

    private void Start()
    {
        // Singleton'lara Awake'te erişilmez (CLAUDE.md NGO notu).
        _levelDirector = LevelDirector.Instance;
        if (_levelDirector != null)
            _levelDirector.ServerLevelResolved += HandleLevelResolved;
        else
            Debug.LogError("[CustomerDirector] LevelDirector bulunamadı; müşteri gelmez.");

        if (orderSpots.Length < maxConcurrentCustomers || deliverySpots.Length < maxConcurrentCustomers)
            Debug.LogError($"[CustomerDirector] Sipariş ({orderSpots.Length}) ve teslim ({deliverySpots.Length}) yerleri eşzamanlı sınırdan ({maxConcurrentCustomers}) az olamaz.");
    }

    private void OnDestroy()
    {
        if (_levelDirector != null)
            _levelDirector.ServerLevelResolved -= HandleLevelResolved;

        if (Instance == this)
            Instance = null;
    }

    // Round başında (seviye çözülünce) sunucuda bir kez.
    private void HandleLevelResolved(ResolvedLevel level)
    {
        ClearAll();
        _level = level;
        _nextOrderIndex = 0;
        _backupsUsed = 0;
        _backupsPending = 0;
        _spawnedCount = 0;
        AllCustomersFinished = false;
        // İlk müşteri hazırlık fazından sonra gelir (GDD 3.4.3); faz kapalıysa hemen.
        _arrivalTimer = level.PrepPhaseEnabled ? level.PrepPhaseSeconds : 0f;
        Debug.Log($"[Müşteri] Akış başladı: {level.Orders.Count} müşteri, {level.BackupOrders.Count} yedek, aynı anda en fazla {maxConcurrentCustomers}. İlk müşteri {_arrivalTimer:0.#} sn sonra.");
    }

    private void Update()
    {
        if (_level == null || !IsServer)
            return;

        // Round bitti / oturum kapandı: akış durur, kalan müşteriler kaldırılır.
        if (GameLoopManager.Instance == null || !GameLoopManager.Instance.IsRoundActive)
        {
            ClearAll();
            _level = null;
            return;
        }

        // Duraklatmada (kopma) sayaç işlemez.
        if (!GameLoopManager.CanPlayersAct)
            return;

        _arrivalTimer -= Time.deltaTime;

        if (!HasRoom())
            return;

        // Yedek müşteri (GDD 3.4.4): sabır hatasından sonra, yer olur olmaz gelir; aralık beklemez.
        if (_backupsPending > 0)
        {
            _backupsPending--;
            Spawn(_level.BackupOrders[_backupsUsed++], true);
            return;
        }

        if (_nextOrderIndex >= _level.Orders.Count || _arrivalTimer > 0f)
            return;

        Spawn(_level.Orders[_nextOrderIndex++], false);

        // Sonraki müşteri, bu geldikten "aralık" kadar sonra (temel aralık × kısalma eğrisi). Sınır doluysa yer
        // açılana kadar bekler (GDD 3.4.2).
        if (_nextOrderIndex < _level.Orders.Count)
            _arrivalTimer = _level.Orders[_nextOrderIndex].IntervalBefore * IntervalMultiplier(_nextOrderIndex);
    }

    private float IntervalMultiplier(int orderIndex)
    {
        var config = _levelDirector.Config;
        if (config == null || config.IntervalCurve == null || _level.Orders.Count < 2)
            return 1f;

        return Mathf.Max(0f, config.IntervalCurve.Evaluate(orderIndex / (float)(_level.Orders.Count - 1)));
    }

    private bool HasRoom() => _active.Count < maxConcurrentCustomers && _queue.Count < orderSpots.Length;

    // Sıradaki herkesi kendi yerine yürütür; başa geçen müşteri pencerenin önüne varınca sabrı başlar.
    private void RefreshQueue()
    {
        for (int i = 0; i < _queue.Count; i++)
        {
            _queue[i].ServerMoveTo(orderSpots[i].position, orderSpots[i].rotation, arrived =>
            {
                if (_queue.Count > 0 && _queue[0] == arrived)
                    arrived.ServerStartWaiting();
            });
        }
    }

    private void Spawn(ResolvedLevel.Order order, bool isBackup)
    {
        var customer = Instantiate(customerPrefab, entrancePoint.position, entrancePoint.rotation);
        customer.NetworkObject.Spawn();
        _spawnedCount++;
        customer.ServerInitialize(order, isBackup, isBackup ? $"Yedek müşteri {_backupsUsed}" : $"Müşteri {_nextOrderIndex}");
        customer.ServerOrderTaken += HandleOrderTaken;
        customer.ServerPatienceExpired += HandlePatienceExpired;
        customer.ServerOrderTimeExpired += HandleOrderTimeExpired;
        customer.ServerDeliveryRequested += HandleDeliveryRequested;

        _active.Add(customer);
        _queue.Add(customer);
        RefreshQueue();
        Debug.Log($"[Müşteri] {customer.Label} geldi (sabır {order.Patience:0.#} sn). Restoranda {_active.Count}/{maxConcurrentCustomers}.");
    }

    private void HandleOrderTaken(Customer customer, ulong clientId)
    {
        _queue.Remove(customer);
        RefreshQueue();
        int spot = Array.IndexOf(_deliveryOccupants, null);
        if (spot < 0)
        {
            // Yer sayısı sınırdan az olamayacağı için (Start'ta denetlenir) buraya düşülmemeli.
            Debug.LogError($"[CustomerDirector] {customer.Label} için teslim penceresinde boş yer yok.");
            return;
        }

        _deliveryOccupants[spot] = customer;
        customer.ServerMoveTo(deliverySpots[spot].position, deliverySpots[spot].rotation, null, orderToDeliveryWaypoints);

        // Sipariş süresi (GDD 3.4.1): sinyal sayısı siparişin içeriğinden, taban/katsayı SO'dan, çarpan seviyeden.
        var unmapped = new List<ItemType>();
        int signals = OrderTimeCalculator.CountSignals(customer.Order, _level, unmapped);
        float seconds = OrderTimeCalculator.ComputeSeconds(signals, orderTimeSettings, _level.TimeMultiplier);
        customer.ServerStartOrderTimer(seconds);

        foreach (var item in unmapped)
            Debug.LogWarning($"[Müşteri] {customer.Label} siparişindeki '{item.name}' hiçbir açık kanalda eşleşmiyor; Kasiyer bunu iletemez (seviye verisini kontrol et).");
        Debug.Log($"[Müşteri] {customer.Label} siparişi alındı (client {clientId}); teslim penceresinde {spot + 1}. yere geçiyor. " +
            $"Sipariş: {DescribeOrder(customer.Order)} · {signals} sinyal · süre {seconds:0.#} sn.");
    }

    // GDD 3.4.4: sipariş süresi dolarsa 1 Hata, müşteri ayrılır; yedek havuz TETİKLENMEZ (yalnızca sabır hatasında).
    private void HandleOrderTimeExpired(Customer customer)
    {
        Release(_deliveryOccupants, customer);
        GameLoopManager.Instance.ServerAddError($"{customer.Label} siparişi süresinde teslim edilmedi");
        Leave(customer, CustomerMood.Angry);
    }

    private static string DescribeOrder(ResolvedLevel.Order order)
    {
        if (order.Variant == null)
            return "hamburger yok";

        var missing = new List<string>();
        foreach (var item in order.Missing)
            missing.Add(item.name);
        return order.Variant.name + (missing.Count > 0 ? " (eksik: " + string.Join(", ", missing) + ")" : " (tam)");
    }

    // GDD 3.4.4: sabır dolarsa 1 Hata, müşteri ayrılır, yedek havuzdan (varsa) yenisi gelir.
    private void HandlePatienceExpired(Customer customer)
    {
        _queue.Remove(customer);
        RefreshQueue();
        GameLoopManager.Instance.ServerAddError($"{customer.Label} sabrı doldu (siparişi alınmadı)");

        if (_backupsUsed + _backupsPending < _level.BackupOrders.Count)
        {
            _backupsPending++;
            Debug.Log($"[Müşteri] Yedek havuzdan bir müşteri gelecek (kalan yedek: {_level.BackupOrders.Count - _backupsUsed - _backupsPending}).");
        }
        else
        {
            Debug.Log("[Müşteri] Yedek havuz boş; yerine müşteri gelmeyecek.");
        }

        Leave(customer, CustomerMood.Angry);
    }

    // Teslim (GDD 5.3.1, 3.4): oyuncu elindeki paketi bu müşteriye verdi. Doğrulama YALNIZCA burada, sunucuda
    // yapılır (K6) — istemci yalnızca "bu müşteriye, şu slottaki paketi" niyetini gönderdi. Paketin gerçek içeriği
    // siparişle birebir karşılaştırılır. Doğru: müşteri memnun ayrılır. Yanlış: 1 Hata, müşteri ayrılır, yedek havuz
    // TETİKLENMEZ. Her iki durumda paket ve içindekiler yok edilir.
    private void HandleDeliveryRequested(Customer customer, InteractionContext context)
    {
        var inventory = PlayerInventory.FindForClient(context.ClientId);
        if (inventory == null || !inventory.TryGetItem(context.SlotIndex, out var held) || !held.TryGetComponent(out Package package))
        {
            Debug.LogWarning($"[Müşteri] {customer.Label} teslim reddedildi (client {context.ClientId}): elde paket yok.");
            return;
        }

        var result = DeliveryValidator.Validate(customer.Order, package.BuildProducts());

        if (inventory.ServerTryTakeItemAt(context.SlotIndex, out var taken))
            ItemMover.Despawn(taken);

        Release(_deliveryOccupants, customer);
        if (result.Correct)
        {
            Debug.Log($"[Müşteri] {customer.Label} DOĞRU teslim aldı (client {context.ClientId}); memnun ayrılıyor.");
        }
        else
        {
            GameLoopManager.Instance.ServerAddError($"{customer.Label} yanlış teslim: {result.Reason}");
        }

        Leave(customer, result.Correct ? CustomerMood.Happy : CustomerMood.Angry);
    }

    // Müşteri ayrılmaya başladığı anda restoranda yer açılır (GDD 3.4.2); kapıya varınca yok edilir.
    private void Leave(Customer customer, CustomerMood mood)
    {
        _active.Remove(customer);
        customer.ServerOrderTaken -= HandleOrderTaken;
        customer.ServerPatienceExpired -= HandlePatienceExpired;
        customer.ServerOrderTimeExpired -= HandleOrderTimeExpired;
        customer.ServerDeliveryRequested -= HandleDeliveryRequested;
        customer.ServerStartLeaving(mood);
        customer.ServerMoveTo(entrancePoint.position, entrancePoint.rotation, gone =>
        {
            if (gone != null && gone.IsSpawned)
                gone.NetworkObject.Despawn();
        });

        CheckFinished();
    }

    // Bölüm sonu altyapısı (GDD 3.4): gelecek müşteri kalmadı ve restoran boş. Kazan/kaybet kararı ayrı adımda.
    private void CheckFinished()
    {
        if (AllCustomersFinished || _active.Count > 0 || _backupsPending > 0 || _nextOrderIndex < _level.Orders.Count)
            return;

        AllCustomersFinished = true;
        // GDD 3.4: tüm müşteriler bitti ve hata eşiği aşılmadıysa seviye kazanılır (eşik aşıldıysa round zaten bitti;
        // GameLoopManager ilk sonucu korur).
        GameLoopManager.Instance.ServerEndRound(RoundOutcome.Won);
        Debug.Log($"[Müşteri] Bölümün tüm müşterileri bitti ({_spawnedCount} müşteri geldi, {_backupsUsed} yedek kullanıldı, hata {GameLoopManager.Instance.ErrorCount.Value}).");
        ServerAllCustomersFinished?.Invoke();
    }

    private static void Release(Customer[] occupants, Customer customer)
    {
        int index = Array.IndexOf(occupants, customer);
        if (index >= 0)
            occupants[index] = null;
    }

    private void ClearAll()
    {
        foreach (var customer in FindObjectsByType<Customer>(FindObjectsInactive.Exclude))
        {
            if (customer.IsSpawned && IsServer)
                customer.NetworkObject.Despawn();
        }

        _active.Clear();
        _queue.Clear();
        Array.Clear(_deliveryOccupants, 0, _deliveryOccupants.Length);
    }
}
