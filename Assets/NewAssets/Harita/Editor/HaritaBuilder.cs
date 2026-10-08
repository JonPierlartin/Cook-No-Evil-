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
        CreateArchitectureMaterials();
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
        Add(result, "MI_Arch_Kasa_Tavan", "#F7F1E3");
        Add(result, "MI_Arch_Tavan", "#EEF2F2");
        Add(result, "MI_Arch_Supurgelik", "#5C4A40");
        Add(result, "MI_Arch_Dis", "#9AA0A6");
        Add(result, "MI_Arch_Dis_Zemin", "#8B9096", Grid, "#7C8187", 1.00f, 1.00f, 0.012f);
        return result;
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
