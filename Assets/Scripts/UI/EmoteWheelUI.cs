using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Genel emote çarkı (GDD 3.6.0): E basılıyken açık, TÜM rollerde. Oynanışa etki etmeyen sosyal emote'lar; tek katlı
// (baloncuklar). Görünüm ve seçim biçimi sinyal çarkıyla ORTAKTIR (WheelView): imleç kilitli kalır, biriken fare
// hareketinin yönü seçeneği vurgular, SOL TIK emote'u gönderir. Seçenekler veriden gelir (EmoteSystem.AvailableEmotes);
// kodda emote listesi ya da sayısı yoktur.
//
// İstemci tahmin eder, sunucu karar verir: round oynanabilir mi (GameLoopManager.CanPlayersAct) ve oynayan jest bitti
// mi (EmoteSystem.IsLocalBusy) burada yalnızca GÖSTERİM içindir; asıl karar SelectEmoteServerRpc'dedir. Cooldown
// yoktur; oynayan jest bitene kadar çark "engelli" görünür ve tıklama bir şey göndermez.
public class EmoteWheelUI : MonoBehaviour
{
    // PlayerController (bakış) ve PlayerInteractor (sol tık) çark açıkken fare girdisini tüketmez.
    public static bool IsWheelOpen { get; private set; }

    [SerializeField] private InputActionAsset inputActions;
    [Tooltip("Çarkı açan action'ın adı (Player haritasında; E).")]
    [SerializeField] private string openActionName = "Interact";
    [Tooltip("Ortak çark görünümü (sinyal çarkıyla paylaşılır).")]
    [SerializeField] private WheelView view;

    private InputAction _openAction;
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
            Debug.LogError($"[EmoteWheelUI] '{openActionName}' action'ı bulunamadı; emote çarkı açılamaz.");
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

    // Sunucunun SelectEmoteServerRpc'de uyguladığı koşulun aynısı; rol kısıtı yoktur.
    private static bool CanOpen()
    {
        return GameLoopManager.CanPlayersAct && !RecipeBookUI.IsOpen && !PauseMenuUI.IsOpen && EmoteSystem.Instance != null;
    }

    private void HandleOpenStarted(InputAction.CallbackContext context)
    {
        if (_open || SignalWheelUI.IsWheelOpen || !CanOpen())
            return;

        var emotes = EmoteSystem.Instance.AvailableEmotes;
        int count = emotes != null ? emotes.Length : 0;
        if (count == 0)
            return;

        // Yukarıdan başlayıp eşit aralıkla dizilir; seçeneğin sırası = emote dizini.
        var options = new List<WheelView.Option>(count);
        for (int i = 0; i < count; i++)
        {
            options.Add(new WheelView.Option
            {
                Label = emotes[i] != null ? emotes[i].DisplayName : string.Empty,
                Icon = emotes[i] != null ? emotes[i].Icon : null,
                Angle = 90f + i * 360f / count
            });
        }

        _open = true;
        IsWheelOpen = true;
        _highlighted = -1;
        _accumulated = Vector2.zero;
        view.Show(options, WheelView.Style.Bubbles);
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

    private void Update()
    {
        if (!_open)
            return;

        // Round bitti / oyun durdu / kitap ya da menü açıldı: açık çark kapanır.
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

        if (!mouse.leftButton.wasPressedThisFrame || _highlighted < 0 || busy)
            return;

        EmoteSystem.Instance.SelectEmoteServerRpc(_highlighted);
        // Seçimden sonra vurgu sıfırlanır; çark tuş bırakılana kadar açık kalır.
        _highlighted = -1;
        _accumulated = Vector2.zero;
    }
}
