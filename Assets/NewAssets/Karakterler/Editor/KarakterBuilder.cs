using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Karakter paketini (Assets/NewAssets/Karakterler; artist'in BENIOKU.md'si yanında) projeye kurar:
//  - paketin yeni palet hücrelerini ana palete ekler,
//  - dört karakter materyalini (CNE/Toon) üretir ve FBX'lere eşler,
//  - mevcut karakter prefab'larını (Assets/Prefabs/Characters/Karakter_*) YERİNDE yeniden kurar: prefab dosyası,
//    kimliği ve animasyon ayarları kalır; gövde ve ayakkabılar yeni modelden gelir.
// Prefab düzeni değişmez (ProceduralCharacterAnimator aynı parçaları sürer): kök → Govde, AyakSol/Sag, ElSol/Sag.
// Yeni modelin kemikleri bu pivotların altına alınır: Body → Govde, Foot_L/R → AyakSol/Sag.
// ELLER: şimdilik eski el modeli kullanılır (mevcut bütün el animasyonları ona göre); paketin beş parmaklı
// eldivenleri prefab'da KAPALI durur ("YeniEldivenler"), geçiş ayrı iştir.
// Yeniden çalıştırılabilir.
public static class KarakterBuilder
{
    private const string Root = "Assets/NewAssets/Karakterler";
    private const string MaterialFolder = Root + "/Materials";
    private const string PackagePalettePath = Root + "/Textures/CNE_Karakter_Palet_256.png";
    private const string MainPalettePath = "Assets/NewAssets/Mutfak_C1/Palettes/cook_no_evil_palet_256.png";
    private const string MaskPath = "Assets/NewAssets/Mutfak_C1/Palettes/Mutfak_Beyaz_Prop.png";
    private const string MatcapPath = "Assets/CNEToon/Lookdev/Textures/Lookdev_Matcap_Krom.png";
    private const string PrefabFolder = "Assets/Prefabs/Characters";
    private const string SpareGlovesName = "YeniEldivenler";
    private const string ShoesName = "Ayakkabilar";

    private const int PaletteCells = 16;
    private static readonly Color32 EmptyCell = new(0xFF, 0x00, 0xFF, 0xFF);

    // Karakterler yalnızca dış hat alır (CLAUDE.md çizgi kuralı): "Outline Silhouette" rendering layer'ı.
    private const uint SilhouetteLayer = 1u << 9;

    // Şef'in yüzü yok: yüze dokunan hareketler göz bandının üst kenarına gider (paket: bant 1,17–1,40 m).
    private const float BlindfoldTop = 1.40f;

    // Jest merkezi gövdenin en az bu kadar önünde durur (el gövdenin içine girmesin).
    private const float GestureClearance = 0.12f;

    private readonly struct Character
    {
        public readonly string Model, Prefab, Prefix;

        public Character(string model, string prefab, string prefix)
        {
            Model = model;
            Prefab = prefab;
            Prefix = prefix;
        }
    }

    private static readonly Character[] Characters =
    {
        new("CNE_Sef", "Karakter_Hamburger", "Sef"),
        new("CNE_Komi", "Karakter_Ketcap", "Komi"),
        new("CNE_Kasiyer", "Karakter_Kasa", "Kasiyer"),
    };

    [MenuItem("CNE/Karakterler/Build")]
    public static void Build()
    {
        MergePalette();
        var materials = CreateMaterials();
        foreach (var character in Characters)
            ConfigureModel($"{Root}/Models/{character.Model}.fbx", materials);

        foreach (var character in Characters)
            BuildPrefab(character);

        AssetDatabase.SaveAssets();
        Debug.Log("[CNE] Karakterler kuruldu.");
    }

    // ---------- palet ----------

