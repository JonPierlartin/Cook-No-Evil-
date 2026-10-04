using UnityEngine;

// Karakterin kodla sürülen (prosedürel) animasyonu. Karakterler iskeletli değildir: gövde tek parça, ayaklar ve
// eller havada duran AYRI parçalardır — bu yüzden kol/bacak zinciri çözen bir IK gerekmez; her parça doğrudan
// hedefine taşınır. Hazır klip yoktur; hareket karakterin GERÇEK hızından üretilir, bu yüzden ileri, geri ve yan
// yürüyüş aynı koddan doğru yönde çıkar (ayaklar hareket yönünde adımlar, gövde o yöne eğilir).
//  - Yürüme: iki ayak zıt fazda; havadaki ayak ileri taşınır ve kalkar, yerdeki ayak geride kalır. Adım sıklığı
//    hızla orantılıdır (ayak kaymasın). Gövde adımla birlikte iner-kalkar ve sallanır; eller zıt ayakla salınır.
//  - Jest (yön sinyali, GDD 3.6): bir el göğüs/baş hizasına kalkıp İŞARET PARMAĞIYLA verilen yönü gösterir; el
//    yerinden aşağı inmez (pencere pervazının altında kalıp görünmez olmasın). Süre dışarıdan gelir.
//  - Tutma: elde öğe varken sağ el öğenin altına girer (avuç yukarı, parmaklar yarı kapalı). Alırken el öğeyle
//    birlikte önden gelir; bırakırken el öne uzanıp yerine döner.
// Öncelik: jest > tutma > yürüme salınımı. Tamamen yerel sunum: ağ verisi yoktur; uzak oyuncularda hız,
// NetworkTransform'un taşıdığı konumdan ölçülür.
public class ProceduralCharacterAnimator : MonoBehaviour
{
    [Header("Parçalar (dinlenme pozu prefab'daki yerleşimdir)")]
    [SerializeField] private Transform body;
    [SerializeField] private Transform leftFoot;
    [SerializeField] private Transform rightFoot;
    [Tooltip("El pivotu: +Z parmakların gösterdiği yön, +Y başparmak tarafı.")]
    [SerializeField] private Transform leftHand;
    [SerializeField] private Transform rightHand;
    [SerializeField] private HandPose leftHandPose;
    [SerializeField] private HandPose rightHandPose;

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
    [Tooltip("Jestte elin kalktığı merkez (karakterin yerel uzayı). Karşıdaki oyuncu pencereden görebilsin diye " +
        "pencere açıklığının içinde kalacak yükseklikte olmalı.")]
    [SerializeField] private Vector3 gestureCenter = new(0f, 1.55f, 0.6f);
    [Tooltip("Elin merkezden yana açıklığı (m): sağ el +X, sol el -X tarafında durur.")]
    [SerializeField, Min(0f)] private float gestureSideOffset = 0.25f;
    [Tooltip("Elin gösterilen yöne doğru kaydığı mesafe (m). Aşağı yönde el KAYMAZ, yalnızca parmak aşağı döner.")]
    [SerializeField, Min(0f)] private float gestureReach = 0.25f;
    [Tooltip("İşaret ederken elin yön boyunca ileri-geri vurgusu (m) ve hızı (devir/sn).")]
    [SerializeField, Min(0f)] private float gesturePump = 0.07f;
    [SerializeField, Min(0f)] private float gesturePumpRate = 2f;
    [Tooltip("Elin jeste girip çıkma süresi (sn).")]
    [SerializeField, Min(0.01f)] private float gestureBlendTime = 0.18f;

    [Header("Tutma (elde öğe)")]
    [Tooltip("Elin, öğenin tabanının ne kadar altında durduğu (m).")]
    [SerializeField] private float holdPalmOffset = 0.03f;
    [Tooltip("Elin tutma pozuna girme/çıkma süresi (sn).")]
    [SerializeField, Min(0.01f)] private float holdBlendTime = 0.14f;
    [Tooltip("Alırken öğe ve elin önden geldiği mesafe (m, ileri / aşağı) ve süresi (sn).")]
    [SerializeField] private Vector2 pickReach = new(0.35f, 0.12f);
    [SerializeField, Min(0.01f)] private float pickTime = 0.28f;
    [Tooltip("Bırakırken elin öne uzandığı mesafe (m) ve süresi (sn).")]
    [SerializeField, Min(0f)] private float placeReach = 0.3f;
    [SerializeField, Min(0.01f)] private float placeTime = 0.14f;

