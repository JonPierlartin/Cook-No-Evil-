// Bir etkilesim isteginin "kim + hangi slot" bilgisini TEK deger olarak tasir (Duzeltme "etkilesime
// tiklama anindaki secili slotu tasi", 22 Eyl 2026). Sahibin yazdigi ActiveSlotIndex (NetworkVariable)
// ile ardindan gonderilen bir ServerRpc arasinda SIRA GARANTISI YOKTUR (bkz. CLAUDE.md NGO tuzaklari) —
// bu yuzden etkilesim yolunda (gate'ler, ItemMover, istasyonlarin tamamlanma mantigi) ActiveSlotIndex
// hic OKUNMAZ; tiklama anindaki secili slot RPC'nin KENDI parametresiyle tasinir ve bu deger tum
// zincir boyunca degismeden akar.
//
// Ag uzerinden GONDERILMEZ (INetworkSerializeByMemcpy DEGIL): sunucu tarafinda yalnizca dogrulanmis
// SenderClientId + RPC'nin int slotIndex parametresinden KURULUR (bkz. PlayerInteractor.
// RequestInteractServerRpc); istemci tarafinda ise crosshair/onizleme kendi YEREL secili slotuyla
// aynen kurar. ClientId asla istemciden gelen bir struct alanindan OKUNMAZ — her zaman
// ServerRpcParams.Receive.SenderClientId'den.
public readonly struct InteractionContext
{
    public readonly ulong ClientId;
    public readonly int SlotIndex;

    public InteractionContext(ulong clientId, int slotIndex)
    {
        ClientId = clientId;
        SlotIndex = slotIndex;
    }
}
