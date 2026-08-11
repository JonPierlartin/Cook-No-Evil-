using Unity.Netcode;
using UnityEngine;

// Sef'in masasindaki hamburger birlestirme. Bilincli kapsam genisletmesi (GDD'de
// tanimli degildi) — SADECE tarif veri modelini ve masadaki malzeme dogrulama
// mantigini kapsar; musteri/siparis/NPC/kuyruk sistemi KURULMUYOR. activeRecipe
// test icin Inspector'dan sabit secilir, gercek bir siparis kaynagina baglanmaz.
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(HoldOrPressInteractable))]
public class BurgerAssemblyStation : NetworkBehaviour
{
    [SerializeField] private BurgerRecipe activeRecipe;
    [Tooltip("Id -> IngredientType cozumlemesi icin kayit defteri. DumbwaiterSystem kuruldugunda AYNI dizi Inspector'dan atanmali.")]
    [SerializeField] private IngredientType[] registeredIngredients;

    public readonly NetworkList<int> PlacedIngredients = new();

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

    private void HandleInteractionCompleted()
    {
        PlaceIngredientServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlaceIngredientServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        if (RoleManager.Instance == null || RoleManager.Instance.GetRole(senderId) != PlayerRole.Sef)
            return;

        if (NetworkManager == null || !NetworkManager.ConnectedClients.TryGetValue(senderId, out var client) || client.PlayerObject == null)
            return;

        var inventory = client.PlayerObject.GetComponent<PlayerInventory>();
        if (inventory == null)
            return;

        int activeSlot = inventory.ActiveSlotIndex.Value;
        if (activeSlot < 0 || activeSlot >= inventory.Slots.Count)
            return;

        int ingredientId = inventory.Slots[activeSlot];
        if (ingredientId == PlayerInventory.EmptySlot)
            return;

        var ingredientType = FindIngredientType(ingredientId);
        if (ingredientType == null)
            return;

        // Ilk yerlestirme kesinlikle ekmek olmali; sonrasi icin sira kurali yok
        // (kullanici karari) — sadece aktif tarifin (varyasyonu dahil) izin
        // verdigi malzemeler kabul edilir.
        bool isValid = PlacedIngredients.Count == 0
            ? ingredientType.IsBread
            : activeRecipe != null && activeRecipe.AllowsIngredient(ingredientType);

        if (!isValid)
            return;

        if (!inventory.ServerTryRemoveActiveItem(out _))
            return;

        PlacedIngredients.Add(ingredientId);

        // Test edilebilirlik icin: tarif tamamlaninca otomatik sifirlanir, boylece
        // musteri/siparis sistemine gerek kalmadan art arda test edilebilir.
        if (IsRecipeComplete())
            PlacedIngredients.Clear();
    }

    private bool IsRecipeComplete()
    {
        if (activeRecipe == null)
            return false;

        foreach (var requirement in activeRecipe.RequiredIngredients)
        {
            int have = 0;
            foreach (int placedId in PlacedIngredients)
            {
                if (placedId == requirement.Type.Id)
                    have++;
            }

            if (have < requirement.Quantity)
                return false;
        }

        return true;
    }

    private IngredientType FindIngredientType(int id)
    {
        if (registeredIngredients == null)
            return null;

        foreach (var ingredient in registeredIngredients)
        {
            if (ingredient != null && ingredient.Id == id)
                return ingredient;
        }

        return null;
    }
}
