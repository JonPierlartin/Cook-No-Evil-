using System;
using UnityEngine;

// Bir emote'un el hareketi — VERİDİR (EmoteDefinition'da durur); ProceduralCharacterAnimator oynatır. Karakterlerde
// iskelet/klip olmadığı için emote, elin gideceği yer + yönü + parmak kıvrımı + küçük bir salınım olarak tarif
// edilir; yeni bir emote kod değil veri ekler. Değerler SAĞ EL içindir ve karakterin yerel uzayındadır; sol elde
// X ekseninde aynalanır. Elin yeri gövdenin sınır kutusuna ORANLA verilir (hamburger, ketçap ve kasa çok farklı
// ölçülerde: sabit bir noktaya göre metreyle yazılan konum birinde başın üstünde, ötekinde havada kalıyordu).
[Serializable]
public class EmoteHandPose
{
    [Tooltip("Kapalıysa bu emote'un el hareketi yoktur (yer tutucu tepki oynar).")]
    [SerializeField] private bool enabled;
    [Tooltip("İki el birden (sol el aynalı). Kapalıysa tek el: sağ el, sağ elde öğe varsa sol el.")]
    [SerializeField] private bool bothHands;
    [Tooltip("Elin GÖVDEYE göre yeri, gövdenin sınır kutusuna oranla: x = yan (0 orta, 1 gövdenin sağ kenarı), " +
        "y = yükseklik (0 gövdenin altı, 1 tepesi), z = ön-arka (1 gövdenin ön yüzü). Karakterlerin boyu ve eni çok " +
        "farklı olduğu için yer metreyle değil oranla verilir; aynı emote her karakterde gövdenin aynı yerine gelir.")]
    [SerializeField] private Vector3 anchor = new(1f, 0.7f, 1f);
    [Tooltip("Bu noktadan dışarı pay (m): el gövdenin içine girmesin diye.")]
    [SerializeField] private Vector3 offset;
    [Tooltip("Parmakların gösterdiği yön.")]
    [SerializeField] private Vector3 fingerDirection = Vector3.up;
    [Tooltip("Başparmağın baktığı yön.")]
    [SerializeField] private Vector3 thumbDirection = Vector3.left;
    [Header("Parmaklar (0 = açık, 1 = kapalı)")]
    [SerializeField, Range(0f, 1f)] private float indexCurl;
    [SerializeField, Range(0f, 1f)] private float othersCurl;
    [SerializeField, Range(0f, 1f)] private float thumbCurl;
    [Header("Salınım")]
    [Tooltip("Elin ileri-geri gittiği mesafe (m), eksen başına.")]
    [SerializeField] private Vector3 swing;
    [Tooltip("Elin bilekten sallandığı eksen ve açı (derece).")]
    [SerializeField] private Vector3 wagAxis = Vector3.forward;
    [SerializeField] private float wagAngle;
    [Tooltip("Salınım hızı (devir/sn).")]
    [SerializeField, Min(0f)] private float rate = 2f;

    public bool Enabled => enabled;
    public bool BothHands => bothHands;
    public Vector3 Anchor => anchor;
    public Vector3 Offset => offset;
    public Vector3 FingerDirection => fingerDirection;
    public Vector3 ThumbDirection => thumbDirection;
    public float IndexCurl => indexCurl;
    public float OthersCurl => othersCurl;
    public float ThumbCurl => thumbCurl;
    public Vector3 Swing => swing;
    public Vector3 WagAxis => wagAxis;
    public float WagAngle => wagAngle;
    public float Rate => rate;
}
