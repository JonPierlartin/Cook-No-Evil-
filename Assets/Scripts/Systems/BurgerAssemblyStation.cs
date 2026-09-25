using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Şef'in masasındaki hamburger birleştirme (GDD 6.7.3). İstasyon TARİF DOĞRULAMASI YAPMAZ — yanlış
// malzeme (marul yerine domates) konabilmeli, hatanın kendisi oyunun konusu; yalnızca KATEGORİ SIRASI
// ve ekmeğin iki parçalı kuralı denetlenir. Konan her malzeme replike PlacedIngredients listesine bir
// katman olarak yazılır ve BurgerStackVisual bunu her istemcide tezgahın üstünde görünür bir yığın
// olarak çizer (geri bildirim).
//
// Ekmek iki parçadır, tek envanter öğesidir: BOŞ yığına konan bütün ekmek ALT ekmektir — envanterden
// düşmez, elde YARIM kalır (BreadHalf); DOLU yığına yalnızca YARIM ekmek üst olarak konur ve hamburger
// tamamlanır: yığındaki katmanlar (+ üst ekmek) tek bir Hamburger öğesine kopyalanır, öğe Şef'in
// envanterine girer, yığın temizlenir.
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(HoldOrPressInteractable))]
public class BurgerAssemblyStation : NetworkBehaviour, IInteractionGate
{
    [Tooltip("Id -> ItemType (yığın katmanları tür id'si tutar; kategori ve görsel buradan çözülür).")]
    [SerializeField] private ItemRegistry registry;
    [Tooltip("Tamamlanınca üretilen öğenin türü (Hamburger; itemPrefab'ında BurgerAssembly olmalı).")]
    [SerializeField] private ItemType hamburgerType;
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

        if (!TryEvaluate(context, out var inventory, out var item, out var reason))
        {
            Debug.LogWarning($"[BurgerAssemblyStation] reddetti (clientId={context.ClientId}): {reason}.");
            return;
        }

        var itemType = item.Type;
        bool isBread = itemType.Category == ItemCategory.Ekmek;

        if (isBread && PlacedIngredients.Count == 0)
        {
            // ALT ekmek: ogeyi TUKETME — elde yarim kalir; yiginin tabani olarak katman yazilir.
            item.GetComponent<BreadHalf>().ServerMarkHalved();
            PlacedIngredients.Add(new BurgerLayerEntry(itemType.Id, 0));
            return;
        }

        if (isBread)
        {
            CompleteBurger(context, inventory, item);
            return;
        }

        // Sira: ONCE slottan cikarilir, SONRA despawn edilir (slot hicbir an despawn olmus bir ogeyi
        // gostermez). Tezgah malzemeyi ag nesnesi olarak TUTMAZ, yalnizca katman kaydi yazar. Slot,
        // ActiveSlotIndex DEGIL, baglamdan (bkz. InteractionContext.cs) — tiklama anindaki secili slot.
        if (!inventory.ServerTryTakeItemAt(context.SlotIndex, out var taken))
            return;

