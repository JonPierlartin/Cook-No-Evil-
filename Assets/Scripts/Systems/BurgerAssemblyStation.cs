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
    private void HandleInteractionCompleted(InteractionContext context)
    {
        var role = RoleManager.Instance != null ? RoleManager.Instance.GetRole(context.ClientId) : PlayerRole.None;
        Debug.Log($"[BurgerAssemblyStation] HandleInteractionCompleted cagrildi (clientId={context.ClientId}, role={role}).");

        if (!TryEvaluate(context, out var inventory, out var itemType, out var reason))
        {
            Debug.LogWarning($"[BurgerAssemblyStation] reddetti (clientId={context.ClientId}, rol={role}): {reason}.");
            return;
        }

        // Sira: ONCE slottan cikarilir, SONRA despawn edilir (slot hicbir an despawn olmus bir ogeyi
        // gostermez). Tezgah malzemeyi yalnizca tur numarasi olarak tutar (PlacedIngredients) — bu adimda
        // oge tezgaha parent edilmez, yok edilir (parent/yuva Adim 4.2'de). Slot, ActiveSlotIndex DEGIL,
        // baglamdan (bkz. InteractionContext.cs) — tiklama anindaki secili slot budur.
        if (!inventory.ServerTryTakeItemAt(context.SlotIndex, out var item))
            return;

        PlacedIngredients.Add(itemType.Id);
        ItemMover.Despawn(item);

        // Test edilebilirlik icin: tarif tamamlaninca otomatik sifirlanir, boylece
        // musteri/siparis sistemine gerek kalmadan art arda test edilebilir.
        if (IsRecipeComplete())
            PlacedIngredients.Clear();
    }

    // Kural TEK yerde (GDD 4.1.2, 6.3, 6.7.3): rol izinli, elde (baglam slotunda) bir malzeme var ve
    // tezgah onu su an kabul ediyor. Sunucu bunu tamamlanmada, crosshair her karede (istemcide) sorar.
    public bool CanInteract(InteractionContext context, out string reason)
    {
        return TryEvaluate(context, out _, out _, out reason);
    }

    private bool TryEvaluate(InteractionContext context, out PlayerInventory inventory, out ItemType itemType, out string reason)
    {
        inventory = null;
        itemType = null;

        if (RoleManager.Instance == null)
        {
            reason = "RoleManager yok";
            return false;
        }

        if (!IsRoleAllowed(RoleManager.Instance.GetRole(context.ClientId)))
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

        // ActiveSlotIndex (NetworkVariable) DEGIL — context.SlotIndex (bkz. InteractionContext.cs).
        if (!inventory.TryGetItem(context.SlotIndex, out var item))
        {
            reason = "elde malzeme yok";
            return false;
        }

        itemType = item.Type;
        if (itemType == null)
        {
            reason = "öğenin türü atanmamış";
            return false;
        }

        // Ilk yerlestirme kesinlikle ekmek olmali; sonrasi icin sira kurali yok
        // (kullanici karari) — sadece aktif tarifin (varyasyonu dahil) izin
        // verdigi malzemeler kabul edilir.
        bool isValid = PlacedIngredients.Count == 0
            ? itemType.IsBread
            : activeRecipe != null && activeRecipe.AllowsIngredient(itemType);

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
}
