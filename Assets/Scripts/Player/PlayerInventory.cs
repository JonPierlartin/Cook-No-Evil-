using Unity.Netcode;
using UnityEngine;

// Generic 4-slotlu envanter — tum roller icin AYNI kavram (eskiden planlanan
// Yamak'a-ozel 3-durumlu CarryState enum'i yerine gecti). Slot degerleri
// IngredientType.Id referansidir, -1 bos slot demektir. Sadece server yazar;
// ItemHandoffSlot/PackagingStation/BurgerAssemblyStation gibi istasyonlarin
// ServerRpc'leri basarili oldugunda bu API'yi cagirir. ActiveSlotIndex ise
// sadece sahibinin (owner) etkiledigi bir secim oldugu icin
// NetworkVariableWritePermission.Owner ile dogrudan client tarafindan yazilir.
[RequireComponent(typeof(NetworkObject))]
public class PlayerInventory : NetworkBehaviour
{
    public const int SlotCount = 4;
    public const int EmptySlot = -1;

    public readonly NetworkList<int> Slots = new();

    public readonly NetworkVariable<int> ActiveSlotIndex =
        new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            Slots.Clear();
            for (int i = 0; i < SlotCount; i++)
                Slots.Add(EmptySlot);
        }
    }

    public void SetActiveSlot(int slotIndex)
    {
        if (!IsOwner || slotIndex < 0 || slotIndex >= SlotCount)
            return;

        ActiveSlotIndex.Value = slotIndex;
    }

    public bool ServerTryAddItem(int ingredientId)
    {
        if (!IsServer)
            return false;

        for (int i = 0; i < Slots.Count; i++)
        {
            if (Slots[i] != EmptySlot)
                continue;

            Slots[i] = ingredientId;
            return true;
        }

        return false;
    }

    public bool ServerTryRemoveItem(int slotIndex)
    {
        if (!IsServer || slotIndex < 0 || slotIndex >= Slots.Count || Slots[slotIndex] == EmptySlot)
            return false;

        Slots[slotIndex] = EmptySlot;
        return true;
    }

    // BurgerAssemblyStation/PackagingStation gibi "oyuncunun su an sectigi
    // malzemeyi kullan" davranisi icin uygunluk metodu.
    public bool ServerTryRemoveActiveItem(out int ingredientId)
    {
        ingredientId = EmptySlot;

        if (!IsServer)
            return false;

        int index = ActiveSlotIndex.Value;
        if (index < 0 || index >= Slots.Count || Slots[index] == EmptySlot)
            return false;

        ingredientId = Slots[index];
        Slots[index] = EmptySlot;
        return true;
    }
}
