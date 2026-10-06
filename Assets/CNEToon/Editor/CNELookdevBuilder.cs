using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// ToonLookdev test sahnesini ve örnek varlıklarını (materyal, doku, mesh) KODLA üretir: CNE → Lookdev → Build Scene.
// Sahne yeniden üretilebilir olsun diye elle kurulmadı; menü her çalıştığında sahneyi baştan yazar (materyaller ve
// dokular varsa korunur, böylece elle yapılan materyal ayarı kaybolmaz).
public static class CNELookdevBuilder
{
    private const string Root = "Assets/CNEToon/Lookdev";
    private const string MaterialFolder = Root + "/Materials";
    private const string TextureFolder = Root + "/Textures";
    private const string PaletteFolder = Root + "/Palettes";
    private const string MeshFolder = Root + "/Meshes";
    private const string ScenePath = "Assets/Scenes/ToonLookdev.unity";
    private const string LightingSettingsPath = Root + "/ToonLookdevLighting.lighting";
    private const string OutlineLayerName = "Outline";
    private const int BlindRendererIndex = 1;

    [MenuItem("CNE/Lookdev/Build Scene")]
    public static void Build()
    {
        foreach (string folder in new[] { Root, MaterialFolder, TextureFolder, PaletteFolder, MeshFolder })
            EnsureFolder(folder);

        // Dokular ve mesh.
        var whiteMask = CreateTexture(PaletteFolder + "/Lookdev_Beyaz_Prop.png", 4, 4, (x, y) => Color.white, TextureWrapMode.Clamp, false);
        var matcap = CreateTexture(TextureFolder + "/Lookdev_Matcap_Krom.png", 256, 256, ChromeMatcap, TextureWrapMode.Clamp, true);
        var hatch = CreateTexture(TextureFolder + "/Lookdev_Tarama.png", 64, 64, HatchPattern, TextureWrapMode.Repeat, false);
        var bevelCube = CreateBevelCube(MeshFolder + "/Lookdev_BevelKup.asset", 0.5f, 0.08f);

        // Materyaller.
        var backdrop = CreateMaterial("Fon", "#172A3A");
        var floor = CreateMaterial("Zemin", "#2A4558");
        var ketchup = CreateMaterial("Sinyal_Ketcap", "#E0262B");
        var mustard = CreateMaterial("Sinyal_Hardal", "#F5C518");
        var mayo = CreateMaterial("Sinyal_Mayonez", "#FAF6EA");
        var barbecue = CreateMaterial("Sinyal_Barbeku", "#8C4A22");
        var glove = CreateMaterial("Eldiven_Beyaz", "#FFFFFF", m =>
        {
            Enable(m, "_RIM_ON", "_Rim");
        });
        var steel = CreateMaterial("Paslanmaz_Celik", "#A9B3BA", m =>
        {
            Enable(m, "_SPECULAR_ON", "_Specular");
            m.SetTexture("_PropMap", whiteMask);
            m.SetFloat("_SpecSize", 0.08f);
        });
        var chrome = CreateMaterial("Krom", "#C9D2D8", m =>
        {
            Enable(m, "_MATCAP_ON", "_MatCap");
            m.SetTexture("_PropMap", whiteMask);
            m.SetTexture("_MatCapTex", matcap);
        });
        var neon = CreateMaterial("Neon", "#101820", m =>
        {
            Enable(m, "_EMISSION_ON", "_Emission");
            m.SetTexture("_PropMap", whiteMask);
            m.SetColor("_EmissionColor", new Color(0.2f, 4f, 3.2f));
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
        });
        var hatched = CreateMaterial("Tarama", "#C98A4B", m =>
        {
            Enable(m, "_HATCH_ON", "_Hatch");
            m.SetTexture("_PropMap", whiteMask);
            m.SetTexture("_HatchTex", hatch);
            m.SetFloat("_HatchStrength", 0.35f);
            m.SetFloat("_HatchScale", 6f);
        });
        var prop = CreateMaterial("Nesne", "#E8873A");

        // Sahne: açık sahnelere dokunmadan ek olarak kurulur, kaydedilir, kapatılır.
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        var previousActive = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(scene);
        try
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.10f, 0.13f, 0.18f);
            RenderSettings.fog = false;

            uint outlineBit = OutlineLayerBit();

