using UnityEngine;

// Karakterin kodla sürülen (prosedürel) animasyonu. Karakterler iskeletli değildir: gövde tek parça, ayaklar ve
// eller havada duran AYRI parçalardır — bu yüzden kol/bacak zinciri çözen bir IK gerekmez; her parça doğrudan
// hedefine taşınır. Hazır klip yoktur; hareket karakterin GERÇEK hızından üretilir, bu yüzden ileri, geri ve yan
// yürüyüş aynı koddan doğru yönde çıkar (ayaklar hareket yönünde adımlar, gövde o yöne eğilir).
//  - Yürüme: iki ayak zıt fazda; havadaki ayak ileri taşınır ve kalkar, yerdeki ayak geride kalır. Adım sıklığı
//    hızla orantılıdır (ayak kaymasın). Gövde adımla birlikte hafifçe iner-kalkar ve sallanır; eller zıt ayakla
//    birlikte salınır.
//  - Jest (yön sinyali, GDD 3.6): bir el göğüs önüne kalkıp verilen yönü işaret eder; süre dışarıdan gelir
//    (SignalValue). Jest sırasında o elin yürüme salınımı durur.
// Tamamen yerel sunum: ağ verisi yoktur; uzak oyuncularda hız, NetworkTransform'un taşıdığı konumdan ölçülür.
public class ProceduralCharacterAnimator : MonoBehaviour
{
    [Header("Parçalar (dinlenme pozu prefab'daki yerleşimdir)")]
    [SerializeField] private Transform body;
    [SerializeField] private Transform leftFoot;
    [SerializeField] private Transform rightFoot;
    [Tooltip("El pivotu: +Z parmakların gösterdiği yön, +Y başparmak tarafı.")]
    [SerializeField] private Transform leftHand;
    [SerializeField] private Transform rightHand;

    [Header("Yürüme")]
    [Tooltip("Bir adımda ayağın dinlenme noktasından ileri/geri gittiği mesafe (m).")]
    [SerializeField, Min(0.01f)] private float strideLength = 0.28f;
    [Tooltip("Havadaki ayağın en fazla yüksekliği (m).")]
    [SerializeField, Min(0f)] private float stepHeight = 0.12f;
    [Tooltip("Havadaki ayağın burnunun kalkma açısı (derece).")]
    [SerializeField] private float footPitch = 18f;
    [Tooltip("Bu hızda (m/sn) yürüyüş tam genliğe ulaşır; altında adımlar küçülür.")]
    [SerializeField, Min(0.01f)] private float fullStrideSpeed = 1.5f;
    [Tooltip("Gövdenin adım başına inip kalkması (m).")]
    [SerializeField, Min(0f)] private float bodyBob = 0.035f;
    [Tooltip("Gövdenin hareket yönüne eğilmesi (derece).")]
    [SerializeField] private float bodyLean = 5f;
    [Tooltip("Gövdenin adımla yana sallanması (derece).")]
    [SerializeField] private float bodySway = 3f;
    [Tooltip("Ellerin yürürken ileri/geri salınımı (m).")]
    [SerializeField, Min(0f)] private float handSwing = 0.14f;
    [Tooltip("Hız değişimine uyum hızı (büyük = daha çabuk).")]
    [SerializeField, Min(0.1f)] private float responsiveness = 10f;

    [Header("Dururken")]
    [Tooltip("Nefes: gövdenin dururken inip kalkması (m) ve hızı (devir/sn).")]
    [SerializeField, Min(0f)] private float idleBob = 0.012f;
    [SerializeField, Min(0f)] private float idleRate = 0.35f;

