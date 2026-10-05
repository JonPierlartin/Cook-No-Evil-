using UnityEngine;

// Kodla üretilen (klipsiz) bir el animasyonu — ad verilmiş, yeniden kullanılabilir VERİ. Artist klibi olmayan sinyal ve
// emote'lar bunu oynatır; artist klibi gelince sinyaldeki klip önceliklidir ve bu varlık yedek olarak yerinde kalır.
// Üç tür: yön gösterme (işaret parmağı), sayı (açık parmak sayısı) ve serbest poz (EmoteHandPose).
// Oynatan: ProceduralCharacterAnimator.Play.
[CreateAssetMenu(fileName = "ProceduralHandAnimation", menuName = "Cook No Evil/Procedural Hand Animation")]
public class ProceduralHandAnimation : ScriptableObject
{
    public enum Kind
    {
        Point,
        Count,
        Pose
    }

    [SerializeField] private Kind kind;
    [Tooltip("Point: işaret edilen yön (karakterin yerel uzayı: yukarı (0,1,0), kendi sağı (1,0,0)).")]
    [SerializeField] private Vector3 direction = Vector3.up;
    [Tooltip("Count: açılacak parmak sayısı.")]
    [SerializeField, Min(1)] private int count = 1;
    [Tooltip("Pose: elin yeri, yönü, parmakları ve salınımı.")]
    [SerializeField] private EmoteHandPose pose = new();

    public Kind AnimationKind => kind;
    public Vector3 Direction => direction;
    public int Count => count;
    public EmoteHandPose Pose => pose;
}
