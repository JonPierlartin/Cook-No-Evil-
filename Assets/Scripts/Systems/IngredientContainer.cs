using Unity.Netcode;
using UnityEngine;

// Sabit bir malzemeyi, etkilesen oyuncunun envanterine veren dunya nesnesi (malzeme kabi).
// Birlestirme tezgahindan (BurgerAssemblyStation) ayri bir nesnedir (GDD 4.1.1, 6.7.3).
// HoldOrPressInteractable olayi yalnizca sunucuda tetiklenir (BeginPress sadece sunucudan
// cagrilir); olay etkilesen oyuncunun kimligini tasidigi icin SenderClientId okunmaz.
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(HoldOrPressInteractable))]
public class IngredientContainer : NetworkBehaviour
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

    private void HandleInteractionCompleted(ulong clientId)
    {
        if (!IsServer)
            return;

        if (ingredient == null)
        {
            Debug.LogWarning($"[IngredientContainer] '{name}' reddetti (clientId={clientId}): malzeme atanmamış.");
            return;
        }

        var role = RoleManager.Instance != null ? RoleManager.Instance.GetRole(clientId) : PlayerRole.None;
        if (!IsRoleAllowed(role))
        {
            Debug.LogWarning($"[IngredientContainer] '{name}' reddetti (clientId={clientId}, rol={role}): rol izinli değil.");
            return;
        }

        if (!NetworkManager.ConnectedClients.TryGetValue(clientId, out var client) || client.PlayerObject == null)
        {
            Debug.LogWarning($"[IngredientContainer] '{name}' reddetti (clientId={clientId}, rol={role}): oyuncu objesi bulunamadı.");
            return;
        }

        var inventory = client.PlayerObject.GetComponent<PlayerInventory>();
        if (inventory == null)
        {
            Debug.LogWarning($"[IngredientContainer] '{name}' reddetti (clientId={clientId}, rol={role}): PlayerInventory yok.");
            return;
        }

        if (!inventory.HasFreeSlot())
        {
            Debug.LogWarning($"[IngredientContainer] '{name}' reddetti (clientId={clientId}, rol={role}): boş slot yok.");
            return;
        }

        inventory.ServerTryAddItem(ingredient.Id);
    }

    private bool IsRoleAllowed(PlayerRole role)
    {
        return allowedRoles == null || allowedRoles.Length == 0 || System.Array.IndexOf(allowedRoles, role) >= 0;
    }
}