    [Header("Jest (yön sinyali)")]
    [Tooltip("Jestte elin kalktığı merkez (karakterin yerel uzayı): göğsün önü.")]
    [SerializeField] private Vector3 gestureCenter = new(0f, 1.1f, 0.6f);
    [Tooltip("Elin merkezden yana açıklığı (m): sağ el +X, sol el -X tarafında durur.")]
    [SerializeField, Min(0f)] private float gestureSideOffset = 0.25f;
    [Tooltip("Elin gösterilen yöne doğru uzandığı mesafe (m).")]
    [SerializeField, Min(0f)] private float gestureReach = 0.3f;
    [Tooltip("İşaret ederken elin yön boyunca ileri-geri vurgusu (m) ve hızı (devir/sn).")]
    [SerializeField, Min(0f)] private float gesturePump = 0.07f;
    [SerializeField, Min(0f)] private float gesturePumpRate = 2f;
    [Tooltip("Elin jeste girip çıkma süresi (sn).")]
    [SerializeField, Min(0.01f)] private float gestureBlendTime = 0.18f;

    private struct Rest
    {
        public Vector3 Position;
        public Quaternion Rotation;
    }

    private Rest _bodyRest, _leftFootRest, _rightFootRest, _leftHandRest, _rightHandRest;
    private Vector3 _lastWorldPosition;
    private Vector3 _localVelocity;
    private float _phase;
    private float _weight;

    private Transform _gestureHand;
    private Vector3 _gestureDirection;
    private float _gestureElapsed;
    private float _gestureDuration;

    // Sahibinin birinci şahıs görüşünde görünür kalması gereken parçalar (eller) için.
    public Transform LeftHand => leftHand;
    public Transform RightHand => rightHand;

    private void Awake()
    {
        _bodyRest = Capture(body);
        _leftFootRest = Capture(leftFoot);
        _rightFootRest = Capture(rightFoot);
        _leftHandRest = Capture(leftHand);
        _rightHandRest = Capture(rightHand);
        _lastWorldPosition = transform.position;
    }

    private void OnEnable()
    {
        _lastWorldPosition = transform.position;
        _localVelocity = Vector3.zero;
    }

    private static Rest Capture(Transform part) => new() { Position = part.localPosition, Rotation = part.localRotation };

    // Yön, karakterin yerel uzayındadır (ör. yukarı = (0,1,0), karakterin sağı = (1,0,0)).
    public void PlayGesture(Vector3 localDirection, float duration)
    {
        if (localDirection.sqrMagnitude < 0.0001f || duration <= 0f)
            return;

        _gestureDirection = localDirection.normalized;
        // Gösterilen taraftaki el kullanılır; yukarı/aşağı gibi yansız yönlerde sağ el.
        _gestureHand = _gestureDirection.x < -0.3f ? leftHand : rightHand;
        _gestureElapsed = 0f;
        _gestureDuration = duration;
    }

    public void CancelGesture()
    {
        // Kalan süreyi çıkış geçişine indir: el bir anda ışınlanmaz, hızla yerine döner.
        if (_gestureHand != null)
            _gestureElapsed = Mathf.Max(_gestureElapsed, _gestureDuration - gestureBlendTime);
    }

