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
        _wheelOpen = false;
        if (wheelRoot != null)
            wheelRoot.SetActive(false);

        if (_highlightedIndex >= 0)
            EmoteSystem.Instance?.SelectEmoteServerRpc(_highlightedIndex);

        ResetHighlight();

        // Cark kapaninca round hala aktifse (normal durum, emote'lar zaten sadece
        // round'da gonderilebiliyor) imleci FPS kontrolu icin tekrar kilitle/gizle;
        // degilse (ör. bu sirada round bittiyse) acik/gorunur birak — LobbyUIController'daki
        // kuralla ayni.
        bool roundActive = RoleManager.Instance != null && RoleManager.Instance.IsRoundActive.Value;
        Cursor.lockState = roundActive ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !roundActive;
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

        float sliceSize = 360f / slotIcons.Length;
        int index = Mathf.RoundToInt(angle / sliceSize) % slotIcons.Length;
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
