using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Sadece Kasiyer icin aktif E-basili-tutma radyal emote menusu. HoldOrPressInteractable'dan
// BAGIMSIZ (bu bir dunya-objesi etkilesimi degil, rol-bazli bir UI menusu) — kendi
// ham Interact (E) basma/birakma girisini okur ve carktaki dilimi mouse pozisyonuna
// gore secer. Ayni bilesen, Yamak tarafinda alinan son emote ikonunu kisa sureligine
// gosterir (Risk Duzeltmesi 2'deki "sadece ilgili rolun HUD'unda instantiate/goster"
// deseniyle tutarli).
public class EmoteWheelUI : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;

    [Header("Kasiyer - Cark")]
    [SerializeField] private GameObject wheelRoot;
    [SerializeField] private RectTransform wheelCenter;
    [SerializeField] private Image[] slotIcons;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightedColor = Color.yellow;

    [Header("Yamak - Alinan Emote")]
    [SerializeField] private Image receivedEmoteIcon;
    [SerializeField] private float receivedEmoteDisplayDuration = 3f;

    private InputAction _interactAction;
    private InputAction _pointAction;
    private int _highlightedIndex = -1;
    private bool _wheelOpen;
    private Coroutine _receivedEmoteRoutine;

    private void Start()
    {
        if (wheelRoot != null)
            wheelRoot.SetActive(false);

        if (receivedEmoteIcon != null)
            receivedEmoteIcon.gameObject.SetActive(false);

        InitializeWheelIcons();

        if (RoleManager.Instance != null)
            RoleManager.Instance.OnLocalRoleAssigned += HandleLocalRoleAssigned;

        if (EmoteSystem.Instance != null)
            EmoteSystem.Instance.OnEmoteReceived += HandleEmoteReceived;
    }

    private void OnDestroy()
    {
        if (RoleManager.Instance != null)
            RoleManager.Instance.OnLocalRoleAssigned -= HandleLocalRoleAssigned;

        if (EmoteSystem.Instance != null)
            EmoteSystem.Instance.OnEmoteReceived -= HandleEmoteReceived;

        if (_interactAction != null)
        {
            _interactAction.started -= HandleInteractStarted;
            _interactAction.canceled -= HandleInteractCanceled;
        }
    }

    // BULUNAN HATA: carktaki dilim Image'larina hicbir yerde sprite atanmiyordu —
    // sadece HighlightSlot/ResetHighlight .color'i degistiriyordu (secili/normal tonu).
    // Bu yuzden Kasiyer kendi carkinda uc dilimi de sprite'siz (varsayilan beyaz
    // dikdortgen) goruyordu; Yamak'in gordugu renk dogruydu cunku o ayri bir Image
    // (receivedEmoteIcon) ve HandleEmoteReceived zaten sprite atiyordu.
    private void InitializeWheelIcons()
    {
        if (slotIcons == null || EmoteSystem.Instance == null || EmoteSystem.Instance.AvailableEmotes == null)
            return;

        var availableEmotes = EmoteSystem.Instance.AvailableEmotes;
        for (int i = 0; i < slotIcons.Length && i < availableEmotes.Length; i++)
        {
            if (slotIcons[i] != null && availableEmotes[i] != null)
                slotIcons[i].sprite = availableEmotes[i].Icon;
        }
    }

    private void HandleLocalRoleAssigned(PlayerRole role)
    {
        if (role != PlayerRole.Kasiyer || _interactAction != null)
            return;

        var playerMap = inputActions.FindActionMap("Player");
        playerMap.Enable();
        _interactAction = playerMap.FindAction("Interact");
        _pointAction = inputActions.FindActionMap("UI").FindAction("Point");
        _interactAction.started += HandleInteractStarted;
        _interactAction.canceled += HandleInteractCanceled;
    }

    private void HandleInteractStarted(InputAction.CallbackContext context)
    {
        // Savunma amacli tekrar kontrol: HandleLocalRoleAssigned zaten Kasiyer-disi
        // rollerde bu callback'i hic abone etmiyor, ama gercek 3-kisilik testte Yamak'ta
        // da imlecin acildigi bildirildi — GUNCEL rolu burada da dogrulamak (onbellege
        // guvenmek yerine, RoleManager'in gecmis round-baslama senkron sorunlarindaki
        // ayni "canli oku" duzeltmesiyle tutarli) bu sinifi kokten kapatiyor.
        if (RoleManager.Instance == null || RoleManager.Instance.LocalRole != PlayerRole.Kasiyer)
            return;

        _wheelOpen = true;
        if (wheelRoot != null)
            wheelRoot.SetActive(true);

        // Round aktifken imlec kilitli/gizli (FPS kontrolu icin, bkz. LobbyUIController).
        // Kilitliyken InputSystem'in mutlak Point/Mouse.position degeri artik hareket
        // etmiyor (sabit kaliyor) — bu da carktaki mutlak-pozisyon tabanli dilim secimini
        // calismaz hale getirirdi. Cark acik oldugu surece imleci gecici serbest birakiyoruz.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void HandleInteractCanceled(InputAction.CallbackContext context)
    {
        if (RoleManager.Instance == null || RoleManager.Instance.LocalRole != PlayerRole.Kasiyer)
            return;

        _wheelOpen = false;
        if (wheelRoot != null)
            wheelRoot.SetActive(false);

        if (_highlightedIndex >= 0)
            EmoteSystem.Instance?.SelectEmoteServerRpc(_highlightedIndex);

        ResetHighlight();

        // Cark kapaninca imleci FPS kontrolu icin tekrar kilitle/gizle (normal durum)
        // veya acik/gorunur birak — LobbyUIController.ShouldLockCursor (YEREL bayrak)
        // kullanilir, RoleManager.IsRoundActive DEGIL: o bir NetworkVariable, host
        // disconnect sonrasi client'ta hicbir yerde resetlenmedigi icin stale kalabiliyor
        // (bkz. LobbyUIController — ayni sinif bug host-disconnect senaryosunda da
        // bulunup duzeltildi).
        bool shouldLock = LobbyUIController.Instance != null && LobbyUIController.Instance.ShouldLockCursor;
        Cursor.lockState = shouldLock ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !shouldLock;
    }

    private void Update()
    {
        if (!_wheelOpen || _pointAction == null || wheelCenter == null || slotIcons == null || slotIcons.Length == 0)
            return;

        Vector2 mouseScreenPos = _pointAction.ReadValue<Vector2>();
        Vector2 centerScreenPos = RectTransformUtility.WorldToScreenPoint(null, wheelCenter.position);
        Vector2 direction = mouseScreenPos - centerScreenPos;

        if (direction.sqrMagnitude < 4f)
            return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (angle < 0f)
            angle += 360f;

        // Dilim ikonlari carkta 90 derece (yukari) merkezli baslayip saat yonunun
        // tersine yerlestiriliyor (slot i -> 90 + i*sliceSize derece). Bunu hesaba
        // katmadan (0 derece = sag) index hesaplamak her zaman komsu dilimi
        // isaretliyordu (bulunan gercek hata: mouse'un uzerinde durdugu degil,
        // yanindaki dilim parliyordu). Aciyi ayni offsetle kaydirip normalize ediyoruz.
        float sliceSize = 360f / slotIcons.Length;
        float adjustedAngle = angle - 90f;
        if (adjustedAngle < 0f)
            adjustedAngle += 360f;

        int index = Mathf.RoundToInt(adjustedAngle / sliceSize) % slotIcons.Length;
        HighlightSlot(index);
    }

    private void HighlightSlot(int index)
    {
        if (index == _highlightedIndex)
            return;

        ResetHighlight();
        _highlightedIndex = index;
        if (slotIcons[index] != null)
            slotIcons[index].color = highlightedColor;
    }

    private void ResetHighlight()
    {
        if (_highlightedIndex >= 0 && slotIcons[_highlightedIndex] != null)
            slotIcons[_highlightedIndex].color = normalColor;

        _highlightedIndex = -1;
    }

    private void HandleEmoteReceived(int emoteIndex)
    {
        if (receivedEmoteIcon == null || EmoteSystem.Instance?.AvailableEmotes == null)
            return;

        if (emoteIndex < 0 || emoteIndex >= EmoteSystem.Instance.AvailableEmotes.Length)
            return;

        receivedEmoteIcon.sprite = EmoteSystem.Instance.AvailableEmotes[emoteIndex].Icon;
        receivedEmoteIcon.gameObject.SetActive(true);

        if (_receivedEmoteRoutine != null)
            StopCoroutine(_receivedEmoteRoutine);
        _receivedEmoteRoutine = StartCoroutine(HideReceivedEmoteAfterDelay());
    }

    private IEnumerator HideReceivedEmoteAfterDelay()
    {
        yield return new WaitForSeconds(receivedEmoteDisplayDuration);
        receivedEmoteIcon.gameObject.SetActive(false);
        _receivedEmoteRoutine = null;
    }
}
