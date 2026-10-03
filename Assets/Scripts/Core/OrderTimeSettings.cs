using UnityEngine;

// Sipariş süresi formülünün ayarları (GDD 3.4.1): süre = (taban + sinyal sayısı × katsayı) × seviye çarpanı.
// Taban ve katsayı tüm seviyelerde ortaktır ve buradan ayarlanır (playtest'te yeniden kalibre edilecek); seviye
// çarpanı LevelConfig'tedir. Kodda hiçbiri yazılı değildir.
[CreateAssetMenu(fileName = "OrderTimeSettings", menuName = "Cook No Evil/Order Time Settings")]
public class OrderTimeSettings : ScriptableObject
{
    [Tooltip("Sinyal sayısından bağımsız taban süre (sn). GDD başlangıç değeri: 32.")]
    [SerializeField, Min(0f)] private float baseSeconds = 32f;
    [Tooltip("Sinyal başına eklenen süre (sn). GDD başlangıç değeri: 4,3.")]
    [SerializeField, Min(0f)] private float secondsPerSignal = 4.3f;

    public float BaseSeconds => baseSeconds;
    public float SecondsPerSignal => secondsPerSignal;
}
