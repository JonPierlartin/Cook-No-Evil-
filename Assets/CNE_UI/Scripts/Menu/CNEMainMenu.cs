using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CookNoEvil.UI
{
    /// <summary>
    /// Ana menu: LOBI OLUSTUR / LOBILERE GOZAT / NASIL OYNANIR / AYARLAR / CIKIS.
    /// Lobi ekranlari projede; satirlar olaylara (onCreateLobby vb.) Inspector'dan baglanir.
    /// AYARLAR karti bu prefabin icinde; CIKIS oyunu kapatir.
    /// Kaynaklar (ayar deposu, muzik calar, mikrofon) burada bir kez atanir; menu onlari ayarlar kartina ve
    /// jukebox seritlerine iletir.
    /// Menu her gorundugunde (SetActive true) panonun satirlari sirayla yerine oturur, neon logo yanar;
    /// gizlenince acik ayarlar karti kaydedilip animasyonsuz kapanir.
    /// Prefabin kokunde kendi Canvas'i vardir; arka plan yoktur (arkada 3B sahne gorunur).
    /// </summary>
    [AddComponentMenu("Cook No Evil/UI/Ana Menu")]
    public class CNEMainMenu : MonoBehaviour
    {
        [Header("Kaynaklar (projede atanir)")]
        [Tooltip("Ayar deposu; ayarlar kartina iletilir")] public CNESettingsSource settingsSource;
        [Tooltip("Muzik calar; jukebox seritlerine iletilir")] public CNEMusicSource musicSource;
        [Tooltip("Mikrofon testi (istege bagli); bossa test satiri gizlenir")] public CNEMicLevelSource micLevelSource;

        [Header("Pano satirlari")]
        public Button createLobbyButton;
        public Button browseLobbiesButton;
        public Button howToPlayButton;
        public Button settingsButton;
        public Button quitButton;
        [Tooltip("NASIL OYNANIR satiri gorunsun mu? (Ekrani yoksa kapatin.)")] public bool showHowToPlay = true;

        [Header("Parcalar")]
        public CNESettingsPanel settingsPanel;
        [Tooltip("Surum yazisi ({0} = Application.version)")] public CNELocalizedText versionLabel;
        [Tooltip("Acilista sirayla oturan satirlar")] public RectTransform[] introRows = new RectTransform[0];
        public bool playIntro = true;
        [Tooltip("Harfli pano; boyu gorunen satir sayisina gore ayarlanir")] public RectTransform board;
        public float boardRowHeight = 80f;
        [Tooltip("Panonun ust + alt ic boslugu")] public float boardPadding = 76f;

        [Header("Olaylar: lobi ekranlarini buraya baglayin")]
        public UnityEvent onCreateLobby = new UnityEvent();
        public UnityEvent onBrowseLobbies = new UnityEvent();
        public UnityEvent onHowToPlay = new UnityEvent();
        public UnityEvent onQuitRequested = new UnityEvent();
        [Tooltip("CIKIS'ta oyunu kapat (editorde Play Mode'u durdurur).")] public bool quitApplication = true;

        void Awake()
        {
            if (createLobbyButton != null) createLobbyButton.onClick.AddListener(CreateLobby);
            if (browseLobbiesButton != null) browseLobbiesButton.onClick.AddListener(BrowseLobbies);
            if (howToPlayButton != null) howToPlayButton.onClick.AddListener(HowToPlay);
            if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
            if (quitButton != null) quitButton.onClick.AddListener(Quit);
            BindSources();
        }

        void OnEnable()
        {
            if (howToPlayButton != null) howToPlayButton.gameObject.SetActive(showHowToPlay);
            FitBoard();
            if (versionLabel != null) versionLabel.SetArgs(Application.version);
            if (playIntro && Application.isPlaying) PlayIntro();
        }

        void OnDisable()
        {
            if (settingsPanel != null) settingsPanel.CloseSilently();
        }

        /// <summary>Kaynaklari ayarlar kartina ve butun jukebox seritlerine iletir (onlarda atanmis olan korunur).</summary>
        public void BindSources()
        {
            if (settingsPanel != null) settingsPanel.Bind(settingsSource, micLevelSource);
            var strips = GetComponentsInChildren<CNEJukeboxStrip>(true);
            for (int i = 0; i < strips.Length; i++)
                if (strips[i].source == null) strips[i].source = musicSource;
        }

        public void CreateLobby()
        {
            Report(onCreateLobby, "LOBI OLUSTUR", "onCreateLobby");
        }

        public void BrowseLobbies()
        {
            Report(onBrowseLobbies, "LOBILERE GOZAT", "onBrowseLobbies");
        }

        public void HowToPlay()
        {
            Report(onHowToPlay, "NASIL OYNANIR", "onHowToPlay");
        }

        public void OpenSettings()
        {
            if (settingsPanel != null) settingsPanel.Open();
        }

        public void Quit()
        {
            onQuitRequested.Invoke();
            if (quitApplication) StartCoroutine(QuitAfter(0.3f)); // ses bitsin
        }

        void Report(UnityEvent evt, string label, string field)
        {
            evt.Invoke();
            if (evt.GetPersistentEventCount() == 0)
                Debug.LogWarning("[CNE UI] '" + label + "' secildi ama CNEMainMenu." + field + " olayina bir sey baglanmamis.", this);
        }

        IEnumerator QuitAfter(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>Panonun boyunu gorunen satir sayisina gore ayarlar (ornegin NASIL OYNANIR gizliyse kisalir).</summary>
        public void FitBoard()
        {
            if (board == null || introRows == null) return;
            int visible = 0;
            for (int i = 0; i < introRows.Length; i++)
                if (introRows[i] != null && introRows[i].gameObject.activeSelf) visible++;
            var size = board.sizeDelta;
            size.y = boardPadding + visible * boardRowHeight;
            board.sizeDelta = size;
        }

        /// <summary>Satirlar soldan kayarak ve harfleri ziplayarak sirayla yerine oturur.</summary>
        public void PlayIntro()
        {
            var rows = introRows;
            if (rows == null || rows.Length == 0) return;
            int order = 0;
            for (int i = 0; i < rows.Length; i++)
            {
                var row = rows[i];
                if (row == null || !row.gameObject.activeSelf) continue;
                var group = row.GetComponent<CanvasGroup>();
                if (group == null) group = row.gameObject.AddComponent<CanvasGroup>();
                var content = row.childCount > 0 ? (RectTransform)row.GetChild(0) : row;
                var hop = row.GetComponentInChildren<CNELetterHop>();
                float delay = 0.35f + order * 0.07f;
                order++;
                var g = group;
                var c = content;
                var rest = Vector2.zero; // Content satiri doldurur; dinlenme konumu her zaman sifir
                g.alpha = 0f;
                CNETween.To(row, "intro", 0.26f, t =>
                {
                    g.alpha = Mathf.Clamp01(t * 2f);
                    c.anchoredPosition = rest + new Vector2(Mathf.LerpUnclamped(-36f, 0f, t), 0f);
                }, x => CNEEase.OutBack(x, 1.4f), () =>
                {
                    c.anchoredPosition = rest;
                    if (hop != null) hop.Hop();
                }, delay);
            }
        }
    }
}
