using UnityEngine;

// Akvaryumdaki balıkların yüzmesi. Yalnızca görsel: her istemcide yerel çalışır, ağdan gitmez, oyun durumuna bakmaz.
// Balıklar (adı fishPrefix ile başlayan çocuklar) suyun hacmi içinde rastgele bir noktaya doğru yüzer, yön
// değiştirirken yumuşakça döner, ara sıra olduğu yerde süzülür; gövde yüzme hızına bağlı olarak sağa sola kıvrılır.
// Balığın pivotu gövdesinin merkezindedir; burnunun baktığı yerel eksen noseAxis'tir.
public class AquariumFish : MonoBehaviour
{
    [Tooltip("Suyun hacmini veren renderer (balıklar bunun sınırları içinde kalır).")]
    [SerializeField] private Renderer water;
    [Tooltip("Balık düğümlerinin ad öneki.")]
    [SerializeField] private string fishPrefix = "Fish_";
    [Tooltip("Balığın burnunun baktığı yerel eksen.")]
    [SerializeField] private Vector3 noseAxis = Vector3.left;
    [Tooltip("Balıkların camdan, yüzeyden ve tabandan uzak duracağı pay (m).")]
    [SerializeField] private Vector3 margin = new(0.16f, 0.07f, 0.06f);

    [Header("Yüzme")]
    [SerializeField] private float minSpeed = 0.07f;
    [SerializeField] private float maxSpeed = 0.18f;
    [Tooltip("Yön değiştirme hızı (derece / sn).")]
    [SerializeField] private float turnSpeed = 110f;
    [Tooltip("Hedefe bu kadar yaklaşınca yeni hedef seçilir (m).")]
    [SerializeField] private float arriveDistance = 0.08f;
    [Tooltip("Yeni hedefin yüksekliği en çok bu kadar değişir (m): balıklar çoğunlukla yatay yüzer.")]
    [SerializeField] private float maxDepthChange = 0.12f;

    [Header("Süzülme")]
    [Tooltip("Hedefe varınca olduğu yerde süzülme olasılığı.")]
    [SerializeField, Range(0f, 1f)] private float pauseChance = 0.35f;
    [SerializeField] private float minPause = 0.6f;
    [SerializeField] private float maxPause = 2.2f;
    [Tooltip("Süzülürken hızın yüzme hızına oranı.")]
    [SerializeField, Range(0f, 1f)] private float driftFactor = 0.12f;

    [Header("Kıvrılma")]
    [Tooltip("Gövdenin sağa sola en çok döndüğü açı (derece).")]
    [SerializeField] private float wiggleAngle = 9f;
    [Tooltip("En yüksek hızda saniyedeki kıvrılma sayısı.")]
    [SerializeField] private float wiggleFrequency = 3.2f;
    [Tooltip("Yukarı / aşağı yüzerken gövdenin eğimi, yönün eğimine oranı.")]
    [SerializeField, Range(0f, 1f)] private float pitchFactor = 0.5f;

    private struct Fish
    {
        public Transform Transform;
        public Vector3 Heading;
        public Vector3 Target;
        public float Speed;
        public float PauseUntil;
        public float WigglePhase;
    }

    private Fish[] _fish;
    private Bounds _volume;
    private Quaternion _noseToForward;

    private void Start()
    {
        if (water == null)
        {
            enabled = false;
            return;
        }

        // Suyun hacmi, balıkların ebeveyninin (bu nesnenin) yerel uzayında.
        var local = water.localBounds;
        _volume = new Bounds(transform.InverseTransformPoint(water.transform.TransformPoint(local.center)), Vector3.zero);
        for (int i = 0; i < 8; i++)
        {
            var corner = local.center + Vector3.Scale(local.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
            _volume.Encapsulate(transform.InverseTransformPoint(water.transform.TransformPoint(corner)));
        }

        _volume.extents = Vector3.Max(_volume.extents - margin, Vector3.one * 0.01f);
        _noseToForward = Quaternion.Inverse(Quaternion.LookRotation(noseAxis.normalized, Vector3.up));

        var found = new System.Collections.Generic.List<Fish>();
        foreach (Transform child in transform)
        {
            if (!child.name.StartsWith(fishPrefix))
                continue;

            var fish = new Fish
            {
                Transform = child,
                Heading = child.localRotation * noseAxis.normalized,
                WigglePhase = Random.value * Mathf.PI * 2f,
            };
            fish.Transform.localPosition = _volume.ClosestPoint(child.localPosition);
            PickTarget(ref fish);
            found.Add(fish);
        }

        _fish = found.ToArray();
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        for (int i = 0; i < _fish.Length; i++)
        {
            var fish = _fish[i];
            var position = fish.Transform.localPosition;
            bool drifting = Time.time < fish.PauseUntil;
            float speed = drifting ? fish.Speed * driftFactor : fish.Speed;

            var toTarget = fish.Target - position;
            if (!drifting)
            {
                if (toTarget.magnitude < arriveDistance)
                {
                    if (Random.value < pauseChance)
                        fish.PauseUntil = Time.time + Random.Range(minPause, maxPause);
                    PickTarget(ref fish);
                    toTarget = fish.Target - position;
                }

                fish.Heading = Vector3.RotateTowards(fish.Heading, toTarget.normalized, turnSpeed * Mathf.Deg2Rad * deltaTime, 0f);
            }

            position = _volume.ClosestPoint(position + fish.Heading * (speed * deltaTime));
            fish.Transform.localPosition = position;

            // Gövde yönüne bakar (eğim azaltılmış), üstüne hıza bağlı kıvrılma biner.
            var facing = new Vector3(fish.Heading.x, fish.Heading.y * pitchFactor, fish.Heading.z);
            if (facing.sqrMagnitude < 1e-6f)
                facing = noseAxis;
            fish.WigglePhase += wiggleFrequency * Mathf.PI * 2f * (speed / Mathf.Max(maxSpeed, 1e-4f)) * deltaTime;
            float wiggle = Mathf.Sin(fish.WigglePhase) * wiggleAngle;
            fish.Transform.localRotation = Quaternion.LookRotation(facing.normalized, Vector3.up) * Quaternion.AngleAxis(wiggle, Vector3.up) * _noseToForward;

            _fish[i] = fish;
        }
    }

    private void PickTarget(ref Fish fish)
    {
        var min = _volume.min;
        var max = _volume.max;
        float currentY = fish.Transform.localPosition.y;
        fish.Target = new Vector3(
            Random.Range(min.x, max.x),
            Mathf.Clamp(currentY + Random.Range(-maxDepthChange, maxDepthChange), min.y, max.y),
            Random.Range(min.z, max.z));
        fish.Speed = Random.Range(minSpeed, maxSpeed);
    }
}
