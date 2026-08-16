using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Kasiyer VE Yamak icin aktif E-basili-tutma radyal emote menusu (Sef'te hic acilmaz;
// Yamak'in secimi Kasiyer'e gore kisitli — bkz. EmoteSystem.YamakEmoteLimit).
// HoldOrPressInteractable'dan BAGIMSIZ (bu bir dunya-objesi etkilesimi degil, rol-bazli
// bir UI menusu) — kendi ham Interact (E) basma/birakma girisini okur. Rust'in insa
// carki gibi: imlec HER ZAMAN kilitli/gizli kalir, dilim secimi carktan beri biriken
// ham mouse delta'sinin yonune gore yapilir (bkz. Update()). Secilen emote'un GORSEL
// TEPKISI (renk parlamasi + egilme/ziplama) secimi yapan oyuncunun kendi karakterinde
// (PlayerEmoteReactor, herkesin ekraninda) oynatiliyor — eskiden burada Yamak'a ozel
// ayri bir "ReceivedEmoteIcon" UI'i vardi, o tasarim kaldirildi (bkz. EmoteSystem.cs
// ust notu).
public class EmoteWheelUI : MonoBehaviour
{
    // PlayerController, cark acikken kamera yaw/pitch'inin mouse delta'sini OKUMAMASI
    // icin bunu kontrol eder — mouse delta'nin tek tuketicisi ayni anda ya cark ya
    // kamera olur, ikisi birden degil. Statik: bu bilesenden client basina tek bir
    // ornek olur (GameplayCanvas uzerinde), instance referansina gerek yok.
    public static bool IsWheelOpen { get; private set; }

    [SerializeField] private InputActionAsset inputActions;

    [Header("Kasiyer - Cark")]
    [SerializeField] private GameObject wheelRoot;
    [SerializeField] private Image[] slotIcons;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightedColor = Color.yellow;

    // Merkez panel: imlecin isaret ettigi (en yakin acidaki) dilimin BUYUK ikonu +
    // adi + kisa aciklamasi — imlec hareket ettikce (HighlightSlot her degisimde)
    // canli guncellenir. Rust'in insa carkindaki merkez bilgi paneline benzer bir
    // sunum — mimari/network tarafina (broadcast RPC, role-gating) DOKUNMUYOR, sadece
    // sunum katmani.
    [Header("Merkez Panel - Secili Emote Detayi")]
    [SerializeField] private Image centerIcon;
    [SerializeField] private Text centerDisplayName;
    [SerializeField] private Text centerDescription;

    private InputAction _interactAction;
    private int _highlightedIndex = -1;
    private int _activeSlotCount;
    private bool _wheelOpen;
    // Cark acikken imlec KILITLI/GIZLI kalir (bkz. HandleInteractStarted) — mutlak
    // ekran pozisyonu artik anlamsiz (sabit kalir). Bunun yerine, cark acildigindan
    // beri biriken ham mouse delta'sinin YONU, hangi dilimin vurgulanacagini belirler
    // (Rust'in insa carki gibi).
    private Vector2 _accumulatedOffset;

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

        IsWheelOpen = false;
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

        // "Oyun durduruldu" (bkz. PlayerController/PlayerInteractor ayni kontrol) carki
        // da kapsar — YENI bir cark acilamaz. Zaten acik bir cark varsa (HandleInteractCanceled)
        // kapatilmasini BILEREK engellemiyoruz, PlayerInteractor'daki ayni prensiple tutarli.
        if (GameLoopManager.Instance != null && GameLoopManager.Instance.IsGamePaused.Value)
            return;

        // Ayni/farkli emote farketmeksizin, son secimden itibaren cooldown suresi
        // dolmadan cark hic acilmiyor (yamakEmoteLimit gibi client tarafinda da
        // kontrol edilir — sunucu zaten SelectEmoteServerRpc icinde ayrica reddeder).
        // Cark acilamadigi icin secim yapilamaz, ekstra bir "kilitli dilim" gosterimine
        // gerek kalmiyor.
        if (EmoteSystem.Instance != null && EmoteSystem.Instance.IsOnCooldown)
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
        IsWheelOpen = true;
        if (wheelRoot != null)
            wheelRoot.SetActive(true);

        ClearCenterPanel();
        _accumulatedOffset = Vector2.zero;

        // BULUNAN HATA: cark acilirken imlec BILEREK serbest/gorunur birakiliyordu —
        // Rust'in insa carkinda imlec HER ZAMAN kilitli/gizli kalir (normal FPS gorusu),
        // sadece ham mouse delta'si dilim secimi icin kullanilir. Cursor.lockState/visible
        // artik burada HIC DEGISTIRILMIYOR — round aktifken zaten kilitli/gizli olan
        // durum oldugu gibi korunuyor (bkz. Update() — artik mutlak pozisyon degil
        // biriken delta okunuyor).
    }

    private void HandleInteractCanceled(InputAction.CallbackContext context)
    {
        if (RoleManager.Instance == null || !IsWheelRole(RoleManager.Instance.LocalRole))
            return;

        _wheelOpen = false;
        IsWheelOpen = false;
        if (wheelRoot != null)
            wheelRoot.SetActive(false);

        if (_highlightedIndex >= 0)
            EmoteSystem.Instance?.SelectEmoteServerRpc(_highlightedIndex);

        ResetHighlight();
        ClearCenterPanel();
        _accumulatedOffset = Vector2.zero;

        // Cursor.lockState/visible artik burada da DEGISTIRILMIYOR — cark hicbir zaman
        // imlec kilit durumunu degistirmedigi icin geri alacak bir sey de yok (bkz.
        // HandleInteractStarted).
    }

    private void Update()
    {
        if (!_wheelOpen || slotIcons == null || _activeSlotCount == 0)
            return;

        // Imlec kilitli/gizli oldugu icin mutlak pozisyon yerine, cark acildigindan beri
        // biriken ham mouse delta'si "sanal imlec pozisyonu" gibi kullanilir — Mouse.current
        // her zaman gercek donanim delta'sini raporlar, Cursor.lockState'ten bagimsizdir.
        Vector2 delta = Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
        _accumulatedOffset += delta;

        if (_accumulatedOffset.sqrMagnitude < 4f)
            return;

        float angle = Mathf.Atan2(_accumulatedOffset.y, _accumulatedOffset.x) * Mathf.Rad2Deg;
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

        RefreshCenterPanel(index);
    }

    private void ResetHighlight()
    {
        if (_highlightedIndex >= 0 && slotIcons[_highlightedIndex] != null)
            slotIcons[_highlightedIndex].color = normalColor;

        _highlightedIndex = -1;
    }

    // Imlecin isaret ettigi dilimin buyuk ikonu/adi/aciklamasi — HighlightSlot her
    // gercek degisimde (ayni dilimde kalirken tekrar tekrar degil) cagirir.
    private void RefreshCenterPanel(int index)
    {
        var availableEmotes = EmoteSystem.Instance != null ? EmoteSystem.Instance.AvailableEmotes : null;
        if (availableEmotes == null || index < 0 || index >= availableEmotes.Length || availableEmotes[index] == null)
            return;

        var emote = availableEmotes[index];

        if (centerIcon != null)
        {
            centerIcon.sprite = emote.Icon;
            centerIcon.enabled = emote.Icon != null;
        }

        if (centerDisplayName != null)
            centerDisplayName.text = emote.DisplayName;

        if (centerDescription != null)
            centerDescription.text = emote.Description;
    }

    private void ClearCenterPanel()
    {
        if (centerIcon != null)
        {
            centerIcon.sprite = null;
            centerIcon.enabled = false;
        }

        if (centerDisplayName != null)
            centerDisplayName.text = string.Empty;

        if (centerDescription != null)
            centerDescription.text = string.Empty;
    }
}
