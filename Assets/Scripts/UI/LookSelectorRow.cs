using UnityEngine;
using UnityEngine.UI;

// Ayarlar kartına "GÖRÜNÜM" satırını ekler: düğmeye basıldıkça kayıtlı görünümler arasında dolaşır (LookPreference).
// Kayıtlı görünüm yoksa satır eklenmez. Satır çalışırken, var olan bir satır (müzik parçası satırı) kopyalanarak
// kurulur ve kart bir satır uzatılır; sahne dosyası değişmez. Görünümler deneme olduğu için kalıcı arayüz kurulmadı.
public class LookSelectorRow : MonoBehaviour
{
    private const string RowName = "Satir_Gorunum";
    private const string TemplateRowName = "Satir_Muzik";
    private const string RowPrefix = "Satir_";
    private const string ShadowName = "Golge";
    private const string RowLabel = "GÖRÜNÜM";
    private const string OffLabel = "KAPALI";
    private const float RowStep = 52f;

    private Text _valueText;
    private bool _built;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        var host = new GameObject(nameof(LookSelectorRow)) { hideFlags = HideFlags.HideAndDontSave };
        DontDestroyOnLoad(host);
        host.AddComponent<LookSelectorRow>();
    }

    private void OnEnable() => LookPreference.Changed += Refresh;

    private void OnDisable() => LookPreference.Changed -= Refresh;

    private void Update()
    {
        if (!_built && LookPreference.Options.Count > 0)
            TryBuild();
    }

    private void TryBuild()
    {
        var menu = FindAnyObjectByType<SettingsMenuUI>(FindObjectsInactive.Include);
        if (menu == null)
            return;

        _built = true;
        var card = (RectTransform)menu.transform;
        var template = card.Find(TemplateRowName) as RectTransform;
        if (template == null || card.Find(RowName) != null)
            return;

        // En alttaki satırın altına yerleşir.
        float lowest = float.MaxValue;
        foreach (RectTransform child in card)
        {
            if (child.name.StartsWith(RowPrefix))
                lowest = Mathf.Min(lowest, child.anchoredPosition.y);
        }

        var row = Instantiate(template, card);
        row.name = RowName;
        row.anchoredPosition = new Vector2(template.anchoredPosition.x, lowest - RowStep);

        // Şablonun düğmesi müzik parçasını değiştirir; kopyada o davranış kaldırılır.
        foreach (var musicButton in row.GetComponentsInChildren<MusicTrackButton>(true))
            DestroyImmediate(musicButton);

        var button = row.GetComponentInChildren<Button>(true);
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(LookPreference.SelectNext);
        _valueText = button.GetComponentInChildren<Text>(true);

        foreach (var text in row.GetComponentsInChildren<Text>(true))
        {
            if (text != _valueText)
                text.text = RowLabel;
        }

        // Kart bir satır uzar: satırlar üst kenarla birlikte yukarı, alttaki düğmeler aşağı kayar.
        card.sizeDelta += new Vector2(0f, RowStep);
        foreach (RectTransform child in card)
        {
            bool isBottomButton = child.GetComponent<Button>() != null;
            child.anchoredPosition += new Vector2(0f, isBottomButton ? -RowStep * 0.5f : RowStep * 0.5f);
        }

        // Kartın gölgesi (kartın kardeşi) de aynı kadar uzar.
        if (card.parent.Find(ShadowName) is RectTransform shadow)
            shadow.sizeDelta += new Vector2(0f, RowStep);

        Refresh();
    }

    private void Refresh()
    {
        if (_valueText != null)
            _valueText.text = LookPreference.SelectedLabel ?? OffLabel;
    }
}
