using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Test/teshis amacli gecici ekran bildirimi — LMB etkilesim denemelerini (basarili/
// basarisiz) konsola bakmadan gorsel olarak gostermek icin (gercek coklu-makine
// testinde konsola erisim genelde sadece host'ta oluyor). PlayerInteractor'daki her
// deneme broadcast ClientRpc ile TUM client'lara yayinlanir (EmoteSystem'deki broadcast
// deseniyle tutarli), boylece hem tiklayan hem host (hem ucuncu oyuncu) gorur.
public class InteractionToastUI : MonoBehaviour
{
    public static InteractionToastUI Instance { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private Text messageText;
    [SerializeField] private float displayDuration = 1.5f;

    private Coroutine _hideRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (panel != null)
            panel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Show(string message)
    {
        if (messageText != null)
            messageText.text = message;

        if (panel != null)
            panel.SetActive(true);

        if (_hideRoutine != null)
            StopCoroutine(_hideRoutine);
        _hideRoutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);

        if (panel != null)
            panel.SetActive(false);

        _hideRoutine = null;
    }
}
