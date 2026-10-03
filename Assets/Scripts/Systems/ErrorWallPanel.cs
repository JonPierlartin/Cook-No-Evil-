using System.Collections.Generic;
using UnityEngine;

// Duvar hata paneli (GDD 7.1.2): dünyada duran fiziksel gösterge; yan yana X'ler, her hatada soldan bir X yanar.
// X sayısı kaybetme eşiğidir (GameLoopManager.MaxErrors — tek yer); durum replike hata sayacından okunur
// (GameLoopManager.ErrorCount). Her istemcide yerel çalışır, karar vermez.
//
// K2 İSTİSNASI — projede TEK yer: Şef'in kontur görüşüne bir nesnenin dahil olup olmaması burada bir OYUN DURUMU
// tarafından sürülür. Emsal değildir; başka hiçbir nesne için kullanılmaz.
//  - SÖNÜK X geometri DEĞİLDİR: panel yüzeyine çizilmiş bir işarettir (dünya uzayı Canvas). Derinlik/normal
//    tamponuna yazmadığı için Şef'in dünyasında hiç yoktur; Kasiyer ve Komi onu soluk olarak görür.
//  - YANAN X gerçek geometridir: panelden öne çıkan parlak bir X. Kontur bunu kenar olarak yakalar (Şef görür),
//    diğer roller parlak renk olarak görür.
// (GDD'nin ilk yöntemi — rendering layer mask ile kontur filtresi — bu projede uygulanamaz: kontur, derinlik/normal
// tamponu üstünde çalışan tam ekran bir geçiştir; nesne başına filtreleme noktası yoktur. GDD'nin yedek planı
// kullanıldı: yanan X fiziksel olarak öne çıkar.)
public class ErrorWallPanel : MonoBehaviour
{
    [Tooltip("Sönük X şablonu (panel yüzeyindeki Canvas'ta). Her hata hakkı için kopyalanır.")]
    [SerializeField] private RectTransform dimTemplate;
    [Tooltip("Yanan X şablonu (geometri; panelden öne çıkar). Her hata hakkı için kopyalanır.")]
    [SerializeField] private Transform litTemplate;
    [Tooltip("X'lerin dizildiği genişlik (m, panelin yerel X ekseni).")]
    [SerializeField, Min(0.1f)] private float rowWidth = 0.9f;

    private readonly List<GameObject> _dim = new();
    private readonly List<GameObject> _lit = new();
    private GameLoopManager _loop;

    private void Start()
    {
        dimTemplate.gameObject.SetActive(false);
        litTemplate.gameObject.SetActive(false);

        // Singleton'lara Awake'te erişilmez (CLAUDE.md NGO notu).
        _loop = GameLoopManager.Instance;
        if (_loop == null)
        {
            Debug.LogError("[ErrorWallPanel] GameLoopManager bulunamadı; panel boş kalır.");
            return;
        }

        Build(_loop.MaxErrors);
        _loop.ErrorCount.OnValueChanged += HandleErrorCountChanged;
        Apply(_loop.ErrorCount.Value);
    }

    private void OnDestroy()
    {
        if (_loop != null)
            _loop.ErrorCount.OnValueChanged -= HandleErrorCountChanged;
    }

    private void HandleErrorCountChanged(int previous, int current) => Apply(current);

    // X sayısı eşikten gelir; şablonlar satıra eşit aralıkla dizilir. Canvas birimi ile dünya birimi arasındaki
    // oran Canvas'ın ölçeğinden okunur.
    private void Build(int count)
    {
        float canvasScale = dimTemplate.lossyScale.x / Mathf.Max(transform.lossyScale.x, 0.0001f);
        for (int i = 0; i < count; i++)
        {
            float x = count > 1 ? (i / (float)(count - 1) - 0.5f) * rowWidth : 0f;

            var dim = Instantiate(dimTemplate, dimTemplate.parent);
            dim.anchoredPosition = new Vector2(x / canvasScale, dim.anchoredPosition.y);
            _dim.Add(dim.gameObject);

            var lit = Instantiate(litTemplate, litTemplate.parent);
            lit.localPosition = new Vector3(x, litTemplate.localPosition.y, litTemplate.localPosition.z);
            _lit.Add(lit.gameObject);
        }
    }

    // Soldan 'errors' kadar X yanar: yanan X'in geometrisi açılır, sönük işareti kapanır.
    private void Apply(int errors)
    {
        for (int i = 0; i < _lit.Count; i++)
        {
            bool lit = i < errors;
            _lit[i].SetActive(lit);
            _dim[i].SetActive(!lit);
        }
    }
}
