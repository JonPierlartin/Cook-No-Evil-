using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Duvar malzeme panosu (GDD 3.6.2, 3.6.3): dünyada duran fiziksel pano; bu bölümdeki kanal eşleşmelerini gösterir
// (garnitür → numara, protein → yön, ileride diğer kanallar). İçerik YALNIZCA replike veriden gelir
// (LevelDirector.SignalRows) — kodda malzeme/kanal listesi veya satır sayısı yoktur; eşleşmesi olan her satır için
// şablondan bir satır kopyalanır. Satır sırası = replike sıra = kanalın değer sırası (numara sırası): eşleşme
// değişince malzemenin panodaki YERİ de değişir. Sunucu round başında yazar; her istemci aynı listeyi çizer.
public class SignalMappingBoard : MonoBehaviour
{
    [Tooltip("Tür id'si -> ItemType (ikon). Satırlar ağdan yalnızca id taşır.")]
    [SerializeField] private ItemRegistry registry;
    [Tooltip("Satırların dizildiği kap (dikey layout).")]
    [SerializeField] private RectTransform rowContainer;
    [Tooltip("Satır şablonu; her eşleşme için kopyalanır. Sahnede kapalı durur.")]
    [SerializeField] private SignalMappingBoardRow rowTemplate;

    private readonly List<GameObject> _rows = new();
    private LevelDirector _director;
    private bool _dirty;

    private void Start()
    {
        rowTemplate.gameObject.SetActive(false);

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
        foreach (var row in _rows)
            Destroy(row);
        _rows.Clear();

        var rows = _director.SignalRows;
        for (int i = 0; i < rows.Count; i++)
        {
            // Eşleşmesi olmayan değer (ör. bu bölümde proteini olmayan yön) panoda satır açmaz.
            if (rows[i].ItemId == SignalRow.NoItem)
                continue;

            var item = registry != null ? registry.Find(rows[i].ItemId) : null;
            if (item == null || !_director.TryGetSignalValue(rows[i].ChannelIndex, rows[i].ValueIndex, out var value))
                continue;

            var row = Instantiate(rowTemplate, rowContainer);
            row.gameObject.SetActive(true);
            row.Show(value, item);
            _rows.Add(row.gameObject);
        }
    }
}
