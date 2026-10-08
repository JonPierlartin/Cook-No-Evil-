using System;
using System.Collections.Generic;
using UnityEngine;

// Mekânın akustiği (GDD 10.4, K4): ses duvardan ve kapalı kapıdan GEÇMEZ, yalnızca açıklıklardan (pencereler) geçer.
// İki nokta arasındaki "ses yolu" odalar ve açıklıklar üzerinden en kısa yoldur; her açıklık yola sabit bir ek
// mesafe (kayıp) ekler. Sonuç bir "etkin mesafe"dir: sesli sohbet ve dünya efektleri zayıflamayı bundan hesaplar.
//  - Aynı odadaki iki nokta: düz mesafe.
//  - Komşu odalar (ör. İstasyon ↔ Mutfak): pencereden dolaşan yol + bir açıklık kaybı.
//  - Kasa ↔ Mutfak: aralarında açıklık yok (kapı kapalı); yol İstasyon'dan, iki pencereden dolaşır → uzak ve zayıf.
//    Yapay bir "ses geçmez" kuralı yoktur; mekânın sonucudur.
// Odalar ve açıklıklar veridir (sahnedeki bileşende); harita kurulumu yazar. Yalnızca yerel hesap: ağ durumu yok.
public class AcousticSpace : MonoBehaviour
{
    [Serializable]
    public struct Room
    {
        public string name;
        public Vector3 center;
        public Vector3 size;
    }

    // Açıklık: iki odayı bağlayan pencere. Ses, açıklığın genişliği boyunca herhangi bir noktadan geçebilir
    // (center ± halfSpan).
    [Serializable]
    public struct Portal
    {
        public string name;
        public int roomA;
        public int roomB;
        public Vector3 center;
        public Vector3 halfSpan;
    }

    [SerializeField] private Room[] rooms = Array.Empty<Room>();
    [SerializeField] private Portal[] portals = Array.Empty<Portal>();

    [Header("Sesli sohbet (playtest parametreleri)")]
    [Tooltip("Etkin mesafe bunun altındayken ses tam duyulur (m).")]
    [SerializeField, Min(0f)] private float fullVoiceDistance = 6f;
    [Tooltip("Etkin mesafe buna ulaşınca ses tamamen kesilir (m).")]
    [SerializeField, Min(0.1f)] private float zeroVoiceDistance = 18f;
    [Tooltip("Sesin geçtiği her açıklığın (pencere) yola eklediği mesafe (m). Büyüdükçe iki pencere ötesi daha zor duyulur.")]
    [SerializeField, Min(0f)] private float portalPenalty = 6f;

    // Açıklığın genişliği boyunca denenen nokta sayısı (uçlar dahil).
    private const int PortalSamples = 3;
    private const int NoRoom = -1;

    public static AcousticSpace Instance { get; private set; }

    private static AudioListener _listener;

    private float[] _cost;
    private int[] _previous;
    private bool[] _done;
    private Vector3[] _nodes;

    // Etkin dinleyicinin (yerel oyuncunun kamerası; lobide sahne kamerası) dönüşümü. Yoksa null.
    public static Transform Listener
    {
        get
        {
            if (_listener == null || !_listener.isActiveAndEnabled)
                _listener = FindAnyObjectByType<AudioListener>();

            return _listener != null ? _listener.transform : null;
        }
    }

    private void OnEnable()
    {
        Instance = this;
        int nodeCount = 2 + portals.Length * PortalSamples;
        _cost = new float[nodeCount];
        _previous = new int[nodeCount];
        _done = new bool[nodeCount];
        _nodes = new Vector3[nodeCount];
    }

    private void OnDisable()
    {
        if (Instance == this)
            Instance = null;
    }

    // Kurulum (editör) yazar.
    public void Configure(Room[] newRooms, Portal[] newPortals)
    {
        rooms = newRooms;
        portals = newPortals;
    }

    // Sesli sohbetin seviyesi (0–1): etkin mesafe fullVoiceDistance'ta 1, zeroVoiceDistance'ta 0.
    // apparent: sesin dinleyiciye geldiği yön için kaynak noktası (aynı odadaysa konuşanın kendisi, değilse sesin
    // geçtiği son açıklık).
    public float VoiceGain(Vector3 speaker, Vector3 listener, out Vector3 apparent)
    {
        float distance = EffectiveDistance(speaker, listener, out apparent);
        if (float.IsInfinity(distance))
            return 0f;

        return Mathf.Clamp01((zeroVoiceDistance - distance) / Mathf.Max(zeroVoiceDistance - fullVoiceDistance, 0.01f));
    }

