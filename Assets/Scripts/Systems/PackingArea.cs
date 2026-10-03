using UnityEngine;

// Paketleme alanı (GDD 5.3.1): kese kağıdının ağzı açık durduğu yuva. Bir ItemSlot'tur (kağıdı koyma/alma aynı
// kural) ve ek olarak: alanda bir paket dururken elinde ürün olan oyuncu tıklarsa ürün PAKETİN İÇİNE girer.
// Paketten ürün geri alınamaz (GDD: "geri alma yok"). Faz 0'da pişmişlik/doluluk engeli YOKTUR — çiğ köfteli
// hamburger de girer; hata Komi'nin sorumluluğudur.
public class PackingArea : ItemSlot
{
    [Tooltip("Pakete girebilen ürün türleri. Boş bırakılırsa paket dışındaki her öğe girebilir.")]
    [SerializeField] private ItemType[] packableTypes;
    [Tooltip("Açıksa bir pakete aynı türden ikinci ürün girmez (Ersel, 3 Eki: pakete tek hamburger). Farklı " +
        "türden ürünler (ileride içecek, dondurma) eklenebilir.")]
    [SerializeField] private bool onePerType = true;

    protected override bool TryEvaluate(InteractionContext context, out PlayerInventory inventory, out Item occupant, out string reason)
    {
        if (!IsPacking(context, out inventory, out occupant, out var package, out var held))
            return base.TryEvaluate(context, out inventory, out occupant, out reason);

        if (onePerType && package.ContainsType(held.Type))
        {
            reason = "pakette bu üründen zaten var";
            return false;
        }

        // Pakete ürün koymak, alana öğe koymakla aynı rol kuralına tabidir.
        if (!CanRolePlace(context.ClientId))
        {
            reason = "rol pakete ürün koyamaz";
            return false;
        }

        reason = null;
        return true;
    }

    protected override void HandleInteractionCompleted(InteractionContext context)
    {
        if (!IsServer)
            return;

        if (!IsPacking(context, out var inventory, out _, out var package, out _))
        {
            base.HandleInteractionCompleted(context);
            return;
        }

        if (!TryEvaluate(context, out _, out _, out var reason))
        {
            Debug.LogWarning($"[PackingArea] '{name}' reddetti (clientId={context.ClientId}): {reason}.");
            return;
        }

        ItemMover.PackInto(package, inventory, context.SlotIndex);
    }

    // "Bu tıklama pakete ürün koyma mı": alanda bir paket var VE oyuncunun bağlam slotunda pakete girebilen bir
    // ürün var. Değilse tıklama normal yuva davranışıdır (kağıdı koy / al).
    private bool IsPacking(InteractionContext context, out PlayerInventory inventory, out Item occupant, out Package package, out Item held)
    {
        package = null;
        held = null;
        inventory = PlayerInventory.FindForClient(context.ClientId);
        if (!TryGetOccupant(out occupant) || inventory == null)
            return false;

        return occupant.TryGetComponent(out package)
            && inventory.TryGetItem(context.SlotIndex, out held)
            && IsPackable(held);
    }

    private bool IsPackable(Item item)
    {
        // Paket pakete girmez.
        if (item.Type == null || item.TryGetComponent<Package>(out _))
            return false;

        return packableTypes == null || packableTypes.Length == 0 || System.Array.IndexOf(packableTypes, item.Type) >= 0;
    }
}
