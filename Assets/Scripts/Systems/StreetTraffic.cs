using System;
using UnityEngine;

// Sokağın canlılığı: şeritlerden geçen araçlar ve kaldırımda yürüyen figüranlar. Yalnızca GÖRSEL fon — her istemcide
// yerel ve rastgele çalışır, ağdan gitmez, oyun durumunu etkilemez (müşteriler bunlardan DEĞİLDİR; onları
// CustomerDirector yönetir). Her hat bir doğma ve bir kaybolma noktası arasında düz bir çizgidir; hat başına
// aralık, hız ve prefab listesi veridir.
public class StreetTraffic : MonoBehaviour
{
    [Serializable]
    public class Lane
    {
        public string name;
        [Tooltip("Yolcunun doğduğu nokta.")]
        public Transform start;
        [Tooltip("Yolcunun kaybolduğu nokta.")]
        public Transform end;
        [Tooltip("Bu hatta doğabilecek prefab'lar (rastgele seçilir). Kökünde StreetTraveller olmalı.")]
        public StreetTraveller[] prefabs;
        [Tooltip("İki doğma arasındaki süre (sn): en az / en çok.")]
        public Vector2 interval = new(6f, 14f);
        [Tooltip("Hız (m/sn): en az / en çok.")]
        public Vector2 speed = new(6f, 8f);

        [NonSerialized] public float NextSpawnTime;
        [NonSerialized] public StreetTraveller Last;
    }

    [SerializeField] private Lane[] lanes = Array.Empty<Lane>();
    [Tooltip("Aynı hatta art arda doğan iki yolcu arasında bırakılan en az mesafe (m): hızlı olan öndekinin içine girmesin.")]
    [SerializeField, Min(0f)] private float minGap = 9f;

    private void OnEnable()
    {
        // İlk yolcular aynı anda doğmasın: her hat rastgele bir gecikmeyle başlar.
        foreach (var lane in lanes)
            lane.NextSpawnTime = Time.time + UnityEngine.Random.Range(0f, lane.interval.y);
    }

    private void Update()
    {
        foreach (var lane in lanes)
        {
            if (Time.time < lane.NextSpawnTime || lane.start == null || lane.end == null || lane.prefabs.Length == 0)
                continue;

            // Öndeki henüz doğma noktasından yeterince uzaklaşmadıysa bekle.
            if (lane.Last != null && Vector3.Distance(lane.Last.transform.position, lane.start.position) < minGap)
                continue;

            Spawn(lane);
            lane.NextSpawnTime = Time.time + UnityEngine.Random.Range(lane.interval.x, lane.interval.y);
        }
    }

    private void Spawn(Lane lane)
    {
        var prefab = lane.prefabs[UnityEngine.Random.Range(0, lane.prefabs.Length)];
        if (prefab == null)
            return;

        var direction = lane.end.position - lane.start.position;
        var rotation = direction.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(direction.normalized, Vector3.up) : Quaternion.identity;
        var traveller = Instantiate(prefab, lane.start.position, rotation, transform);

        // Hız, öndekini geçmeyecek şekilde seçilir (şeritte sollama yok).
        float speed = UnityEngine.Random.Range(lane.speed.x, lane.speed.y);
        if (lane.Last != null)
            speed = Mathf.Min(speed, lane.Last.Speed);

        traveller.Begin(lane.end.position, speed);
        lane.Last = traveller;
    }
}
