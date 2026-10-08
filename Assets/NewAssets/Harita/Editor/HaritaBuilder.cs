using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// İstasyon ve Kasa paketlerinin (Assets/NewAssets/Istasyon, Assets/NewAssets/Kasa) Unity tarafı ve mimari
// materyalleri. Menü: CNE → Harita → Build. Modeller C1 mutfağıyla aynı yoldan kurulur (MutfakC1Builder): yerleşim
// GLB'sinden prefab, materyaller aynı adlı CNE/Toon materyalleriyle değişir, çizgi ve gölge bayrakları asset'in kendi
// GLB dosyasından okunur. Var olan materyalin üzerine YAZILMAZ; prefab'lar her çalıştırmada yeniden kurulur.
public static class HaritaBuilder
{
    private const string PrefabFolder = "Assets/Prefabs/Harita";
    public const string StationPrefabPath = PrefabFolder + "/Istasyon_Yerlesim.prefab";
    public const string CounterPrefabPath = PrefabFolder + "/Kasa_Yerlesim.prefab";
    public const string StationModelFolder = "Assets/NewAssets/Istasyon/Models";
    public const string CounterModelFolder = "Assets/NewAssets/Kasa/Models";
    public const string HallPrefabPath = PrefabFolder + "/Salon_Yerlesim.prefab";
    public const string HallModelFolder = "Assets/NewAssets/Salon/Models";
    public const string StreetPrefabPath = PrefabFolder + "/Sokak_Yerlesim.prefab";
    private const string StreetModelFolder = "Assets/NewAssets/Sokak/Models";
    private const string StreetActorFolder = PrefabFolder + "/Sokak";
    private const string StreetTextureFolder = "Assets/NewAssets/Sokak/Textures";
    private const string StreetMaterialFolder = "Assets/NewAssets/Sokak/Materials";
    public const string SignFaceMaterialName = "MI_Tabela_Zemin";
    public const string SignFaceMaterialPath = "Assets/NewAssets/Salon/Materials/MI_Tabela_Zemin.mat";
    private const string HallTextureFolder = "Assets/NewAssets/Salon/Textures";
    private const string HallMaterialFolder = "Assets/NewAssets/Salon/Materials";
    private const string PalettePath = "Assets/NewAssets/Mutfak_C1/Palettes/cook_no_evil_palet_256.png";
    private const string MaterialFolder = "Assets/NewAssets/Harita/Materials";

    private const float ArchitectureLightTint = 0.15f;
    private const float WallShadowStrength = 0.35f;
    private const float CeilingShadowStrength = 0.1f;

    // Desen türleri (CNE/Toon _PatternType).
    private const float Tile = 0f;
    private const float Grid = 1f;
    private const float Stripe = 2f;
    private const float Checker = 3f;

    [MenuItem("CNE/Harita/Build")]
    public static void Build()
    {
        var materials = MutfakC1Builder.CreateMaterials();
        MutfakC1Builder.BuildLayoutPrefab(StationModelFolder + "/Istasyon_Yerlesim.glb", StationModelFolder,
            StationPrefabPath, "Istasyon_Yerlesim", materials);
        MutfakC1Builder.BuildLayoutPrefab(CounterModelFolder + "/Kasa_Yerlesim.glb", CounterModelFolder,
            CounterPrefabPath, "Kasa_Yerlesim", materials);
        foreach (var pair in CreateHallMaterials())
            materials[pair.Key] = pair.Value;
        // Salon paketi hiçbir asset'e çizgi vermiyor ("oyuncu etkileşmez"); Ersel (8 Eki): salon da projenin çizgi
        // kuralına uysun. Eşyalar (zemine / masaya oturanlar) Outline katmanına girer; tablo, tabela, lamba, vitrin
        // ve kapılar girmez (diğer odalardaki pano ve lambalarla aynı).
        MutfakC1Builder.BuildLayoutPrefab(HallModelFolder + "/Salon_Yerlesim.glb", HallModelFolder,
            HallPrefabPath, "Salon_Yerlesim", materials, outlineFollowsShadow: true);
        CreateArchitectureMaterials();
        var streetMaterials = CreateStreetMaterials(materials);
        BuildStreetPrefab(streetMaterials);
        BuildStreetActors(streetMaterials);
        AssetDatabase.SaveAssets();
        Debug.Log("[CNE] Harita prefab'ları ve mimari materyalleri hazır.");
    }

