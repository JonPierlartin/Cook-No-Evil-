using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum CustomerState : byte
{
    // Sipariş penceresine yürüyor ya da sırada arkada bekliyor (tıklanamaz, sabır işlemiyor).
    Arriving,
    // Sipariş penceresinde; sabır sayacı işliyor (GDD 3.4.4).
    WaitingToOrder,
    // Siparişi alındı; teslim penceresine geçti / orada bekliyor.
    Ordered,
    // Dükkandan ayrılıyor (teslim aldı ya da süresi doldu).
    Leaving
}

// Müşterinin ayrılırkenki hâli (GDD 7.1.1: sevinç / öfke — yer tutucu). Yalnızca sunucu yazar.
public enum CustomerMood : byte
{
    Neutral,
    Happy,
    Angry
}

// Müşteri (GDD 3.4.2, 3.4.4, 3.6.1, 5.3.1): sunucu sahipli ağ nesnesi. Durumu, sayaçlarını ve hareketini YALNIZCA
// sunucu işletir (K6); istemciler replike durumu gösterir (CustomerVisual, CustomerTimerDisplay,
// CustomerOrderDisplay). Nereye gideceğine, ne zaman geleceğine ve teslimin doğru olup olmadığına CustomerDirector
// karar verir; müşteri kendi sayaçlarını sayar ve tıklamayı (sipariş alma / teslim) yöneticiye iletir.
// Siparişin içeriği (çözümlenmiş slot) sunucuda müşterinin üstünde tutulur; pop-up için gereken kısmı (varyant +
// eksik malzemeler) replike edilir. İki ayrı sayaç (GDD 3.4.4): sabır (sipariş alınana kadar) ve sipariş süresi
// (alındıktan teslimata kadar).
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(HoldOrPressInteractable))]
public class Customer : NetworkBehaviour, IInteractionGate
{
    [Tooltip("Siparişi alabilen roller (GDD 3.6.1: Kasiyer).")]
    [SerializeField] private PlayerRole[] orderTakerRoles = { PlayerRole.Kasiyer };
    [Tooltip("Paketi teslim edebilen roller (GDD 5.3.1: Kasiyer).")]
    [SerializeField] private PlayerRole[] deliveryRoles = { PlayerRole.Kasiyer };
    [Tooltip("Yürüme hızı (m/sn). Yer tutucu hareket: verilen noktalar arasında düz çizgide gider.")]
    [SerializeField, Min(0.1f)] private float moveSpeed = 2.5f;

