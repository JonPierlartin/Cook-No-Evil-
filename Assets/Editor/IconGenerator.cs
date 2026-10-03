using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// İkon üretici (editör aracı): bir 3B görseli sabit açıdan, saydam arka planla render edip sprite asset'i olarak
// kaydeder. Çalışma zamanında render YOKTUR; çıktı asset'tir. Üç kullanım:
//  - ItemType: türün visualPrefab'ı -> hotbar/pano ikonu (ItemType.icon'a atanır)
//  - SignalValue: yer tutucu işaret (visualPrefab) -> pano/çark ikonu (SignalValue.icon'a atanır)
//  - BurgerVariant: varyantın yığını (BurgerStackBuilder ile) -> pop-up/kitapçık resmi (BurgerVariant.image)
// Render ayrı bir önizleme sahnesinde yapılır (PreviewRenderUtility); açık sahneye dokunmaz.
public static class IconGenerator
{
    private const string OutputFolder = "Assets/Data/Icons";
    private const int Size = 256;
    // Önizleme sahnesindeki kameraya göre bakış: yiyecekler hafif yukarıdan, çapraz.
    private static readonly Vector3 ItemViewEuler = new(30f, -30f, 0f);
    // İşaretler düz karşıdan ve Kasiyer'in arkasından (ok yönü ekranda da aynı yöne baksın: +X sağda).
    private static readonly Vector3 SignalViewEuler = Vector3.zero;
    // Varyant resminde proteinin pişmişlik fazı (pişmiş). ProgressProfile sırası: 0 çiğ, 1 pişmiş.
    private const int VariantProteinPhase = 1;

    [MenuItem("Cook No Evil/İkon Üret/Tüm Malzemeler (ItemType)")]
    public static void GenerateAllItemIcons()
    {
        int count = 0;
        foreach (var type in LoadAll<ItemType>())
            count += GenerateItemIcon(type) ? 1 : 0;

        AssetDatabase.SaveAssets();
        Debug.Log($"[IconGenerator] {count} malzeme ikonu üretildi ({OutputFolder}).");
    }

