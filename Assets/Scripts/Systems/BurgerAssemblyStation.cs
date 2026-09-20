using Unity.Netcode;
using UnityEngine;

// Sef'in masasindaki hamburger birlestirme. Bilincli kapsam genisletmesi (GDD'de
// tanimli degildi) — SADECE tarif veri modelini ve masadaki malzeme dogrulama
// mantigini kapsar; musteri/siparis/NPC/kuyruk sistemi KURULMUYOR. activeRecipe
// test icin Inspector'dan sabit secilir, gercek bir siparis kaynagina baglanmaz.
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(HoldOrPressInteractable))]
public class BurgerAssemblyStation : NetworkBehaviour, IInteractionGate
{
    [SerializeField] private BurgerRecipe activeRecipe;
    [Tooltip("Id -> IngredientType cozumlemesi icin kayit defteri. DumbwaiterSystem kuruldugunda AYNI dizi Inspector'dan atanmali.")]
    [SerializeField] private IngredientType[] registeredIngredients;
    [Tooltip("Bu istasyonu kullanabilecek roller. Bos birakilirsa herkes kullanabilir.")]
    [SerializeField] private PlayerRole[] allowedRoles;

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

    // GECICI TESHIS — Adim 6'da kaldirilacak.
    private void HandleInteractionCompleted(ulong clientId)
    {
        var role = RoleManager.Instance != null ? RoleManager.Instance.GetRole(clientId) : PlayerRole.None;
        Debug.Log($"[BurgerAssemblyStation] HandleInteractionCompleted cagrildi (clientId={clientId}, role={role}).");

        if (!TryEvaluate(clientId, out var inventory, out int ingredientId, out var reason))
        {
            Debug.LogWarning($"[BurgerAssemblyStation] reddetti (clientId={clientId}, rol={role}): {reason}.");
            return;
        }

        if (!inventory.ServerTryRemoveActiveItem(out _))
            return;

        PlacedIngredients.Add(ingredientId);

        // Test edilebilirlik icin: tarif tamamlaninca otomatik sifirlanir, boylece
        // musteri/siparis sistemine gerek kalmadan art arda test edilebilir.
        if (IsRecipeComplete())
            PlacedIngredients.Clear();
    }

    // Kural TEK yerde (GDD 4.1.2, 6.3, 6.7.3): rol izinli, elde (aktif slot) bir malzeme var ve
    // tezgah onu su an kabul ediyor. Sunucu bunu tamamlanmada, crosshair her karede (istemcide) sorar.
    public bool CanInteract(ulong clientId, out string reason)
    {
        return TryEvaluate(clientId, out _, out _, out reason);
    }

    private bool TryEvaluate(ulong clientId, out PlayerInventory inventory, out int ingredientId, out string reason)
    {
        inventory = null;
        ingredientId = PlayerInventory.EmptySlot;

        if (RoleManager.Instance == null)
        {
            reason = "RoleManager yok";
            return false;
        }

        if (!IsRoleAllowed(RoleManager.Instance.GetRole(clientId)))
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

        int activeSlot = inventory.ActiveSlotIndex.Value;
        if (activeSlot < 0 || activeSlot >= inventory.Slots.Count || inventory.Slots[activeSlot] == PlayerInventory.EmptySlot)
        {
            reason = "elde malzeme yok";
            return false;
        }

        ingredientId = inventory.Slots[activeSlot];
        var ingredientType = FindIngredientType(ingredientId);
        if (ingredientType == null)
        {
            reason = "malzeme kayıtlı değil";
            return false;
        }

        // Ilk yerlestirme kesinlikle ekmek olmali; sonrasi icin sira kurali yok
        // (kullanici karari) — sadece aktif tarifin (varyasyonu dahil) izin
        // verdigi malzemeler kabul edilir.
        bool isValid = PlacedIngredients.Count == 0
            ? ingredientType.IsBread
            : activeRecipe != null && activeRecipe.AllowsIngredient(ingredientType);

        if (!isValid)
        {
            reason = "malzeme bu tezgaha konamaz";
            return false;
        }

        reason = null;
        return true;
    }

    private bool IsRoleAllowed(PlayerRole role)
    {
        return allowedRoles == null || allowedRoles.Length == 0 || System.Array.IndexOf(allowedRoles, role) >= 0;
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
