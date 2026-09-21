using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

// LMB (Attack action) ile dunya objeleriyle etkilesim: sadece hedef bulup sunucuya
// "bununla etkilesmek istiyorum" niyetini bildiren genel bir yonlendirici. Hangi eylemin
// (alma/birakma/paketleme) gerceklestigine KARIsMAZ — bu, hedeflenen istasyonun kendi
// mantigina (ve rol/envanter dogrulamasi icin kendi ServerRpc'sine) ait. Boylece Bilesen
// 2'nin istasyon objeleri bu bilesen tarafindan YENIDEN YAZILMADAN, oldugu gibi kullanilir.
//
// K6 (CLAUDE.md) DUZELTMESI: Istemci ARTIK etkilesimin sonucuna karar vermiyor. Kendi
// raycast'iyle sadece bir HEDEF secip NetworkObjectReference'ini sunucuya gonderiyor;
// gecerlilik (hedef spawn edilmis mi, menzil icinde mi, oyuncu hedefe donuk mu) TAMAMEN
// sunucuda dogrulaniyor. Basili-tutma zamanlayicisi HoldOrPressInteractable.Update()'te
// zaten var (degistirilmedi) — BeginPress()/EndPress() artik SADECE sunucu tarafindan
// cagriliyor, bu yuzden o zamanlayici da fiilen sunucu-otoriteli calisiyor.
//
// GDD 4.1.2 (1) crosshair: ayni raycast (TryGetCurrentTarget) her karede de calisir ve sonuc
// HoldOrPressInteractable.CanInteract sorgusuyla Feedback'e cevrilir. Bu SADECE gosterim
// tahminidir — RPC gonderilmez; gercek karar, ayni sorguyu soran sunucudadir (K6).
[RequireComponent(typeof(NetworkObject))]
public class PlayerInteractor : NetworkBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactRange = 2.5f;
    [SerializeField] private LayerMask interactableLayer;

    [Tooltip("Sunucu yon dogrulamasi: oyuncu kokunun yatay forward'i ile hedefe olan yon arasindaki dot product bu esigin ustunde olmalidir (1 = tam karsida, 0 = 90 derece). NetworkTransform yalnizca yaw'i senkronize ettigi icin (pitch yerel, bkz. PlayerController) dogrulama sadece yatay duzlemde yapilabilir.")]
    [SerializeField, Range(0f, 1f)] private float aimDotThreshold = 0.5f;

    private InputAction _attackAction;

    // Yalnizca yerel (owner) oyuncunun etkilesimcisi; CrosshairUI ve PlacementPreview buradan okur.
    public static PlayerInteractor Local { get; private set; }
    // Crosshair'in su an baktigi hedef (yoksa null) ve o hedef icin crosshair durumu — her karede
    // TEK raycast'ten (TryGetCurrentTarget) uretilir.
    public HoldOrPressInteractable CurrentTarget { get; private set; }
    public CrosshairState Feedback { get; private set; }

    // Sadece sunucuda anlamlidir: bu oyuncunun su an basili tuttugu hedef. Client'in kendi
    // kopyasinda bu alan hic kullanilmaz (RequestInteractServerRpc/RequestEndInteractServerRpc
    // govdeleri NGO tarafindan yalnizca sunucuda calistirilir).
    private HoldOrPressInteractable _serverPressedInteractable;

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

        Local = this;
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
        ClearLocal();
        enabled = false;
    }

    private void ClearLocal()
    {
        if (Local == this)
            Local = null;

        CurrentTarget = null;
        Feedback = CrosshairState.Neutral;
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
        ClearLocal();
    }

    // enabled yalnizca owner'da true (bkz. ApplyOwnershipState/ApplyNonOwnerState).
    private void Update()
    {
        CurrentTarget = TryGetCurrentTarget(out var target) ? target : null;
        Feedback = ComputeFeedback(CurrentTarget);
    }

    private CrosshairState ComputeFeedback(HoldOrPressInteractable target)
    {
        if (target == null)
            return CrosshairState.Neutral;

        return target.CanInteract(NetworkManager.LocalClientId, out _)
            ? CrosshairState.Usable
            : CrosshairState.Blocked;
    }

    // Istemci burada YALNIZCA niyetini ve hedefini bildirir. Sonuc (basarili/basarisiz)
    // burada hesaplanmaz — asagidaki RequestInteractServerRpc'nin sunucu tarafindaki
    // govdesinde belirlenir.
    private void HandleAttackStarted(InputAction.CallbackContext context)
    {
        // "Oyun durduruldu" (round sirasinda bir oyuncu koptugunda, bkz. GameLoopManager)
        // TUM oyuncular icin gecerli — sadece kopan oyuncunun kendi objesi degil. Bu, gereksiz
        // bir RPC gonderimini onlemek icin sadece bir ON-kontroldur; asil yetki asagidaki
        // RequestInteractServerRpc icindeki sunucu-taraf kontrolundedir (K6 geregi).
        if (GameLoopManager.Instance != null && GameLoopManager.Instance.IsGamePaused)
            return;

        if (!TryGetCurrentTarget(out var target))
            return;

        var targetNetworkObject = target.GetComponentInParent<NetworkObject>();
        if (targetNetworkObject == null)
        {
            Debug.LogWarning($"[PlayerInteractor] Hedef '{target.name}' bir NetworkObject'e sahip degil, istek gonderilemiyor.");
            return;
        }

        RequestInteractServerRpc(targetNetworkObject);
    }

    private void HandleAttackCanceled(InputAction.CallbackContext context)
    {
        RequestEndInteractServerRpc();
    }

    // Sunucu, cagiranin kimligini ServerRpcParams'tan okur ve gecerliligi (hedef spawn
    // edilmis mi, HoldOrPressInteractable tasiyor mu, menzil ve yon uygun mu) TAMAMEN
    // kendi tarafinda dogrular. Istemcinin hesapladigi hicbir sonuc parametresi YOKTUR.
    [ServerRpc]
    private void RequestInteractServerRpc(NetworkObjectReference targetRef, ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        // Sunucu-taraf pause kontrolu (K6 geregi — "Dogrulamalar sunucu tarafinda yapilir,
        // UI kontroluyle degil"): istemcideki on-kontrol (HandleAttackStarted) bypass edilse
        // bile sunucu, round durdurulmusken hicbir yeni etkilesimi kabul etmez.
        if (GameLoopManager.Instance != null && GameLoopManager.Instance.IsGamePaused)
        {
            Debug.LogWarning($"[PlayerInteractor] Sunucu etkilesim istegini reddetti (client={senderId}): oyun durduruldu.");
            return;
        }

        if (!TryValidateTarget(senderId, targetRef, out var interactable, out var reason))
        {
            Debug.LogWarning($"[PlayerInteractor] Sunucu etkilesim istegini reddetti (client={senderId}): {reason}");
            return;
        }

        _serverPressedInteractable = interactable;
        interactable.BeginPress(senderId);
    }

    [ServerRpc]
    private void RequestEndInteractServerRpc(ServerRpcParams rpcParams = default)
    {
        if (_serverPressedInteractable == null)
            return;

        _serverPressedInteractable.EndPress();
        _serverPressedInteractable = null;
    }

    private bool TryValidateTarget(ulong senderId, NetworkObjectReference targetRef, out HoldOrPressInteractable interactable, out string reason)
    {
        interactable = null;
        reason = string.Empty;

        if (!targetRef.TryGet(out var targetObject) || targetObject == null)
        {
            reason = "hedef NetworkObject cozumlenemedi (spawn edilmemis olabilir)";
            return false;
        }

        interactable = targetObject.GetComponentInChildren<HoldOrPressInteractable>();
        if (interactable == null)
        {
            reason = "hedefte HoldOrPressInteractable yok";
            return false;
        }

        if (NetworkManager == null || !NetworkManager.ConnectedClients.TryGetValue(senderId, out var client) || client.PlayerObject == null)
        {
            reason = "cagiran oyuncunun PlayerObject'i bulunamadi";
            return false;
        }

        Transform playerRoot = client.PlayerObject.transform;
        Vector3 toTarget = targetObject.transform.position - playerRoot.position;
        float distance = toTarget.magnitude;

        if (distance > interactRange)
        {
            reason = $"menzil disi ({distance:F2}m > {interactRange:F2}m)";
            interactable = null;
            return false;
        }

        // Yalnizca yatay duzlemde dogrulama: NetworkTransform oyuncu kokunde ve
        // Owner-otoriteli, yani yaw senkronize edilir ama pitch edilmez (pitch
        // PlayerController'da yerel olarak CameraPivot'a uygulanir) — sunucu istemcinin
        // tam nisan vektorunu yeniden uretemez. Faz 0 icin yeterlidir (odalar ayridir).
        Vector3 flatToTarget = new Vector3(toTarget.x, 0f, toTarget.z).normalized;
        Vector3 flatForward = new Vector3(playerRoot.forward.x, 0f, playerRoot.forward.z).normalized;
        float dot = Vector3.Dot(flatForward, flatToTarget);

        if (dot < aimDotThreshold)
        {
            reason = $"yon disi (dot={dot:F2} < {aimDotThreshold:F2})";
            interactable = null;
            return false;
        }

        // Rol / envanter / istasyon kurallari: crosshair'in istemcide sordugu SORGUNUN AYNISI.
        if (!interactable.CanInteract(senderId, out var gateReason))
        {
            reason = gateReason;
            interactable = null;
            return false;
        }

        return true;
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