    // Sesin kat ettiği yol + açıklık kayıpları (m). Yol yoksa sonsuz.
    public float EffectiveDistance(Vector3 from, Vector3 to)
    {
        return EffectiveDistance(from, to, out _);
    }

    public float EffectiveDistance(Vector3 from, Vector3 to, out Vector3 apparent)
    {
        apparent = from;
        int fromRoom = RoomOf(from);
        int toRoom = RoomOf(to);
        // Odaların dışındaki nokta (lobi kamerası, sokak): akustik model uygulanmaz, düz mesafe.
        if (fromRoom == NoRoom || toRoom == NoRoom || fromRoom == toRoom)
            return Vector3.Distance(from, to);

        // Düğümler: 0 = kaynak, 1 = dinleyici, sonrası açıklıkların örnek noktaları.
        _nodes[0] = from;
        _nodes[1] = to;
        for (int p = 0; p < portals.Length; p++)
        {
            for (int s = 0; s < PortalSamples; s++)
            {
                float t = PortalSamples == 1 ? 0f : s / (PortalSamples - 1f) * 2f - 1f;
                _nodes[2 + p * PortalSamples + s] = portals[p].center + portals[p].halfSpan * t;
            }
        }

        int count = _nodes.Length;
        for (int i = 0; i < count; i++)
        {
            _cost[i] = float.PositiveInfinity;
            _previous[i] = -1;
            _done[i] = false;
        }

        _cost[0] = 0f;
        while (true)
        {
            int current = -1;
            for (int i = 0; i < count; i++)
            {
                if (!_done[i] && (current < 0 || _cost[i] < _cost[current]))
                    current = i;
            }

            if (current < 0 || float.IsInfinity(_cost[current]))
                return float.PositiveInfinity;

            if (current == 1)
                break;

            _done[current] = true;
            for (int next = 1; next < count; next++)
            {
                if (_done[next] || !ShareRoom(current, next, fromRoom, toRoom))
                    continue;

                // Bir açıklığa varmak kaybını da getirir.
                float step = Vector3.Distance(_nodes[current], _nodes[next]) + (next >= 2 ? portalPenalty : 0f);
                if (_cost[current] + step < _cost[next])
                {
                    _cost[next] = _cost[current] + step;
                    _previous[next] = current;
                }
            }
        }

        // Dinleyiciye en son hangi düğümden gelindi: sesin "geldiği yer".
        int last = _previous[1];
        if (last >= 2)
            apparent = _nodes[last];

        return _cost[1];
    }

    private bool ShareRoom(int a, int b, int fromRoom, int toRoom)
    {
        GetRooms(a, fromRoom, toRoom, out int a1, out int a2);
        GetRooms(b, fromRoom, toRoom, out int b1, out int b2);
        return a1 == b1 || a1 == b2 || (a2 != NoRoom && (a2 == b1 || a2 == b2));
    }

    private void GetRooms(int node, int fromRoom, int toRoom, out int first, out int second)
    {
        if (node == 0)
        {
            first = fromRoom;
            second = NoRoom;
        }
        else if (node == 1)
        {
            first = toRoom;
            second = NoRoom;
        }
        else
        {
            var portal = portals[(node - 2) / PortalSamples];
            first = portal.roomA;
            second = portal.roomB;
        }
    }

    private int RoomOf(Vector3 point)
    {
        for (int i = 0; i < rooms.Length; i++)
        {
            var offset = point - rooms[i].center;
            var half = rooms[i].size * 0.5f;
            if (Mathf.Abs(offset.x) <= half.x && Mathf.Abs(offset.y) <= half.y && Mathf.Abs(offset.z) <= half.z)
                return i;
        }

        return NoRoom;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.6f);
        foreach (var room in rooms)
            Gizmos.DrawWireCube(room.center, room.size);

        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.9f);
        foreach (var portal in portals)
            Gizmos.DrawLine(portal.center - portal.halfSpan, portal.center + portal.halfSpan);
    }
}
