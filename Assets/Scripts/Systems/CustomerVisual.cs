using UnityEngine;

// Müşterinin yer tutucu görsel geri bildirimi: gövde rengi replike duruma göre değişir (geliyor / sipariş bekliyor /
// siparişi alındı / ayrılıyor). Herkes görür; süre bilgisi taşımaz (o yalnızca Kasiyer'e — CustomerTimerDisplay).
// Final karakter ve animasyon gelince bu bileşen değişir, Customer değişmez.
public class CustomerVisual : MonoBehaviour
{
    [SerializeField] private Customer customer;
    [SerializeField] private Renderer body;
    [SerializeField] private Color arrivingColor = new(0.75f, 0.75f, 0.75f);
    [SerializeField] private Color waitingColor = new(0.95f, 0.8f, 0.3f);
    [SerializeField] private Color orderedColor = new(0.35f, 0.6f, 0.95f);
    [SerializeField] private Color leavingColor = new(0.45f, 0.45f, 0.45f);
    [Tooltip("Ayrılırken: memnun (doğru teslim) / öfkeli (yanlış ya da geç). GDD 7.1.1 sevinç/öfke animasyonunun yer tutucusu.")]
    [SerializeField] private Color happyColor = new(0.25f, 0.85f, 0.3f);
    [SerializeField] private Color angryColor = new(0.9f, 0.15f, 0.1f);

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private MaterialPropertyBlock _block;

    private void Awake()
    {
        _block = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        customer.State.OnValueChanged += HandleStateChanged;
        customer.Mood.OnValueChanged += HandleMoodChanged;
        Apply(customer.State.Value);
    }

    private void OnDisable()
    {
        customer.State.OnValueChanged -= HandleStateChanged;
        customer.Mood.OnValueChanged -= HandleMoodChanged;
    }

    private void HandleMoodChanged(CustomerMood previous, CustomerMood current) => Apply(customer.State.Value);

    private void HandleStateChanged(CustomerState previous, CustomerState current) => Apply(current);

    private void Apply(CustomerState state)
    {
        Color color = state switch
        {
            CustomerState.WaitingToOrder => waitingColor,
            CustomerState.Ordered => orderedColor,
            CustomerState.Leaving => customer.Mood.Value switch
            {
                CustomerMood.Happy => happyColor,
                CustomerMood.Angry => angryColor,
                _ => leavingColor
            },
            _ => arrivingColor
        };

        body.GetPropertyBlock(_block);
        _block.SetColor(BaseColorId, color);
        body.SetPropertyBlock(_block);
    }
}