    [Header("Parmaklar (0 = açık, 1 = kapalı)")]
    [SerializeField, Range(0f, 1f)] private float restCurl = 0.18f;
    [SerializeField, Range(0f, 1f)] private float holdCurl = 0.45f;
    [Tooltip("Parmak pozunun değişme hızı.")]
    [SerializeField, Min(0.1f)] private float fingerSpeed = 8f;

    private struct Rest
    {
        public Vector3 Position;
        public Quaternion Rotation;
    }

    private struct Curl
    {
        public float Index, Others, Thumb;
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

    private Transform _holdTarget;
    private float _holdWeight;
    private float _pickElapsed;
    private float _placeElapsed = float.MaxValue;
    private Vector3 _holdLocalPosition;
    private Quaternion _holdLocalRotation = Quaternion.identity;

    private Curl _leftCurl, _rightCurl;

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
        _leftCurl = _rightCurl = new Curl { Index = restCurl, Others = restCurl, Thumb = restCurl };
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

    // Elde tutulan öğenin görseli (yoksa null). Her karede çağrılabilir; değişince alma / bırakma hareketi başlar.
    public void SetHoldTarget(Transform target)
    {
        if (target == _holdTarget)
            return;

        if (target != null)
            _pickElapsed = 0f;          // yeni öğe: alma hareketi
        else if (_holdTarget != null || _holdWeight > 0f)
            _placeElapsed = 0f;         // öğe gitti: bırakma hareketi

        _holdTarget = target;
    }

    // Alma sırasında öğenin (ve elin) tutma noktasına göre kayması: önden ve alttan gelip yerine oturur.
    // HeldItemVisual öğeyi bu kadar kaydırır; el öğeyi izlediği için ikisi birlikte hareket eder.
    public Vector3 GetHoldDisplacement(Transform anchor)
    {
        float t = Mathf.Clamp01(_pickElapsed / pickTime);
        float remaining = 1f - Mathf.SmoothStep(0f, 1f, t);
        return (anchor.forward * pickReach.x - anchor.up * pickReach.y) * remaining;
    }

    private void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f)
            return;

        // Oyun durduğunda (bölüm sonu ekranı, kopma duraklatması) karakter de donar: nefes ve salınım dahil hiçbir
        // şey oynamaz. Konum izlemesi sürer ki devam edince sahte bir hız sıçraması olmasın.
        if (!GameLoopManager.CanPlayersAct)
        {
            _lastWorldPosition = transform.position;
            return;
        }

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

        _pickElapsed += dt;
        if (_placeElapsed < float.MaxValue)
            _placeElapsed += dt;

        // Eller karşı ayakla birlikte salınır.
        ApplyHand(leftHand, leftHandPose, ref _leftCurl, _leftHandRest, _phase + Mathf.PI, direction, dt, false);
        ApplyHand(rightHand, rightHandPose, ref _rightCurl, _rightHandRest, _phase, direction, dt, true);
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

    private void ApplyHand(Transform hand, HandPose pose, ref Curl curl, Rest rest, float phase, Vector3 direction, float dt, bool holdsItems)
    {
        var position = rest.Position + direction * (Mathf.Sin(phase) * handSwing * _weight);
        var rotation = rest.Rotation;
        var targetCurl = new Curl { Index = restCurl, Others = restCurl, Thumb = restCurl };
        float side = hand == leftHand ? -1f : 1f;

        if (holdsItems)
            ApplyHold(side, dt, ref position, ref rotation, ref targetCurl);

        if (hand == _gestureHand)
            ApplyGesture(side, dt, ref position, ref rotation, ref targetCurl);

        hand.localPosition = position;
        hand.localRotation = rotation;

        float step = fingerSpeed * dt;
        curl.Index = Mathf.MoveTowards(curl.Index, targetCurl.Index, step);
        curl.Others = Mathf.MoveTowards(curl.Others, targetCurl.Others, step);
        curl.Thumb = Mathf.MoveTowards(curl.Thumb, targetCurl.Thumb, step);
        if (pose != null)
            pose.Apply(curl.Index, curl.Others, curl.Thumb);
    }

