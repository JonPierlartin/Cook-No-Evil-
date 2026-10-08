using UnityEngine;

// Sokaktan geçen aracın görünüşü: tekerlekler hıza göre döner, gövde hafifçe yaylanır, gövde rengi doğarken
// rastgele seçilir. Hızı StreetTraveller'dan okur. Yalnızca görsel.
[RequireComponent(typeof(StreetTraveller))]
public class StreetCar : MonoBehaviour
{
    [Tooltip("Yaylanan gövde (tekerlekler ve gölge yaylanmaz).")]
    [SerializeField] private Transform body;
    [Tooltip("Sol tekerlekler (aks yerel X).")]
    [SerializeField] private Transform[] leftWheels;
    [Tooltip("Sağ tekerlekler: 180° dönük durdukları için ters yönde döndürülür.")]
    [SerializeField] private Transform[] rightWheels;
    [Tooltip("Tekerlek yarıçapı (m): açısal hız = hız / yarıçap.")]
    [SerializeField, Min(0.01f)] private float wheelRadius = 0.33f;

    [Header("Gövde rengi")]
    [SerializeField] private Renderer bodyRenderer;
    [Tooltip("Gövde renginin durduğu materyal yuvası.")]
    [SerializeField] private int bodyMaterialIndex;
    [Tooltip("Doğarken bunlardan biri seçilir. Boşsa prefab'daki renk kalır.")]
    [SerializeField] private Material[] bodyColors;

    [Header("Yaylanma")]
    [SerializeField] private float bobHeight = 0.012f;
    [SerializeField] private float bobFrequency = 2.2f;

    private StreetTraveller _traveller;
    private Vector3 _bodyRestPosition;
    private float _bobPhase;

    private void Awake()
    {
        _traveller = GetComponent<StreetTraveller>();
        if (body != null)
            _bodyRestPosition = body.localPosition;

        _bobPhase = Random.value * Mathf.PI * 2f;
        if (bodyRenderer != null && bodyColors != null && bodyColors.Length > 0)
        {
            var materials = bodyRenderer.sharedMaterials;
            if (bodyMaterialIndex >= 0 && bodyMaterialIndex < materials.Length)
            {
                materials[bodyMaterialIndex] = bodyColors[Random.Range(0, bodyColors.Length)];
                bodyRenderer.sharedMaterials = materials;
            }
        }
    }

    private void Update()
    {
        float degrees = _traveller.Speed / wheelRadius * Mathf.Rad2Deg * Time.deltaTime;
        foreach (var wheel in leftWheels)
            wheel.Rotate(degrees, 0f, 0f, Space.Self);
        foreach (var wheel in rightWheels)
            wheel.Rotate(-degrees, 0f, 0f, Space.Self);

        if (body == null)
            return;

        _bobPhase += bobFrequency * Mathf.PI * 2f * Time.deltaTime;
        body.localPosition = _bodyRestPosition + Vector3.up * (Mathf.Sin(_bobPhase) * bobHeight);
    }
}