            var environment = new GameObject("Ortam");
            var floorObject = Primitive(PrimitiveType.Cube, "Zemin", environment.transform, new Vector3(0f, -0.25f, 0f), new Vector3(14f, 0.5f, 9f), floor);
            var wall = Primitive(PrimitiveType.Cube, "Fon", environment.transform, new Vector3(0f, 2.5f, 3.5f), new Vector3(14f, 6f, 0.5f), backdrop);
            // Arka sıra bir basamağın üstünde durur ki ön sıradaki nesneler onu kapatmasın.
            var step = Primitive(PrimitiveType.Cube, "Basamak", environment.transform, new Vector3(0f, 0.6f, 1.8f), new Vector3(13f, 1.2f, 1.6f), floor);
            foreach (var staticObject in new[] { floorObject, wall, step })
                GameObjectUtility.SetStaticEditorFlags(staticObject, StaticEditorFlags.ContributeGI | StaticEditorFlags.ReflectionProbeStatic);

            // Arka sıra: sinyal renkleri, eldiven, tarama, çelik, krom, neon.
            var samples = new GameObject("Ornekler");
            var row = new (string name, Material material, bool outline)[]
            {
                ("Kure_Ketcap", ketchup, true), ("Kure_Hardal", mustard, true), ("Kure_Mayonez", mayo, true),
                ("Kure_Barbeku", barbecue, true), ("Kure_Eldiven", glove, true), ("Kure_Tarama", hatched, true),
                ("Kure_Celik", steel, true), ("Kure_Krom", chrome, true), ("Kure_Neon", neon, false),
            };
            float spacing = 1.25f;
            float start = -(row.Length - 1) * spacing * 0.5f;
            for (int i = 0; i < row.Length; i++)
            {
                var sphere = Primitive(PrimitiveType.Sphere, row[i].name, samples.transform, new Vector3(start + i * spacing, 1.7f, 1.8f), Vector3.one, row[i].material);
                if (row[i].outline)
                    AddRenderingLayer(sphere, outlineBit);
            }

            // Ön sıra: Outline katmanında olan ve olmayan aynı nesne; bevel'lı ve bevel'sız küp.
            var front = new GameObject("Kontur_Karsilastirma");
            var withOutline = Primitive(PrimitiveType.Capsule, "Nesne_OutlineKatmaninda", front.transform, new Vector3(-3f, 1f, -1.2f), Vector3.one, prop);
            AddRenderingLayer(withOutline, outlineBit);
            Primitive(PrimitiveType.Capsule, "Nesne_KatmanYok", front.transform, new Vector3(-1.5f, 1f, -1.2f), Vector3.one, prop);

            var bevel = Primitive(PrimitiveType.Cube, "Kup_Bevelli", front.transform, new Vector3(1.5f, 0.5f, -1.2f), Vector3.one, prop);
            bevel.GetComponent<MeshFilter>().sharedMesh = bevelCube;
            bevel.transform.rotation = Quaternion.Euler(0f, 30f, 0f);
            AddRenderingLayer(bevel, outlineBit);
            var hard = Primitive(PrimitiveType.Cube, "Kup_Bevelsiz", front.transform, new Vector3(3f, 0.5f, -1.2f), Vector3.one, prop);
            hard.transform.rotation = Quaternion.Euler(0f, 30f, 0f);
            AddRenderingLayer(hard, outlineBit);

            // Işıklar: ana ışık Mixed (Baked Indirect) — direkt ışık realtime ve basamaklı kalır, lightmap yalnızca
            // sekme ışığını taşır. Lamba: ek ışıkların basamaklı çalıştığını göstermek için.
            var lights = new GameObject("Isiklar");
            var sunObject = new GameObject("AnaIsik");
            sunObject.transform.SetParent(lights.transform, false);
            sunObject.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            var sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.lightmapBakeType = LightmapBakeType.Mixed;
            sun.color = new Color32(0xFF, 0xE9, 0xC7, 0xFF);
            sun.intensity = 1f;
            sun.shadows = LightShadows.Soft;

            var lampObject = new GameObject("Lamba");
            lampObject.transform.SetParent(lights.transform, false);
            lampObject.transform.position = new Vector3(4.6f, 2.7f, 0.6f);
            var lamp = lampObject.AddComponent<Light>();
            lamp.type = LightType.Point;
            lamp.lightmapBakeType = LightmapBakeType.Realtime;
            lamp.color = new Color32(0xFF, 0xB0, 0x60, 0xFF);
            lamp.intensity = 3f;
            lamp.range = 4f;

