using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Duvar malzeme panosu (GDD 3.6.2, 3.6.3): dünyada duran fiziksel pano; bu bölümdeki kanal eşleşmelerini gösterir.
// Kod (numara, yön oku) panoya YAZILMAZ — oyuncu onu malzemenin panodaki YERİNDEN çıkarır:
//  - Değerleri çarkta sabit açı taşıyan kanal (yön): malzemeler o açılarda, bir merkezin çevresinde durur
//    (yukarıdaki = "yukarı").
//  - Diğer kanallar (sayı): malzemeler değer sırasıyla alt alta dizilir (üstten 1., 2., ...).
// İçerik YALNIZCA replike veriden gelir (LevelDirector.SignalRows): her kanal için bir bölüm, eşleşmesi olan her
// satır için bir giriş. Kodda kanal/malzeme listesi veya sayısı yoktur. Eşleşme değişince malzemenin yeri değişir.
// Sunucu round başında yazar; her istemci aynı listeyi çizer.
public class SignalMappingBoard : MonoBehaviour
{
    [Tooltip("Tür id'si -> ItemType (resim, ad). Satırlar ağdan yalnızca id taşır.")]
    [SerializeField] private ItemRegistry registry;
    [Tooltip("Kanal bölümlerinin yan yana dizildiği kap (yatay layout).")]
    [SerializeField] private RectTransform sectionContainer;
    [Tooltip("Boş bölüm şablonu; eşleşmesi olan her kanal için kopyalanır. Sahnede kapalı durur.")]
    [SerializeField] private RectTransform sectionTemplate;
    [Tooltip("Alt alta dizilen kanalların giriş şablonu.")]
    [SerializeField] private SignalMappingBoardEntry listEntryTemplate;
    [Tooltip("Açıyla yerleşen kanalların giriş şablonu.")]
    [SerializeField] private SignalMappingBoardEntry radialEntryTemplate;
    [Tooltip("Açıyla yerleşimde girişin merkezden uzaklığı (bölümün boyutuna oranla, 0–0,5).")]
    [SerializeField, Range(0f, 0.5f)] private float radialRadius = 0.32f;
    [Tooltip("Açıyla yerleşimde bir girişin yarı boyutu (bölümün boyutuna oranla).")]
    [SerializeField] private Vector2 radialEntryHalfSize = new(0.2f, 0.17f);

    private struct Mapping
    {
        public SignalValue Value;
        public ItemType Item;
    }

    private readonly List<GameObject> _sections = new();
    private LevelDirector _director;
    private bool _dirty;

    private void Start()
    {
        sectionTemplate.gameObject.SetActive(false);
        listEntryTemplate.gameObject.SetActive(false);
        radialEntryTemplate.gameObject.SetActive(false);

        // Singleton'lara Awake'te erişilmez (CLAUDE.md NGO notu).
        _director = LevelDirector.Instance;
        if (_director == null)
        {
            Debug.LogError("[SignalMappingBoard] LevelDirector bulunamadı; pano boş kalır.");
            return;
        }

        _director.SignalRows.OnListChanged += HandleRowsChanged;
        _dirty = true;
    }

    private void OnDestroy()
    {
        if (_director != null)
            _director.SignalRows.OnListChanged -= HandleRowsChanged;
    }

    // Liste eleman eleman dolar; her değişiklikte değil, karede bir kez yeniden kurulur.
    private void HandleRowsChanged(NetworkListEvent<SignalRow> change) => _dirty = true;

    private void LateUpdate()
    {
        if (!_dirty)
            return;

        _dirty = false;
        Rebuild();
    }

    private void Rebuild()
    {
        foreach (var section in _sections)
            Destroy(section);
        _sections.Clear();

        // Satırlar kanal kanal, kanalın değer sırasıyla gelir; kanal başına eşleşmeler toplanır.
        var order = new List<int>();
        var byChannel = new Dictionary<int, List<Mapping>>();
        var rows = _director.SignalRows;
        for (int i = 0; i < rows.Count; i++)
        {
            // Eşleşmesi olmayan değer (ör. bu bölümde proteini olmayan yön) panoda yer almaz.
            if (rows[i].ItemId == SignalRow.NoItem)
                continue;

            var item = registry != null ? registry.Find(rows[i].ItemId) : null;
            if (item == null || !_director.TryGetSignalValue(rows[i].ChannelIndex, rows[i].ValueIndex, out var value))
                continue;

            if (!byChannel.TryGetValue(rows[i].ChannelIndex, out var mappings))
            {
                mappings = new List<Mapping>();
                byChannel.Add(rows[i].ChannelIndex, mappings);
                order.Add(rows[i].ChannelIndex);
            }

            mappings.Add(new Mapping { Value = value, Item = item });
        }

        foreach (int channelIndex in order)
            BuildSection(byChannel[channelIndex]);
    }

    private void BuildSection(List<Mapping> mappings)
    {
        var section = Instantiate(sectionTemplate, sectionContainer);
        section.gameObject.SetActive(true);
        _sections.Add(section.gameObject);

        // Tüm değerlerin sabit açısı varsa açıyla, yoksa alt alta (çarktaki kuralın aynısı: karışık kullanılmaz).
        bool radial = true;
        foreach (var mapping in mappings)
            radial &= mapping.Value.UseWheelAngle;

        for (int i = 0; i < mappings.Count; i++)
        {
            var entry = Instantiate(radial ? radialEntryTemplate : listEntryTemplate, section);
            entry.gameObject.SetActive(true);
            entry.Show(mappings[i].Item);

            // Yerleşim tamamen çapalarla (bölümün boyutuna oranla) yapılır; satır/giriş sayısına duyarsızdır.
            var rect = (RectTransform)entry.transform;
            if (radial)
            {
                float angle = mappings[i].Value.WheelAngle * Mathf.Deg2Rad;
                var center = new Vector2(0.5f, 0.5f) + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radialRadius;
                rect.anchorMin = center - radialEntryHalfSize;
                rect.anchorMax = center + radialEntryHalfSize;
            }
            else
            {
                rect.anchorMin = new Vector2(0f, 1f - (i + 1) / (float)mappings.Count);
                rect.anchorMax = new Vector2(1f, 1f - i / (float)mappings.Count);
            }

            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
