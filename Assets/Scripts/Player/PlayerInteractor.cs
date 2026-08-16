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
        ApplyOwnershipState();
    }

    // BULUNAN HATA: round-ici rejoin'de PlayerSpawner ayni objeyi yeniden Spawn ETMIYOR,
    // sadece ChangeOwnership cagiriyor — OnNetworkSpawn objenin yasam dongusunde BIR KEZ
    // calisir, ownership sonradan degistiginde NGO bunu tekrar cagirmaz (bkz.
    // PlayerController'daki ayni kok neden notu). Duzeltme: kurulum/abonelik mantigi
    // ApplyOwnershipState'e alindi, hem OnNetworkSpawn'da HEM OnOwnershipChanged'da
    // cagiriliyor; cift-abonelik olmasin diye once mevcut abonelik varsa kaldiriliyor.
    //
    // IKINCI BULUNAN HATA (bkz. PlayerController'daki ayni notun detayi): NGO,
    // DontDestroyWithOwner=true bir objenin sahibi kopunca sahipligi OTOMATIK olarak
    // ServerClientId'ye devrediyor — host da her zaman client 0=ServerClientId oldugu
    // icin bu, host'ta IsOwner'i yanlislikla true yapip kopan oyuncunun LMB kontrolunu
    // host'a devrediyordu. current == ServerClientId geldiginde IsOwner'a guvenilmeyip
    // acikca sahipsiz durum zorlaniyor.
    protected override void OnOwnershipChanged(ulong previous, ulong current)
    {
        if (current == NetworkManager.ServerClientId)
        {
            ApplyNonOwnerState();
            return;
        }

        ApplyOwnershipState();
    }

    private void ApplyOwnershipState()
    {
        if (!IsOwner)
        {
            ApplyNonOwnerState();
            return;
        }

        UnsubscribeAttackAction();

        enabled = true;

        var playerMap = inputActions.FindActionMap("Player");
        playerMap.Enable();
        _attackAction = playerMap.FindAction("Attack");
        _attackAction.started += HandleAttackStarted;
        _attackAction.canceled += HandleAttackCanceled;
    }

    private void ApplyNonOwnerState()
    {
        UnsubscribeAttackAction();
        enabled = false;
    }

    private void UnsubscribeAttackAction()
    {
        if (_attackAction == null)
            return;

        _attackAction.started -= HandleAttackStarted;
        _attackAction.canceled -= HandleAttackCanceled;
        _attackAction = null;
    }

    public override void OnNetworkDespawn()
    {
        UnsubscribeAttackAction();
    }

    private void HandleAttackStarted(InputAction.CallbackContext context)
    {
        // "Oyun durduruldu" TUM oyunculari kapsar (bkz. PlayerController.Update ayni
        // kontrol) — YENI bir etkilesim baslatilamaz. Zaten devam eden bir basimin
        // EndPress'ini (HandleAttackCanceled) BILEREK engellemiyoruz, aksi halde
        // HoldOrPressInteractable "basili" durumda takili kalirdi.
        if (GameLoopManager.Instance != null && GameLoopManager.Instance.IsGamePaused.Value)
            return;

        if (!TryGetCurrentTarget(out var target))
        {
            // TESHIS (gercek build'de LMB etkilesiminin calismama raporu icin):
            // bu log SADECE raycast hicbir HoldOrPressInteractable bulamadiginda basar
            // (her frame degil, sadece tiklama aninda). Kamera pozisyonu/yonu de basiliyor
            // ki bir sonraki gercek testte oyuncunun GERCEKTEN istasyona (bilinen konum:
            // BurgerAssemblyStation ~ (7, 0.5, 4)) bakip bakmadigi Player.log'dan
            // dogrulanabilsin — bu, "kod hatasi" ile "hassas nisan/gorsel geri bildirim
            // eksikligi" ihtimallerini kesin ayirt eder.
            Vector3 camPos = playerCamera != null ? playerCamera.transform.position : Vector3.zero;
            Vector3 camFwd = playerCamera != null ? playerCamera.transform.forward : Vector3.zero;
            Debug.Log($"[PlayerInteractor] Hedef bulunamadi (camera={(playerCamera != null)}, range={interactRange}, layerMask={interactableLayer.value}, camPos={camPos}, camForward={camFwd}).");
            ReportInteractionAttemptServerRpc(false);
            return;
        }

        // Basis anindaki hedef "kilitlenir" — basili tutarken crosshair objeden
        // ayrilsa bile EndPress orijinal hedefe gider (HoldOrPressInteractable
        // kendi started/canceled durumunu buna gore yonetiyor).
        _pressedTarget = target;
        _pressedTarget.BeginPress();
        ReportInteractionAttemptServerRpc(true);
    }

    // Test/teshis amacli: gercek 3 kisilik testte LMB etkilesim denemesini (basarili/
    // basarisiz) konsola bakmadan gorebilmek icin — broadcast oldugu icin hem tiklayan
    // client hem HOST (hem ucuncu oyuncu) ekraninda kisa bir metin gosterir. Yukaridaki
    // camPos/camForward konsol logu ile BIRLIKTE calisir, biri digerinin yerine gecmez.
    // Varsayilan RequireOwnership=true kullanildi (EmoteSystem'in aksine, bu RPC
    // Player.prefab'in KENDI NetworkObject'inde — cagiran zaten dogal olarak sahibi).
    [ServerRpc]
    private void ReportInteractionAttemptServerRpc(bool succeeded)
    {
        InteractionAttemptClientRpc(succeeded);
    }

    [ClientRpc]
    private void InteractionAttemptClientRpc(bool succeeded)
    {
        InteractionToastUI.Instance?.Show(succeeded ? "Küple Etkileşime Geçiyorsun" : "Etkileşim Hedefi Bulunamadı");
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
