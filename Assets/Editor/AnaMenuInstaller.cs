using System.Collections.Generic;
using CookNoEvil.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Ana menüyü AÇIK sahneye kurar: CNE → Ana Menü → Kur. Yeniden çalıştırılabilir, pencere açmaz.
//  1. Arayüz kitinin menüsü (CNE_MainMenu prefab örneği) sahnenin ilk ekranı olur; oyunun sistemlerine köprü
//     asset'leriyle (ayarlar, müzik) ve Inspector'da görünen kalıcı dinleyicilerle (lobi pencereleri) bağlanır.
//  2. Lobi paneli ikiye ayrılır: "lobi içi" görünümü (zemin, logo, kart) ayrı bir alt panele alınır ve ana menüde
//     gizlenir; lobi pencereleri yerinde kalır ve menünün üstünde açılır. Eski ilk ekran düğmeleri kapatılır
//     (silinmez: geliştirici düğmelerinin şablonu).
//  3. Ana menünün ekleri: durum metni ve geliştirici düğmelerinin kabı (menünün düzenine girmez, sağ üst köşe).
//  4. Gece ortamı (CNEMenuAmbience): sahnede KAPALI duran ışık düzeneği, kamera kadraj noktaları ve profil.
//     Gece yalnızca ana menü görünürken, çalışma zamanında uygulanır; sahneye gece değeri yazılmaz.
// Kit dosyalarına (Assets/CNE_UI) dokunulmaz; kit prefab'ları her kurulumda yeniden yazıldığı için menüye özgü her
// şey sahnedeki örnekte durur.
public static class AnaMenuInstaller
{
    private const string MenuPrefabPath = "Assets/CNE_UI/Prefabs/CNE_MainMenu.prefab";
    private const string DataFolder = "Assets/Data/UI";
    private const string SettingsSourcePath = DataFolder + "/GameSettingsUISource.asset";
    private const string MusicSourcePath = DataFolder + "/MusicPlayerUISource.asset";
    private const string NightProfilePath = DataFolder + "/MenuNightProfile.asset";
    private const string SignFontPath = "Assets/CNE_UI/Fonts/Righteous-Regular.ttf";

    private const string MenuName = "CNE_MainMenu";
    private const string SessionPanelName = "LobiIci";
    private const string OverlayName = "AnaMenuEkleri";
    private const string StatusName = "DurumYazisi";
    private const string DeveloperButtonsName = "GelistiriciDugmeleri";
    private const string NightRigName = "MenuNight";
    private const string AnchorAName = "MenuCameraAnchor";
    private const string AnchorBName = "MenuCameraAnchorB";
    private const string AmbienceName = "MenuAmbience";

    // Lobi içi görünümüne taşınan parçalar (lobi pencereleri taşınmaz).
    private static readonly string[] SessionChildren = { "Karartma", "Logo", "MuzikDugmesi", "LobbyContent" };

    // Atıf isteyen parçalar (Assets/Audio/Music/LISANSLAR.txt): klip adı → jukebox'ta adın yanında görünen atıf.
    private static readonly (string clip, string credit)[] TrackCredits =
    {
        ("Muzik_ChubbyCat", "PlayOnLoop.com (CC-BY 4.0)"),
    };

    // ---------- gece ortamı (başlangıç değerleri; editör önizlemesiyle ayarlandı) ----------
    private static readonly Color32 MoonColor = new(0xB7, 0xCA, 0xE8, 0xFF);
    private const float MoonIntensity = 0.2f;
    private const float MoonElevation = 30f;            // kuzeyden (sokak tarafı) vitrinden içeri
    private static readonly Color32 LampColor = new(0xFF, 0xD9, 0xA0, 0xFF);
    private const float LampIntensity = 0.6f;
    private const float LampRange = 3.6f;
    private static readonly Color32 JukeboxColor = new(0x9B, 0x7F, 0xD4, 0xFF);
    private static readonly Color32 AquariumColor = new(0x2E, 0x9E, 0x98, 0xFF);
    private const float AccentRange = 2.5f;
    private static readonly Color32 NightBackground = new(0x24, 0x23, 0x3A, 0xFF);
    private static readonly Color32 NightAmbientTint = new(0x9E, 0xB3, 0xFF, 0xFF);
    private const float NightAmbient = 0.22f;
    private const float NightReflection = 0.35f;
    private const float MenuFieldOfView = 45f;
    private const float DriftSeconds = 55f;

