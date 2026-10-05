using UnityEngine;

// Karakterin kodla sürülen (prosedürel) animasyonu. Karakterler iskeletli değildir: gövde tek parça, ayaklar ve
// eller havada duran AYRI parçalardır — bu yüzden kol/bacak zinciri çözen bir IK gerekmez; her parça doğrudan
// hedefine taşınır. Hazır klip yoktur; hareket karakterin GERÇEK hızından üretilir, bu yüzden ileri, geri ve yan
// yürüyüş aynı koddan doğru yönde çıkar (ayaklar hareket yönünde adımlar, gövde o yöne eğilir).
//  - Yürüme: iki ayak zıt fazda; havadaki ayak ileri taşınır ve kalkar, yerdeki ayak geride kalır. Adım sıklığı
//    hızla orantılıdır (ayak kaymasın). Gövde adımla birlikte iner-kalkar ve sallanır; eller zıt ayakla salınır.
//  - Jest (yön sinyali, GDD 3.6): bir el göğüs/baş hizasına kalkıp İŞARET PARMAĞIYLA verilen yönü gösterir; el
//    yerinden aşağı inmez (pencere pervazının altında kalıp görünmez olmasın). Süre dışarıdan gelir.
//  - Sayı (sayı sinyali): eller jest merkezinde, avuç karşıya; sayı kadar parmak açılır (bir elde dört parmak
//    var; fazlası sol ele taşar).
//  - Klip (artist'in el animasyonu): klip, gizli bir iskelet kopyasında örneklenir; bileğin dinlenmeye göre
//    yer değiştirmesi, elin yönü ve parmak dönüşleri karakterin eline aktarılır. Klip tek karakter (ketçap) üstünde
//    ve sağ el için yapıldığından konum MUTLAK alınmaz: hareket bizim elin dinlenme yerine eklenir, böylece üç
//    karakterde de çalışır; aynalanınca sol elle oynar.
//  - Emote (genel çark, GDD 3.6.0): el hareketi veridir (EmoteHandPose); el oraya gider, parmaklar pozu alır,
//    salınır. Tek elli emote sağ elle oynar; sağ elde öğe varsa sol elle (aynalı).
//  - Tutma: elde öğe varken sağ el öğenin altına girer (avuç yukarı, parmaklar yarı kapalı). Alırken el öğeyle
//    birlikte önden gelir; bırakırken el öne uzanıp yerine döner.
// Öncelik: klip > jest > emote > tutma > yürüme salınımı. Tamamen yerel sunum: ağ verisi yoktur; uzak oyuncularda hız,
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

    [Tooltip("Adım bu yürüyüş ağırlığının altındaysa (durmaya yakın) adım sayılmaz — ses çıkmaz.")]
    [SerializeField, Range(0f, 1f)] private float stepSoundMinWeight = 0.25f;

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

    [Header("Yüz")]
    [Tooltip("Sağ kaşın dış ucu (karakterin yerel uzayı, gövde yüzeyinde). Yüze dokunan emote'lar (selam) buna göre " +
        "yerleşir; sol el için X'te aynalanır. Karakterlerin yüzü gövdenin farklı yerinde olduğu için prefab başınadır.")]
    [SerializeField] private Vector3 browPoint = new(0.15f, 1.35f, 0.3f);

    [Header("Klip (artist'in el animasyonu)")]
    [Tooltip("Kliplerin örneklendiği gizli iskelet: kökünün altında klip yollarının başladığı düğüm (rig.001) durur. " +
        "Görünmez; yalnızca kemik dönüşümleri okunur.")]
    [SerializeField] private Transform clipRigPrefab;
    [Tooltip("İskelette bilek, orta parmak kökü ve başparmak kökü kemiklerinin adları (elin yeri ve yönü bunlardan çıkar).")]
    [SerializeField] private string clipWristBone = "DEF-hand.L";
    [SerializeField] private string clipKnuckleBone = "DEF-f_middle.01.L";
    [SerializeField] private string clipThumbBone = "DEF-thumb.01.L";
    [Tooltip("Parmak kemiklerinin ad ön ekleri: bu adlı kemiklerin dönüşü iskeletten ele kopyalanır.")]
    [SerializeField] private string[] clipFingerPrefixes = { "DEF-f_", "DEF-thumb" };
    [Tooltip("Açıksa klibin en uç noktası jest merkezinin yüksekliğine kaldırılır. Artist klipleri bel hizasında " +
        "oynuyor; karşıdaki oyuncu pencere pervazının üstünden görebilsin diye el yukarı taşınır.")]
    [SerializeField] private bool liftClipToGestureHeight = true;

    [Header("Tutma (elde öğe)")]
    [Tooltip("Avuç yüzeyinin, öğenin tabanının ne kadar altında durduğu (m).")]
    [SerializeField] private float holdPalmOffset = 0.02f;
    [Tooltip("Avuç ortasının bilekten (el pivotundan) parmak yönünde uzaklığı (m). Öğe bileğin değil AVUCUN ortasına " +
        "otursun diye el bu kadar geriye çekilir.")]
    [SerializeField] private float holdPalmForward = 0.1f;
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
    [Tooltip("Öğe tutarken parmakların kıvrımı: düşük = açık, rahat avuç.")]
    [SerializeField, Range(0f, 1f)] private float holdCurl = 0.1f;
    [SerializeField, Range(0f, 1f)] private float holdThumbCurl = 0.05f;
    [Tooltip("Parmak pozunun değişme hızı.")]
    [SerializeField, Min(0.1f)] private float fingerSpeed = 8f;

    private struct Rest
    {
        public Vector3 Position;
        public Quaternion Rotation;
    }

    private struct Curl
    {
        // Others = orta parmak.
        public float Index, Others, Pinky, Thumb;
    }

    private Rest _bodyRest, _leftFootRest, _rightFootRest, _leftHandRest, _rightHandRest;
    private Vector3 _lastWorldPosition;
    private Vector3 _localVelocity;
    private float _phase;
    private bool _leftFootAirborne;
    private float _weight;

    private Transform _gestureHand;
    private Vector3 _gestureDirection;
    private float _gestureElapsed;
    private float _gestureDuration;

    private Bounds _bodyBounds;
    private bool _hasBodyBounds;
    // Bir eldeki parmak sayısı (el modeli: işaret, orta, serçe, başparmak) — sayı jesti bu sırayla açar.
    private const int FingersPerHand = 4;

    private int _count;
    private float _countElapsed;
    private float _countDuration;

    private struct BonePair
    {
        public Transform Hand;
        public Transform Rig;
    }

    private AnimationClip _clip;
    private float _clipElapsed;
    private float _clipDuration;
    private bool _clipMirrored;
    private float _clipLift;
    private float _clipPeakDistance;
    private Vector3 _clipWristStart;
    private Transform _clipRig;
    private Transform _clipWrist, _clipKnuckle, _clipThumb;
    private BonePair[] _leftFingerPairs, _rightFingerPairs;

    private EmoteHandPose _emote;
    private float _emoteElapsed;
    private float _emoteDuration;

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

    // Yere basan her adımda bir artar (adım sesi bunu izler). Yalnızca gerçekten yürürken sayar.
    public int StepCount { get; private set; }

    private void Awake()
    {
        _bodyRest = Capture(body);
        _leftFootRest = Capture(leftFoot);
        _rightFootRest = Capture(rightFoot);
        _leftHandRest = Capture(leftHand);
        _rightHandRest = Capture(rightHand);
        _lastWorldPosition = transform.position;
        _leftCurl = _rightCurl = new Curl { Index = restCurl, Others = restCurl, Pinky = restCurl, Thumb = restCurl };
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
        if (_emote != null)
            _emoteElapsed = Mathf.Max(_emoteElapsed, _emoteDuration - gestureBlendTime);
        if (_count > 0)
            _countElapsed = Mathf.Max(_countElapsed, _countDuration - gestureBlendTime);
        if (_clip != null)
            _clipElapsed = Mathf.Max(_clipElapsed, _clipDuration - gestureBlendTime);
    }

    // Kodla üretilen el animasyonu (ad verilmiş veri): türüne göre yön / sayı / poz oynatır.
    public void Play(ProceduralHandAnimation animation, float duration)
    {
        if (animation == null)
            return;

        switch (animation.AnimationKind)
        {
            case ProceduralHandAnimation.Kind.Point:
                PlayGesture(animation.Direction, duration);
                break;
            case ProceduralHandAnimation.Kind.Count:
                PlayCount(animation.Count, duration);
                break;
            default:
                PlayEmote(animation.Pose, duration);
                break;
        }
    }

    // Artist'in el klibi. duration: klibin oynayacağı süre (hız buradan çıkar). mirrored: sol elle, aynalı.
    public void PlayClip(AnimationClip clip, float duration, bool mirrored)
    {
        if (clip == null || duration <= 0f || !EnsureClipRig())
            return;

        _clip = clip;
        _clipElapsed = 0f;
        _clipDuration = duration;
        _clipMirrored = mirrored;

        // Klibin dinlenme noktası ve en uzak noktası (bir kez, başlarken): el oraya vardığında tam yüksekliğe çıkar.
        _clipWristStart = SampleWrist(0f);
        var peakDelta = Vector3.zero;
        const int probes = 16;
        for (int i = 1; i < probes; i++)
        {
            var delta = SampleWrist(clip.length * i / probes) - _clipWristStart;
            if (delta.sqrMagnitude > peakDelta.sqrMagnitude)
                peakDelta = delta;
        }

        _clipPeakDistance = peakDelta.magnitude;
        var rest = mirrored ? _leftHandRest : _rightHandRest;
        _clipLift = liftClipToGestureHeight ? Mathf.Max(0f, gestureCenter.y - (rest.Position.y + peakDelta.y)) : 0f;
    }

    private bool EnsureClipRig()
    {
        if (_clipRig != null)
            return true;

        if (clipRigPrefab == null)
            return false;

        _clipRig = Instantiate(clipRigPrefab, transform);
        _clipRig.localPosition = Vector3.zero;
        _clipRig.localRotation = Quaternion.identity;

        var rigBones = new System.Collections.Generic.Dictionary<string, Transform>();
        foreach (var bone in _clipRig.GetComponentsInChildren<Transform>(true))
        {
            if (!rigBones.ContainsKey(bone.name))
                rigBones.Add(bone.name, bone);
        }

        rigBones.TryGetValue(clipWristBone, out _clipWrist);
        rigBones.TryGetValue(clipKnuckleBone, out _clipKnuckle);
        rigBones.TryGetValue(clipThumbBone, out _clipThumb);
        if (_clipWrist == null || _clipKnuckle == null || _clipThumb == null)
        {
            Debug.LogError($"[ProceduralCharacterAnimator] '{name}': klip iskeletinde bilek/parmak kemikleri bulunamadı; klipler oynatılamaz.", this);
            Destroy(_clipRig.gameObject);
            _clipRig = null;
            return false;
        }

        _leftFingerPairs = CollectFingerPairs(leftHand, rigBones);
        _rightFingerPairs = CollectFingerPairs(rightHand, rigBones);
        return true;
    }

    private BonePair[] CollectFingerPairs(Transform hand, System.Collections.Generic.Dictionary<string, Transform> rigBones)
    {
        var pairs = new System.Collections.Generic.List<BonePair>();
        foreach (var bone in hand.GetComponentsInChildren<Transform>(true))
        {
            bool isFinger = false;
            foreach (var prefix in clipFingerPrefixes)
                isFinger |= bone.name.StartsWith(prefix);

            if (isFinger && rigBones.TryGetValue(bone.name, out var rigBone))
                pairs.Add(new BonePair { Hand = bone, Rig = rigBone });
        }

        return pairs.ToArray();
    }

    // Klibi 'time' anında iskelete uygular ve bileğin karakter uzayındaki yerini döndürür.
    private Vector3 SampleWrist(float time)
    {
        _clip.SampleAnimation(_clipRig.gameObject, time);
        return ClipToCharacter(_clipRig.InverseTransformPoint(_clipWrist.position));
    }

    // Artist dosyasında karakter -Z'ye bakar; bizde +Z. Y ekseni çevresinde yarım tur (x ve z işaret değiştirir);
    // aynalı oynatmada x bir kez daha çevrilir.
    private Vector3 ClipToCharacter(Vector3 value)
    {
        return new Vector3(_clipMirrored ? value.x : -value.x, value.y, -value.z);
    }

    // Sayı sinyali: 'count' kadar parmak açılır.
    public void PlayCount(int count, float duration)
    {
        if (count <= 0 || duration <= 0f)
            return;

        _count = Mathf.Min(count, FingersPerHand * 2);
        _countElapsed = 0f;
        _countDuration = duration;
    }

    // Genel emote'un el hareketi (veri). Poz kapalıysa bir şey oynamaz.
    public void PlayEmote(EmoteHandPose pose, float duration)
    {
        if (pose == null || !pose.Enabled || duration <= 0f)
            return;

        _emote = pose;
        _emoteElapsed = 0f;
        _emoteDuration = duration;
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

        // Ayaklar zıt fazdadır: sol ayağın havada/yerde durumu her değiştiğinde bir ayak yere basmıştır.
        bool leftAirborne = Mathf.Cos(_phase) > 0f;
        if (leftAirborne != _leftFootAirborne)
        {
            _leftFootAirborne = leftAirborne;
            if (_weight > stepSoundMinWeight)
                StepCount++;
        }

        ApplyFoot(leftFoot, _leftFootRest, _phase, direction);
        ApplyFoot(rightFoot, _rightFootRest, _phase + Mathf.PI, direction);
        ApplyBody(direction);

        _pickElapsed += dt;
        if (_placeElapsed < float.MaxValue)
            _placeElapsed += dt;

        if (_emote != null)
            _emoteElapsed += dt;
        if (_count > 0)
            _countElapsed += dt;
        if (_clip != null)
            _clipElapsed += dt;

        // Eller karşı ayakla birlikte salınır.
        ApplyHand(leftHand, leftHandPose, ref _leftCurl, _leftHandRest, _phase + Mathf.PI, direction, dt, false);
        ApplyHand(rightHand, rightHandPose, ref _rightCurl, _rightHandRest, _phase, direction, dt, true);

        if (_emote != null && _emoteElapsed >= _emoteDuration)
            _emote = null;
        if (_count > 0 && _countElapsed >= _countDuration)
            _count = 0;
        if (_clip != null && _clipElapsed >= _clipDuration)
            _clip = null;
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
        var targetCurl = new Curl { Index = restCurl, Others = restCurl, Pinky = restCurl, Thumb = restCurl };
        float side = hand == leftHand ? -1f : 1f;

        if (holdsItems)
            ApplyHold(side, dt, ref position, ref rotation, ref targetCurl);

        if (_emote != null && UsesEmote(side))
            ApplyEmote(side, ref position, ref rotation, ref targetCurl);

        if (_count > 0 && RaisedFingers(side) > 0)
            ApplyCount(side, ref position, ref rotation, ref targetCurl);

        if (hand == _gestureHand)
            ApplyGesture(side, dt, ref position, ref rotation, ref targetCurl);

        bool clipHand = _clip != null && (side < 0f) == _clipMirrored;
        float clipBlend = 0f;
        if (clipHand)
            clipBlend = ApplyClip(side, ref position, ref rotation);

        hand.localPosition = position;
        hand.localRotation = rotation;

        float step = fingerSpeed * dt;
        curl.Index = Mathf.MoveTowards(curl.Index, targetCurl.Index, step);
        curl.Others = Mathf.MoveTowards(curl.Others, targetCurl.Others, step);
        curl.Pinky = Mathf.MoveTowards(curl.Pinky, targetCurl.Pinky, step);
        curl.Thumb = Mathf.MoveTowards(curl.Thumb, targetCurl.Thumb, step);
        if (pose != null)
            pose.Apply(curl.Index, curl.Others, curl.Pinky, curl.Thumb);

        // Klip oynarken parmakları klip sürer (kemik dönüşleri iskeletten kopyalanır).
        if (clipHand)
        {
            foreach (var pair in side < 0f ? _leftFingerPairs : _rightFingerPairs)
                pair.Hand.localRotation = Quaternion.Slerp(pair.Hand.localRotation, pair.Rig.localRotation, clipBlend);
        }
    }

    // Klibin o anki bilek hareketini ve el yönünü uygular; geçiş ağırlığını döndürür.
    private float ApplyClip(float side, ref Vector3 position, ref Quaternion rotation)
    {
        float blendIn = Mathf.Clamp01(_clipElapsed / gestureBlendTime);
        float blendOut = Mathf.Clamp01((_clipDuration - _clipElapsed) / gestureBlendTime);
        float blend = Mathf.SmoothStep(0f, 1f, Mathf.Min(blendIn, blendOut));

        float time = Mathf.Clamp01(_clipElapsed / _clipDuration) * _clip.length;
        var wrist = SampleWrist(time);
        var knuckle = ClipToCharacter(_clipRig.InverseTransformPoint(_clipKnuckle.position));
        var thumb = ClipToCharacter(_clipRig.InverseTransformPoint(_clipThumb.position));

        // Konum: bileğin klipteki dinlenmeye göre yer değiştirmesi, bizim elin dinlenme yerine eklenir. El uzaklaştıkça
        // jest yüksekliğine doğru kaldırılır (uç noktada tam).
        var delta = wrist - _clipWristStart;
        float reach = _clipPeakDistance > 0.0001f ? Mathf.Clamp01(delta.magnitude / _clipPeakDistance) : 0f;
        var rest = side < 0f ? _leftHandRest : _rightHandRest;
        var clipPosition = rest.Position + delta + Vector3.up * (_clipLift * Mathf.SmoothStep(0f, 1f, reach));

        // Yön: parmaklar bilekten orta parmak köküne, başparmak tarafı bilekten başparmak köküne bakar.
        var clipRotation = Quaternion.LookRotation(knuckle - wrist, thumb - wrist);

        position = Vector3.Lerp(position, clipPosition, blend);
        rotation = Quaternion.Slerp(rotation, clipRotation, blend);
        return blend;
    }

    private void ApplyHold(float side, float dt, ref Vector3 position, ref Quaternion rotation, ref Curl targetCurl)
    {
        bool holding = _holdTarget != null;
        if (holding)
        {
            // El öğenin tabanının hemen altında, avuç yukarı, parmaklar öğenin baktığı yöne.
            var worldPosition = _holdTarget.position - _holdTarget.up * holdPalmOffset - _holdTarget.forward * holdPalmForward;
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
        // Açık avuç: öğe elin üstünde durur (tepsi taşır gibi), parmaklar kavramaz.
        targetCurl = new Curl { Index = holdCurl, Others = holdCurl, Pinky = holdCurl, Thumb = holdThumbCurl };
    }

    // Bu elin açacağı parmak sayısı: önce sağ el dolar, artan sol ele geçer.
    private int RaisedFingers(float side)
    {
        return side > 0f ? Mathf.Min(_count, FingersPerHand) : Mathf.Max(0, _count - FingersPerHand);
    }

    private void ApplyCount(float side, ref Vector3 position, ref Quaternion rotation, ref Curl targetCurl)
    {
        float blendIn = Mathf.Clamp01(_countElapsed / gestureBlendTime);
        float blendOut = Mathf.Clamp01((_countDuration - _countElapsed) / gestureBlendTime);
        float blend = Mathf.SmoothStep(0f, 1f, Mathf.Min(blendIn, blendOut));

        // El jest merkezinde (pencereden görünen yükseklik), parmaklar yukarı, avuç karşıya; hafifçe öne vurgular.
        float pump = Mathf.Sin(_countElapsed * gesturePumpRate * Mathf.PI * 2f) * gesturePump * 0.5f;
        var countPosition = gestureCenter + Vector3.right * (side * gestureSideOffset) + Vector3.forward * pump;
        var countRotation = Quaternion.LookRotation(Vector3.up, Vector3.left * side);

        position = Vector3.Lerp(position, countPosition, blend);
        rotation = Quaternion.Slerp(rotation, countRotation, blend);

        // Sıra: işaret, orta, serçe, başparmak.
        int raised = RaisedFingers(side);
        targetCurl = new Curl
        {
            Index = raised >= 1 ? 0f : 1f,
            Others = raised >= 2 ? 0f : 1f,
            Pinky = raised >= 3 ? 0f : 1f,
            Thumb = raised >= 4 ? 0f : 0.9f
        };
    }

    // Sağ el öğe tutuyorsa emote'a katılmaz (öğe havada kalmasın); tek elli emote o zaman sol ele geçer.
    private bool UsesEmote(float side)
    {
        bool rightBusy = _holdTarget != null || _holdWeight > 0f;
        return side > 0f ? !rightBusy : _emote.BothHands || rightBusy;
    }

    private void ApplyEmote(float side, ref Vector3 position, ref Quaternion rotation, ref Curl targetCurl)
    {
        float blendIn = Mathf.Clamp01(_emoteElapsed / gestureBlendTime);
        float blendOut = Mathf.Clamp01((_emoteDuration - _emoteElapsed) / gestureBlendTime);
        float blend = Mathf.SmoothStep(0f, 1f, Mathf.Min(blendIn, blendOut));

        float wave = Mathf.Sin(_emoteElapsed * _emote.Rate * Mathf.PI * 2f);
        SampleEmote(_emote, side, wave, out var emotePosition, out var emoteRotation);

        position = Vector3.Lerp(position, emotePosition, blend);
        rotation = Quaternion.Slerp(rotation, emoteRotation, blend);
        targetCurl = new Curl { Index = _emote.IndexCurl, Others = _emote.OthersCurl, Pinky = _emote.OthersCurl, Thumb = _emote.ThumbCurl };
    }

    // Emote pozunda elin karakter yerel uzayındaki yeri ve dönüşü (side: sağ el +1, sol el -1; wave: salınım -1..1).
    // Tek kural burada: oyun da düzenleyici önizlemesi de bunu çağırır.
    public void SampleEmote(EmoteHandPose pose, float side, float wave, out Vector3 position, out Quaternion rotation)
    {
        // Değerler sağ el içindir; sol elde X'te aynalanır (dönme ekseni aynalanınca y ve z işaret değiştirir).
        var mirror = new Vector3(side, 1f, 1f);
        Vector3 anchor;
        if (pose.AnchorToBrow)
        {
            anchor = browPoint;
        }
        else
        {
            var bounds = GetBodyBounds();
            anchor = new Vector3(
                bounds.center.x + pose.Anchor.x * bounds.extents.x,
                bounds.min.y + pose.Anchor.y * bounds.size.y,
                bounds.center.z + pose.Anchor.z * bounds.extents.z);
        }

        anchor.x *= side;
        position = anchor + Vector3.Scale(pose.Offset + pose.Swing * wave, mirror);

        var wagAxis = Vector3.Scale(pose.WagAxis, new Vector3(1f, side, side));
        rotation = Quaternion.AngleAxis(pose.WagAngle * wave, wagAxis)
            * Quaternion.LookRotation(Vector3.Scale(pose.FingerDirection, mirror), Vector3.Scale(pose.ThumbDirection, mirror));
    }

    // Gövdenin karakter yerel uzayındaki sınır kutusu (dinlenme pozunda, bir kez ölçülür). Dünya AABB'si değil:
    // renderer'ların yerel sınırları karakter köküne taşınır.
    private Bounds GetBodyBounds()
    {
        if (_hasBodyBounds)
            return _bodyBounds;

        var min = Vector3.positiveInfinity;
        var max = Vector3.negativeInfinity;
        foreach (var bodyRenderer in body.GetComponentsInChildren<Renderer>(true))
        {
            var local = bodyRenderer.localBounds;
            for (int i = 0; i < 8; i++)
            {
                var corner = local.center + Vector3.Scale(local.extents,
                    new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                var point = transform.InverseTransformPoint(bodyRenderer.transform.TransformPoint(corner));
                min = Vector3.Min(min, point);
                max = Vector3.Max(max, point);
            }
        }

        _bodyBounds = new Bounds((min + max) * 0.5f, max - min);
        _hasBodyBounds = true;
        return _bodyBounds;
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
        targetCurl = new Curl { Index = 0f, Others = 1f, Pinky = 1f, Thumb = 0.9f };

        if (_gestureElapsed >= _gestureDuration)
            _gestureHand = null;
    }
}
