using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Sinyal çarkı (GDD 3.6.0): R basılıyken açık, iç içe — önce kategori, sonra değer. Dilimler YALNIZCA replike
// veriden (LevelDirector.SignalRows -> SignalWheelModel) kurulur; kodda kategori/değer listesi ve sayısı yoktur.
// Seçim: imleç kilitli kalır, çark açıldığından beri biriken fare hareketinin yönü dilimi vurgular; SOL TIK seçer
// (kategori -> içine gir, değer -> sinyali gönder), SAĞ TIK üst kata döner.
//
// İstemci tahmin eder, sunucu karar verir: kim açabilir (EmoteSystem.CanRoleSignal), round oynanabilir mi
// (GameLoopManager.CanPlayersAct) ve oynayan sinyal bitti mi (EmoteSystem.IsLocalBusy) burada yalnızca GÖSTERİM
// içindir; asıl karar RequestSignalServerRpc'dedir. Oynayan sinyal bitene kadar çark "engelli" görünür ve tıklama
// bir şey göndermez.
public class SignalWheelUI : MonoBehaviour
{
    // PlayerController (bakış) ve PlayerInteractor (sol tık) çark açıkken fare girdisini tüketmez.
    public static bool IsWheelOpen { get; private set; }

    [SerializeField] private InputActionAsset inputActions;
    [Tooltip("Çarkı açan action'ın adı (Player haritasında; R).")]
    [SerializeField] private string openActionName = "SignalWheel";

    [Header("Çark")]
    [SerializeField] private GameObject wheelRoot;
    [Tooltip("Dilim şablonu: kökünde Image, çocuğunda Text. Her seçenek için kopyalanır.")]
    [SerializeField] private GameObject sliceTemplate;
    [SerializeField] private Text centerLabel;
    [SerializeField] private float radius = 170f;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightedColor = Color.yellow;
    [Tooltip("Oynayan sinyal bitene kadar dilimlerin rengi (engelli).")]
    [SerializeField] private Color blockedColor = new(0.45f, 0.45f, 0.45f, 1f);

    private readonly List<Image> _slices = new();
    private readonly List<SignalRow> _rowBuffer = new();
    private InputAction _openAction;
    private List<SignalWheelModel.Option> _top;
    private List<SignalWheelModel.Option> _current;
    private int _highlighted = -1;
    private Vector2 _accumulated;
    private bool _open;

    private void Start()
    {
        wheelRoot.SetActive(false);
        sliceTemplate.SetActive(false);

        var playerMap = inputActions.FindActionMap("Player");
        playerMap.Enable();
        _openAction = playerMap.FindAction(openActionName);
        if (_openAction == null)
        {
            Debug.LogError($"[SignalWheelUI] '{openActionName}' action'ı bulunamadı; sinyal çarkı açılamaz.");
            return;
        }

        _openAction.started += HandleOpenStarted;
        _openAction.canceled += HandleOpenCanceled;
    }

    private void OnDestroy()
    {
        if (_openAction != null)
        {
            _openAction.started -= HandleOpenStarted;
            _openAction.canceled -= HandleOpenCanceled;
        }

        IsWheelOpen = false;
    }

    // Çarkı açma koşulu — sunucunun RequestSignalServerRpc'de uyguladığı kuralların aynısı (rol + round).
    private static bool CanOpen()
    {
        return GameLoopManager.CanPlayersAct
            && EmoteSystem.Instance != null && RoleManager.Instance != null
            && EmoteSystem.Instance.CanRoleSignal(RoleManager.Instance.LocalRole);
    }

    private void HandleOpenStarted(InputAction.CallbackContext context)
    {
        if (_open || EmoteWheelUI.IsWheelOpen || !CanOpen() || LevelDirector.Instance == null)
            return;

        _rowBuffer.Clear();
        var rows = LevelDirector.Instance.SignalRows;
        for (int i = 0; i < rows.Count; i++)
            _rowBuffer.Add(rows[i]);

        _top = SignalWheelModel.Build(LevelDirector.Instance.Config, _rowBuffer, EmoteSystem.Instance.OrderDoneSignal);
        if (_top.Count == 0)
            return;

        _open = true;
        IsWheelOpen = true;
        wheelRoot.SetActive(true);
        ShowLevel(_top);
    }

    private void HandleOpenCanceled(InputAction.CallbackContext context) => Close();

    private void Close()
    {
        if (!_open)
            return;

        _open = false;
        IsWheelOpen = false;
        wheelRoot.SetActive(false);
    }

    private void ShowLevel(List<SignalWheelModel.Option> options)
    {
        _current = options;
        _highlighted = -1;
        _accumulated = Vector2.zero;

        foreach (var slice in _slices)
            Destroy(slice.gameObject);
        _slices.Clear();

        // Dilim i, yukarıdan (90°) başlayıp saat yönünün tersine eşit aralıkla dizilir.
        float step = 360f / options.Count;
        for (int i = 0; i < options.Count; i++)
        {
            var sliceObject = Instantiate(sliceTemplate, sliceTemplate.transform.parent);
            sliceObject.SetActive(true);

            float angle = (90f + i * step) * Mathf.Deg2Rad;
            ((RectTransform)sliceObject.transform).anchoredPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

            var label = sliceObject.GetComponentInChildren<Text>(true);
            if (label != null)
                label.text = options[i].Label;

            _slices.Add(sliceObject.GetComponent<Image>());
        }

        RefreshVisuals();
    }

    private void Update()
    {
        if (!_open)
            return;

        // Round bitti / oyun durdu / rol değişti: açık çark kapanır.
        if (!CanOpen())
        {
            Close();
            return;
        }

        var mouse = Mouse.current;
        if (mouse == null)
            return;

        _accumulated += mouse.delta.ReadValue();
        if (_accumulated.sqrMagnitude >= 4f)
        {
            float angle = Mathf.Atan2(_accumulated.y, _accumulated.x) * Mathf.Rad2Deg - 90f;
            if (angle < 0f)
                angle += 360f;

            _highlighted = Mathf.RoundToInt(angle / (360f / _current.Count)) % _current.Count;
        }

        RefreshVisuals();

        if (mouse.rightButton.wasPressedThisFrame && _current != _top)
        {
            ShowLevel(_top);
            return;
        }

        if (!mouse.leftButton.wasPressedThisFrame || _highlighted < 0 || EmoteSystem.Instance.IsLocalBusy)
            return;

        var option = _current[_highlighted];
        if (!option.IsLeaf)
        {
            ShowLevel(option.Children);
            return;
        }

        EmoteSystem.Instance.RequestSignalServerRpc(option.ChannelIndex, option.ValueIndex);
        ShowLevel(_top);
    }

    private void RefreshVisuals()
    {
        bool busy = EmoteSystem.Instance != null && EmoteSystem.Instance.IsLocalBusy;
        for (int i = 0; i < _slices.Count; i++)
            _slices[i].color = busy ? blockedColor : (i == _highlighted ? highlightedColor : normalColor);

        if (centerLabel != null)
            centerLabel.text = _highlighted >= 0 ? _current[_highlighted].Label : string.Empty;
    }
}
