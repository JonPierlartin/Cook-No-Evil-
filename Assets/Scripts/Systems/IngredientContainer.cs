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
    [SerializeField] private IngredientType ingredient;
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

    // Kural TEK yerde (GDD 4.1.2, 6.3): malzeme atanmis, rol izinli, envanterde bos slot var.
    // Sunucu bunu tamamlanmada, crosshair her karede (istemcide) sorar.
    public bool CanInteract(ulong clientId, out string reason)
    {
        return TryEvaluate(clientId, out _, out reason);
    }

    private bool TryEvaluate(ulong clientId, out PlayerInventory inventory, out string reason)
    {
        inventory = null;

        if (ingredient == null)
        {
            reason = "malzeme atanmamış";
            return false;
        }

        var role = RoleManager.Instance != null ? RoleManager.Instance.GetRole(clientId) : PlayerRole.None;
        if (!IsRoleAllowed(role))
        {
            reason = "rol izinli değil";
            return false;
        }

        inventory = PlayerInventory.FindForClient(clientId);
        if (inventory == null)
        {
            reason = "oyuncu envanteri bulunamadı";
            return false;
        }

        if (!inventory.HasFreeSlot())
        {
            reason = "boş slot yok";
            return false;
        }

        reason = null;
        return true;
    }

    private void HandleInteractionCompleted(ulong clientId)
    {
        if (!IsServer)
            return;

        if (!TryEvaluate(clientId, out var inventory, out var reason))
        {
            Debug.LogWarning($"[IngredientContainer] '{name}' reddetti (clientId={clientId}): {reason}.");
            return;
        }

        inventory.ServerTryAddItem(ingredient.Id);
    }

    private bool IsRoleAllowed(PlayerRole role)
    {
        return allowedRoles == null || allowedRoles.Length == 0 || System.Array.IndexOf(allowedRoles, role) >= 0;
    }
}
