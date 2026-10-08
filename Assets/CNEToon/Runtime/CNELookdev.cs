using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// ToonLookdev test sahnesine ana menüden giriş ve çıkış.
// Sahne EK olarak (additive) yüklenir: ana sahne bellekte kalır (ağ nesneleri, Steam, menü durumu bozulmaz), yalnızca
// çizen / ışık veren kök nesneleri test sahnesi açıkken kapatılır ve çıkışta geri açılır. Test sahnesi etkin sahne
// yapılır ki ortam ışığı ve lightmap ayarları ondan gelsin.
public static class CNELookdev
{
    public const string SceneName = "ToonLookdev";

    private static readonly List<GameObject> HiddenRoots = new();
    private static Scene _previousScene;

    public static bool IsOpen { get; private set; }

    public static void Enter()
    {
        if (IsOpen)
            return;

        if (!Application.CanStreamedLevelBeLoaded(SceneName))
        {
            Debug.LogWarning($"[CNELookdev] '{SceneName}' sahnesi Build Settings'te yok; açılamadı.");
            return;
        }

        IsOpen = true;
        _previousScene = SceneManager.GetActiveScene();
        SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Additive).completed += _ =>
        {
            HideVisualRoots(_previousScene);
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(SceneName));
        };
    }

    public static void Exit()
    {
        if (!IsOpen)
            return;

        IsOpen = false;
        foreach (var root in HiddenRoots)
        {
            if (root != null)
                root.SetActive(true);
        }

        HiddenRoots.Clear();
        if (_previousScene.IsValid())
            SceneManager.SetActiveScene(_previousScene);

        SceneManager.UnloadSceneAsync(SceneName);
    }

    // Yalnızca görüntüye ya da ışığa katkısı olan kökler kapatılır; ağ / oturum nesneleri açık kalır.
    private static void HideVisualRoots(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            if (!root.activeSelf)
                continue;

            bool visual = root.GetComponentInChildren<Renderer>() != null
                || root.GetComponentInChildren<Light>() != null
                || root.GetComponentInChildren<Canvas>() != null
                || root.GetComponentInChildren<Camera>() != null
                || root.GetComponentInChildren<Volume>() != null;
            if (!visual)
                continue;

            root.SetActive(false);
            HiddenRoots.Add(root);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        IsOpen = false;
        HiddenRoots.Clear();
    }

    // Ana menünün ilk ekranına "test sahnesi" düğmesini ekler: ilk ekran düğmelerinden birini kopyalar ve onunla
    // birlikte görünür / gizlenir. Sahne dosyasına dokunmaz.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AddMenuButton()
    {
        var lobby = Object.FindAnyObjectByType<LobbyUIController>(FindObjectsInactive.Include);
        if (lobby == null || lobby.InitialButtonTemplate == null)
            return;

        var settings = CNELookSettings.Load();
        string label = settings != null ? settings.lookdevButtonLabel : SceneName;

        var template = lobby.InitialButtonTemplate;
        // Düğme ana menünün düzenine girmez: geliştirici kabına eklenir ve ana menüyle birlikte görünür.
        var parent = lobby.DeveloperButtonParent != null ? lobby.DeveloperButtonParent : template.transform.parent;
        var button = Object.Instantiate(template, parent);
        button.name = "ToonLookdevButton";
        button.gameObject.SetActive(true);

        // Şablonun etiketi yerelleştirme bileşeniyle sürülür; kopyada kalırsa metni ezer.
        foreach (var localized in button.GetComponentsInChildren<UnityEngine.Localization.Components.LocalizeStringEvent>(true))
            Object.DestroyImmediate(localized);

        var text = button.GetComponentInChildren<Text>(true);
        if (text != null)
            text.text = label;

        button.transform.SetSiblingIndex(template.transform.GetSiblingIndex() + 1);
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(Enter);

        // Kap ana menüyle birlikte açılıp kapanır (LobbyUIController); kap yoksa düğme şablonunu izler.
        if (lobby.DeveloperButtonParent != null)
            return;

        var visibility = template.transform.parent.gameObject.AddComponent<CNEMenuButtonVisibility>();
        visibility.Target = template.gameObject;
        visibility.Follower = button.gameObject;
    }
}