    // Paketin paletinde olup ana palette BOŞ duran hücreler ana palete kopyalanır. Ana palette dolu ama farklı
    // renkte bir hücre varsa dokunulmaz ve hata yazılır (iki paket aynı hücreyi farklı kullanıyor demektir).
    private static void MergePalette()
    {
        var package = LoadPixels(PackagePalettePath);
        var main = LoadPixels(MainPalettePath);
        if (package.width != main.width || package.height != main.height)
        {
            Debug.LogError("[CNE] Karakter paleti ile ana paletin boyutu farklı; palet birleştirilmedi.");
            return;
        }

        int cell = main.width / PaletteCells;
        int added = 0;
        for (int row = 0; row < PaletteCells; row++)
        {
            for (int column = 0; column < PaletteCells; column++)
            {
                int x = column * cell, y = main.height - (row + 1) * cell;
                Color32 wanted = package.GetPixel(x + cell / 2, y + cell / 2);
                Color32 current = main.GetPixel(x + cell / 2, y + cell / 2);
                if (Same(wanted, current) || Same(wanted, EmptyCell))
                    continue;

                if (!Same(current, EmptyCell))
                {
                    Debug.LogError($"[CNE] Palet çakışması S{row}·K{column}: paket #{ColorUtility.ToHtmlStringRGB(wanted)}, " +
                        $"ana palet #{ColorUtility.ToHtmlStringRGB(current)}. Hücre değiştirilmedi.");
                    continue;
                }

                main.SetPixels(x, y, cell, cell, package.GetPixels(x, y, cell, cell));
                added++;
            }
        }

        if (added == 0)
            return;

        File.WriteAllBytes(MainPalettePath, main.EncodeToPNG());
        AssetDatabase.ImportAsset(MainPalettePath);
        Debug.Log($"[CNE] Ana palete {added} hücre eklendi.");
    }

    private static Texture2D LoadPixels(string path)
    {
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.LoadImage(File.ReadAllBytes(path));
        return texture;
    }

    private static bool Same(Color32 a, Color32 b) => a.r == b.r && a.g == b.g && a.b == b.b;

    // ---------- materyaller ----------

    private static Dictionary<string, Material> CreateMaterials()
    {
        if (!AssetDatabase.IsValidFolder(MaterialFolder))
            AssetDatabase.CreateFolder(Root, "Materials");

        var palette = AssetDatabase.LoadAssetAtPath<Texture2D>(MainPalettePath);
        var mask = AssetDatabase.LoadAssetAtPath<Texture2D>(MaskPath);
        var matcap = AssetDatabase.LoadAssetAtPath<Texture2D>(MatcapPath);
        var toon = Shader.Find("CNE/Toon");

        return new Dictionary<string, Material>
        {
            ["MI_Char_Palette"] = CreateMaterial("MI_Char_Palette", toon, m =>
            {
                m.SetTexture("_BaseMap", palette);
                Enable(m, "_RIM_ON", "_Rim");
            }),
            ["MI_Char_Glove"] = CreateMaterial("MI_Char_Glove", toon, m => m.SetTexture("_BaseMap", palette)),
            ["MI_Char_Chrome"] = CreateMaterial("MI_Char_Chrome", toon, m =>
            {
                m.SetTexture("_BaseMap", palette);
                m.SetTexture("_PropMap", mask);
                m.SetTexture("_MatCapTex", matcap);
                Enable(m, "_MATCAP_ON", "_MatCap");
            }),
            ["MI_Char_Steel"] = CreateMaterial("MI_Char_Steel", toon, m =>
            {
                m.SetTexture("_BaseMap", palette);
                m.SetTexture("_PropMap", mask);
                Enable(m, "_SPECULAR_ON", "_Specular");
            }),
        };
    }

    // Var olan materyalin üzerine yazılmaz (elle yapılan ayar kaybolmasın).
    private static Material CreateMaterial(string name, Shader shader, System.Action<Material> configure)
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

    // ---------- model içe alma ----------