        // Koyulan ogenin fazi (orn. ciglik) kaydedilir: yigin ciglik rengini gosterir.
        int phaseIndex = taken.TryGetComponent(out ServerProgress progress) ? progress.PhaseIndex.Value : 0;
        PlacedIngredients.Add(new BurgerLayerEntry(itemType.Id, phaseIndex));
        ItemMover.Despawn(taken);
    }

    // UST ekmek (yarim ekmek): hamburger ogesi dogar, yigin katmanlari (+ ust ekmek) ogeye kopyalanir.
    // Sira: hamburger DOGAR -> katmanlar yazilir -> yarim ekmek slottan alinir -> hamburger, baglamdaki
    // slottan baslayan GDD 4.1 slot kuraliyla envantere girer (ekmegin az once bosalan slotu, secili
    // slot zaten oradadir) -> yigin temizlenir -> ekmek despawn edilir. Herhangi bir adim basarisiz olursa
    // geri alinir: yarim hamburger ortada kalmaz, ekmek yerinde durur.
    private void CompleteBurger(InteractionContext context, PlayerInventory inventory, Item topBread)
    {
        var burger = ItemMover.SpawnCarried(hamburgerType, transform.position);
        if (burger == null)
            return;

        if (!burger.TryGetComponent(out BurgerAssembly assembly))
        {
            Debug.LogError($"[BurgerAssemblyStation] '{hamburgerType.name}' itemPrefab'inda BurgerAssembly yok.", hamburgerType);
            ItemMover.Despawn(burger);
            return;
        }

        var layers = new List<BurgerLayerEntry>(PlacedIngredients.Count + 1);
        foreach (var layer in PlacedIngredients)
            layers.Add(layer);

        layers.Add(new BurgerLayerEntry(topBread.Type.Id, 0));
        assembly.ServerSetLayers(layers);

        if (!inventory.ServerTryTakeItemAt(context.SlotIndex, out var takenBread))
        {
            Debug.LogError($"[BurgerAssemblyStation] ust ekmek slottan alinamadi (clientId={context.ClientId}); hamburger geri alindi.");
            ItemMover.Despawn(burger);
            return;
        }

        if (!inventory.ServerTryAddItem(burger, context.SlotIndex))
        {
            Debug.LogError($"[BurgerAssemblyStation] hamburger envantere yazilamadi (clientId={context.ClientId}); ekmek geri kondu.");
            inventory.ServerTrySetItemAt(context.SlotIndex, takenBread);
            ItemMover.Despawn(burger);
            return;
        }

        PlacedIngredients.Clear();
        ItemMover.Despawn(takenBread);
    }

    // Kural TEK yerde (GDD 4.1.2, 6.3, 6.7.3): rol izinli, baglam slotunda bir malzeme var, kategori
    // sirasina ve ekmegin iki parcali kuralina uyuyor. Sunucu bunu tamamlanmada, crosshair her karede
    // (istemcide) sorar.
    public bool CanInteract(InteractionContext context, out string reason)
    {
        return TryEvaluate(context, out _, out _, out reason);
    }

    private bool TryEvaluate(InteractionContext context, out PlayerInventory inventory, out Item item, out string reason)
    {
        inventory = null;
        item = null;

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
        if (!inventory.TryGetItem(context.SlotIndex, out item))
        {
            reason = "elde malzeme yok";
            return false;
        }

        var itemType = item.Type;
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

        if (itemType.Category == ItemCategory.Ekmek)
            return TryEvaluateBread(context, inventory, item, out reason);

        reason = null;
        return true;
    }

    // Ekmegin iki parcali kurali (GDD 6.7.3): bos yigina yalnizca BUTUN ekmek (alt); dolu yigina
    // yalnizca YARIM ekmek (ust) — butun ekmek ust olamaz, yarim ekmek alt olamaz. Ust ekmek
    // hamburgeri tamamlar: hamburger turu spawn edilebilir olmali ve envanterde (ekmegin kendi slotu
    // disinda) bos slot bulunmali — yarim hamburger ortada kalmaz. Envanterin doluluğu Şef'in kendi
    // bilgisi, durum sizintisi degil.
    private bool TryEvaluateBread(InteractionContext context, PlayerInventory inventory, Item item, out string reason)
    {
        if (!item.TryGetComponent(out BreadHalf bread))
        {
            reason = "ekmek yarılanamıyor";
            return false;
        }

        bool stackEmpty = PlacedIngredients.Count == 0;
        if (stackEmpty && bread.IsHalved.Value)
        {
            reason = "yarım ekmek alt ekmek olamaz";
            return false;
        }

        if (!stackEmpty && !bread.IsHalved.Value)
        {
            reason = "üst ekmek yarılanmış olmalı";
            return false;
        }

        if (!stackEmpty)
        {
            if (hamburgerType == null || hamburgerType.ItemPrefab == null)
            {
                reason = "hamburger türü atanmamış";
                return false;
            }

            if (!inventory.HasFreeSlot(context.SlotIndex))
            {
                reason = "boş slot yok";
                return false;
            }
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
