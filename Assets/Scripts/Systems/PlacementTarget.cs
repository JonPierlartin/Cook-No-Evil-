using UnityEngine;

// GDD 4.1.2 (2): bir yerlestirme hedefi — elindeki nesnenin onizlemesinin (PlacementPreview) ve
// konan nesnenin duracagi nokta. Pasif bir isaretleyicidir: onizlemenin gorunup gorunmeyecegini
// bu bilesen DEGIL, crosshair'in mevcut durumu (PlayerInteractor.CurrentTarget + Feedback) belirler.
//
// Hedefe nisan alinabilmesi icin (GDD 4.1.2: hedef secimi nisanla yapilir) bu bilesen bir
// HoldOrPressInteractable ile ayni nesnede durur; birden fazla yuvali istasyonlarda her yuva
// kendi PlacementTarget + HoldOrPressInteractable'ini tasir.
[DisallowMultipleComponent]
[RequireComponent(typeof(HoldOrPressInteractable))]
public class PlacementTarget : MonoBehaviour
{
    [Tooltip("Onizlemenin (ve konan nesnenin) duracagi nokta — orn. tezgahin ust yuzu. Bos birakilirsa bu nesnenin kendi transform'u kullanilir.")]
    [SerializeField] private Transform placementPoint;

    public Transform PlacementPoint => placementPoint != null ? placementPoint : transform;
}