    // Mimari materyalleri: düz renk + dünya uzayında desen (mimaride UV yok). Renkler paketlerdeki mimari
    // referanslarından (ARCH_Kasa, ARCH_Istasyon: MI_Arch_*); mutfak zemini ve derzler önizleme görsellerinden.
    public static Dictionary<string, Material> CreateArchitectureMaterials()
    {
        MutfakC1Builder.EnsureFolder(MaterialFolder);
        var result = new Dictionary<string, Material>();

        Add(result, "MI_Arch_Mutfak_Duvar", "#2F5D63", Tile, "#25494E", 0.30f, 0.15f, 0.008f);
        Add(result, "MI_Arch_Mutfak_Zemin", "#45484C", Grid, "#383B3F", 0.50f, 0.50f, 0.008f);
        Add(result, "MI_Arch_Istasyon_Duvar", "#A7CFE6", Tile, "#8DB6CF", 0.30f, 0.15f, 0.008f);
        Add(result, "MI_Arch_Istasyon_Zemin", "#5A6670");
        Add(result, "MI_Arch_Kasa_Duvar", "#F2E4C4");
        Add(result, "MI_Arch_Kasa_Lambri", "#A8DCCB", Stripe, "#8FC7B4", 0.10f, 0.10f, 0.006f);
        Add(result, "MI_Arch_Kasa_Bordur", "#C3CCD2");
        Add(result, "MI_Arch_Kasa_Zemin", "#E2DDD0", Checker, "#2A2C35", 0.50f, 0.50f, 0f);
        // Salonun zemini Kasa'yla aynı dama; ayrı materyal, çünkü karolar her odanın kendi köşesinden başlar.
        Add(result, "MI_Arch_Salon_Zemin", "#E2DDD0", Checker, "#2A2C35", 0.50f, 0.50f, 0f);
        Add(result, "MI_Arch_Kasa_Tavan", "#F7F1E3");
        Add(result, "MI_Arch_Tavan", "#EEF2F2");
        Add(result, "MI_Arch_Supurgelik", "#5C4A40");
        Add(result, "MI_Arch_Dis", "#9AA0A6");
        // Salonun dış cephesi ve kapı eşikleri (ARCH_Salon).
        Add(result, "MI_Arch_Dis_Plint", "#4A535B");
        Add(result, "MI_Arch_Dis_Turkuaz", "#2E9E98");
        Add(result, "MI_Arch_Dis_Krem", "#F2E4C4");
        Add(result, "MI_Arch_Esik", "#A9B3BA");
        Add(result, "MI_Arch_Dis_Zemin", "#8B9096", Grid, "#7C8187", 1.00f, 1.00f, 0.012f);
        return result;
    }

    // Sokak paketi OBJ'dir (hiyerarşi, soket ve bayrak taşımaz): birleşik yerleşim dosyası tek model olarak içe
    // alınır, materyaller ada göre proje materyalleriyle değişir. Paket kuralı: sokak oyuncunun etkileşmediği fondur —
    // çizgi yok, gerçek zamanlı gölge yok.
    private static void BuildStreetPrefab(Dictionary<string, Material> materials)
    {
        var model = LoadStreetModel("Sokak_Yerlesim");
        if (model == null)
            return;

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
        try
        {
            instance.name = "Sokak_Yerlesim";
            ApplyStreetLook(instance, materials);
            PrefabUtility.SaveAsPrefabAsset(instance, StreetPrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    private static GameObject LoadStreetModel(string name)
    {
        string path = $"{StreetModelFolder}/{name}.obj";
        if (AssetImporter.GetAtPath(path) is ModelImporter importer
            && (importer.importNormals != ModelImporterNormals.Import || importer.generateSecondaryUV || importer.isReadable))
        {
            importer.importNormals = ModelImporterNormals.Import;
            importer.generateSecondaryUV = false;
            importer.isReadable = false;
            importer.SaveAndReimport();
        }

        var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (model == null)
            Debug.LogError($"[CNE] Sokak modeli bulunamadı: {path}");

        return model;
    }

    // Sokak fondur: materyaller proje materyalleriyle değişir, gölge ve çizgi yok.
    private static void ApplyStreetLook(GameObject root, Dictionary<string, Material> materials)
    {
        uint lineLayers = RenderingLayerMask.GetMask("Outline", "Outline Silhouette");
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            var slots = renderer.sharedMaterials;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null && materials.TryGetValue(slots[i].name, out var replacement))
                    slots[i] = replacement;
                else
                    Debug.LogWarning($"[CNE] Sokak: '{renderer.name}' için '{(slots[i] != null ? slots[i].name : "boş")}' materyali yok.");
            }

            renderer.sharedMaterials = slots;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.renderingLayerMask &= ~lineLayers;
        }
    }

