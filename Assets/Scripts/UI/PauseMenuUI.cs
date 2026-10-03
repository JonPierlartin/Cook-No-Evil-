using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// ESC menüsü (GDD 8.3, K9): YERELDİR — oyunu durdurmaz; bir oyuncu menüyü açtığında diğerleri oynamaya devam eder.
// GameLoopManager'daki duraklatma (kopma) ile ilgisi yoktur. Menü açıkken yalnızca yerel oyuncunun bakışı,
// hareketi ve tıklamaları durur (PlayerController / PlayerInteractor / çarklar IsOpen'ı okur).
// ESC bağlama duyarlıdır: tarif kitapçığı açıkken ESC yalnızca kitabı kapatır, menü açılmaz.
// "Lobiye dön": oturumdan ayrılıp ilk ekrana döner (uygulamayı kapatıp açmadan yeniden host/join için).
public class PauseMenuUI : MonoBehaviour
{
    public static bool IsOpen { get; private set; }

    [Tooltip("Menünün kökü; kapalıyken gizlidir.")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button leaveButton;

    private void Start()
    {
        panel.SetActive(false);
        resumeButton.onClick.AddListener(Close);
        leaveButton.onClick.AddListener(HandleLeaveClicked);
    }

    private void OnDisable()
    {
        // Oyun arayüzü kapanırsa (lobiye dönüş, kopma) menü de kapanır.
        IsOpen = false;
        if (panel != null)
            panel.SetActive(false);
    }

    private void Update()
    {
        // Sonuç ekranında ESC menüsü çalışmaz (Ersel, 3 Eki): orada tek akış host'un seçimidir. Açıkken bölüm
        // biterse menü kapanır.
        bool resultScreen = GameLoopManager.Instance != null && GameLoopManager.Instance.CurrentRoundState.Value == RoundState.RoundEnded;
        if (resultScreen)
        {
            if (IsOpen)
            {
                IsOpen = false;
                panel.SetActive(false);
            }

            return;
        }

        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            // Kitap açıksa (ya da bu karede ESC ile kapandıysa) ESC kitaba aittir.
            if (RecipeBookUI.IsOpen || RecipeBookUI.LastCloseFrame == Time.frameCount)
                return;

            if (IsOpen)
                Close();
            else
                Open();
        }

        if (!IsOpen)
            return;

        // Düğmelere tıklanabilsin diye imleç serbest (odak geri gelince de serbest kalsın).
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Open()
    {
        IsOpen = true;
        panel.SetActive(true);
    }

    private void Close()
    {
        if (!IsOpen)
            return;

        IsOpen = false;
        panel.SetActive(false);

        // Round sürüyorsa imleç yeniden kilitlenir (sonuç ekranında serbest kalır).
        bool playing = GameLoopManager.Instance != null && GameLoopManager.Instance.IsRoundActive;
        Cursor.lockState = playing ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !playing;
    }

    private void HandleLeaveClicked()
    {
        IsOpen = false;
        panel.SetActive(false);
        if (LobbyUIController.Instance != null)
            LobbyUIController.Instance.LeaveToInitialScreen();
    }
}