    private void ApplyHold(float side, float dt, ref Vector3 position, ref Quaternion rotation, ref Curl targetCurl)
    {
        bool holding = _holdTarget != null;
        if (holding)
        {
            // El öğenin tabanının hemen altında, avuç yukarı, parmaklar öğenin baktığı yöne.
            var worldPosition = _holdTarget.position - _holdTarget.up * holdPalmOffset;
            var worldRotation = Quaternion.LookRotation(_holdTarget.forward, _holdTarget.right * side);
            _holdLocalPosition = transform.InverseTransformPoint(worldPosition);
            _holdLocalRotation = Quaternion.Inverse(transform.rotation) * worldRotation;
            _holdWeight = Mathf.MoveTowards(_holdWeight, 1f, dt / holdBlendTime);
        }
        else if (_placeElapsed < placeTime)
        {
            // Bırakma: el son tutma pozundan öne uzanır (öğeyi yerine koyar gibi), ağırlık korunur.
            _holdWeight = Mathf.Max(_holdWeight, 0f);
        }
        else
        {
            _holdWeight = Mathf.MoveTowards(_holdWeight, 0f, dt / holdBlendTime);
        }

        if (_holdWeight <= 0f)
            return;

        var holdPosition = _holdLocalPosition;
        if (!holding)
        {
            float push = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_placeElapsed / placeTime));
            holdPosition += (_holdLocalRotation * Vector3.forward) * (placeReach * push);
        }

        float blend = Mathf.SmoothStep(0f, 1f, _holdWeight);
        position = Vector3.Lerp(position, holdPosition, blend);
        rotation = Quaternion.Slerp(rotation, _holdLocalRotation, blend);
        targetCurl = new Curl { Index = holdCurl, Others = holdCurl, Thumb = holdCurl * 0.6f };
    }

    private void ApplyGesture(float side, float dt, ref Vector3 position, ref Quaternion rotation, ref Curl targetCurl)
    {
        _gestureElapsed += dt;
        float blendIn = Mathf.Clamp01(_gestureElapsed / gestureBlendTime);
        float blendOut = Mathf.Clamp01((_gestureDuration - _gestureElapsed) / gestureBlendTime);
        float blend = Mathf.SmoothStep(0f, 1f, Mathf.Min(blendIn, blendOut));

        // El yalnızca yana ve yukarı kayar; aşağı kaymaz (aşağıyı parmak gösterir, el görünür yükseklikte kalır).
        var shift = new Vector3(_gestureDirection.x, Mathf.Max(0f, _gestureDirection.y), _gestureDirection.z) * gestureReach;
        float pump = Mathf.Sin(_gestureElapsed * gesturePumpRate * Mathf.PI * 2f) * gesturePump;
        var pointPosition = gestureCenter + Vector3.right * (side * gestureSideOffset) + shift + _gestureDirection * pump;

        // İşaret parmağı (+Z) gösterilen yöne bakar; yukarı/aşağı işaret ederken başparmak karakterin içine döner.
        var up = Mathf.Abs(_gestureDirection.y) > 0.9f ? Vector3.left * side : Vector3.up;
        var pointRotation = Quaternion.LookRotation(_gestureDirection, up);

        position = Vector3.Lerp(position, pointPosition, blend);
        rotation = Quaternion.Slerp(rotation, pointRotation, blend);
        // İşaret: işaret parmağı düz, diğerleri ve başparmak kapalı.
        targetCurl = new Curl { Index = 0f, Others = 1f, Thumb = 0.9f };

        if (_gestureElapsed >= _gestureDuration)
            _gestureHand = null;
    }
}