            var probes = new GameObject("IsikProblari").AddComponent<LightProbeGroup>();
            probes.transform.SetParent(lights.transform, false);
            var positions = new List<Vector3>();
            for (int x = -6; x <= 6; x += 3)
                for (int z = -3; z <= 3; z += 3)
                    for (int y = 0; y < 2; y++)
                        positions.Add(new Vector3(x, 0.4f + y * 1.8f, z));
            probes.probePositions = positions.ToArray();

            // Kameralar.
            var target = new GameObject("KameraHedefi");
            target.transform.position = new Vector3(0f, 1.0f, 0.4f);

            var cameraObject = new GameObject("Kamera");
            cameraObject.transform.position = new Vector3(0f, 3.2f, -8.5f);
            cameraObject.transform.LookAt(target.transform.position);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = (Color)new Color32(0x17, 0x2A, 0x3A, 0xFF);
            camera.fieldOfView = 45f;
            camera.allowHDR = true;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<UniversalAdditionalCameraData>();

            var chefObject = new GameObject("SefKamerasi");
            chefObject.transform.SetParent(cameraObject.transform, false);
            var chef = chefObject.AddComponent<Camera>();
            chef.CopyFrom(camera);
            chef.depth = camera.depth + 1f;
            chef.enabled = false;
            var chefData = chefObject.AddComponent<UniversalAdditionalCameraData>();
            chefData.SetRenderer(BlindRendererIndex);
            chefData.renderPostProcessing = false;