    // ---------- sokakta hareket edenler: araçlar ve figüran ----------

    // Araç soketleri (paket manifest'i → soketler_unity): tekerlek yanı (x), ön / arka aks (z), egzoz (z).
    private static readonly (string model, string prefab, float wheelX, float frontZ, float rearZ, float exhaustZ)[] CarSpecs =
    {
        ("SM_Car_Sedan", "Arac_Sedan", 0.80f, 1.45f, -1.45f, -2.49f),
        ("SM_Car_Pickup", "Arac_Pikap", 0.81f, 1.60f, -1.55f, -2.52f),
        ("SM_Van", "Arac_Kamyonet", 0.82f, 1.70f, -1.70f, -2.53f),
    };

    private const float WheelAxleHeight = 0.3188f;
    private static readonly Vector3 ExhaustOffset = new(0.55f, 0.30f, 0f);
    private static readonly Vector3 CarShadowScale = new(2.3f, 1f, 5.2f);
    private static readonly Vector3 PedestrianShadowScale = new(0.70f, 1f, 0.35f);
    private const float ShadowLift = 0.01f;

    public static string StreetActorPath(string name) => $"{StreetActorFolder}/{name}.prefab";

    private static void BuildStreetActors(Dictionary<string, Material> materials)
    {
        MutfakC1Builder.EnsureFolder(StreetActorFolder);
        var bodyColors = new[]
        {
            materials["MI_CarBody_Nane"], materials["MI_CarBody_Bebek"], materials["MI_CarBody_Lila"], materials["MI_CarBody_Gri"],
        };

        foreach (var spec in CarSpecs)
            BuildCar(spec.model, spec.prefab, spec.wheelX, spec.frontZ, spec.rearZ, spec.exhaustZ, materials, bodyColors);

        BuildPedestrian(materials);
    }

    private static void BuildCar(string modelName, string prefabName, float wheelX, float frontZ, float rearZ, float exhaustZ,
        Dictionary<string, Material> materials, Material[] bodyColors)
    {
        var root = new GameObject(prefabName);
        try
        {
            var body = AddStreetModel(root.transform, modelName, "Govde", materials);
            var left = new[]
            {
                AddStreetModel(root.transform, "SM_Wheel", "Teker_OnSol", materials),
                AddStreetModel(root.transform, "SM_Wheel", "Teker_ArkaSol", materials),
            };
            var right = new[]
            {
                AddStreetModel(root.transform, "SM_Wheel", "Teker_OnSag", materials),
                AddStreetModel(root.transform, "SM_Wheel", "Teker_ArkaSag", materials),
            };
            // Unity ekseninde aracın solu −X; sağ tekerlekler 180° dönük (jant dışa baksın).
            left[0].localPosition = new Vector3(-wheelX, WheelAxleHeight, frontZ);
            left[1].localPosition = new Vector3(-wheelX, WheelAxleHeight, rearZ);
            right[0].localPosition = new Vector3(wheelX, WheelAxleHeight, frontZ);
            right[1].localPosition = new Vector3(wheelX, WheelAxleHeight, rearZ);
            foreach (var wheel in right)
                wheel.localRotation = Quaternion.Euler(0f, 180f, 0f);

            var shadow = AddStreetModel(root.transform, "SM_BlobShadow", "Golge", materials);
            shadow.localPosition = new Vector3(0f, ShadowLift, 0f);
            shadow.localScale = CarShadowScale;

            var exhaust = BuildExhaust(root.transform, materials["MI_FX_Puff"]);
            exhaust.localPosition = new Vector3(ExhaustOffset.x, ExhaustOffset.y, exhaustZ);
            exhaust.localRotation = Quaternion.Euler(-15f, 180f, 0f);

            root.AddComponent<StreetTraveller>();
            var car = root.AddComponent<StreetCar>();
            var serialized = new SerializedObject(car);
            serialized.FindProperty("body").objectReferenceValue = body;
            SetArray(serialized.FindProperty("leftWheels"), left);
            SetArray(serialized.FindProperty("rightWheels"), right);

            var bodyRenderer = body.GetComponentInChildren<Renderer>();
            int slot = System.Array.FindIndex(bodyRenderer.sharedMaterials, m => m != null && m.name.StartsWith("MI_CarBody"));
            serialized.FindProperty("bodyRenderer").objectReferenceValue = bodyRenderer;
            serialized.FindProperty("bodyMaterialIndex").intValue = slot;
            SetArray(serialized.FindProperty("bodyColors"), bodyColors);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, StreetActorPath(prefabName));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    // Egzoz dumanı: arkaya doğru çıkan, büyüyüp sönen birkaç puf.
    private static Transform BuildExhaust(Transform parent, Material material)
    {
        var exhaust = new GameObject("Egzoz", typeof(ParticleSystem));
        exhaust.transform.SetParent(parent, false);
        var particles = exhaust.GetComponent<ParticleSystem>();

        var main = particles.main;
        main.startLifetime = 0.9f;
        main.startSpeed = 0.6f;
        main.startSize = 0.2f;
        main.startColor = new Color(1f, 1f, 1f, 0.5f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 24;

        var emission = particles.emission;
        emission.rateOverTime = 7f;

        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 10f;
        shape.radius = 0.03f;

        var size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 2.6f));

        var color = particles.colorOverLifetime;
        color.enabled = true;
        var fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        color.color = fade;

        var renderer = exhaust.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return exhaust.transform;
    }