    public readonly NetworkVariable<CustomerState> State =
        new(CustomerState.Arriving, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<CustomerMood> Mood =
        new(CustomerMood.Neutral, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // Sabır: kalan ve toplam süre (sn). Yalnızca sunucu yazar; çark bu ikisinden çizilir.
    public readonly NetworkVariable<float> PatienceRemaining =
        new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<float> PatienceTotal =
        new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Sipariş süresi (GDD 3.4.1): sipariş alınınca başlar. Yalnızca sunucu yazar.
    public readonly NetworkVariable<float> OrderTimeRemaining =
        new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<float> OrderTimeTotal =
        new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Pop-up içeriği (GDD 3.6.1): varyantın dizini (LevelDirector.TryGetVariant; -1 = hamburger yok) ve istenmeyen
    // malzemelerin tür id'leri. Yalnızca sunucu yazar.
    public readonly NetworkVariable<int> OrderVariantIndex =
        new(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkList<int> OrderMissingItemIds = new();

    // Yalnızca sunucuda dolu.
    public ResolvedLevel.Order Order { get; private set; }
    public bool IsBackup { get; private set; }
    public string Label { get; private set; }

    // Sunucu olayları (CustomerDirector dinler).
    public event Action<Customer, ulong> ServerOrderTaken;
    public event Action<Customer> ServerPatienceExpired;
    public event Action<Customer> ServerOrderTimeExpired;
    // Bir oyuncu bu müşteriye paket vermek istedi (bağlam: kim, hangi slot). Doğrulamayı yönetici yapar.
    public event Action<Customer, InteractionContext> ServerDeliveryRequested;

    // Her istemcide: bir müşterinin ayrılış hâli belli oldu (teslim geri bildirimi bunu dinler).
    public static event Action<Customer, CustomerMood> MoodChanged;

    private HoldOrPressInteractable _interactable;
    private readonly Queue<Vector3> _path = new();
    private Quaternion _targetRotation;
    private bool _moving;
    private Action<Customer> _onArrived;

    private void Awake()
    {
        _interactable = GetComponent<HoldOrPressInteractable>();
    }

    private void OnEnable()
    {
        _interactable.OnInteractionCompleted += HandleInteractionCompleted;
    }

    private void OnDisable()
    {
        _interactable.OnInteractionCompleted -= HandleInteractionCompleted;
    }

    public override void OnNetworkSpawn()
    {
        Mood.OnValueChanged += HandleMoodChanged;
    }

    public override void OnNetworkDespawn()
    {
        Mood.OnValueChanged -= HandleMoodChanged;
    }

    private void HandleMoodChanged(CustomerMood previous, CustomerMood current) => MoodChanged?.Invoke(this, current);

    // Spawn'dan hemen sonra, sunucuda.
    public void ServerInitialize(ResolvedLevel.Order order, bool isBackup, string label)
    {
        Order = order;
        IsBackup = isBackup;
        Label = label;
        State.Value = CustomerState.Arriving;
        PatienceTotal.Value = order.Patience;
        PatienceRemaining.Value = order.Patience;

        OrderVariantIndex.Value = LevelDirector.Instance != null ? LevelDirector.Instance.GetVariantIndex(order.Variant) : -1;
        OrderMissingItemIds.Clear();
        foreach (var missing in order.Missing)
            OrderMissingItemIds.Add(missing.Id);
    }

    // Sipariş alındı: sipariş süresi başlar (süreyi yönetici hesaplar — OrderTimeCalculator).
    public void ServerStartOrderTimer(float seconds)
    {
        OrderTimeTotal.Value = seconds;
        OrderTimeRemaining.Value = seconds;
    }

    // Hedefe yürür. waypoints verilirse önce onlardan sırayla geçer (duvarın içinden geçmemek için dışarıdan
    // dolaşma — rota sahnedeki noktalardan gelir, burada hesaplanmaz).
    public void ServerMoveTo(Vector3 position, Quaternion rotation, Action<Customer> onArrived, IReadOnlyList<Transform> waypoints = null)
    {
        _path.Clear();
        if (waypoints != null)
        {
            foreach (var waypoint in waypoints)
            {
                if (waypoint != null)
                    _path.Enqueue(waypoint.position);
            }
        }

        _path.Enqueue(position);
        _targetRotation = rotation;
        _onArrived = onArrived;
        _moving = true;
    }

    // Yalnızca SON hedefi değiştirir; henüz geçilmemiş ara noktalar korunur. Sokaktan kapıya yürüyen müşterinin
    // sıradaki yeri değişirse (öndeki ayrıldı) yine kapıdan girsin, duvarın içinden kestirmesin diye.
    public void ServerRedirect(Vector3 position, Quaternion rotation, Action<Customer> onArrived)
    {
        if (!_moving || _path.Count <= 1)
        {
            ServerMoveTo(position, rotation, onArrived);
            return;
        }

        var remaining = _path.ToArray();
        _path.Clear();
        for (int i = 0; i < remaining.Length - 1; i++)
            _path.Enqueue(remaining[i]);
        _path.Enqueue(position);
        _targetRotation = rotation;
        _onArrived = onArrived;
    }

    // Sipariş penceresinin önüne (sıranın başına) vardı: sabır sayacı başlar ve siparişi alınabilir (GDD 3.4.4).
    // Sırada arkada bekleyen müşterinin sabrı işlemez — Kasiyer onun siparişini zaten alamaz.
    public void ServerStartWaiting()
    {
        if (State.Value == CustomerState.Arriving)
            State.Value = CustomerState.WaitingToOrder;
    }

    public void ServerStartLeaving(CustomerMood mood)
    {
        State.Value = CustomerState.Leaving;
        Mood.Value = mood;
    }

    private void Update()
    {
        // Sayaçlar ve hareket yalnızca sunucuda; duraklatmada (kopma) hepsi durur.
        if (!IsServer || !IsSpawned || !GameLoopManager.CanPlayersAct)
            return;

        if (_moving)
            Move();

        if (State.Value == CustomerState.WaitingToOrder)
        {
            PatienceRemaining.Value = Mathf.Max(0f, PatienceRemaining.Value - Time.deltaTime);
            if (PatienceRemaining.Value <= 0f)
                ServerPatienceExpired?.Invoke(this);
        }
        else if (State.Value == CustomerState.Ordered && OrderTimeTotal.Value > 0f)
        {
            OrderTimeRemaining.Value = Mathf.Max(0f, OrderTimeRemaining.Value - Time.deltaTime);
            if (OrderTimeRemaining.Value <= 0f)
                ServerOrderTimeExpired?.Invoke(this);
        }
    }

    private void Move()
    {
        var target = _path.Peek();
        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
        var toTarget = target - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(toTarget);

        if ((transform.position - target).sqrMagnitude > 0.0001f)
            return;

        transform.position = target;
        _path.Dequeue();
        if (_path.Count > 0)
            return;

        _moving = false;
        transform.rotation = _targetRotation;
        var callback = _onArrived;
        _onArrived = null;
        callback?.Invoke(this);
    }

    // Kural TEK yerde (istemci crosshair'i ve sunucu kararı aynı sorgu):
    //  - sipariş bekliyor  : siparişi alabilen rol tıklar -> sipariş alınır
    //  - siparişi alınmış  : teslim edebilen rol, bağlam slotunda PAKET varken tıklar -> teslim (GDD 5.3.1).
    //    Herhangi bir müşteriye verilebilir (sıra yok); doğru müşteri olup olmadığına burada BAKILMAZ — o,
    //    sunucudaki doğrulamanın işidir (crosshair doğru müşteriyi sızdırmaz).
    public bool CanInteract(InteractionContext context, out string reason)
    {
        var role = RoleManager.Instance != null ? RoleManager.Instance.GetRole(context.ClientId) : PlayerRole.None;

        if (State.Value == CustomerState.WaitingToOrder)
        {
            if (!IsRoleIn(orderTakerRoles, role))
            {
                reason = "rol sipariş alamaz";
                return false;
            }

            reason = null;
            return true;
        }

        if (State.Value == CustomerState.Ordered)
        {
            if (!IsRoleIn(deliveryRoles, role))
            {
                reason = "rol teslim edemez";
                return false;
            }

            var inventory = PlayerInventory.FindForClient(context.ClientId);
            if (inventory == null || !inventory.TryGetItem(context.SlotIndex, out var held) || !held.TryGetComponent<Package>(out _))
            {
                reason = "elde paket yok";
                return false;
            }

            reason = null;
            return true;
        }

        reason = "müşteriyle şu an etkileşilemez";
        return false;
    }

    private void HandleInteractionCompleted(InteractionContext context)
    {
        if (!IsServer)
            return;

        if (!CanInteract(context, out var reason))
        {
            Debug.LogWarning($"[Customer] '{Label}' etkileşimi reddetti (client={context.ClientId}): {reason}.");
            return;
        }

        if (State.Value == CustomerState.WaitingToOrder)
        {
            // Sabır sayacı durur (durum değişti); gerisini yönetici yürütür (teslim penceresinde yer verir).
            State.Value = CustomerState.Ordered;
            ServerOrderTaken?.Invoke(this, context.ClientId);
            return;
        }

        ServerDeliveryRequested?.Invoke(this, context);
    }

    private static bool IsRoleIn(PlayerRole[] roles, PlayerRole role) => roles != null && Array.IndexOf(roles, role) >= 0;
}