    // "Kapanış saati": yalnızca birkaç lamba yanar (salon yerleşimindeki adlar).
    private static readonly string[] LitLamps = { "SM_PendantLight_5", "SM_PendantLight_6", "SM_PendantLight_3", "SM_CeilingLight_1" };

    // Kadraj (dünya): salondan sipariş penceresine doğru; kamera iki nokta arasında çok yavaş gidip gelir.
    private static readonly Vector3 AnchorAPosition = new(1.1f, 1.9f, -13.5f);
    private static readonly Vector3 AnchorATarget = new(6.2f, 1.55f, -7.3f);
    private static readonly Vector3 AnchorBPosition = new(2.3f, 1.88f, -13.1f);
    private static readonly Vector3 AnchorBTarget = new(6.8f, 1.55f, -7.1f);

    [MenuItem("CNE/Ana Menü/Kur")]
    public static void Install()
    {
        var scene = SceneManager.GetActiveScene();
        var lobby = Object.FindAnyObjectByType<LobbyUIController>(FindObjectsInactive.Include);
        var browser = Object.FindAnyObjectByType<LobbyBrowserUI>(FindObjectsInactive.Include);
        if (lobby == null || browser == null)
        {
            Debug.LogError("[CNE] Ana menü kurulamadı: sahnede LobbyUIController / LobbyBrowserUI yok.");
            return;
        }

        var settingsSource = EnsureAsset<GameSettingsUISource>(SettingsSourcePath);
        var musicSource = EnsureAsset<MusicPlayerUISource>(MusicSourcePath);

        var menu = PlaceMenu(lobby.GetComponent<Canvas>());
        ConfigureMenu(menu, browser, settingsSource, musicSource);
        var sessionPanel = SplitLobbyPanel(lobby);
        var overlay = BuildOverlay(sessionPanel.parent, out var status, out var developerButtons);
        WireLobby(lobby, menu.gameObject, sessionPanel.gameObject, overlay, status, developerButtons);
        ApplyTrackCredits();
        InstallNight(menu.transform);

        // Açılış durumu sahnede: ana menü görünür, lobi içi gizli (denetleyici açılışta ayrıca kurmaz).
        menu.gameObject.SetActive(true);
        sessionPanel.gameObject.SetActive(false);
        overlay.SetActive(true);

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[CNE] Ana menü sahneye kuruldu.");
    }

    // ---------- menü ----------

