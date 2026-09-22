using Unity.Netcode;
using UnityEngine;

// Bir ogenin DUNYA gorselini ServerProgress.PhaseIndex'e gore boyar (GDD 5.2.1: Cig/Pismis/Yanmis
// yer tutucu renkleri, ItemType.PhaseColors'tan). Elde tutulan AYRI KOPYA icin bkz. HeldItemVisual
// — o, bu bileseni DEGIL, dogrudan ServerProgress'i okuyup AYNI ItemPhaseColoring.Apply'i cagirir
// (kopya bu NetworkObject'in cocugu degildir, kendi yasam dongusunu yonetir).
//
// K2 (Sef'in korlugu): renk yalnizca RENK tamponuna yazilir, Sef'in kontur render'i (K2) derinlik+
// normal tamponundan uretildigi icin bu renk Sef'e ZATEN gorunmez — burada Sef icin ayrica bir sey
// YAPILMAZ (istisna degildir, K2'nin kendisi zaten yeterli).
[RequireComponent(typeof(Item))]
[RequireComponent(typeof(ServerProgress))]
public class ItemPhaseVisual : NetworkBehaviour
{
    private Item _item;
    private ServerProgress _progress;
    private Renderer[] _renderers;

    private void Awake()
    {
        _item = GetComponent<Item>();
        _progress = GetComponent<ServerProgress>();
        _renderers = GetComponentsInChildren<Renderer>(true);
    }

    public override void OnNetworkSpawn()
    {
        _progress.PhaseIndex.OnValueChanged += HandlePhaseChanged;
        ApplyCurrentColor();
    }

    public override void OnNetworkDespawn()
    {
        _progress.PhaseIndex.OnValueChanged -= HandlePhaseChanged;
    }

    private void HandlePhaseChanged(int previous, int current) => ApplyCurrentColor();

    private void ApplyCurrentColor() => ItemPhaseColoring.Apply(_renderers, _item.Type, _progress.PhaseIndex.Value);
}
