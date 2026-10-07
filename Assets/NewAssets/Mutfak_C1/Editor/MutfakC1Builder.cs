using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// C1 mutfak paketinin (Assets/NewAssets/Mutfak_C1) Unity tarafı: toon materyallerini üretir ve yerleşim dosyasından
// (C1_Mutfak_Yerlesim.glb) materyalleri değiştirilmiş bir prefab kurar. Menü: CNE → Mutfak C1 → Build.
// Modeller GLB olarak glTFast ile içe alınır (pivot, hareketli parça ve soketler dosyadan gelir); glTFast'ın kendi
// materyalleri kullanılmaz — her biri aynı adlı CNE/Toon materyaliyle değiştirilir.
// Var olan materyalin üzerine YAZILMAZ (elle yapılan ayar kaybolmasın); prefab her çalıştırmada yeniden kurulur.
public static class MutfakC1Builder
{
    private const string Root = "Assets/NewAssets/Mutfak_C1";
    private const string ModelFolder = Root + "/Models";
    private const string MaterialFolder = Root + "/Materials";
    private const string PalettePath = Root + "/Palettes/cook_no_evil_palet_256.png";
    private const string MaskPath = Root + "/Palettes/Mutfak_Beyaz_Prop.png";
    private const string WoodPath = Root + "/Textures/T_Mese_Damar_256.png";
    private const string MatcapPath = "Assets/CNEToon/Lookdev/Textures/Lookdev_Matcap_Krom.png";
    private const string LayoutModel = ModelFolder + "/C1_Mutfak_Yerlesim.glb";
    private const string PrefabFolder = "Assets/Prefabs/Mutfak";
    public const string LayoutPrefabPath = PrefabFolder + "/Mutfak_C1_Yerlesim.prefab";
    private const string ReferenceFloorPrefix = "REF_";
    private const string OutlineLayerName = "Outline";
    private const string SilhouetteLayerName = "Outline Silhouette";

    // Yalnızca dış hat alan asset'ler: ince ve sık ayrıntıda (ızgara çubukları, fritöz sepetleri ve sapları) iç
    // çizgiler birbirine girip yüzeyi karartıyor; ızgaradaki et seçilmiyordu. Diğerleri iç kırımlarıyla çizilir.
    private static readonly HashSet<string> SilhouetteOnlyAssets = new() { "SM_Grill", "SM_Fryer" };

    // Paketteki değerler (BENIOKU.md → Materyaller).
    private const float PaletteEmission = 2.5f;
    private const float DisplayEmission = 2f;
    private const float GlassOpacity = 0.25f;

    [MenuItem("CNE/Mutfak C1/Build")]
    public static void Build()
    {
        EnsureFolder(MaterialFolder);
        EnsureFolder(PrefabFolder);
        ConfigureTextures();

        var materials = CreateMaterials();
        BuildLayoutPrefab(materials);
        AssetDatabase.SaveAssets();
        Debug.Log($"[CNE] Mutfak C1 hazır: {LayoutPrefabPath}");
    }

    private static void ConfigureTextures()
    {
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(MaskPath) == null)
        {
            // Parlama / emission / desen maskesi: paletli materyallerde yüzeyin tamamı maskelidir (beyaz).
            var mask = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;
            mask.SetPixels(pixels);
            File.WriteAllBytes(MaskPath, mask.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(mask);
            AssetDatabase.ImportAsset(MaskPath);
        }

        Configure(WoodPath, importer =>
        {
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Bilinear;
        });

        foreach (string decal in new[] { "DC_FireWarning", "DC_FryerDisplay", "DC_FridgeDisplay" })
        {
            Configure($"{Root}/Textures/{decal}.png", importer =>
            {
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = true;
            });
        }
    }

    private static void Configure(string path, Action<TextureImporter> apply)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            return;

