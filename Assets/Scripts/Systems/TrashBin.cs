using Unity.Netcode;
using UnityEngine;

// GDD 5.3.2 Çöp: herhangi bir öğe (yanmış et, çiğ kalmış ürün, yanlış paket/hamburger...) çöpe atılabilir; öğe
// eline alınır, çöp kutusuna sol tık ile atılır, İMHA EDİLİR ve geri alınamaz. Faz 0'da yangın yok: yanmış et
// söndürme gerekmeden alınıp atılır (5.3.2 faz kısıtı). Hangi rolün kullanabileceği kutu başına ayarlanır
// (GDD 6.3; her odada kendi rolünün kutusu). Öğenin durumuna bakılmaz — Şef'e durum sızdırmaz (4.1.2 ①).
// Geri bildirim: öğe elden ve hotbar'dan anında kalkar (replike envanter).
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(HoldOrPressInteractable))]
public class TrashBin : NetworkBehaviour, IInteractionGate
{
    [Tooltip("Bu çöp kutusunu kullanabilecek roller (GDD 6.3). Boş bırakılırsa herkes kullanabilir.")]
    [SerializeField] private PlayerRole[] allowedRoles;

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

    // Kural TEK yerde: rol izinli + bağlam slotunda (tıklama anındaki seçili slot) bir öğe var.
    public bool CanInteract(InteractionContext context, out string reason)
    {
        return TryEvaluate(context, out _, out reason);
    }

    private bool TryEvaluate(InteractionContext context, out PlayerInventory inventory, out string reason)
    {
        inventory = null;

        var role = RoleManager.Instance != null ? RoleManager.Instance.GetRole(context.ClientId) : PlayerRole.None;
        if (!(allowedRoles == null || allowedRoles.Length == 0 || System.Array.IndexOf(allowedRoles, role) >= 0))
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

        // ActiveSlotIndex DEĞİL — context.SlotIndex (bkz. InteractionContext.cs).
        if (!inventory.TryGetItem(context.SlotIndex, out _))
        {
            reason = "elde öğe yok";
            return false;
        }

        reason = null;
        return true;
    }

    private void HandleInteractionCompleted(InteractionContext context)
    {
        if (!IsServer)
            return;

        if (!TryEvaluate(context, out var inventory, out var reason))
        {
            Debug.LogWarning($"[TrashBin] '{name}' reddetti (clientId={context.ClientId}): {reason}.");
            return;
        }

        // Sıra: önce slottan çıkar, sonra yok et (slot hiçbir an despawn olmuş öğeyi göstermez).
        if (inventory.ServerTryTakeItemAt(context.SlotIndex, out var item))
            ItemMover.Despawn(item);
    }
}
