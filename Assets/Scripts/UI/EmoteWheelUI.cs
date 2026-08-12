using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Kasiyer VE Yamak icin aktif E-basili-tutma radyal emote menusu (Sef'te hic acilmaz;
// Yamak'in secimi Kasiyer'e gore kisitli — bkz. EmoteSystem.YamakEmoteLimit).
// HoldOrPressInteractable'dan BAGIMSIZ (bu bir dunya-objesi etkilesimi degil, rol-bazli
// bir UI menusu) — kendi ham Interact (E) basma/birakma girisini okur ve carktaki dilimi
// mouse pozisyonuna gore secer. Secilen emote'un GORSEL TEPKISI (renk parlamasi +
// egilme/ziplama) artik secimi yapan oyuncunun kendi karakterinde (PlayerEmoteReactor,
// herkesin ekraninda) oynatiliyor — eskiden burada Yamak'a ozel ayri bir
// "ReceivedEmoteIcon" UI'i vardi, o tasarim kaldirildi (bkz. EmoteSystem.cs ust notu).
public class EmoteWheelUI : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;

    [Header("Kasiyer - Cark")]
    [SerializeField] private GameObject wheelRoot;
    [SerializeField] private RectTransform wheelCenter;
    [SerializeField] private Image[] slotIcons;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightedColor = Color.yellow;

    private InputAction _interactAction;
    private InputAction _pointAction;
    private int _highlightedIndex = -1;
    private int _activeSlotCount;
    private bool _wheelOpen;

    private void Start()
    {
        if (wheelRoot != null)
            wheelRoot.SetActive(false);

        InitializeWheelIcons();

        if (RoleManager.Instance != null)
            RoleManager.Instance.OnLocalRoleAssigned += HandleLocalRoleAssigned;
    }

    private void OnDestroy()
    {
        if (RoleManager.Instance != null)
            RoleManager.Instance.OnLocalRoleAssigned -= HandleLocalRoleAssigned;

        if (_interactAction != null)
        {
            _interactAction.started -= HandleInteractStarted;
            _interactAction.canceled -= HandleInteractCanceled;
        }
    }

    // BULUNAN HATA: carktaki dilim Image'larina hicbir yerde sprite atanmiyordu —
    // sadece HighlightSlot/ResetHighlight .color'i degistiriyordu (secili/normal tonu).
    // Bu yuzden Kasiyer kendi carkinda uc dilimi de sprite'siz (varsayilan beyaz
    // dikdortgen) goruyordu. Duzeltildi.
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

    // Kasiyer tam listeyle, Yamak kisitli (ilk N) listeyle carka erisebilir (bkz.
    // EmoteSystem.YamakEmoteLimit). Sef'in carka hic erisimi yok.
    private static bool IsWheelRole(PlayerRole role) => role == PlayerRole.Kasiyer || role == PlayerRole.Yamak;

    private void HandleLocalRoleAssigned(PlayerRole role)
    {
        if (!IsWheelRole(role) || _interactAction != null)
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
        if (RoleManager.Instance == null || !IsWheelRole(RoleManager.Instance.LocalRole))
            return;

        // Kasiyer icin tum dilimler, Yamak icin sadece ilk N dilim aktif/tiklanabilir
        // olur (kisitli liste — bkz. EmoteSystem.YamakEmoteLimit). Geri kalan dilimler
        // gizlenir ki Yamak yanlislikla erisimi olmayan bir emote'u secmeye calismasin.
        bool isKasiyer = RoleManager.Instance.LocalRole == PlayerRole.Kasiyer;
        int yamakLimit = EmoteSystem.Instance != null ? EmoteSystem.Instance.YamakEmoteLimit : 0;
        _activeSlotCount = isKasiyer ? slotIcons.Length : Mathf.Clamp(yamakLimit, 0, slotIcons.Length);

        for (int i = 0; i < slotIcons.Length; i++)
        {
            if (slotIcons[i] != null)
                slotIcons[i].gameObject.SetActive(i < _activeSlotCount);
        }

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
        if (RoleManager.Instance == null || !IsWheelRole(RoleManager.Instance.LocalRole))
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
        if (!_wheelOpen || _pointAction == null || wheelCenter == null || slotIcons == null || _activeSlotCount == 0)
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
        float sliceSize = 360f / _activeSlotCount;
        float adjustedAngle = angle - 90f;
        if (adjustedAngle < 0f)
            adjustedAngle += 360f;

        int index = Mathf.RoundToInt(adjustedAngle / sliceSize) % _activeSlotCount;
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
}
