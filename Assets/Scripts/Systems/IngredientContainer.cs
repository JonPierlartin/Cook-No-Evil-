using Unity.Netcode;
using UnityEngine;

// Sabit bir malzemeyi, etkilesen oyuncunun envanterine veren dunya nesnesi (malzeme kabi).
// Birlestirme tezgahindan (BurgerAssemblyStation) ayri bir nesnedir (GDD 4.1.1, 6.7.3).
// HoldOrPressInteractable olayi yalnizca sunucuda tetiklenir (BeginPress sadece sunucudan
// cagrilir); olay etkilesen oyuncunun kimligini tasidigi icin SenderClientId okunmaz.
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(HoldOrPressInteractable))]
public class IngredientContainer : NetworkBehaviour, IInteractionGate
{
    [SerializeField] private ItemType ingredient;
    [Tooltip("Bu kabi kullanabilecek roller. Bos birakilirsa herkes alabilir.")]
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

    // Kural TEK yerde (GDD 4.1.2, 6.3): malzeme atanmis ve spawn edilebilir (itemPrefab dolu), rol izinli,
    // envanterde bos slot var.
    // Sunucu bunu tamamlanmada, crosshair her karede (istemcide) sorar.
    public bool CanInteract(InteractionContext context, out string reason)
    {
        return TryEvaluate(context, out _, out reason);
    }

    private bool TryEvaluate(InteractionContext context, out PlayerInventory inventory, out string reason)
    {
        inventory = null;

        if (ingredient == null)
        {
            reason = "malzeme atanmamış";
            return false;
        }

        // Bu tur henuz gercek bir oge olarak spawn edilemiyor (orn. Kofte): alinamaz, crosshair "engelli".
        if (ingredient.ItemPrefab == null)
        {
            reason = "malzemenin öğe prefab'ı yok";
            return false;
        }

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

        if (!inventory.HasFreeSlot(context.SlotIndex))
        {
            reason = "boş slot yok";
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
            Debug.LogWarning($"[IngredientContainer] '{name}' reddetti (clientId={context.ClientId}): {reason}.");
            return;
        }

        // Sira: ONCE oge dogar, SONRA slota yazilir. Slot yazimi basarisiz olursa oge hemen despawn
        // edilir — sahipsiz oge kalmaz. (Kapi ayni cagrida bos slot dogruladi; basarisizlik bir hatadir.)
        var item = ItemMover.SpawnCarried(ingredient, transform.position);
        if (item == null)
            return;

        if (!inventory.ServerTryAddItem(item, context.SlotIndex))
        {
            Debug.LogError($"[IngredientContainer] '{name}': oge slota yazilamadi (clientId={context.ClientId}); oge geri alindi.");
            ItemMover.Despawn(item);
        }
    }

    private bool IsRoleAllowed(PlayerRole role)
    {
        return allowedRoles == null || allowedRoles.Length == 0 || System.Array.IndexOf(allowedRoles, role) >= 0;
    }
}
