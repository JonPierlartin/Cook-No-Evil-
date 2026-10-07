using System;
using UnityEngine;

// Artist'in el klibi karakterin gövdesine NASIL oturtulur. Klipler tek bir karaktere (ketçap) göre yapıldı; üç
// karakterin gövdesi ve yüzü farklı yerlerde olduğu için hareketin neye göre yerleşeceği veriyle seçilir.
public enum ClipAnchorMode
{
    // Elin dinlenme yerinden klipteki yer değiştirme kadar oynar; uç noktada jest yüksekliğine kaldırılır.
    Rest,
    // Eller klipte buluşuyor (ovuşturma): buluşma noktası gövdenin önüne, jest merkezine gelir.
    HandsMeet,
    // El yüze gidiyor (gözüm üstünde): seçilen anda işaret parmağının ucu karakterin kaş noktasına gelir.
    Face,
}

[Serializable]
public class ClipAnchor
{
    [SerializeField] private ClipAnchorMode mode = ClipAnchorMode.Rest;
    [Tooltip("Yüz modunda: parmak ucunun yüze değdiği an (klibin 0–1 arası oranı).")]
    [SerializeField, Range(0f, 1f)] private float time = 0.5f;
    [Tooltip("Yüz modunda: kaş noktasından pay (m; sağ el için, sol elde X aynalanır). Parmak yüzün içine girmesin diye.")]
    [SerializeField] private Vector3 offset;

    public ClipAnchorMode Mode => mode;
    public float Time => time;
    public Vector3 Offset => offset;
}
