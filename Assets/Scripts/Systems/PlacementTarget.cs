using UnityEngine;

// GDD 4.1.2 (2): bir yerlestirme hedefinin ISARETI — elindeki nesnenin onizlemesinin (PlacementPreview)
// gorunecegi nokta, bu bilesenin bulundugu nesnenin KENDI transform'udur. Pasif bir isaretleyicidir:
// onizlemenin gorunup gorunmeyecegini bu bilesen DEGIL, crosshair'in mevcut durumu
// (PlayerInteractor.CurrentTarget + Feedback) ve varsa hedefin ItemSlot'u belirler.
//
// Yuvada (ItemSlot) bu nesne yuvanin NetworkObject'idir: konan oge ona TrySetParent ile baglanir ve K7
// geregi parent duz bir Transform olamaz, o yuzden nokta ayri bir alanda tutulmaz. Yuvasiz hedefte
// (orn. tuketen birlestirme tezgahi) bu bilesen, onizleme noktasini gosteren bos bir cocuk nesneye
// konur; PlacementPreview hedefin cocuklarinda arar.
[DisallowMultipleComponent]
public class PlacementTarget : MonoBehaviour
{
    public Transform PlacementPoint => transform;
}
