using System.IO;
using UnityEditor;
using UnityEngine;

// DENEME — toon shader'ın ramp dokusunu üretir (menü: Cook No Evil → Stilize Deneme → Ramp Dokusu Üret).
// Ramp yatay bir şerittir: sol uç gölge (siyah), sağ uç ışık (beyaz); arada yumuşak bir basamak vardır.
// Basamağın yeri ve yumuşaklığı aşağıdaki iki sabitle değişir; değiştirip menüden yeniden üretmek yeterlidir.
public static class StylizedRampGenerator
{
    public const string RampPath = "Assets/StylizedTest/StylizedRamp.png";

    // Işık-gölge sınırının ramp üzerindeki yeri (0–1) ve geçişin genişliği. Küçük genişlik = keskin, çizgi film gibi.
    private const float Threshold = 0.48f;
    private const float Softness = 0.16f;
    // Gölge tarafında çok hafif bir ikinci ton (tam düz durmasın).
    private const float ShadowLift = 0.12f;

    [MenuItem("Cook No Evil/Stilize Deneme/Ramp Dokusu Üret")]
    public static void Generate()
    {
        const int width = 256;
        const int height = 8;
        var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        for (int x = 0; x < width; x++)
        {
            float u = x / (width - 1f);
            float step = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(Threshold - Softness * 0.5f, Threshold + Softness * 0.5f, u));
            float value = Mathf.Lerp(u * ShadowLift, 1f, step);
            var color = new Color(value, value, value, 1f);
            for (int y = 0; y < height; y++)
                texture.SetPixel(x, y, color);
        }

        File.WriteAllBytes(RampPath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(RampPath);

        var importer = (TextureImporter)AssetImporter.GetAtPath(RampPath);
        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();

        Debug.Log($"[StylizedRampGenerator] Ramp üretildi: {RampPath}");
    }
}
