using UnityEngine;

// Bir kanalın tek değeri (ör. Yön/Yukarı, Sayı/3). Çarkta gösterilen ve animasyonla oynatılan birim.
// Çark ve animasyon bağlantısı sonraki adımlarda eklenir; şimdilik kimlik + görünen ad.
[CreateAssetMenu(fileName = "SignalValue", menuName = "Cook No Evil/Signal Value")]
public class SignalValue : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField] private Sprite icon;

    public string DisplayName => displayName;
    public Sprite Icon => icon;
}
