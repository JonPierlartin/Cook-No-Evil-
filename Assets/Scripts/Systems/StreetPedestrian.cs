using UnityEngine;

// Kaldırımda yürüyen figüran kartının görünüşü: yürüme atlasından kare oynatır (satır = karakter, sütun = kare),
// yürüdüğü yöne bakar, doğarken rastgele bir karakter ve hafif bir renk tonu alır. Kart düzdür ve restorana
// (dünya +Z) bakar; gövde dönmez. Hızı StreetTraveller'dan okur. Yalnızca görsel.
[RequireComponent(typeof(StreetTraveller))]
public class StreetPedestrian : MonoBehaviour
{
    [SerializeField] private Renderer card;
    [Tooltip("Atlasın ızgarası: sütun (kare) ve satır (karakter) sayısı.")]
    [SerializeField] private Vector2Int atlasGrid = new(16, 4);
    [Tooltip("Bir yürüme döngüsündeki kare sayısı (satırın başından).")]
    [SerializeField, Min(1)] private int framesPerCycle = 12;
    [Tooltip("Bir döngüde kat edilen yol (m): kare hızı yürüme hızına bağlanır, ayaklar kaymaz.")]
    [SerializeField, Min(0.1f)] private float metersPerCycle = 1.3f;
    [Tooltip("Rastgele renk tonları (kartın tamamına çarpılır).")]
    [SerializeField] private Color[] tints = { Color.white };

    private static readonly int BaseMapScaleOffset = Shader.PropertyToID("_BaseMap_ST");
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    private StreetTraveller _traveller;
    private MaterialPropertyBlock _block;
    private int _row;
    private float _cycle;
    private bool _mirrored;
    private Color _tint;

    private void Awake()
    {
        _traveller = GetComponent<StreetTraveller>();
        _block = new MaterialPropertyBlock();
        _row = Random.Range(0, atlasGrid.y);
        _cycle = Random.value;
        _tint = tints != null && tints.Length > 0 ? tints[Random.Range(0, tints.Length)] : Color.white;
    }

    private void Start()
    {
        // Kök yürüme yönüne dönük doğar; kart ise hep restorana bakar. Atlastaki karakter dünya −X'e doğru yürür
        // gibi çizilidir: +X'e yürüyen figüran aynalanır.
        _mirrored = transform.forward.x > 0f;
        card.transform.rotation = Quaternion.identity;
    }

    private void LateUpdate()
    {
        _cycle = Mathf.Repeat(_cycle + _traveller.Speed / metersPerCycle * Time.deltaTime, 1f);
        int frame = Mathf.Min(framesPerCycle - 1, Mathf.FloorToInt(_cycle * framesPerCycle));

        float width = 1f / atlasGrid.x;
        float height = 1f / atlasGrid.y;
        // Satırlar atlasta yukarıdan aşağıya dizilidir; doku koordinatı aşağıdan başlar.
        float offsetY = 1f - (_row + 1) * height;
        var scaleOffset = _mirrored
            ? new Vector4(-width, height, (frame + 1) * width, offsetY)
            : new Vector4(width, height, frame * width, offsetY);

        card.GetPropertyBlock(_block);
        _block.SetVector(BaseMapScaleOffset, scaleOffset);
        _block.SetColor(BaseColor, _tint);
        card.SetPropertyBlock(_block);
    }
}