    [MenuItem("Cook No Evil/İkon Üret/Seçili Asset'ler (ItemType / SignalValue / BurgerVariant)")]
    public static void GenerateSelected()
    {
        int count = 0;
        foreach (var selected in Selection.objects)
        {
            if (selected is ItemType type)
                count += GenerateItemIcon(type) ? 1 : 0;
            else if (selected is SignalValue signal)
                count += GenerateSignalIcon(signal) ? 1 : 0;
            else if (selected is BurgerVariant variant)
                count += GenerateVariantImage(variant) ? 1 : 0;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[IconGenerator] {count} ikon üretildi ({OutputFolder}).");
    }

    public static bool GenerateItemIcon(ItemType type)
    {
        if (type == null || type.VisualPrefab == null)
        {
            Debug.LogWarning($"[IconGenerator] '{(type != null ? type.name : "?")}' türünün visualPrefab'ı yok; ikon üretilmedi.");
            return false;
        }

        var instance = Object.Instantiate(type.VisualPrefab);
        // Durum rengi olan türler (köfte) ilk fazıyla (çiğ) çizilir — oyundaki kuralın aynısı.
        ItemPhaseColoring.Apply(instance.GetComponentsInChildren<Renderer>(), type, 0);
        var sprite = RenderToSprite(instance, ItemViewEuler, $"{OutputFolder}/{type.name}_Icon.png");
        return Assign(type, "icon", sprite);
    }

    public static bool GenerateSignalIcon(SignalValue signal)
    {
        if (signal == null || signal.VisualPrefab == null)
        {
            Debug.LogWarning($"[IconGenerator] '{(signal != null ? signal.name : "?")}' sinyalinin visualPrefab'ı yok; ikon üretilmedi.");
            return false;
        }

        var instance = Object.Instantiate(signal.VisualPrefab);
        var sprite = RenderToSprite(instance, SignalViewEuler, $"{OutputFolder}/Sinyal_{signal.name}_Icon.png");
        return Assign(signal, "icon", sprite);
    }

    // Varyantın yığını oyundaki hamburgerle AYNI kuralla kurulur (BurgerStackBuilder): alt ekmek, varyantın
    // malzemeleri (verideki sırayla), üst ekmek. Ekmek varyant listesinde yoktur (her hamburgerde sabit).
    public static bool GenerateVariantImage(BurgerVariant variant)
    {
        var registry = LoadFirst<ItemRegistry>();
        ItemType bread = null;
        foreach (var type in LoadAll<ItemType>())
        {
            if (type.Category == ItemCategory.Ekmek)
                bread = type;
        }

        if (variant == null || registry == null || bread == null)
        {
            Debug.LogWarning("[IconGenerator] Varyant resmi üretilemedi: varyant, ItemRegistry veya ekmek türü bulunamadı.");
            return false;
        }

        var layers = new List<BurgerLayerEntry> { new(bread.Id, 0) };
        foreach (var ingredient in variant.Ingredients)
        {
            if (ingredient.item != null)
                layers.Add(new BurgerLayerEntry(ingredient.item.Id, ingredient.item.Category == ItemCategory.Protein ? VariantProteinPhase : 0));
        }

        layers.Add(new BurgerLayerEntry(bread.Id, 0));

        var root = new GameObject("VariantStack");
        BurgerStackBuilder.Build(layers, registry, root.transform, new List<GameObject>());
        var sprite = RenderToSprite(root, ItemViewEuler, $"{OutputFolder}/Varyant_{variant.name}.png");
        return Assign(variant, "image", sprite);
    }

    // instance'ı önizleme sahnesine alır (ve işi bitince yok eder), sınır kutusunu kadraja sığdırıp render eder.
    private static Sprite RenderToSprite(GameObject instance, Vector3 viewEuler, string assetPath)
    {
        var preview = new PreviewRenderUtility();
        RenderTexture target = null;
        Texture2D texture = null;
        try
        {
            preview.AddSingleGO(instance);
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            var renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                Debug.LogWarning($"[IconGenerator] '{instance.name}' içinde görünür renderer yok.");
                return null;
            }

            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
                bounds.Encapsulate(renderer.bounds);

            float radius = bounds.extents.magnitude;
            var rotation = Quaternion.Euler(viewEuler);

            // Kadraj: sınır kutusunun köşeleri kameranın sağ/yukarı eksenlerine izdüşürülür; kare kadraja sığan en
            // küçük yarı-boy alınır (küre yarıçapı yassı nesnelerde kadrajı boş bırakıyordu).
            float halfSize = 0f;
            for (int xi = -1; xi <= 1; xi += 2)
            for (int yi = -1; yi <= 1; yi += 2)
            for (int zi = -1; zi <= 1; zi += 2)
            {
                var offset = Vector3.Scale(bounds.extents, new Vector3(xi, yi, zi));
                halfSize = Mathf.Max(halfSize, Mathf.Abs(Vector3.Dot(offset, rotation * Vector3.right)),
                    Mathf.Abs(Vector3.Dot(offset, rotation * Vector3.up)));
            }

            var camera = preview.camera;
            camera.orthographic = true;
            camera.orthographicSize = halfSize * 1.08f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = radius * 10f + 1f;
            camera.transform.SetPositionAndRotation(bounds.center - rotation * Vector3.forward * (radius * 4f + 0.5f), rotation);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;

            preview.lights[0].intensity = 1.6f;
            preview.lights[0].transform.rotation = rotation * Quaternion.Euler(25f, 25f, 0f);
            preview.lights[1].intensity = 0.9f;
            preview.ambientColor = new Color(0.8f, 0.8f, 0.8f);

            // Önizleme ışıkları ve ortam ışığı yalnızca PreviewRenderUtility.Render ile devreye girer (doğrudan
            // camera.Render önizleme sahnesinin ışık ayarını kurmaz, görüntü karanlık kalır). Sonuç sRGB bir
            // hedefe kopyalanıp alfa kanalıyla okunur.
            preview.BeginPreview(new Rect(0, 0, Size, Size), GUIStyle.none);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            preview.Render(true);

            target = RenderTexture.GetTemporary(Size, Size, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(camera.targetTexture, target);

            var previous = RenderTexture.active;
            RenderTexture.active = target;
            texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
            preview.EndPreview();

            Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
            File.WriteAllBytes(assetPath, texture.EncodeToPNG());
        }
        finally
        {
            if (target != null)
                RenderTexture.ReleaseTemporary(target);
            if (texture != null)
                Object.DestroyImmediate(texture);
            preview.Cleanup();
        }

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
        if (importer.textureType != TextureImporterType.Sprite || !importer.alphaIsTransparency)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    private static bool Assign(Object asset, string propertyName, Sprite sprite)
    {
        if (sprite == null)
            return false;

        var serialized = new SerializedObject(asset);
        serialized.FindProperty(propertyName).objectReferenceValue = sprite;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
        return true;
    }

    private static List<T> LoadAll<T>() where T : Object
    {
        var result = new List<T>();
        foreach (var guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}"))
            result.Add(AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)));

        return result;
    }

    private static T LoadFirst<T>() where T : Object
    {
        var all = LoadAll<T>();
        return all.Count > 0 ? all[0] : null;
    }
}
