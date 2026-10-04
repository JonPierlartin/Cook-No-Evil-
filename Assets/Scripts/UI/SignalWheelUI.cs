using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Sinyal çarkı (GDD 3.6.0): R basılıyken açık, iç içe — önce kategori (dilimler), sonra değer (baloncuklar).
// Seçenekler YALNIZCA replike veriden (LevelDirector.SignalRows -> SignalWheelModel) kurulur; kodda kategori/değer
// listesi ve sayısı yoktur. Görünüm genel emote çarkıyla ORTAKTIR (WheelView).
// Seçim: imleç kilitli kalır, çark açıldığından beri biriken fare hareketinin yönü seçeneği vurgular; SOL TIK seçer
// (kategori -> içine gir, değer -> sinyali gönder), SAĞ TIK üst kata döner.
//
// İstemci tahmin eder, sunucu karar verir: kim açabilir (EmoteSystem.CanRoleSignal), round oynanabilir mi
// (GameLoopManager.CanPlayersAct) ve oynayan jest bitti mi (EmoteSystem.IsLocalBusy) burada yalnızca GÖSTERİM
// içindir; asıl karar RequestSignalServerRpc'dedir. Oynayan jest bitene kadar çark "engelli" görünür ve tıklama
// bir şey göndermez.
public class SignalWheelUI : MonoBehaviour
{
    // PlayerController (bakış) ve PlayerInteractor (sol tık) çark açıkken fare girdisini tüketmez.
    public static bool IsWheelOpen { get; private set; }

    [SerializeField] private InputActionAsset inputActions;
    [Tooltip("Çarkı açan action'ın adı (Player haritasında; R).")]
    [SerializeField] private string openActionName = "SignalWheel";
    [Tooltip("Ortak çark görünümü (emote çarkıyla paylaşılır).")]
    [SerializeField] private WheelView view;

    private readonly List<SignalRow> _rowBuffer = new();
    private InputAction _openAction;
    private List<SignalWheelModel.Option> _top;
    private List<SignalWheelModel.Option> _current;
    private int _highlighted = -1;
    private Vector2 _accumulated;
    private bool _open;

    private void Start()
    {
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
        // Tarif kitapçığı ya da ESC menüsü açıkken çark açılmaz.
        return GameLoopManager.CanPlayersAct && !RecipeBookUI.IsOpen && !PauseMenuUI.IsOpen
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
        ShowLevel(_top);
    }

    private void HandleOpenCanceled(InputAction.CallbackContext context) => Close();

    private void Close()
    {
        if (!_open)
            return;

        _open = false;
        IsWheelOpen = false;
        view.Hide();
    }

    private void ShowLevel(List<SignalWheelModel.Option> options)
    {
        _current = options;
        _highlighted = -1;
        _accumulated = Vector2.zero;

        // Açılar veriden (yön değerleri kendi yönünde durur) ya da eşit aralıkla — bkz. SignalWheelModel.
        var angles = SignalWheelModel.ResolveAngles(options);
        var viewOptions = new List<WheelView.Option>(options.Count);
        for (int i = 0; i < options.Count; i++)
            viewOptions.Add(new WheelView.Option { Label = options[i].Label, Icon = options[i].Icon, Angle = angles[i] });

        // Üst kat (kategoriler) dilim, alt kat (jestin kendisi) baloncuk görünür.
        view.Show(viewOptions, options == _top ? WheelView.Style.Wedges : WheelView.Style.Bubbles);
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
            _highlighted = view.Pick(_accumulated);

        bool busy = EmoteSystem.Instance.IsLocalBusy;
        view.Refresh(_highlighted, busy);

        if (mouse.rightButton.wasPressedThisFrame && _current != _top)
        {
            ShowLevel(_top);
            return;
        }

        if (!mouse.leftButton.wasPressedThisFrame || _highlighted < 0 || busy)
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
}
