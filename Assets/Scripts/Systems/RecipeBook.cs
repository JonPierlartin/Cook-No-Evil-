using Unity.Netcode;
using UnityEngine;

// Tarif kitapçığı — dünyadaki nesne (GDD 3.6.2): Kasa'da tezgahta durur, eline alınır ama envantere GİRMEZ.
// Etkileşim diğerleriyle aynı yoldan geçer (istemci niyet gönderir, sunucu doğrular — K6); sunucu yalnızca
// "açabilirsin" kararını verir ve bunu YALNIZCA tıklayan oyuncuya iletir. Kitabın görünümü yereldir (RecipeBookUI):
// ağ durumu yoktur, diğer oyuncular bir şey görmez.
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(HoldOrPressInteractable))]
public class RecipeBook : NetworkBehaviour, IInteractionGate
{
    [Tooltip("Kitabı açabilen roller (GDD 3.6.2: Kasiyer).")]
    [SerializeField] private PlayerRole[] readerRoles = { PlayerRole.Kasiyer };

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

    public bool CanInteract(InteractionContext context, out string reason)
    {
        var role = RoleManager.Instance != null ? RoleManager.Instance.GetRole(context.ClientId) : PlayerRole.None;
        if (readerRoles == null || System.Array.IndexOf(readerRoles, role) < 0)
        {
            reason = "rol kitabı açamaz";
            return false;
        }

        reason = null;
        return true;
    }

    private void HandleInteractionCompleted(InteractionContext context)
    {
        if (!IsServer || !CanInteract(context, out _))
            return;

        // Yalnızca tıklayan oyuncuya (parametresiz ClientRpc herkese giderdi).
        OpenClientRpc(new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { context.ClientId } } });
    }

    [ClientRpc]
    private void OpenClientRpc(ClientRpcParams rpcParams = default)
    {
        if (RecipeBookUI.Instance != null)
            RecipeBookUI.Instance.Open();
    }
}