        apply(importer);
        importer.SaveAndReimport();
    }

    private static Dictionary<string, Material> CreateMaterials()
    {
        var palette = AssetDatabase.LoadAssetAtPath<Texture2D>(PalettePath);
        var mask = AssetDatabase.LoadAssetAtPath<Texture2D>(MaskPath);
        var wood = AssetDatabase.LoadAssetAtPath<Texture2D>(WoodPath);
        var matcap = AssetDatabase.LoadAssetAtPath<Texture2D>(MatcapPath);
        var toon = Shader.Find("CNE/Toon");

        var result = new Dictionary<string, Material>();

        result["MI_Palette"] = CreateMaterial("MI_Palette", toon, m => m.SetTexture("_BaseMap", palette));

        // Paslanmaz: renk paletten, keskin kenarlı parıltı.
        result["MI_Palette_Steel"] = CreateMaterial("MI_Palette_Steel", toon, m =>
        {
            m.SetTexture("_BaseMap", palette);
            m.SetTexture("_PropMap", mask);
            Enable(m, "_SPECULAR_ON", "_Specular");
            m.SetFloat("_SpecSize", 0.06f);
        });

        // Krom: matcap.
        result["MI_Palette_Chrome"] = CreateMaterial("MI_Palette_Chrome", toon, m =>
        {
            m.SetTexture("_BaseMap", palette);
            m.SetTexture("_PropMap", mask);
            m.SetTexture("_MatCapTex", matcap);
            Enable(m, "_MATCAP_ON", "_MatCap");
        });

        // Işıklı yüzeyler: kendi palet renginde ışır.
        result["MI_Palette_Emission"] = CreateMaterial("MI_Palette_Emission", toon, m =>
        {
            m.SetTexture("_BaseMap", palette);
            m.SetTexture("_PropMap", mask);
            Enable(m, "_EMISSION_ON", "_Emission");
            m.SetColor("_EmissionColor", Color.white * PaletteEmission);
            m.SetFloat("_EmissionBaseTint", 1f);
        });

        result["MI_Wood_Mese"] = CreateMaterial("MI_Wood_Mese", toon, m => m.SetTexture("_BaseMap", wood));

        result["MI_Decal_FireWarning"] = Decal("MI_Decal_FireWarning", "DC_FireWarning", toon, mask, 0f);
        result["MI_Decal_FryerDisplay"] = Decal("MI_Decal_FryerDisplay", "DC_FryerDisplay", toon, mask, DisplayEmission);
        result["MI_Decal_FridgeDisplay"] = Decal("MI_Decal_FridgeDisplay", "DC_FridgeDisplay", toon, mask, DisplayEmission);

        // Cam: CNE/Toon yalnızca opak çizer; cam URP Lit'in saydam yüzeyidir. Derinliğe yazmadığı için Şef'in kontur
        // görüşünde görünmez (bilinen sınır; bkz. CLAUDE.md K2 "yarı saydam nesneler").
        result["MI_Palette_Glass"] = CreateMaterial("MI_Palette_Glass", Shader.Find("Universal Render Pipeline/Lit"), m =>
        {
            m.SetTexture("_BaseMap", palette);
            m.SetColor("_BaseColor", new Color(1f, 1f, 1f, GlassOpacity));
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_Smoothness", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = (int)RenderQueue.Transparent;
        });

        return result;
    }

    private static Material Decal(string name, string texture, Shader shader, Texture2D mask, float emission)
    {
        return CreateMaterial(name, shader, m =>
        {
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{Root}/Textures/{texture}.png"));
            Enable(m, "_ALPHATEST_ON", "_AlphaClip");
            m.SetFloat("_Cutoff", 0.5f);
            m.renderQueue = (int)RenderQueue.AlphaTest;
            if (emission <= 0f)
                return;

            m.SetTexture("_PropMap", mask);
            Enable(m, "_EMISSION_ON", "_Emission");
            m.SetColor("_EmissionColor", Color.white * emission);
            m.SetFloat("_EmissionBaseTint", 1f);
        });
    }

    private static Material CreateMaterial(string name, Shader shader, Action<Material> configure)
    {
        string path = $"{MaterialFolder}/{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
            return material;

        material = new Material(shader);
        configure(material);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void Enable(Material material, string keyword, string toggleProperty)
    {
        material.EnableKeyword(keyword);
        material.SetFloat(toggleProperty, 1f);
    }

    private static void BuildLayoutPrefab(Dictionary<string, Material> materials)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(LayoutModel);
        if (model == null)
        {
            Debug.LogError($"[CNE] Yerleşim modeli bulunamadı: {LayoutModel}");
            return;
        }

        uint outlineBit = RenderingLayerMask.GetMask(OutlineLayerName);
        uint silhouetteBit = RenderingLayerMask.GetMask(SilhouetteLayerName);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
        try
        {
            instance.name = "Mutfak_C1_Yerlesim";
            foreach (Transform asset in instance.transform)
            {
                // Referans zemin yalnızca ölçü içindir.
                if (asset.name.StartsWith(ReferenceFloorPrefix))
                {
                    asset.gameObject.SetActive(false);
                    continue;
                }

                string assetName = AssetNameOf(asset.name);
                var flags = ReadFlags(assetName);
                uint lineBit = SilhouetteOnlyAssets.Contains(assetName) ? silhouetteBit : outlineBit;
                foreach (var renderer in asset.GetComponentsInChildren<Renderer>(true))
                {
                    var shared = renderer.sharedMaterials;
                    bool glass = false;
                    for (int i = 0; i < shared.Length; i++)
                    {
                        if (shared[i] == null || !materials.TryGetValue(shared[i].name, out var replacement))
                        {
                            Debug.LogWarning($"[CNE] '{renderer.name}': '{(shared[i] != null ? shared[i].name : "boş")}' için materyal yok.");
                            continue;
                        }

                        glass |= shared[i].name == "MI_Palette_Glass";
                        shared[i] = replacement;
                    }

                    renderer.sharedMaterials = shared;
                    renderer.shadowCastingMode = flags.CastShadow && !glass ? ShadowCastingMode.On : ShadowCastingMode.Off;
                    // Diğer bitler korunur; çizgi yalnızca asset listesinde "Outline" işaretli olanlara.
                    renderer.renderingLayerMask &= ~(outlineBit | silhouetteBit);
                    if (flags.Outline)
                        renderer.renderingLayerMask |= lineBit;
                }
            }

            PrefabUtility.SaveAsPrefabAsset(instance, LayoutPrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    // Yerleşimdeki ad "SM_Grill_01" → asset "SM_Grill" (sondaki sıra numarası atılır).
    private static string AssetNameOf(string layoutName)
    {
        int cut = layoutName.LastIndexOf('_');
        return cut > 0 && int.TryParse(layoutName.Substring(cut + 1), out _) ? layoutName.Substring(0, cut) : layoutName;
    }

    private struct AssetFlags
    {
        public bool Outline;
        public bool CastShadow;
    }

    [Serializable]
    private class GltfDocument
    {
        public GltfScene[] scenes;
        public GltfNode[] nodes;
    }

    [Serializable]
    private class GltfScene
    {
        public int[] nodes;
    }

    [Serializable]
    private class GltfNode
    {
        public string name;
        public GltfExtras extras;
    }

    [Serializable]
    private class GltfExtras
    {
        public bool cne_outline;
        public bool cne_cast_shadow;
    }

    // Asset'in kendi GLB dosyasındaki kök düğümün "extras" alanı: çizgi alır mı, gölge atar mı (asset listesindeki
    // Outline ve Gölge sütunları). Dosya bulunamazsa ikisi de açık sayılır.
    private static AssetFlags ReadFlags(string assetName)
    {
        var fallback = new AssetFlags { Outline = true, CastShadow = true };
        string path = $"{ModelFolder}/{assetName}.glb";
        if (!File.Exists(path))
            return fallback;

        // GLB: 12 bayt başlık, ardından JSON parçası (4 bayt uzunluk + 4 bayt tür + içerik).
        byte[] bytes = File.ReadAllBytes(path);
        int length = BitConverter.ToInt32(bytes, 12);
        var document = JsonUtility.FromJson<GltfDocument>(Encoding.UTF8.GetString(bytes, 20, length));
        if (document?.scenes == null || document.scenes.Length == 0 || document.scenes[0].nodes.Length == 0)
            return fallback;

        var root = document.nodes[document.scenes[0].nodes[0]];
        return root.extras == null
            ? fallback
            : new AssetFlags { Outline = root.extras.cne_outline, CastShadow = root.extras.cne_cast_shadow };
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
