using System;
using UnityEngine;

// Headless (input-agnostic) etkilesim primitive'i: dunyada "bu objeye LMB ile
// etkilesildi" bilgisini BeginPress()/EndPress() cagrilariyla alir, kim/nasil
// cagirdigina (raycast, trigger, vb.) karisMAZ — PlayerInteractor bu cagriyi
// yapan taraf. Unity InputSystem'in kendi "Hold" interaction'ina bagli DEGIL:
// ham basma/birakma zamanlamasini kendi Update()'inde sayar, boylece her istasyon
// (orn. Paketleme istasyonu vs Diyafon) kendi holdDuration'ini bagimsiz secebilir.
[DisallowMultipleComponent]
public class HoldOrPressInteractable : MonoBehaviour
{
    [SerializeField] private InteractionType interactionType = InteractionType.Press;
    [Tooltip("Sadece InteractionType.Hold icin: basili tutulmasi gereken sure (saniye).")]
    [SerializeField] private float holdDuration = 2.5f;

    // Ham basma/birakma — orn. Diyafon'un "basili tutuldugu surece kanal acik" davranisi icin.
    public event Action OnPressBegin;
    public event Action OnPressEnd;

    // Press: BeginPress ile aninda tamamlanir. Hold: holdDuration dolunca tamamlanir.
    public event Action OnInteractionCompleted;
    // Sadece Hold: sure dolmadan birakilirsa tetiklenir.
    public event Action OnInteractionCancelled;

    public bool IsPressed { get; private set; }

    private float _pressStartTime;
    private bool _completedThisPress;

    public void BeginPress()
    {
        if (IsPressed)
            return;

        IsPressed = true;
        _completedThisPress = false;
        _pressStartTime = Time.time;
        OnPressBegin?.Invoke();

        if (interactionType == InteractionType.Press)
        {
            _completedThisPress = true;
            OnInteractionCompleted?.Invoke();
        }
    }

    public void EndPress()
    {
        if (!IsPressed)
            return;

        IsPressed = false;
        OnPressEnd?.Invoke();

        if (interactionType == InteractionType.Hold && !_completedThisPress)
            OnInteractionCancelled?.Invoke();
    }

    private void Update()
    {
        if (!IsPressed || interactionType != InteractionType.Hold || _completedThisPress)
            return;

        if (Time.time - _pressStartTime >= holdDuration)
        {
            _completedThisPress = true;
            OnInteractionCompleted?.Invoke();
        }
    }
}