            var controller = new GameObject("LookdevDenetimi").AddComponent<CNELookdevController>();
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("mainLight").objectReferenceValue = sun;
            serialized.FindProperty("playerCamera").objectReferenceValue = camera;
            serialized.FindProperty("chefCamera").objectReferenceValue = chef;
            serialized.FindProperty("orbitTarget").objectReferenceValue = target.transform;
            serialized.FindProperty("toneNoneProfile").objectReferenceValue = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/CNEToon/Post/CNE_Global.asset");
            serialized.FindProperty("toneNeutralProfile").objectReferenceValue = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/CNEToon/Post/CNE_Global_Neutral.asset");
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Lightmapping.SetLightingSettingsForScene(scene, CreateLightingSettings());
            EditorSceneManager.SaveScene(scene, ScenePath);
        }
        finally
        {
            SceneManager.SetActiveScene(previousActive);
            EditorSceneManager.CloseScene(scene, true);
        }

        AddToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log($"[CNE] Test sahnesi üretildi: {ScenePath}. Lightmap için sahneyi açıp Lighting penceresinden Generate Lighting.");
    }

    private static LightingSettings CreateLightingSettings()
    {
        var settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(LightingSettingsPath);
        if (settings != null)
            return settings;

        settings = new LightingSettings
        {
            name = "ToonLookdevLighting",
            bakedGI = true,
            realtimeGI = false,
            mixedBakeMode = MixedLightingMode.IndirectOnly,
            lightmapper = LightingSettings.Lightmapper.ProgressiveCPU,
            lightmapResolution = 12f,
            lightmapMaxSize = 512,
            directSampleCount = 16,
            indirectSampleCount = 128,
            ao = true,
        };
        AssetDatabase.CreateAsset(settings, LightingSettingsPath);
        return settings;
    }

    private static uint OutlineLayerBit()
    {
        uint mask = RenderingLayerMask.GetMask(OutlineLayerName);
        if (mask == 0)
            Debug.LogWarning($"[CNE] '{OutlineLayerName}' rendering layer'ı tanımlı değil; test nesneleri çizgi almayacak.");

        return mask;
    }

    // Diğer bitler korunur: yalnızca Outline biti eklenir.
    private static void AddRenderingLayer(GameObject target, uint bit)
    {
        var renderer = target.GetComponent<Renderer>();
        renderer.renderingLayerMask |= bit;
    }

    private static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        var instance = GameObject.CreatePrimitive(type);
        instance.name = name;
        instance.transform.SetParent(parent, false);
        instance.transform.position = position;
        instance.transform.localScale = scale;
        instance.GetComponent<Renderer>().sharedMaterial = material;
        return instance;
    }

    private static Material CreateMaterial(string name, string hex, System.Action<Material> configure = null)
    {
        string path = $"{MaterialFolder}/Lookdev_{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
            return material;

        material = new Material(Shader.Find("CNE/Toon"));
        ColorUtility.TryParseHtmlString(hex, out var color);
        material.SetColor("_BaseColor", color);
        configure?.Invoke(material);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void Enable(Material material, string keyword, string toggleProperty)
    {
        material.EnableKeyword(keyword);
        material.SetFloat(toggleProperty, 1f);
    }

    private static Texture2D CreateTexture(string path, int width, int height, System.Func<int, int, Color> pixel, TextureWrapMode wrap, bool srgb)
    {
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null)
            return existing;

        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                texture.SetPixel(x, y, pixel(x, y));

        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.sRGBTexture = srgb;
        importer.wrapMode = wrap;
        importer.mipmapEnabled = wrap == TextureWrapMode.Repeat;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // Krom matcap'i: üstte açık gök, ortada koyu ufuk bandı, altta sıcak zemin, sol üstte parlak leke.
    private static Color ChromeMatcap(int x, int y)
    {
        float u = x / 255f * 2f - 1f;
        float v = y / 255f * 2f - 1f;
        if (u * u + v * v > 1f)
            return new Color(0.5f, 0.5f, 0.5f);

        var sky = Color.Lerp(new Color(0.55f, 0.66f, 0.78f), new Color(0.95f, 0.97f, 1f), Mathf.InverseLerp(0.05f, 1f, v));
        var ground = Color.Lerp(new Color(0.16f, 0.15f, 0.18f), new Color(0.62f, 0.52f, 0.44f), Mathf.InverseLerp(-0.05f, -1f, v));
        var color = v > 0.05f ? sky : v < -0.05f ? ground : new Color(0.10f, 0.11f, 0.14f);
        float highlight = Mathf.SmoothStep(0.22f, 0.12f, Vector2.Distance(new Vector2(u, v), new Vector2(-0.35f, 0.5f)));
        return Color.Lerp(color, Color.white, highlight);
    }

    private static Color HatchPattern(int x, int y)
    {
        bool line = (x + y) % 8 < 2;
        return line ? new Color(0.35f, 0.35f, 0.35f) : Color.white;
    }

    // Bevel'lı (pahlı) küp: her yüz 3×3 hücre; köşe ve kenar hücreleri iç kutudan yarıçap kadar dışarı itilir.
    // Normaller yuvarlatılmıştır (bevel'da keskin normal kırığı yoktur — kontur açısından bevel'sız küple fark budur).
    private static Mesh CreateBevelCube(string path, float half, float radius)
    {
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null)
            return existing;

        float inner = half - radius;
        float[] grid = { -half, -inner, inner, half };
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();

        Vector3[] faceNormals = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
        foreach (var normal in faceNormals)
        {
            Vector3 tangent = Mathf.Abs(normal.y) > 0.5f ? Vector3.right : Vector3.up;
            Vector3 bitangent = Vector3.Cross(normal, tangent);
            int baseIndex = vertices.Count;

            for (int j = 0; j < 4; j++)
            {
                for (int i = 0; i < 4; i++)
                {
                    Vector3 point = normal * half + tangent * grid[i] + bitangent * grid[j];
                    Vector3 clamped = new Vector3(
                        Mathf.Clamp(point.x, -inner, inner), Mathf.Clamp(point.y, -inner, inner), Mathf.Clamp(point.z, -inner, inner));
                    Vector3 direction = (point - clamped).normalized;
                    vertices.Add(clamped + direction * radius);
                    normals.Add(direction);
                    uvs.Add(new Vector2(i / 3f, j / 3f));
                }
            }

            for (int j = 0; j < 3; j++)
            {
                for (int i = 0; i < 3; i++)
                {
                    int a = baseIndex + j * 4 + i;
                    int b = a + 1;
                    int c = a + 4;
                    int d = c + 1;
                    triangles.AddRange(new[] { a, b, d, a, d, c });
                }
            }
        }

        var mesh = new Mesh { name = "Lookdev_BevelKup" };
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        Unwrapping.GenerateSecondaryUVSet(mesh);
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    private static void AddToBuildSettings(string scenePath)
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (scenes.Exists(s => s.path == scenePath))
            return;

        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
