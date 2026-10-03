using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Kese kağıdının görsel hâlleri ve üstündeki içerik fotoğrafı (GDD 5.3.1) — paket görselinin TEK kaynağı
// (KeseKagidi_Visual). Dünya görseli, elde tutulan kopya ve önizleme aynı prefab'dan gelir; hangi hâlin ve hangi
// fotoğrafın gösterileceğini çağıran (Package) söyler. Yalnızca gösterir.
[DisallowMultipleComponent]
public class PackageVisualParts : MonoBehaviour
{
    public enum State
    {
        // Boş ve elde: katlanmış.
        Folded,
        // Paketleme alanında: ağzı açık, ürün eklenebilir.
        Open,
        // İçi dolu ve alan dışında: ağzı kapalı.
        Closed
    }

    [SerializeField] private GameObject folded;
    [SerializeField] private GameObject open;
    [SerializeField] private GameObject closed;

    [Header("Fotoğraf (pop-up ile aynı görsel dil)")]
    [Tooltip("Fotoğrafın kökü; paket boşken kapalıdır.")]
    [SerializeField] private GameObject photoRoot;
    [Tooltip("İçerik girişlerinin dizildiği kap (paket N ürün alabilir).")]
    [SerializeField] private RectTransform entryContainer;
    [Tooltip("Bir ürünün girişi: varyant görseli + yanında ikon kabı. Her ürün için kopyalanır.")]
    [SerializeField] private RectTransform entryTemplate;
    [Tooltip("Girişin içindeki varyant görseli (şablondaki yol).")]
    [SerializeField] private string variantImagePath = "VaryantGorseli";
    [Tooltip("Girişin içindeki ikon kabı (şablondaki yol): eksikler (X'li) ya da uymayan içerik (X'siz).")]
    [SerializeField] private string iconContainerPath = "Ikonlar";
    [Tooltip("İkon şablonu: kökünde malzeme ikonu, 'Carpi' çocuğu X işareti.")]
    [SerializeField] private Image iconTemplate;
    [SerializeField] private string crossPath = "Carpi";

    private readonly List<GameObject> _entries = new();

    public void Show(State state)
    {
        folded.SetActive(state == State.Folded);
        open.SetActive(state == State.Open);
        closed.SetActive(state == State.Closed);
    }

    public void ShowPhoto(IReadOnlyList<PackagePhoto.Entry> entries)
    {
        foreach (var entry in _entries)
        {
            // Destroy kare sonuna ertelenir; aynı karede yeniden kurulurken eskisi layout'a karışmasın.
            entry.SetActive(false);
            entry.transform.SetParent(null, false);
            Destroy(entry);
        }

        _entries.Clear();
        entryTemplate.gameObject.SetActive(false);
        iconTemplate.gameObject.SetActive(false);
        photoRoot.SetActive(entries.Count > 0);

        foreach (var data in entries)
        {
            var entry = Instantiate(entryTemplate, entryContainer);
            entry.gameObject.SetActive(true);
            _entries.Add(entry.gameObject);

            var variantImage = entry.Find(variantImagePath).GetComponent<Image>();
            variantImage.sprite = data.Variant != null ? data.Variant.Image : null;
            // Varyanta uymayan içerik AÇIKÇA farklı görünür: ürün fotoğrafı yok, yalnızca ikon listesi.
            variantImage.gameObject.SetActive(variantImage.sprite != null);

            var icons = entry.Find(iconContainerPath);
            foreach (var item in data.Missing)
                AddIcon(icons, item, true);
            foreach (var item in data.Loose)
                AddIcon(icons, item, false);
        }
    }

    private void AddIcon(Transform parent, ItemType item, bool crossed)
    {
        var icon = Instantiate(iconTemplate, parent);
        icon.sprite = item.Icon;
        icon.gameObject.SetActive(true);
        icon.transform.Find(crossPath).gameObject.SetActive(crossed);
    }
}
