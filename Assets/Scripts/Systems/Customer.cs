using System;
using Unity.Netcode;
using UnityEngine;

public enum CustomerState : byte
{
    // Sipariş penceresine yürüyor (henüz tıklanamaz, sabır işlemiyor).
    Arriving,
    // Sipariş penceresinde; sabır sayacı işliyor (GDD 3.4.4).
    WaitingToOrder,
    // Siparişi alındı; teslim penceresine geçti / orada bekliyor.
    Ordered,
    // Dükkandan ayrılıyor (teslim aldı ya da sabrı doldu).
    Leaving
}

// Müşteri (GDD 3.4.2, 3.4.4, 3.6.1): sunucu sahipli ağ nesnesi. Durumu, sabır sayacını ve hareketini YALNIZCA sunucu
// işletir (K6); istemciler replike durumu gösterir (CustomerVisual, CustomerPatienceDisplay). Nereye gideceğine ve
// ne zaman geleceğine CustomerDirector karar verir; müşteri kendi sabrını sayar ve siparişinin alınmasını yönetir.
// Siparişin içeriği (çözümlenmiş slot) sunucuda müşterinin üstünde tutulur; pop-up ve sipariş süresi sonraki adımda.
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(HoldOrPressInteractable))]
public class Customer : NetworkBehaviour, IInteractionGate
{
    [Tooltip("Siparişi alabilen roller (GDD 3.6.1: Kasiyer).")]
    [SerializeField] private PlayerRole[] orderTakerRoles = { PlayerRole.Kasiyer };
    [Tooltip("Yürüme hızı (m/sn). Yer tutucu hareket: hedefe düz çizgide gider.")]
    [SerializeField, Min(0.1f)] private float moveSpeed = 2.5f;

    public readonly NetworkVariable<CustomerState> State =
        new(CustomerState.Arriving, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // Sabır: kalan ve toplam süre (sn). Yalnızca sunucu yazar; çark bu ikisinden çizilir.
    public readonly NetworkVariable<float> PatienceRemaining =
        new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<float> PatienceTotal =
        new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Yalnızca sunucuda dolu.
    public ResolvedLevel.Order Order { get; private set; }
    public bool IsBackup { get; private set; }
    public string Label { get; private set; }

    // Sunucu olayları (CustomerDirector dinler). Parametre: bu müşteri (+ siparişi alan client).
    public event Action<Customer, ulong> ServerOrderTaken;
    public event Action<Customer> ServerPatienceExpired;

    private HoldOrPressInteractable _interactable;
    private Vector3 _target;
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

    // Spawn'dan hemen sonra, sunucuda.
    public void ServerInitialize(ResolvedLevel.Order order, bool isBackup, string label)
    {
        Order = order;
        IsBackup = isBackup;
        Label = label;
        State.Value = CustomerState.Arriving;
        PatienceTotal.Value = order.Patience;
        PatienceRemaining.Value = order.Patience;
    }

    public void ServerMoveTo(Vector3 position, Quaternion rotation, Action<Customer> onArrived)
    {
        _target = position;
        _targetRotation = rotation;
        _onArrived = onArrived;
        _moving = true;
    }

    // Sipariş penceresine vardı: sabır sayacı başlar (GDD 3.4.4 "geldiği andan").
    public void ServerStartWaiting() => State.Value = CustomerState.WaitingToOrder;

    public void ServerStartLeaving() => State.Value = CustomerState.Leaving;

    private void Update()
    {
        // Sayaç ve hareket yalnızca sunucuda; duraklatmada (kopma) ikisi de durur.
        if (!IsServer || !IsSpawned || !GameLoopManager.CanPlayersAct)
            return;

        if (_moving)
            Move();

        if (State.Value != CustomerState.WaitingToOrder)
            return;

        PatienceRemaining.Value = Mathf.Max(0f, PatienceRemaining.Value - Time.deltaTime);
        if (PatienceRemaining.Value <= 0f)
            ServerPatienceExpired?.Invoke(this);
    }

    private void Move()
    {
        transform.position = Vector3.MoveTowards(transform.position, _target, moveSpeed * Time.deltaTime);
        var toTarget = _target - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(toTarget);

        if ((transform.position - _target).sqrMagnitude > 0.0001f)
            return;

        _moving = false;
        transform.SetPositionAndRotation(_target, _targetRotation);
        var callback = _onArrived;
        _onArrived = null;
        callback?.Invoke(this);
    }

    // Kural TEK yerde (istemci crosshair'i ve sunucu kararı aynı sorgu): rol izinli + müşteri sipariş bekliyor.
    public bool CanInteract(InteractionContext context, out string reason)
    {
        var role = RoleManager.Instance != null ? RoleManager.Instance.GetRole(context.ClientId) : PlayerRole.None;
        if (orderTakerRoles == null || Array.IndexOf(orderTakerRoles, role) < 0)
        {
            reason = "rol izinli değil";
            return false;
        }

        if (State.Value != CustomerState.WaitingToOrder)
        {
            reason = "müşteri sipariş beklemiyor";
            return false;
        }

        reason = null;
        return true;
    }

    private void HandleInteractionCompleted(InteractionContext context)
    {
        if (!IsServer)
            return;

        if (!CanInteract(context, out var reason))
        {
            Debug.LogWarning($"[Customer] '{Label}' sipariş alma reddedildi (client={context.ClientId}): {reason}.");
            return;
        }

        // Sabır sayacı durur (durum değişti); gerisini yönetici yürütür (teslim penceresinde yer verir).
        State.Value = CustomerState.Ordered;
        ServerOrderTaken?.Invoke(this, context.ClientId);
    }
}