    private void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f)
            return;

        // Gerçek hız, karakterin yerel uzayında ve yatayda: ileri (+Z), geri (-Z), yan (±X).
        var worldDelta = transform.position - _lastWorldPosition;
        _lastWorldPosition = transform.position;
        worldDelta.y = 0f;
        var measured = transform.InverseTransformDirection(worldDelta / dt);
        // Işınlanma (doğma, yeniden başlatma) adım üretmesin.
        if (measured.sqrMagnitude > 400f)
            measured = Vector3.zero;

        _localVelocity = Vector3.Lerp(_localVelocity, measured, Mathf.Clamp01(responsiveness * dt));
        float speed = _localVelocity.magnitude;
        var direction = speed > 0.01f ? _localVelocity / speed : Vector3.forward;

        float targetWeight = Mathf.Clamp01(speed / fullStrideSpeed);
        _weight = Mathf.MoveTowards(_weight, targetWeight, responsiveness * 0.5f * dt);

        // Bir tam döngü = iki adım; ayak yerdeyken hareket hızında geriye kayar (kayma olmasın diye sıklık hıza bağlı).
        _phase += speed / (2f * strideLength) * dt * Mathf.PI;
        if (_phase > Mathf.PI * 2f)
            _phase -= Mathf.PI * 2f;

        ApplyFoot(leftFoot, _leftFootRest, _phase, direction);
        ApplyFoot(rightFoot, _rightFootRest, _phase + Mathf.PI, direction);
        ApplyBody(direction);

        // Eller karşı ayakla birlikte salınır.
        ApplyHand(leftHand, _leftHandRest, _phase + Mathf.PI, direction, dt);
        ApplyHand(rightHand, _rightHandRest, _phase, direction, dt);
    }

    private void ApplyFoot(Transform foot, Rest rest, float phase, Vector3 direction)
    {
        float along = Mathf.Sin(phase);
        // cos > 0: ayak ileri taşınıyor (havada); cos < 0: yerde, geride kalıyor.
        float swing = Mathf.Max(0f, Mathf.Cos(phase));

        foot.localPosition = rest.Position
            + direction * (along * strideLength * _weight)
            + Vector3.up * (swing * stepHeight * _weight);

        // Havadaki ayağın burnu hareket yönünde kalkar (hareket yönüne dik eksen etrafında).
        var axis = Vector3.Cross(Vector3.up, direction);
        foot.localRotation = Quaternion.AngleAxis(-swing * footPitch * _weight, axis) * rest.Rotation;
    }

    private void ApplyBody(Vector3 direction)
    {
        // Adım başına iki kez iner-kalkar; dururken yavaş nefes.
        float bob = -Mathf.Abs(Mathf.Sin(_phase)) * bodyBob * _weight
            + Mathf.Sin(Time.time * idleRate * Mathf.PI * 2f) * idleBob * (1f - _weight);
        body.localPosition = _bodyRest.Position + Vector3.up * bob;

        var leanAxis = Vector3.Cross(Vector3.up, direction);
        var lean = Quaternion.AngleAxis(bodyLean * _weight, leanAxis);
        var sway = Quaternion.AngleAxis(Mathf.Sin(_phase) * bodySway * _weight, direction);
        body.localRotation = sway * lean * _bodyRest.Rotation;
    }

    private void ApplyHand(Transform hand, Rest rest, float phase, Vector3 direction, float dt)
    {
        var walkPosition = rest.Position + direction * (Mathf.Sin(phase) * handSwing * _weight);
        var walkRotation = rest.Rotation;

        if (hand != _gestureHand)
        {
            hand.localPosition = walkPosition;
            hand.localRotation = walkRotation;
            return;
        }

        _gestureElapsed += dt;
        float blendIn = Mathf.Clamp01(_gestureElapsed / gestureBlendTime);
        float blendOut = Mathf.Clamp01((_gestureDuration - _gestureElapsed) / gestureBlendTime);
        float blend = Mathf.SmoothStep(0f, 1f, Mathf.Min(blendIn, blendOut));

        float side = hand == leftHand ? -1f : 1f;
        float pump = Mathf.Sin(_gestureElapsed * gesturePumpRate * Mathf.PI * 2f) * gesturePump;
        var pointPosition = gestureCenter + Vector3.right * (side * gestureSideOffset) + _gestureDirection * (gestureReach + pump);

        // Parmaklar (+Z) gösterilen yöne bakar; yukarı/aşağı işaret ederken başparmak karakterin içine döner.
        var up = Mathf.Abs(_gestureDirection.y) > 0.9f ? Vector3.left * side : Vector3.up;
        var pointRotation = Quaternion.LookRotation(_gestureDirection, up);

        hand.localPosition = Vector3.Lerp(walkPosition, pointPosition, blend);
        hand.localRotation = Quaternion.Slerp(walkRotation, pointRotation, blend);

        if (_gestureElapsed >= _gestureDuration)
            _gestureHand = null;
    }
}
