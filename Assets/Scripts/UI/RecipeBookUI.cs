using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Tarif kitapçığının görünümü (GDD 3.6.2). Tamamen YEREL: yalnızca okuyan oyuncunun ekranında açılır, ağ senkronu
// yoktur. Açıkken oyuncu kitaba kilitlenir (PlayerController hareket/bakışı, PlayerInteractor dünya tıklamasını,
// sinyal ve emote çarkları açılmayı IsOpen'dan okuyup durdurur).
//  - Sol tık: sonraki açılım (sondan sonra içindekilere döner). İçindekilerde bir kategorinin ÜSTÜNE sol tık o
//    kategorinin ilk açılımına atlar.
//  - Sağ tık: önceki açılım.
//  - ESC: YALNIZCA kitabı kapatır (K9: ESC bağlama duyarlıdır; duraklatma menüsü kitap açıkken açılmaz).
// İçerik RecipeBookModel'den: bu bölümde açık varyantlar (LevelDirector.OpenVariantIndices, replike). Kodda sayfa
// sayısı ya da liste yoktur. Açılımın resmi BurgerVariant.Image'dır — müşteri pop-up'ıyla AYNI asset.
public class RecipeBookUI : MonoBehaviour
{
    public static RecipeBookUI Instance { get; private set; }

    // PlayerController / PlayerInteractor / çarklar bunu okur.
    public static bool IsOpen { get; private set; }

    [Tooltip("Kitabın kökü; kapalıyken gizlidir.")]
    [SerializeField] private GameObject root;

    [Header("İçindekiler açılımı")]
    [SerializeField] private GameObject contentsPage;
    [SerializeField] private RectTransform categoryContainer;
    [Tooltip("Kategori girişi şablonu; her kategori için kopyalanır.")]
    [SerializeField] private RecipeBookEntry categoryTemplate;

    [Header("Varyant açılımı (sol: resim, sağ: malzemeler)")]
    [SerializeField] private GameObject variantPage;
    [SerializeField] private UnityEngine.UI.Image variantImage;
    [SerializeField] private RectTransform ingredientContainer;
    [Tooltip("Malzeme girişi şablonu; her malzeme için kopyalanır.")]
    [SerializeField] private RecipeBookEntry ingredientTemplate;

    [SerializeField] private UnityEngine.UI.Text pageLabel;

    private readonly List<GameObject> _spawned = new();
    private readonly List<RectTransform> _categoryRects = new();
    private RecipeBookModel.Book _book;
    private int _spread;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        root.SetActive(false);
        categoryTemplate.gameObject.SetActive(false);
        ingredientTemplate.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        IsOpen = false;
    }

    public void Open()
    {
        if (IsOpen || LevelDirector.Instance == null || !GameLoopManager.CanPlayersAct)
            return;

        _book = RecipeBookModel.Build(LevelDirector.Instance.GetOpenVariants());
        IsOpen = true;
        root.SetActive(true);
        Show(0);
    }

    private void Close()
    {
        if (!IsOpen)
            return;

        IsOpen = false;
        root.SetActive(false);

        // Kitap imleci serbest bırakmıştı (içindekilerde tıklama); round sürüyorsa yeniden kilitlenir.
        bool playing = GameLoopManager.Instance != null && GameLoopManager.Instance.IsRoundActive;
        Cursor.lockState = playing ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !playing;
    }

    private void Update()
    {
        if (!IsOpen)
            return;

        // Round bitti / oyun durdu: kitap kapanır.
        if (!GameLoopManager.CanPlayersAct)
        {
            Close();
            return;
        }

        // İçindekilerde kategoriye tıklanabilsin diye imleç serbest (odak geri gelince de serbest kalsın).
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            Close();
            return;
        }

        var mouse = Mouse.current;
        if (mouse == null)
            return;

        if (mouse.rightButton.wasPressedThisFrame)
        {
            Show(_spread > 0 ? _spread - 1 : _book.Spreads.Count - 1);
            return;
        }

        if (!mouse.leftButton.wasPressedThisFrame)
            return;

        // İçindekiler: tıklanan kategori varsa onun ilk açılımına atla.
        if (_book.Spreads[_spread] == null)
        {
            Vector2 pointer = mouse.position.ReadValue();
            for (int i = 0; i < _categoryRects.Count; i++)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(_categoryRects[i], pointer, null))
                {
                    Show(_book.Categories[i].FirstSpread);
                    return;
                }
            }
        }

        // Sonraki açılım; sondan sonra içindekiler.
        Show((_spread + 1) % _book.Spreads.Count);
    }

    private void Show(int spread)
    {
        _spread = spread;

        foreach (var entry in _spawned)
            Destroy(entry);
        _spawned.Clear();
        _categoryRects.Clear();

        var variant = _book.Spreads[spread];
        contentsPage.SetActive(variant == null);
        variantPage.SetActive(variant != null);
        pageLabel.text = $"{spread + 1} / {_book.Spreads.Count}";

        if (variant == null)
        {
            foreach (var category in _book.Categories)
            {
                var entry = Instantiate(categoryTemplate, categoryContainer);
                entry.gameObject.SetActive(true);
                entry.Show(category.Protein != null ? category.Protein.Icon : null, category.Protein != null ? category.Protein.DisplayName : "?");
                _spawned.Add(entry.gameObject);
                _categoryRects.Add((RectTransform)entry.transform);
            }

            return;
        }

        // Sol: varyantın resmi (pop-up ile aynı asset). Sağ: içerdiği malzemeler, verideki sırayla.
        variantImage.sprite = variant.Image;
        variantImage.enabled = variant.Image != null;
        foreach (var ingredient in variant.Ingredients)
        {
            if (ingredient.item == null)
                continue;

            var entry = Instantiate(ingredientTemplate, ingredientContainer);
            entry.gameObject.SetActive(true);
            entry.Show(ingredient.item.Icon, ingredient.item.DisplayName);
            _spawned.Add(entry.gameObject);
        }
    }
}
