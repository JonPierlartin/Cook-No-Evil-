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
    // Tum event'ler etkilesimi baslatan oyuncunun BAGLAMINI (InteractionContext: clientId + o anki
    // secili slot) tasir (CLAUDE.md NGO notu — etkilesim olaylari kimlik tasimak zorundadir; aksi
    // halde dinleyen taraf, sunucunun kendi yerel cagrisinda varsayilan/yanlis bir SenderClientId'ye
    // dusebilir). Slotun da baglamda tasinmasinin sebebi: ActiveSlotIndex (NetworkVariable) ile
    // ardindan gonderilen bu olay arasinda sira garantisi yok — dinleyenler ASLA ActiveSlotIndex.Value
    // okumamali, BeginPress'e o an gelen context.SlotIndex'i kullanmalidir.
    public event Action<InteractionContext> OnPressBegin;
    public event Action<InteractionContext> OnPressEnd;

    // Press: BeginPress ile aninda tamamlanir. Hold: holdDuration dolunca tamamlanir.
    public event Action<InteractionContext> OnInteractionCompleted;
    // Sadece Hold: sure dolmadan birakilirsa tetiklenir.
    public event Action<InteractionContext> OnInteractionCancelled;

    public bool IsPressed { get; private set; }

    private float _pressStartTime;
    private bool _completedThisPress;
    private InteractionContext _context;
    private IInteractionGate[] _gates;
    private Collider[] _colliders;

    // "Yeterince yakin miyim ve hedefe donuk muyum?" sorusunun TEK cevap noktasi. Istemci
    // (crosshair, yerlestirme onizlemesi) ve sunucu (RequestInteractServerRpc) AYNI fonksiyonu
    // cagirir; iki farkli formul yoktur. Sunucu yalnizca esikleri (maxDistance, minAimDot)
    // genisleterek tolerans ekler — istemci her zaman daha katidir.
    //
    // Olcu: gozden bu nesnenin collider'larinin EN YAKIN yuzey noktasina (pivota degil). Yon:
    // gozun yatay bakis yonu ile goz -> en yakin nokta yonu arasindaki yatay dot product (pitch
    // agdan senkronize edilmedigi icin yalnizca yatay). Not: Collider.ClosestPoint yalnizca Box,
    // Sphere, Capsule ve convex MeshCollider ile calisir.
    public ReachResult CheckReach(Vector3 eye, Vector3 forward, float maxDistance, float minAimDot, out float distance, out float aimDot)
    {
        _colliders ??= GetComponentsInChildren<Collider>();

        Vector3 nearest = eye;
        float nearestSqrDistance = float.PositiveInfinity;
        foreach (var collider in _colliders)
        {
            if (collider == null || !collider.enabled)
                continue;

            Vector3 point = collider.ClosestPoint(eye);
            float sqrDistance = (point - eye).sqrMagnitude;
            if (sqrDistance < nearestSqrDistance)
            {
                nearestSqrDistance = sqrDistance;
                nearest = point;
            }
        }

        distance = Mathf.Sqrt(nearestSqrDistance);

        Vector3 flatToPoint = new Vector3(nearest.x - eye.x, 0f, nearest.z - eye.z);
        Vector3 flatForward = new Vector3(forward.x, 0f, forward.z);
        // Goz collider'in icindeyse / tam ustundeyse yatay yon tanimsizdir: hedefe donuk sayilir.
        aimDot = flatToPoint.sqrMagnitude > Mathf.Epsilon && flatForward.sqrMagnitude > Mathf.Epsilon
            ? Vector3.Dot(flatForward.normalized, flatToPoint.normalized)
            : 1f;

        if (distance > maxDistance)
            return ReachResult.OutOfRange;

        return aimDot < minAimDot ? ReachResult.OutOfAim : ReachResult.InReach;
    }

    // Ayni nesnedeki TUM IInteractionGate'lere sorar; biri bile "hayir" derse sonuc hayirdir.
    // Gate'i olmayan nesne herkese aciktir. Sunucu (RequestInteractServerRpc, istasyonlarin
    // tamamlanma mantigi) ve istemci (crosshair) AYNI sorguyu kullanir — kural tek yerde yasar.
    public bool CanInteract(InteractionContext context, out string reason)
    {
        _gates ??= GetComponents<IInteractionGate>();

        foreach (var gate in _gates)
        {
            if (!gate.CanInteract(context, out reason))
                return false;
        }

        reason = null;
        return true;
    }

    public void BeginPress(InteractionContext context)
    {
        if (IsPressed)
            return;

        IsPressed = true;
        _completedThisPress = false;
        _pressStartTime = Time.time;
        _context = context;
        OnPressBegin?.Invoke(_context);

        if (interactionType == InteractionType.Press)
        {
            _completedThisPress = true;
            OnInteractionCompleted?.Invoke(_context);
        }
    }

    public void EndPress()
    {
        if (!IsPressed)
            return;

        IsPressed = false;
        OnPressEnd?.Invoke(_context);

        if (interactionType == InteractionType.Hold && !_completedThisPress)
            OnInteractionCancelled?.Invoke(_context);
    }

    private void Update()
    {
        if (!IsPressed || interactionType != InteractionType.Hold || _completedThisPress)
            return;

        if (Time.time - _pressStartTime >= holdDuration)
        {
            _completedThisPress = true;
            OnInteractionCompleted?.Invoke(_context);
        }
    }
}
