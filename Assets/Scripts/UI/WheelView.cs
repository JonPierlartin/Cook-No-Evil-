using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Radyal çarkın ORTAK görünümü: sinyal çarkı (R) ve genel emote çarkı (E) aynı bileşeni kullanır, böylece ikisi aynı
// görünür ve aynı davranır. Yalnızca çizer ve "fare şu yöne itildi, hangi seçenek?" sorusunu cevaplar; ne
// seçileceğine, kimin açabileceğine ve seçimin ne yaptığına çağıran (SignalWheelUI / EmoteWheelUI) karar verir.
// Seçenek sayısı veriden gelir; iki görünüm vardır:
//  - Dilimler: az sayıda büyük seçenek (kategori katı) — yırtık kâğıt daire dilimlere bölünür.
//  - Baloncuklar: son kat (jestin kendisi) — merkezin çevresinde yuvarlak kâğıtlar.
public class WheelView : MonoBehaviour
{
    public enum Style
    {
        Wedges,
        Bubbles
    }

    public struct Option
    {
        public string Label;
        public Sprite Icon;
        // Çarktaki açı (derece; 0 = sağ, 90 = yukarı).
        public float Angle;
    }

    [SerializeField] private GameObject root;
    [Tooltip("Dilim şablonu: dolum tipi Radial 360 olan kâğıt daire. Her seçenek için kopyalanır.")]
    [SerializeField] private Image wedgeTemplate;
    [Tooltip("Seçenek içeriği şablonu (zemin + ikon + yazı). Her seçenek için kopyalanır.")]
    [SerializeField] private WheelSlot slotTemplate;
    [Tooltip("Vurgulanan seçeneğin adı (çarkın altında).")]
    [SerializeField] private Text caption;

    [Header("Dilimler")]
    [Tooltip("Dilimler arası boşluk (derece).")]
    [SerializeField] private float wedgeGapDegrees = 5f;
    [Tooltip("Dilimin merkezden dışa itilmesi (px): normal / vurgulu.")]
    [SerializeField] private float wedgeOffset = 8f;
    [SerializeField] private float wedgeHighlightOffset = 26f;
    [Tooltip("Dilim içeriğinin (ikon/yazı) merkezden uzaklığı (px).")]
    [SerializeField] private float wedgeContentRadius = 130f;

    [Header("Baloncuklar")]
    [SerializeField] private float bubbleRadius = 200f;
    [SerializeField] private float bubbleHighlightScale = 1.22f;

    [Header("Renkler")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightedColor = new(1f, 0.90f, 0.55f);
    [Tooltip("Oynayan jest bitene kadar seçeneklerin rengi (engelli).")]
    [SerializeField] private Color blockedColor = new(0.55f, 0.55f, 0.55f);

    private readonly List<Image> _wedges = new();
    private readonly List<WheelSlot> _slots = new();
    private readonly List<float> _angles = new();
    private readonly List<string> _labels = new();
    private Style _style;

    public bool IsShown => root.activeSelf;

    private void Awake()
    {
        root.SetActive(false);
        wedgeTemplate.gameObject.SetActive(false);
        slotTemplate.gameObject.SetActive(false);
    }

    public void Show(IReadOnlyList<Option> options, Style style)
    {
        Clear();
        _style = style;
        root.SetActive(true);

        float sector = options.Count > 0 ? 360f / options.Count : 360f;
        for (int i = 0; i < options.Count; i++)
        {
            float angle = options[i].Angle;
            _angles.Add(angle);
            _labels.Add(options[i].Label);
            var direction = Direction(angle);

            if (style == Style.Wedges)
            {
                var wedge = Instantiate(wedgeTemplate, wedgeTemplate.transform.parent);
                wedge.gameObject.SetActive(true);
                // Dolum tepeden (90°) saat yönünde başlar: dilim, kendi açısının ortasına gelecek şekilde döndürülür.
                float width = Mathf.Max(1f, sector - wedgeGapDegrees);
                wedge.fillAmount = width / 360f;
                wedge.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle + width * 0.5f - 90f);
                _wedges.Add(wedge);
            }

            var slot = Instantiate(slotTemplate, slotTemplate.transform.parent);
            slot.gameObject.SetActive(true);
            slot.Show(options[i].Icon, options[i].Label, style == Style.Bubbles);
            ((RectTransform)slot.transform).anchoredPosition = direction * (style == Style.Bubbles ? bubbleRadius : wedgeContentRadius);
            _slots.Add(slot);
        }

        Refresh(-1, false);
    }

    public void Hide()
    {
        Clear();
        root.SetActive(false);
    }

    // Fare yönüne açıca en yakın seçenek (yoksa -1).
    public int Pick(Vector2 pointerDirection)
    {
        if (_angles.Count == 0 || pointerDirection.sqrMagnitude < 0.0001f)
            return -1;

        float pointer = Mathf.Atan2(pointerDirection.y, pointerDirection.x) * Mathf.Rad2Deg;
        int best = -1;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < _angles.Count; i++)
        {
            float distance = Mathf.Abs(Mathf.DeltaAngle(pointer, _angles[i]));
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = i;
            }
        }

        return best;
    }

    public void Refresh(int highlighted, bool blocked)
    {
        for (int i = 0; i < _slots.Count; i++)
        {
            bool isHighlighted = i == highlighted && !blocked;
            var color = blocked ? blockedColor : (isHighlighted ? highlightedColor : normalColor);
            var direction = Direction(_angles[i]);

            _slots[i].SetTint(color);
            if (_style == Style.Bubbles)
            {
                _slots[i].transform.localScale = Vector3.one * (isHighlighted ? bubbleHighlightScale : 1f);
            }
            else
            {
                float offset = isHighlighted ? wedgeHighlightOffset : wedgeOffset;
                _wedges[i].color = color;
                _wedges[i].rectTransform.anchoredPosition = direction * offset;
                ((RectTransform)_slots[i].transform).anchoredPosition = direction * (wedgeContentRadius + offset);
            }
        }

        if (caption != null)
            caption.text = highlighted >= 0 && highlighted < _labels.Count ? _labels[highlighted] : string.Empty;
    }

    private void Clear()
    {
        foreach (var wedge in _wedges)
            Destroy(wedge.gameObject);
        foreach (var slot in _slots)
            Destroy(slot.gameObject);

        _wedges.Clear();
        _slots.Clear();
        _angles.Clear();
        _labels.Clear();
    }

    private static Vector2 Direction(float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
    }
}
