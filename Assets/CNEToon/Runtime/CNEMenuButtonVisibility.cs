using UnityEngine;

// Kopyalanan menü düğmesini şablonuyla birlikte gösterir / gizler (ilk ekran düğmeleri birlikte açılıp kapanır).
// Düğmenin kendisi kapalıyken Update çalışmayacağı için bu bileşen hep açık kalan kapsayıcıda durur.
public class CNEMenuButtonVisibility : MonoBehaviour
{
    public GameObject Target;
    public GameObject Follower;

    private void Update()
    {
        if (Target != null && Follower != null && Follower.activeSelf != Target.activeSelf)
            Follower.SetActive(Target.activeSelf);
    }
}