    private static void ConfigureModel(string path, Dictionary<string, Material> materials)
    {
        var importer = (ModelImporter)AssetImporter.GetAtPath(path);
        importer.globalScale = 1f;
        importer.useFileScale = true;
        importer.importBlendShapes = true;
        importer.importBlendShapeNormals = ModelImporterNormals.Import;
        importer.importNormals = ModelImporterNormals.Import;
        importer.meshCompression = ModelImporterMeshCompression.Off;
        importer.generateSecondaryUV = false;
        importer.importAnimation = false;
        // Klip ve Animator yok (hareket ProceduralCharacterAnimator'dan): Avatar üretilmez; kemikler düz Transform.
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.NoAvatar;
        importer.optimizeGameObjects = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        foreach (var pair in materials)
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), pair.Key), pair.Value);

        importer.SaveAndReimport();
    }

    // ---------- prefab ----------

    private static void BuildPrefab(Character character)
    {
        string prefabPath = $"{PrefabFolder}/{character.Prefab}.prefab";
        var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Models/{character.Model}.fbx");
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var body = root.transform.Find("Govde");
            var leftFoot = root.transform.Find("AyakSol");
            var rightFoot = root.transform.Find("AyakSag");
            var leftHand = root.transform.Find("ElSol");
            var rightHand = root.transform.Find("ElSag");

            // Önceki kurulumun / eski modelin parçaları.
            ClearChildren(body);
            ClearChildren(leftFoot);
            ClearChildren(rightFoot);
            DestroyChild(root.transform, SpareGlovesName);
            DestroyChild(root.transform, ShoesName);

            // Model düz kopya olarak alınır (prefab bağı yok): kemikler pivotların altına taşınabilsin.
            var clone = Object.Instantiate(model, root.transform);
            clone.name = SpareGlovesName;
            clone.transform.localPosition = Vector3.zero;
            clone.transform.localRotation = Quaternion.identity;
            foreach (var animator in clone.GetComponentsInChildren<Animator>(true))
                Object.DestroyImmediate(animator);

            var bones = new Dictionary<string, Transform>();
            foreach (var bone in clone.GetComponentsInChildren<Transform>(true))
                bones[bone.name] = bone;

            // Gövde: Body kemiği ve onu çizen mesh'ler Govde pivotunun altında (pivot yerinde kalır).
            bones["Body"].SetParent(body, true);
            MoveRenderer(bones, character.Prefix + "_Body", body);
            if (bones.ContainsKey(character.Prefix + "_Face"))
                MoveRenderer(bones, character.Prefix + "_Face", body);

            // Ayaklar: pivot ayağın yere bastığı noktada (adım eğimi tabandan döner); kemik pivotun altında.
            PlaceFoot(leftFoot, bones["Foot_L"]);
            PlaceFoot(rightFoot, bones["Foot_R"]);
            var shoes = MoveRenderer(bones, character.Prefix + "_Shoes", root.transform);
            shoes.name = ShoesName;

            // Eller: eski el modeli, yeni tasarımın el yerinde. Yeni eldivenler (kemikleriyle) kapalı durur.
            leftHand.localPosition = root.transform.InverseTransformPoint(bones["Hand_L"].position);
            rightHand.localPosition = root.transform.InverseTransformPoint(bones["Hand_R"].position);
            foreach (string glove in new[] { "_Glove_L", "_Glove_R" })
                bones[character.Prefix + glove].GetComponent<SkinnedMeshRenderer>().receiveShadows = false;
            clone.SetActive(false);

            ConfigureAnimator(root, body, bones);
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static SkinnedMeshRenderer MoveRenderer(Dictionary<string, Transform> bones, string name, Transform parent)
    {
        var renderer = bones[name].GetComponent<SkinnedMeshRenderer>();
        renderer.transform.SetParent(parent, true);
        renderer.renderingLayerMask |= SilhouetteLayer;
        return renderer;
    }

    private static void PlaceFoot(Transform pivot, Transform bone)
    {
        var position = pivot.parent.InverseTransformPoint(bone.position);
        pivot.localPosition = new Vector3(position.x, 0f, position.z);
        pivot.localRotation = Quaternion.identity;
        bone.SetParent(pivot, true);
    }

    // Karakterin ölçüsüne bağlı animasyon ayarları modelden okunur: kaş noktası (yüze dokunan hareketler) ve jest
    // merkezinin gövdenin önünde kalması. Diğer ayarlar prefab'daki değerleriyle kalır.
    private static void ConfigureAnimator(GameObject root, Transform body, Dictionary<string, Transform> bones)
    {
        var bounds = new Bounds();
        bool any = false;
        foreach (var renderer in body.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            // Bind pozu = dinlenme: mesh sınırı model uzayındadır ve model kökte, dönüşsüz durur.
            if (any) bounds.Encapsulate(renderer.sharedMesh.bounds);
            else bounds = renderer.sharedMesh.bounds;
            any = true;
        }

        var serialized = new SerializedObject(root.GetComponent<ProceduralCharacterAnimator>());
        var gesture = serialized.FindProperty("gestureCenter");
        var center = gesture.vector3Value;
        center.z = Mathf.Max(center.z, bounds.max.z + GestureClearance);
        gesture.vector3Value = center;

        Vector3 brow;
        if (bones.TryGetValue("Brow_R", out var browBone))
        {
            brow = root.transform.InverseTransformPoint(browBone.position);
        }
        else
        {
            // Yüzü olmayan karakter (Şef): göz bandının üst kenarı, gövdenin ön yüzü.
            brow = new Vector3(bounds.extents.x * 0.25f, BlindfoldTop, bounds.max.z);
        }

        serialized.FindProperty("browPoint").vector3Value = new Vector3(Mathf.Abs(brow.x), brow.y, brow.z);
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(parent.GetChild(i).gameObject);
    }

    private static void DestroyChild(Transform parent, string name)
    {
        var child = parent.Find(name);
        if (child != null)
            Object.DestroyImmediate(child.gameObject);
    }
}