    private static void BuildPedestrian(Dictionary<string, Material> materials)
    {
        var root = new GameObject("Figuran");
        try
        {
            var card = AddStreetModel(root.transform, "SM_Ped_Card", "Kart", materials);
            var shadow = AddStreetModel(root.transform, "SM_BlobShadow", "Golge", materials);
            shadow.localPosition = new Vector3(0f, ShadowLift, 0f);
            shadow.localScale = PedestrianShadowScale;

            root.AddComponent<StreetTraveller>();
            var pedestrian = root.AddComponent<StreetPedestrian>();
            var serialized = new SerializedObject(pedestrian);
            serialized.FindProperty("card").objectReferenceValue = card.GetComponentInChildren<Renderer>();
            // Pastel tonlar (palet): figüranlar tek tip görünmesin.
            var tints = serialized.FindProperty("tints");
            string[] hexes = { "#FFFFFF", "#DCEBFF", "#E6DCF7", "#D8F0E4", "#FFE9D6" };
            tints.arraySize = hexes.Length;
            for (int i = 0; i < hexes.Length; i++)
                tints.GetArrayElementAtIndex(i).colorValue = Parse(hexes[i]);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, StreetActorPath("Figuran"));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static Transform AddStreetModel(Transform parent, string modelName, string name, Dictionary<string, Material> materials)
    {
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(LoadStreetModel(modelName), parent);
        instance.name = name;
        ApplyStreetLook(instance, materials);
        return instance.transform;
    }

    private static void SetArray(SerializedProperty property, Object[] values)
    {
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

    // Sokak materyalleri (paketteki tablo). Mimari ve cephe düz renk; mimari ayarlarıyla (ışığın rengi az yansır).
    private static Dictionary<string, Material> CreateStreetMaterials(Dictionary<string, Material> shared)
    {
        MutfakC1Builder.EnsureFolder(StreetMaterialFolder);
        var result = new Dictionary<string, Material>
        {
            ["MI_Palette"] = shared["MI_Palette"],
            ["MI_Palette_Chrome"] = shared["MI_Palette_Chrome"],
            // Araç camı opak palet rengidir.
            ["MI_CarGlass"] = shared["MI_Palette"],
        };

        var toon = Shader.Find("CNE/Toon");
        foreach (var (name, hex) in new[]
        {
            ("MI_Arch_Kaldirim", "#9BAEC9"), ("MI_Arch_Bordur", "#B7CAE8"), ("MI_Arch_Asfalt", "#54627A"),
            ("MI_Arch_YanDuvar", "#587098"), ("MI_Arch_Kapi", "#4A535B"), ("MI_Arch_Cephe", "#688AC1"),
            ("MI_Arch_Cephe_K0", "#899BBB"), ("MI_Arch_Cephe_K4", "#688AC1"), ("MI_Arch_Cephe_K5", "#8B8DCA"),
            ("MI_Arch_Cephe_K6", "#7AA6AC"),
        })
        {
            result[name] = StreetMaterial(name, toon, material =>
            {
                material.SetColor("_BaseColor", Parse(hex));
                material.SetFloat("_LightTint", ArchitectureLightTint);
                material.SetFloat("_ShadowStrength", WallShadowStrength);
            });
        }

        foreach (var (name, hex) in new[]
        {
            ("MI_CarBody_Nane", "#7DCECD"), ("MI_CarBody_Bebek", "#84B9FF"), ("MI_CarBody_Lila", "#A58FD8"), ("MI_CarBody_Gri", "#899BBB"),
        })
        {
            // Araç gövdesi de palet rengini korur (sıcak güneş gri-maviyi kahveye çekiyordu).
            result[name] = StreetMaterial(name, toon, material =>
            {
                material.SetColor("_BaseColor", Parse(hex));
                material.SetFloat("_LightTint", ArchitectureLightTint);
            });
        }

        result["MI_Ext_Signs"] = StreetMaterial("MI_Ext_Signs", toon,
            material => material.SetTexture("_BaseMap", StreetTexture("DC_Ext_Signs", false)));
        result["MI_Ext_Windows"] = StreetMaterial("MI_Ext_Windows", toon,
            material => material.SetTexture("_BaseMap", StreetTexture("DC_Ext_Windows", false)));

        var unlit = Shader.Find("Universal Render Pipeline/Unlit");
        result["MI_Ext_ShopInterior"] = StreetMaterial("MI_Ext_ShopInterior", unlit,
            material => material.SetTexture("_BaseMap", StreetTexture("DC_ShopInterior", false)));
        result["MI_Ext_FarSilhouette"] = StreetMaterial("MI_Ext_FarSilhouette", unlit, material =>
        {
            material.SetTexture("_BaseMap", StreetTexture("T_Ext_FarSilhouette", true));
            material.SetFloat("_AlphaClip", 1f);
            material.SetFloat("_Cutoff", 0.5f);
            material.EnableKeyword("_ALPHATEST_ON");
            material.SetFloat("_Cull", 0f);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
        });
        // Figüran kartı: yürüme atlası, alpha clip. Kareyi ve aynalamayı StreetPedestrian renderer başına yazar.
        result["MI_Ped"] = StreetMaterial("MI_Ped", unlit, material =>
        {
            material.SetTexture("_BaseMap", StreetTexture("T_Ped_Walk_Atlas", true));
            material.SetFloat("_AlphaClip", 1f);
            material.SetFloat("_Cutoff", 0.5f);
            material.EnableKeyword("_ALPHATEST_ON");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
        });
        // Egzoz pufu: saydam, ışıksız parçacık.
        result["MI_FX_Puff"] = StreetMaterial("MI_FX_Puff", Shader.Find("Universal Render Pipeline/Particles/Unlit"), material =>
        {
            material.SetTexture("_BaseMap", StreetTexture("T_FX_Puff", true));
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        });
        // Oval gölge: çarpma (multiply) karışımı, opaklık 0,35.
        result["MI_BlobShadow"] = StreetMaterial("MI_BlobShadow", unlit, material =>
        {
            material.SetTexture("_BaseMap", StreetTexture("T_BlobShadow", true));
            material.SetColor("_BaseColor", new Color(0.141f, 0.137f, 0.227f, 0.35f));
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        });
        return result;
    }

    private static Texture2D StreetTexture(string name, bool alphaIsTransparency)
    {
        string path = $"{StreetTextureFolder}/{name}.png";
        if (AssetImporter.GetAtPath(path) is TextureImporter importer
            && (importer.wrapMode != TextureWrapMode.Clamp || importer.alphaIsTransparency != alphaIsTransparency))
        {
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = true;
            importer.alphaIsTransparency = alphaIsTransparency;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static Material StreetMaterial(string name, Shader shader, System.Action<Material> configure)
    {
        string path = $"{StreetMaterialFolder}/{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
            return material;

        material = new Material(shader);
        configure(material);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    // Salon paketine özgü materyaller: tabela ve tablo yüzleri (decal), cam parıltısı, akvaryum suyu.
    // Tabelaların ışıyan kısmı ayrı bir dokudadır (_E); CNE/Toon'da özellik maskesi olarak bağlanır (G kanalı) ve
    // yüzey kendi renginde ışır.
    private static Dictionary<string, Material> CreateHallMaterials()
    {
        MutfakC1Builder.EnsureFolder(HallMaterialFolder);
        var result = new Dictionary<string, Material>();
        var toon = Shader.Find("CNE/Toon");
        var palette = AssetDatabase.LoadAssetAtPath<Texture2D>(PalettePath);

        foreach (var (name, strength) in new[]
        {
            ("Giris", 1.6f), ("Cikis", 1.6f), ("Teslim", 2f), ("Menu", 2f), ("Logo", 2.2f), ("JukePanel", 1f),
        })
        {
            result["MI_Decal_" + name] = HallMaterial("MI_Decal_" + name, toon, material =>
            {
                material.SetTexture("_BaseMap", HallTexture($"T_Decal_{name}", false));
                material.SetTexture("_PropMap", HallTexture($"T_Decal_{name}_E", true));
                material.EnableKeyword("_EMISSION_ON");
                material.SetFloat("_Emission", 1f);
                material.SetColor("_EmissionColor", Color.white * strength);
                material.SetFloat("_EmissionBaseTint", 1f);
            });
        }

        // Yazısı dile göre değişen tabelaların boş yüzü (yazı sahnede, Localization tablosundan gelir).
        result[SignFaceMaterialName] = HallMaterial(SignFaceMaterialName, toon,
            material => material.SetColor("_BaseColor", Parse("#2C3237")));

        foreach (string name in new[] { "Araba", "Atom", "Milkshake", "Plak" })
        {
            result["MI_Decal_Tablo_" + name] = HallMaterial("MI_Decal_Tablo_" + name, toon,
                material => material.SetTexture("_BaseMap", HallTexture($"T_Decal_Tablo_{name}", false)));
        }

        var lit = Shader.Find("Universal Render Pipeline/Lit");
        result["MI_Palette_GlassStreak"] = HallMaterial("MI_Palette_GlassStreak", lit,
            material => MakeTransparent(material, palette, 0.6f));
        result["MI_Palette_Water"] = HallMaterial("MI_Palette_Water", lit, material =>
        {
            MakeTransparent(material, palette, 0.45f);
            material.SetTexture("_EmissionMap", palette);
            material.SetColor("_EmissionColor", Color.white * 0.3f);
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        });
        return result;
    }

    // Saydam URP Lit (C1'deki cam materyaliyle aynı kurulum): CNE/Toon yalnızca opak çizer.
    private static void MakeTransparent(Material material, Texture2D palette, float opacity)
    {
        material.SetTexture("_BaseMap", palette);
        material.SetColor("_BaseColor", new Color(1f, 1f, 1f, opacity));
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.SetFloat("_Smoothness", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.SetOverrideTag("RenderType", "Transparent");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    private static Texture2D HallTexture(string name, bool linear)
    {
        string path = $"{HallTextureFolder}/{name}.png";
        if (AssetImporter.GetAtPath(path) is TextureImporter importer
            && (importer.wrapMode != TextureWrapMode.Clamp || importer.sRGBTexture == linear))
        {
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = true;
            importer.sRGBTexture = !linear;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static Material HallMaterial(string name, Shader shader, System.Action<Material> configure)
    {
        string path = $"{HallMaterialFolder}/{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
            return material;

        material = new Material(shader);
        configure(material);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void Add(Dictionary<string, Material> result, string name, string hex)
    {
        result[name] = CreateMaterial(name, hex, null);
    }

    private static void Add(Dictionary<string, Material> result, string name, string hex, float patternType,
        string patternHex, float sizeX, float sizeY, float lineWidth)
    {
        result[name] = CreateMaterial(name, hex, material =>
        {
            material.EnableKeyword("_SURFACEPATTERN_ON");
            material.SetFloat("_SurfacePattern", 1f);
            material.SetFloat("_PatternType", patternType);
            material.SetColor("_PatternColor", Parse(patternHex));
            material.SetVector("_PatternSize", new Vector4(sizeX, sizeY, 0f, 0f));
            if (lineWidth > 0f)
                material.SetFloat("_PatternLine", lineWidth);
        });
    }

    private static Material CreateMaterial(string name, string hex, System.Action<Material> configure)
    {
        string path = $"{MaterialFolder}/{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
            return material;

        material = new Material(Shader.Find("CNE/Toon"));
        material.SetColor("_BaseColor", Parse(hex));
        // Mimari palet rengini korur: ışığın rengi az yansır, gölgedeki yüz hafif koyulaşır (tavan neredeyse hiç).
        material.SetFloat("_LightTint", ArchitectureLightTint);
        material.SetFloat("_ShadowStrength", name.Contains("Tavan") ? CeilingShadowStrength : WallShadowStrength);
        configure?.Invoke(material);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static Color Parse(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out var color);
        return color;
    }
}
