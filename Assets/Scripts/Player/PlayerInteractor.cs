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
//
// Menzil/yon: raycast yalnizca HEDEFI secer ("neye bakiyorum"); "yeterince yakin miyim / donuk
// muyum" sorusunu istemcide (crosshair, onizleme) ve sunucuda AYNI fonksiyon cevaplar:
// HoldOrPressInteractable.CheckReach. Istemcinin "olur" dedigini sunucu reddetmemeli: sunucu
// ayni fonksiyona yalnizca daha genis esikler (tolerans) verir.
[RequireComponent(typeof(NetworkObject))]
public class PlayerInteractor : NetworkBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private Camera playerCamera;
    [Tooltip("Goz noktasi (oyuncu kopyasindaki CameraPivot). Menzil/yon kontrolu hem istemcide hem sunucuda buradan olculur; goz yuksekligi kodda tutulmaz.")]
    [SerializeField] private Transform cameraPivot;
    [Tooltip("Goz noktasindan hedef collider'inin en yakin noktasina azami mesafe (m).")]
    [SerializeField] private float interactRange = 2.5f;
    [Tooltip("Nisan isininin carpacagi KATI katmanlar: oyuncular, duvarlar, dekor ve etkilesim hedefleri. Isin ilk carptigi collider bir etkilesim hedefine aitse hedef odur, degilse hedef YOKTUR (GDD 4.1.2: hedef gorunur olmali). Onizleme katmani (PlacementPreview), Ignore Raycast ve UI bu maskede OLMAMALI; trigger'lar zaten yok sayilir.")]
    [SerializeField] private LayerMask aimMask;

    [Tooltip("Yon esigi: gozun yatay bakis yonu ile goz -> hedefin en yakin noktasi yonu arasindaki dot product bu esigin ustunde olmalidir (1 = tam karsida, 0 = 90 derece). NetworkTransform yalnizca yaw'i senkronize ettigi icin (pitch yerel, bkz. PlayerController) kontrol sadece yatay duzlemde yapilir.")]
    [SerializeField, Range(0f, 1f)] private float aimDotThreshold = 0.5f;

    [Header("Sunucu toleransi (ag gecikmesi payi — istemci bunu KULLANMAZ, her zaman daha katidir)")]
    [Tooltip("Sunucu, menzile bu kadar (m) pay ekler.")]
    [SerializeField, Min(0f)] private float serverRangeTolerance = 0.5f;
    [Tooltip("Sunucu, yon esiginden bu kadar (dot birimi) dusuk bir degeri de kabul eder.")]
    [SerializeField, Min(0f)] private float serverAimDotTolerance = 0.15f;

    private InputAction _attackAction;
    private bool _warnedSelfHit;

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

        // Bu RPC, cagiranin KENDI oyuncu objesindeki (sunucudaki kopya) PlayerInteractor'da calisir:
        // goz noktasi o kopyanin CameraPivot'undan, yaw'i o kopyanin kokunden okunur (pitch
        // senkronize degil; kontrol yatay). Istemcinin kullandigi AYNI fonksiyon, yalnizca
        // tolerans eklenmis esiklerle.
        if (cameraPivot == null)
        {
            reason = "goz noktasi (CameraPivot) atanmamis";
            interactable = null;
            return false;
        }

        var reach = interactable.CheckReach(
            cameraPivot.position, transform.forward,
            interactRange + serverRangeTolerance, aimDotThreshold - serverAimDotTolerance,
            out float distance, out float aimDot);

        if (reach == ReachResult.OutOfRange)
        {
            reason = $"menzil disi ({distance:F2}m > {interactRange + serverRangeTolerance:F2}m)";
            interactable = null;
            return false;
        }

        if (reach == ReachResult.OutOfAim)
        {
            reason = $"yon disi (dot={aimDot:F2} < {aimDotThreshold - serverAimDotTolerance:F2})";
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

        if (playerCamera == null || cameraPivot == null)
            return false;

        // Tarama yalnizca HEDEFI secer (uzunluk sinirsiz). Katman maskesi KATI nesneleri kapsar, yani
        // isini ilk carptigi sey durdurur: onunde baska bir oyuncu/engel varsa hedef yoktur (GDD 4.1.2).
        // "Yeterince yakin miyim / donuk muyum" sorusunu tarama DEGIL, sunucuyla ortak CheckReach
        // cevaplar — istemcide tolerans YOK, sunucudan her zaman daha katidir. Sunucu engel kontrolu
        // yapmaz (pitch senkronize degil); istemci daha kati olmak zorunda oldugundan bu kurala uyar.
        if (!Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out var hit, Mathf.Infinity, aimMask, QueryTriggerInteraction.Ignore))
            return false;

        // Yerel oyuncunun KENDI collider'i isini durdurmamali. Bunu Unity'nin belgelenmis kurali saglar: ray
        // baslangici bir collider'in ICINDEYSE o collider algilanmaz; kamera (CameraPivot) kendi
        // CharacterController kapsulunun icinde durur. Kural bozulursa (kapsul kucultulur, kamera disari
        // tasinirsa) hedef sessizce kaybolmasin diye bir kez uyari basilir.
        if (hit.collider.transform.IsChildOf(transform))
        {
            if (!_warnedSelfHit)
            {
                _warnedSelfHit = true;
                Debug.LogWarning($"[PlayerInteractor] Nisan isini oyuncunun KENDI collider'ina ('{hit.collider.name}') carpti; kamera kendi collider'inin icinde olmali. Hedef secilemiyor.");
            }

            return false;
        }

        var candidate = hit.collider.GetComponentInParent<HoldOrPressInteractable>();
        if (candidate == null)
            return false;

        if (candidate.CheckReach(cameraPivot.position, transform.forward, interactRange, aimDotThreshold, out _, out _) != ReachResult.InReach)
            return false;

        interactable = candidate;
        return true;
    }
}
