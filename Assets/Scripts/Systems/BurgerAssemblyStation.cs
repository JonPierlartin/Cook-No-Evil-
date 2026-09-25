using Unity.Netcode;
using UnityEngine;

// Şef'in masasındaki hamburger birleştirme (GDD 6.7.3). İstasyon TARİF DOĞRULAMASI YAPMAZ — yanlış
// malzeme (marul yerine domates) konabilmeli, hatanın kendisi oyunun konusu; yalnızca KATEGORİ SIRASI
// denetlenir. Konan her malzeme replike PlacedIngredients listesine bir katman olarak yazılır ve
// BurgerStackVisual bunu her istemcide tezgahın üstünde görünür bir yığın olarak çizer (geri bildirim).
//
// GEÇİCİ (Adım 6b'de değişecek): ikinci ekmek (üst ekmek) konunca yığın temizlenir ve HİÇBİR ÖĞE
// ÜRETİLMEZ. 6b'de tamamlanan hamburger tek bir öğe olarak envantere girecek (GDD 6.7.3).
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(HoldOrPressInteractable))]
public class BurgerAssemblyStation : NetworkBehaviour, IInteractionGate
{
    [Tooltip("Id -> ItemType (yığın katmanları tür id'si tutar; kategori ve görsel buradan çözülür).")]
    [SerializeField] private ItemRegistry registry;
    [Tooltip("Bu istasyonu kullanabilecek roller. Bos birakilirsa herkes kullanabilir.")]
    [SerializeField] private PlayerRole[] allowedRoles;

    public readonly NetworkList<BurgerLayerEntry> PlacedIngredients = new();

    private HoldOrPressInteractable _interactable;

    public ItemRegistry Registry => registry;

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

    private void HandleInteractionCompleted(InteractionContext context)
    {
        if (!IsServer)
            return;

        if (!TryEvaluate(context, out var inventory, out var itemType, out var reason))
        {
            Debug.LogWarning($"[BurgerAssemblyStation] reddetti (clientId={context.ClientId}): {reason}.");
            return;
        }

        // Sira: ONCE slottan cikarilir, SONRA despawn edilir (slot hicbir an despawn olmus bir ogeyi
        // gostermez). Tezgah malzemeyi ag nesnesi olarak TUTMAZ, yalnizca katman kaydi yazar. Slot,
        // ActiveSlotIndex DEGIL, baglamdan (bkz. InteractionContext.cs) — tiklama anindaki secili slot.
        if (!inventory.ServerTryTakeItemAt(context.SlotIndex, out var item))
            return;

        // Koyulan ogenin fazi (orn. ciglik) kaydedilir: yigin ciglik rengini gosterir.
        int phaseIndex = item.TryGetComponent(out ServerProgress progress) ? progress.PhaseIndex.Value : 0;

        if (itemType.Category == ItemCategory.Ekmek && PlacedIngredients.Count > 0)
        {
            // GECICI: ust ekmek yigini tamamlar; simdilik yalnizca temizlenir, hicbir oge uretilmez (6b).
            PlacedIngredients.Clear();
        }
        else
        {
            PlacedIngredients.Add(new BurgerLayerEntry(itemType.Id, phaseIndex));
        }

        ItemMover.Despawn(item);
    }

    // Kural TEK yerde (GDD 4.1.2, 6.3, 6.7.3): rol izinli, baglam slotunda bir malzeme var ve kategori
    // sirasina uyuyor. Sunucu bunu tamamlanmada, crosshair her karede (istemcide) sorar.
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

        if (!IsCategoryAllowedNext(itemType.Category))
        {
            reason = "malzeme bu sırada konamaz";
            return false;
        }

        reason = null;
        return true;
    }

    // KATEGORI SIRASI — tek yer (GDD 6.7.3): Alt ekmek -> Protein -> Garnitur -> Sos -> Ust ekmek.
    // Kategori ICINDE sira serbest; garnitur ve sos birden fazla kez konabilir (esit sira serbest).
    // Tarif dogrulamasi YOK: yalnizca sira. Bos yigina yalnizca ekmek (alt ekmek) konur; dolu yigina
    // konan ekmek UST ekmektir (sira 4, her seyden sonra). Kategorisi Yok olan tur hicbir zaman konmaz.
    private bool IsCategoryAllowedNext(ItemCategory next)
    {
        if (next == ItemCategory.Yok)
            return false;

        if (PlacedIngredients.Count == 0)
            return next == ItemCategory.Ekmek;

        int lastRank = RankOfLayer(PlacedIngredients.Count - 1);
        return lastRank >= 0 && RankOfNew(next) >= lastRank;
    }

    // Yigindaki katmanin sirasi. Dizin 0 her zaman alt ekmektir (sira 0); registry'de bulunamayan
    // katman -1 (sonraki hicbir seyin konmasina izin vermez — savunmaci, olmamali).
    private int RankOfLayer(int index)
    {
        if (index == 0)
            return 0;

        var type = registry != null ? registry.Find(PlacedIngredients[index].TypeId) : null;
        return type != null ? RankOfNew(type.Category) : -1;
    }

    private static int RankOfNew(ItemCategory category)
    {
        return category switch
        {
            ItemCategory.Protein => 1,
            ItemCategory.Garnitur => 2,
            ItemCategory.Sos => 3,
            ItemCategory.Ekmek => 4, // dolu yigina konan ekmek = ust ekmek
            _ => -1
        };
    }

    private bool IsRoleAllowed(PlayerRole role)
    {
        return allowedRoles == null || allowedRoles.Length == 0 || System.Array.IndexOf(allowedRoles, role) >= 0;
    }
}