    private static CNEMainMenu PlaceMenu(Canvas lobbyCanvas)
    {
        GameObject instance = null;
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == MenuName)
                instance = root;
        }

        if (instance == null)
        {
            instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(MenuPrefabPath));
            instance.name = MenuName;
        }

        // Lobi pencereleri ve hata ekranı menünün ÜSTÜNDE kalır: menünün Canvas'ı lobi Canvas'ının altında çizilir.
        var canvas = instance.GetComponent<Canvas>();
        canvas.renderMode = lobbyCanvas.renderMode;
        canvas.sortingOrder = lobbyCanvas.sortingOrder - 1;
        return instance.GetComponent<CNEMainMenu>();
    }

    private static void ConfigureMenu(CNEMainMenu menu, LobbyBrowserUI browser, CNESettingsSource settings, CNEMusicSource music)
    {
        menu.settingsSource = settings;
        menu.musicSource = music;
        // Mikrofon testi: Steam Voice'tan seviye okuyan kaynak ayrı karar; atanmayınca test satırı gizlidir.
        menu.micLevelSource = null;
        // "Nasıl oynanır" ekranı yok: satır gizlenir, pano kısalır.
        menu.showHowToPlay = false;
        // Eski çıkış düğmesi yalnızca oyunu kapatıyordu; kitinki aynısını yapar.
        menu.quitApplication = true;

        Rebind(menu.onCreateLobby, browser.OpenCreateDialog);
        Rebind(menu.onBrowseLobbies, browser.OpenList);
        EditorUtility.SetDirty(menu);
        PrefabUtility.RecordPrefabInstancePropertyModifications(menu);
    }

    // Kalıcı (Inspector'da görünen) dinleyici: yeniden kurulumda çiftlenmesin diye önce eskiler kaldırılır.
    private static void Rebind(UnityEvent unityEvent, UnityAction action)
    {
        for (int i = unityEvent.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(unityEvent, i);

        UnityEventTools.AddPersistentListener(unityEvent, action);
    }

    // ---------- lobi paneli ----------

    // Lobi panelinin zemini (kareli örtü), karartması, logosu, müzik düğmesi ve kartı "lobi içi" alt paneline alınır.
    private static Transform SplitLobbyPanel(LobbyUIController lobby)
    {
        var serialized = new SerializedObject(lobby);
        var lobbyPanel = ((GameObject)serialized.FindProperty("lobbyPanel").objectReferenceValue).transform;

        var session = lobbyPanel.Find(SessionPanelName) as RectTransform;
        if (session == null)
        {
            session = new GameObject(SessionPanelName, typeof(RectTransform)).GetComponent<RectTransform>();
            session.SetParent(lobbyPanel, false);
            session.SetAsFirstSibling();
            Stretch(session);
        }

        // Zemin görseli panelin kendisindeydi: lobi içine taşınır (ana menüde 3B sahne görünsün).
        if (lobbyPanel.TryGetComponent<Image>(out var background))
        {
            if (!session.TryGetComponent<Image>(out var moved))
                moved = session.gameObject.AddComponent<Image>();

            EditorUtility.CopySerialized(background, moved);
            Object.DestroyImmediate(background);
        }

        int index = 0;
        foreach (string childName in SessionChildren)
        {
            var child = lobbyPanel.Find(childName);
            if (child != null)
                child.SetParent(session, false);

            child = session.Find(childName);
            if (child != null)
                child.SetSiblingIndex(index++);
        }

        return session;
    }

    private static GameObject BuildOverlay(Transform lobbyPanel, out Text status, out Transform developerButtons)
    {
        var old = lobbyPanel.Find(OverlayName);
        if (old != null)
            Object.DestroyImmediate(old.gameObject);

        var overlay = new GameObject(OverlayName, typeof(RectTransform)).GetComponent<RectTransform>();
        overlay.SetParent(lobbyPanel, false);
        // Lobi içinin hemen ardında, lobi pencerelerinin altında.
        overlay.SetSiblingIndex(1);
        Stretch(overlay);

        // Durum metni: ekranın altında, ortada (menünün jukebox şeridi solda, sürüm yazısı sağda).
        var statusObject = new GameObject(StatusName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        var statusRect = (RectTransform)statusObject.transform;
        statusRect.SetParent(overlay, false);
        statusRect.anchorMin = statusRect.anchorMax = new Vector2(0.5f, 0f);
        statusRect.pivot = new Vector2(0.5f, 0f);
        statusRect.anchoredPosition = new Vector2(0f, 150f);
        statusRect.sizeDelta = new Vector2(900f, 44f);
        status = statusObject.GetComponent<Text>();
        status.font = AssetDatabase.LoadAssetAtPath<Font>(SignFontPath);
        status.fontSize = 28;
        status.alignment = TextAnchor.MiddleCenter;
        status.color = CNEPalette.Cream;
        status.horizontalOverflow = HorizontalWrapMode.Overflow;
        status.raycastTarget = false;
        status.text = string.Empty;

        // Geliştirici düğmeleri: sağ üst köşe, küçültülmüş; ana menünün düzenine girmez.
        var buttons = new GameObject(DeveloperButtonsName, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        var buttonsRect = (RectTransform)buttons.transform;
        buttonsRect.SetParent(overlay, false);
        buttonsRect.anchorMin = buttonsRect.anchorMax = buttonsRect.pivot = Vector2.one;
        buttonsRect.anchoredPosition = new Vector2(-24f, -24f);
        buttonsRect.localScale = Vector3.one * 0.7f;
        var layout = buttons.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperRight;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        var fitter = buttons.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        developerButtons = buttons.transform;
        return overlay.gameObject;
    }

    private static void WireLobby(LobbyUIController lobby, GameObject menu, GameObject sessionPanel, GameObject overlay, Text status, Transform developerButtons)
    {
        var serialized = new SerializedObject(lobby);
        serialized.FindProperty("initialScreen").objectReferenceValue = menu;
        serialized.FindProperty("sessionPanel").objectReferenceValue = sessionPanel;
        serialized.FindProperty("initialOverlay").objectReferenceValue = overlay;
        serialized.FindProperty("initialStatusText").objectReferenceValue = status;
        serialized.FindProperty("developerButtonParent").objectReferenceValue = developerButtons;

        // Eski ilk ekran düğmeleri gösterilmez (silinmez).
        foreach (string field in new[] { "hostButton", "browseButton", "quitButton" })
        {
            var button = serialized.FindProperty(field).objectReferenceValue as Button;
            if (button != null)
                button.gameObject.SetActive(false);
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ApplyTrackCredits()
    {
        var player = Object.FindAnyObjectByType<MusicPlayer>(FindObjectsInactive.Include);
        if (player == null)
            return;

        var serialized = new SerializedObject(player);
        var tracks = serialized.FindProperty("tracks");
        for (int i = 0; i < tracks.arraySize; i++)
        {
            var track = tracks.GetArrayElementAtIndex(i);
            var clip = track.FindPropertyRelative("clip").objectReferenceValue;
            foreach (var (clipName, credit) in TrackCredits)
            {
                if (clip != null && clip.name == clipName)
                    track.FindPropertyRelative("credit").stringValue = credit;
            }
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------- gece ortamı ----------

    private static void InstallNight(Transform menu)
    {
        var hall = GameObject.Find("Salon");
        var camera = FindSceneCamera();
        var sun = FindSun();
        if (hall == null || camera == null)
        {
            Debug.LogWarning("[CNE] Gece ortamı kurulamadı: Salon ya da sahne kamerası bulunamadı.");
            return;
        }

        var layout = hall.transform.childCount == 1 ? hall.transform.GetChild(0) : hall.transform;

        // Işık düzeneği: sahnede KAPALI durur; menü görünürken CNEMenuAmbience açar.
        var existing = FindRoot(NightRigName);
        if (existing != null)
            Object.DestroyImmediate(existing);

        var rig = new GameObject(NightRigName);
        var moon = AddLight(rig.transform, "AyIsigi", LightType.Directional, MoonColor, MoonIntensity, 0f, Vector3.zero);
        moon.transform.rotation = Quaternion.Euler(MoonElevation, 0f, 0f);
        moon.shadows = LightShadows.Soft;

        foreach (string lampName in LitLamps)
        {
            var lamp = layout.Find(lampName);
            var socket = lamp != null ? lamp.Find("Socket_Light") : null;
            if (socket != null)
                AddLight(rig.transform, "Lamba_" + lampName, LightType.Point, LampColor, LampIntensity, LampRange, socket.position);
        }

        var jukebox = layout.Find("SM_Jukebox");
        if (jukebox != null)
        {
            var socket = jukebox.Find("Socket_Light");
            var position = socket != null ? socket.position : jukebox.position + Vector3.up * 1.2f;
            AddLight(rig.transform, "Neon_Jukebox", LightType.Point, JukeboxColor, 0.8f, AccentRange, position);
        }

        var aquarium = layout.Find("SM_Aquarium");
        if (aquarium != null)
            AddLight(rig.transform, "Neon_Akvaryum", LightType.Point, AquariumColor, 0.6f, AccentRange, aquarium.position + Vector3.up * 1.75f);

        rig.SetActive(false);

        // Kadraj noktaları (kapalı düzeneğin dışında: bileşen konumlarını okur).
        var anchorA = EnsureRoot(AnchorAName);
        var anchorB = EnsureRoot(AnchorBName);
        anchorA.SetPositionAndRotation(AnchorAPosition, Quaternion.LookRotation(AnchorATarget - AnchorAPosition, Vector3.up));
        anchorB.SetPositionAndRotation(AnchorBPosition, Quaternion.LookRotation(AnchorBTarget - AnchorBPosition, Vector3.up));

        var profile = EnsureAsset<CNEMenuAmbienceProfile>(NightProfilePath);
        profile.ambientMultiplier = NightAmbient;
        profile.ambientTint = NightAmbientTint;
        profile.reflectionMultiplier = NightReflection;
        profile.nightSkybox = null;
        profile.overrideCameraBackground = true;
        profile.cameraBackground = NightBackground;
        EditorUtility.SetDirty(profile);

        var ambienceTransform = menu.Find(AmbienceName);
        if (ambienceTransform == null)
        {
            ambienceTransform = new GameObject(AmbienceName, typeof(RectTransform)).transform;
            ambienceTransform.SetParent(menu, false);
        }

        if (!ambienceTransform.TryGetComponent<CNEMenuAmbience>(out var ambience))
            ambience = ambienceTransform.gameObject.AddComponent<CNEMenuAmbience>();

        ambience.profile = profile;
        ambience.targetCamera = camera;
        ambience.cameraAnchor = anchorA;
        ambience.cameraAnchorB = anchorB;
        ambience.fieldOfView = MenuFieldOfView;
        ambience.driftSeconds = DriftSeconds;
        ambience.activateDuringMenu = new[] { rig };
        ambience.deactivateDuringMenu = new GameObject[0];
        ambience.disableDuringMenu = sun != null ? new Behaviour[] { sun } : new Behaviour[0];
        ambience.menuSun = moon;
        EditorUtility.SetDirty(ambience);
    }

    private static Light AddLight(Transform parent, string name, LightType type, Color color, float intensity, float range, Vector3 position)
    {
        var light = new GameObject(name).AddComponent<Light>();
        light.transform.SetParent(parent, false);
        light.transform.position = position;
        light.type = type;
        light.color = color;
        light.intensity = intensity;
        if (type == LightType.Point)
            light.range = range;
        light.shadows = LightShadows.None;
        return light;
    }

    // Sahnenin kendi kamerası (oyuncu kamerası değil): kök nesnedeki kamera.
    private static Camera FindSceneCamera()
    {
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.TryGetComponent<Camera>(out var camera))
                return camera;
        }

        return null;
    }

    // Gündüz güneşi: gece düzeneğinin dışındaki ilk directional ışık.
    private static Light FindSun()
    {
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name != NightRigName && root.TryGetComponent<Light>(out var light) && light.type == LightType.Directional)
                return light;
        }

        return null;
    }

    // ---------- yardımcılar ----------

    private static GameObject FindRoot(string name)
    {
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == name)
                return root;
        }

        return null;
    }

    private static Transform EnsureRoot(string name)
    {
        var root = FindRoot(name);
        return (root != null ? root : new GameObject(name)).transform;
    }

    private static T EnsureAsset<T>(string path) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null)
            return asset;

        var folders = new List<string>(path.Split('/'));
        folders.RemoveAt(folders.Count - 1);
        string current = folders[0];
        for (int i = 1; i < folders.Count; i++)
        {
            string next = current + "/" + folders[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, folders[i]);
            current = next;
        }

        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
