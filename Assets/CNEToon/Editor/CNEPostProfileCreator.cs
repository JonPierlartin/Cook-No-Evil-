using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// CNE Toon post-process profillerini ve ayar varlığını üretir: CNE → Post → Create Profiles.
// Var olan profil ÜZERİNE YAZILMAZ (elle yapılan ayar kaybolmasın); yeniden üretmek için dosya silinir.
//
// "Kapalı" denen efektler profilde etkisiz değerle AÇIKÇA ezilir: bileşeni hiç eklememek, sahnedeki daha düşük
// öncelikli bir Volume'un (ör. şablondan kalan profil) o efekti açmasına izin verirdi.
public static class CNEPostProfileCreator
{
    private const string PostFolder = "Assets/CNEToon/Post";
    private const string ResourcesFolder = "Assets/CNEToon/Resources";
    private const string SettingsPath = ResourcesFolder + "/CNELookSettings.asset";

    private const float BloomThreshold = 1.3f;
    private const float BloomIntensity = 0.4f;
    private const float BloomScatter = 0.6f;
    private const float GrayscaleSaturation = -100f;

    [MenuItem("CNE/Post/Create Profiles")]
    public static void CreateProfiles()
    {
        EnsureFolder(PostFolder);
        EnsureFolder(ResourcesFolder);

        var global = CreateProfile("CNE_Global", profile => FillLook(profile, TonemappingMode.None));
        CreateProfile("CNE_Global_Neutral", profile => FillLook(profile, TonemappingMode.Neutral));
        var grayscale = CreateProfile("CNE_Debug_Grayscale", profile =>
            Add<ColorAdjustments>(profile).saturation.Override(GrayscaleSaturation));

        var settings = AssetDatabase.LoadAssetAtPath<CNELookSettings>(SettingsPath);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<CNELookSettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
        }

        if (settings.globalProfile == null)
            settings.globalProfile = global;
        if (settings.grayscaleProfile == null)
            settings.grayscaleProfile = grayscale;

        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        Debug.Log($"[CNE] Post-process profilleri hazır: {PostFolder}");
    }

    private static void FillLook(VolumeProfile profile, TonemappingMode tonemapping)
    {
        Add<Tonemapping>(profile).mode.Override(tonemapping);

        // Sanatçı bu üçünü ±10 içinde oynatır.
        var color = Add<ColorAdjustments>(profile);
        color.postExposure.Override(0f);
        color.contrast.Override(0f);
        color.saturation.Override(0f);

        // Yalnızca HDR emission parlar; aydınlık yüzeyler eşiğin altında kalır.
        var bloom = Add<Bloom>(profile);
        bloom.threshold.Override(BloomThreshold);
        bloom.intensity.Override(BloomIntensity);
        bloom.scatter.Override(BloomScatter);

        // Kapalı tutulanlar (etkisiz değerle ezilir).
        var whiteBalance = Add<WhiteBalance>(profile);
        whiteBalance.temperature.Override(0f);
        whiteBalance.tint.Override(0f);

        var mixer = Add<ChannelMixer>(profile);
        mixer.redOutRedIn.Override(100f); mixer.redOutGreenIn.Override(0f); mixer.redOutBlueIn.Override(0f);
        mixer.greenOutRedIn.Override(0f); mixer.greenOutGreenIn.Override(100f); mixer.greenOutBlueIn.Override(0f);
        mixer.blueOutRedIn.Override(0f); mixer.blueOutGreenIn.Override(0f); mixer.blueOutBlueIn.Override(100f);

        var split = Add<SplitToning>(profile);
        split.shadows.Override(Color.grey);
        split.highlights.Override(Color.grey);
        split.balance.Override(0f);

        var neutral = new Vector4(1f, 1f, 1f, 0f);
        var lgg = Add<LiftGammaGain>(profile);
        lgg.lift.Override(neutral); lgg.gamma.Override(neutral); lgg.gain.Override(neutral);

        var smh = Add<ShadowsMidtonesHighlights>(profile);
        smh.shadows.Override(neutral); smh.midtones.Override(neutral); smh.highlights.Override(neutral);

        Add<ColorLookup>(profile).contribution.Override(0f);
        Add<Vignette>(profile).intensity.Override(0f);
        Add<FilmGrain>(profile).intensity.Override(0f);
        Add<ChromaticAberration>(profile).intensity.Override(0f);
        Add<LensDistortion>(profile).intensity.Override(0f);
        Add<DepthOfField>(profile).mode.Override(DepthOfFieldMode.Off);
        Add<MotionBlur>(profile).intensity.Override(0f);
    }

    private static VolumeProfile CreateProfile(string profileName, System.Action<VolumeProfile> fill)
    {
        string path = $"{PostFolder}/{profileName}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (existing != null)
        {
            Debug.Log($"[CNE] {profileName} zaten var, üzerine yazılmadı.");
            return existing;
        }

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, path);
        fill(profile);
        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static T Add<T>(VolumeProfile profile) where T : VolumeComponent
    {
        var component = profile.Add<T>();
        component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
        AssetDatabase.AddObjectToAsset(component, profile);
        return component;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
    }
}
