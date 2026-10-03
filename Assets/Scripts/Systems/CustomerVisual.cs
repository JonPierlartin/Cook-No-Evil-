using UnityEngine;

// Müşterinin yer tutucu görsel geri bildirimi: gövde rengi replike duruma göre değişir (geliyor / sipariş bekliyor /
// siparişi alındı / ayrılıyor). Herkes görür; süre bilgisi taşımaz (o yalnızca Kasiyer'e — CustomerPatienceDisplay).
// Final karakter ve animasyon gelince bu bileşen değişir, Customer değişmez.
public class CustomerVisual : MonoBehaviour
{
    [SerializeField] private Customer customer;
    [SerializeField] private Renderer body;
    [SerializeField] private Color arrivingColor = new(0.75f, 0.75f, 0.75f);
    [SerializeField] private Color waitingColor = new(0.95f, 0.8f, 0.3f);
    [SerializeField] private Color orderedColor = new(0.35f, 0.6f, 0.95f);
    [SerializeField] private Color leavingColor = new(0.45f, 0.45f, 0.45f);

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private MaterialPropertyBlock _block;

    private void Awake()
    {
        _block = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        customer.State.OnValueChanged += HandleStateChanged;
        Apply(customer.State.Value);
    }

    private void OnDisable()
    {
        customer.State.OnValueChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(CustomerState previous, CustomerState current) => Apply(current);

    private void Apply(CustomerState state)
    {
        Color color = state switch
        {
            CustomerState.WaitingToOrder => waitingColor,
            CustomerState.Ordered => orderedColor,
            CustomerState.Leaving => leavingColor,
            _ => arrivingColor
        };

        body.GetPropertyBlock(_block);
        _block.SetColor(BaseColorId, color);
        body.SetPropertyBlock(_block);
    }
}
