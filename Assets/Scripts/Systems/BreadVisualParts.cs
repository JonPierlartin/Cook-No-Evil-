using UnityEngine;

// GDD 6.7.3 "Ekmeğin görseli": alt (düz) ve üst (kubbeli) ekmek AYRI modellerdir ama tek görsel
// prefab'ın (Ekmek_Visual) iki çocuğudur — ekmek modellerinin TEK kaynağı burasıdır. Elde, yuvada,
// önizlemede ve yığında hangi parçanın görüneceğini çağıran seçer (BreadHalf: bütün / yalnızca üst;
// BurgerStackBuilder: yığındaki konumdan alt / üst); prefab referansı başka hiçbir yerde tutulmaz.
//
// Prefab düzeni (K2d): kök tabanda; alt parça kökte, üst parça alt parçanın üstüne oturacak
// yükseklikte durur. Yalnızca üst gösterilirken üst parça tabana (kök) iner — elde ve yuvada havada
// durmaz. Ölçüm yapılmaz: yükseklik prefab'daki yerleşimden gelir.
[DisallowMultipleComponent]
public class BreadVisualParts : MonoBehaviour
{
    public enum Part
    {
        Whole,
        Bottom,
        Top
    }

    [Tooltip("Alt (düz) ekmek parçasının kökü; tabanı görsel kökünde.")]
    [SerializeField] private GameObject bottom;
    [Tooltip("Üst (kubbeli) ekmek parçasının kökü; bütün ekmekte alt parçanın üstünde durur.")]
    [SerializeField] private Transform top;

    private bool _restCaptured;
    private Vector3 _topRestPosition;

    public void Show(Part part)
    {
        // Awake'e dayanılmaz: kopya etkin olmayan bir ebeveynin altında doğarsa Awake gecikir. İlk
        // gösterimden önce üst parça henüz prefab'daki yerindedir.
        if (!_restCaptured)
        {
            _topRestPosition = top.localPosition;
            _restCaptured = true;
        }

        bottom.SetActive(part != Part.Top);
        top.gameObject.SetActive(part != Part.Bottom);
        top.localPosition = part == Part.Top ? Vector3.zero : _topRestPosition;
    }
}
