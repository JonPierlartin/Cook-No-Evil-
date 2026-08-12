using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

// LMB (Attack action) ile dunya objeleriyle etkilesim: sadece hedef bulup
// HoldOrPressInteractable.BeginPress()/EndPress()'i cagiran genel bir yonlendirici.
// Hangi eylemin (alma/birakma/paketleme) gerceklestigine KARIsMAZ — bu, hedeflenen
// istasyonun kendi mantigina (ve rol/envanter dogrulamasi icin kendi ServerRpc'sine)
// ait. Boylece Bilesen 2'nin istasyon objeleri bu bilesen tarafindan YENIDEN
// YAZILMADAN, oldugu gibi kullanilir.
[RequireComponent(typeof(NetworkObject))]
public class PlayerInteractor : NetworkBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactRange = 2.5f;
    [SerializeField] private LayerMask interactableLayer;

    private InputAction _attackAction;
    private HoldOrPressInteractable _pressedTarget;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            enabled = false;
            return;
        }

        var playerMap = inputActions.FindActionMap("Player");
        playerMap.Enable();
        _attackAction = playerMap.FindAction("Attack");
        _attackAction.started += HandleAttackStarted;
        _attackAction.canceled += HandleAttackCanceled;
    }

    public override void OnNetworkDespawn()
    {
        if (_attackAction == null)
            return;

        _attackAction.started -= HandleAttackStarted;
        _attackAction.canceled -= HandleAttackCanceled;
    }

    private void HandleAttackStarted(InputAction.CallbackContext context)
    {
        if (!TryGetCurrentTarget(out var target))
        {
            // TESHIS (gercek build'de LMB etkilesiminin calismama raporu icin):
            // bu log SADECE raycast hicbir HoldOrPressInteractable bulamadiginda basar
            // (her frame degil, sadece tiklama aninda) — bir sonraki gercek testte bu
            // satir Player.log'da COK sik/hic gorunmuyorsa sorun raycast/layer/menzil
            // katmaninda DEGIL, bulunan hedefin kendi mantiginda (network/ownership)
            // aranmali. Teshis netlesince bu log kaldirilmali.
            Debug.Log($"[PlayerInteractor] Hedef bulunamadi (camera={(playerCamera != null)}, range={interactRange}, layerMask={interactableLayer.value}).");
            return;
        }

        // Basis anindaki hedef "kilitlenir" — basili tutarken crosshair objeden
        // ayrilsa bile EndPress orijinal hedefe gider (HoldOrPressInteractable
        // kendi started/canceled durumunu buna gore yonetiyor).
        _pressedTarget = target;
        _pressedTarget.BeginPress();
    }

    private void HandleAttackCanceled(InputAction.CallbackContext context)
    {
        if (_pressedTarget == null)
            return;

        _pressedTarget.EndPress();
        _pressedTarget = null;
    }

    private bool TryGetCurrentTarget(out HoldOrPressInteractable interactable)
    {
        interactable = null;

        if (playerCamera == null)
            return false;

        if (!Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out var hit, interactRange, interactableLayer))
            return false;

        interactable = hit.collider.GetComponentInParent<HoldOrPressInteractable>();
        return interactable != null;
    }
}
