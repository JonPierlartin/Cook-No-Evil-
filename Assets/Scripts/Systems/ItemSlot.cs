using Unity.Netcode;
using UnityEngine;

// GDD 4.1.2 "Yuva ile etkilesim": tek bir ogeyi USTUNDE tutan yerlestirme hedefi (izgara gozu, pencere,
// paketleme alani, test tezgahi...). Yuva kendi NetworkObject'idir; cunku konan oge ona TrySetParent ile
// baglanir ve K7 geregi parent duz bir Transform olamaz.
//
// Dolu/bos bilgisi AYRI bir bayrakta TUTULMAZ, parent iliskisinden turetilir: yuvanin dogrudan cocugu
// olan Item = doluluk. Boylece "dolu" ile "gercekte icinde oge var" hicbir zaman ayrismaz; sunucuda da
// istemcide de (ParentSyncMessage ile gelen hiyerarsi) ayni yerden okunur. Yuvaya konan/yuvadan alinan
// ogenin tasinmasi ItemMover'dadir; bu bilesen yalnizca kurali (gate) ve tetigi tasir.
//
// Yuvanin collider'i (kendi veya cocuklari) uzerindeki ogeyi de kapsamalidir: ogelerde collider yoktur,
// nisan alinan hedef yuvadir. Collider Box/Sphere/Capsule/convex Mesh olmalidir (ClosestPoint).
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(HoldOrPressInteractable))]
[RequireComponent(typeof(PlacementTarget))]
public class ItemSlot : NetworkBehaviour, IInteractionGate
{
    [Tooltip("Bu yuvayi kullanabilecek roller (GDD 6.3). Bos birakilirsa herkes kullanabilir.")]
    [SerializeField] private PlayerRole[] allowedRoles;
    [Tooltip("Bu yuvanin kabul ettigi oge turleri. Bos birakilirsa her tur kabul edilir.")]
    [SerializeField] private ItemType[] acceptedTypes;

    private HoldOrPressInteractable _interactable;

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

    // Yuvanin dogrudan cocugu olan oge (parent'tan turetilmis doluluk). Allocation yapmaz: crosshair
    // ve onizleme her karede sorar.
    public bool TryGetOccupant(out Item item)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).TryGetComponent(out item))
                return true;
        }

        item = null;
        return false;
    }

    public bool IsEmpty => !TryGetOccupant(out _);

    public bool Accepts(ItemType type)
    {
        if (type == null)
            return false;

        return acceptedTypes == null || acceptedTypes.Length == 0 || System.Array.IndexOf(acceptedTypes, type) >= 0;
    }

    // Kural TEK yerde (GDD "Yuva ile etkilesim"); sunucu tamamlanmada, crosshair her karede sorar. SWAP YOK:
    //  - bos yuva : rol izinli + BAGLAM slotunda oge var + turu kabul ediliyor
    //  - dolu yuva: rol izinli + envanterde bos slot var (elindeki ogenin durumu onemsiz)
    public bool CanInteract(InteractionContext context, out string reason)
    {
        return TryEvaluate(context, out _, out _, out reason);
    }

    private bool TryEvaluate(InteractionContext context, out PlayerInventory inventory, out Item occupant, out string reason)
    {
        inventory = null;
        occupant = null;

        var role = RoleManager.Instance != null ? RoleManager.Instance.GetRole(context.ClientId) : PlayerRole.None;
        if (!IsRoleAllowed(role))
        {
            reason = "rol izinli değil";
            return false;
        }

        inventory = PlayerInventory.FindForClient(context.ClientId);
        if (inventory == null)
        {
            reason = "oyuncu envanteri bulunamadı";
            return false;
        }

        if (TryGetOccupant(out occupant))
        {
            if (!inventory.HasFreeSlot(context.SlotIndex))
            {
                reason = "boş slot yok";
                return false;
            }

            reason = null;
            return true;
        }

        // ActiveSlotIndex (NetworkVariable) DEGIL — context.SlotIndex, RPC'nin tasidigi tiklama
        // anindaki secili slot (bkz. InteractionContext.cs).
        if (!inventory.TryGetItem(context.SlotIndex, out var held))
        {
            reason = "elde öğe yok";
            return false;
        }

        if (!Accepts(held.Type))
        {
            reason = "öğe bu yuvaya uygun değil";
            return false;
        }

        reason = null;
        return true;
    }

    private void HandleInteractionCompleted(InteractionContext context)
    {
        if (!IsServer)
            return;

        if (!TryEvaluate(context, out var inventory, out var occupant, out var reason))
        {
            Debug.LogWarning($"[ItemSlot] '{name}' reddetti (clientId={context.ClientId}): {reason}.");
            return;
        }

        if (occupant != null)
            ItemMover.TakeFromSlot(this, inventory, context.SlotIndex);
        else
            ItemMover.PlaceInSlot(this, inventory, context.SlotIndex);
    }

    private bool IsRoleAllowed(PlayerRole role)
    {
        return allowedRoles == null || allowedRoles.Length == 0 || System.Array.IndexOf(allowedRoles, role) >= 0;
    }
}
