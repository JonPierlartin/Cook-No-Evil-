using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// Arayüz sprite üretici (editör aracı): tarif kitapçığı ve duvar panosunun kullandığı panel, kâğıt, kara tahta, ahşap
// çerçeve, gölge, halka vb. sprite'ları KODLA çizip asset olarak kaydeder (9-dilim kenarlıklarıyla). Artist sanatı
// gelene kadar arayüzün "test" gibi görünmemesi için; final sanatta bu dosyalar aynı adla değiştirilir, arayüz
// hiyerarşisi değişmez. Çalışma zamanında üretim yoktur.
public static class UiSpriteGenerator
{
    public const string Folder = "Assets/UI/Generated";

    [MenuItem("Cook No Evil/Arayüz Sprite'larını Üret")]
    public static void GenerateAll()
    {
        Directory.CreateDirectory(Folder);

        // Düz yuvarlak köşeli panel (renk Image.color ile verilir).
        Save("UI_Rounded", 64, 64, 20, (x, y) => new Color(1, 1, 1, Coverage(RoundedBox(x, y, 64, 64, 18))));
        // Yuvarlak köşeli çerçeve (içi boş).
        Save("UI_RoundedOutline", 64, 64, 20, (x, y) =>
        {
            float d = RoundedBox(x, y, 64, 64, 18);
            return new Color(1, 1, 1, Coverage(d) * Coverage(-d - 4f));
        });
        // Yumuşak gölge.
        Save("UI_Shadow", 96, 96, 40, (x, y) =>
        {
            float d = RoundedBox(x, y, 96, 96, 24);
            return new Color(0, 0, 0, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(-d / 18f)) * 0.5f);
        });
        // Daire ve halka.
        Save("UI_Circle", 64, 64, 0, (x, y) => new Color(1, 1, 1, Coverage(Circle(x, y, 32, 32, 30))));
        Save("UI_CircleOutline", 128, 128, 0, (x, y) =>
        {
            float d = Circle(x, y, 64, 64, 60);
            // Kesik çizgili daire: açıya göre aralıklı.
            float angle = Mathf.Atan2(y - 64, x - 64) * Mathf.Rad2Deg;
            float dash = Mathf.Repeat(angle, 15f) < 9f ? 1f : 0f;
            return new Color(1, 1, 1, Coverage(d) * Coverage(-d - 3.5f) * dash);
        });
        // Buruşuk kâğıt daire (çark dilimleri ve baloncukları): kenarı hafif düzensiz, yüzeyi kırışık gölgeli.
        Save("UI_PaperCircle", 256, 256, 0, (x, y) =>
        {
            float angle = Mathf.Atan2(y - 128, x - 128);
            float wobble = (Mathf.PerlinNoise(Mathf.Cos(angle) * 2.2f + 5f, Mathf.Sin(angle) * 2.2f + 5f) - 0.5f) * 7f;
            float d = Circle(x, y, 128, 128, 120 + wobble);
            // Kırışıklar: iki ölçekte gürültünün "sırt"ları.
            float ridge = Mathf.Abs(Mathf.PerlinNoise(x * 0.035f, y * 0.035f) - 0.5f) * 2f;
            float fine = Mathf.Abs(Mathf.PerlinNoise(x * 0.09f + 30f, y * 0.09f + 11f) - 0.5f) * 2f;
            float tone = 0.90f + ridge * 0.08f + fine * 0.04f;
            float rim = Mathf.Clamp01(-d / 10f);
            tone *= Mathf.Lerp(0.86f, 1f, rim);
            return new Color(tone, tone, tone * 0.985f, Coverage(d));
        });
        // Defter halkası: metalik halka, üstte parlama.
        Save("UI_Ring", 64, 64, 0, (x, y) =>
        {
            float d = Circle(x, y, 32, 32, 26);
            float ring = Coverage(d) * Coverage(-d - 9f);
            float shade = Mathf.Lerp(0.55f, 1f, Mathf.Clamp01((y - 8f) / 48f));
            return new Color(shade, shade, shade * 1.02f, ring);
        });
        // Kâğıt: krem, hafif grenli, kenarlara doğru koyulaşan sayfa.
        Save("UI_Paper", 128, 128, 28, (x, y) =>
        {
            float d = RoundedBox(x, y, 128, 128, 10);
            float edge = Mathf.Clamp01(-d / 26f);
            float grain = (Hash(x, y) - 0.5f) * 0.035f;
            float tone = Mathf.Lerp(0.90f, 1f, edge) + grain;
            return new Color(tone, tone * 0.985f, tone * 0.94f, Coverage(d));
        });
        // Kara tahta: koyu yüzey, silgi izleri.
        Save("UI_Chalkboard", 256, 256, 24, (x, y) =>
        {
            float d = RoundedBox(x, y, 256, 256, 12);
            float smudge = Mathf.PerlinNoise(x * 0.018f, y * 0.045f) * 0.10f + Mathf.PerlinNoise(x * 0.11f + 40f, y * 0.11f) * 0.04f;
            float grain = (Hash(x, y) - 0.5f) * 0.03f;
            float tone = 0.13f + smudge + grain;
            return new Color(tone * 0.92f, tone * 1.12f, tone * 1.02f, Coverage(d));
        });
        // Ahşap çerçeve: yatay damarlı, kenarda açık pah.
        Save("UI_Wood", 128, 128, 30, (x, y) =>
        {
            float d = RoundedBox(x, y, 128, 128, 14);
            float grainLine = Mathf.PerlinNoise(x * 0.03f, y * 0.55f) * 0.16f + Mathf.PerlinNoise(x * 0.2f, y * 1.9f) * 0.05f;
            float bevel = Mathf.Clamp01((-d - 1f) / 6f);
            float tone = Mathf.Lerp(1.18f, 1f, bevel) * (0.82f + grainLine);
            return new Color(0.62f * tone, 0.40f * tone, 0.22f * tone, Coverage(d));
        });
        // Kesik çizgi (döşenir).
        Save("UI_Dash", 32, 8, 0, (x, y) => new Color(1, 1, 1, x < 18 && y >= 2 && y < 6 ? 1f : 0f), TextureWrapMode.Repeat);
        // Yapışkan bant: yarı saydam, uçları tırtıklı.
        Save("UI_Tape", 128, 40, 0, (x, y) =>
        {
            float zigzag = Mathf.Abs(Mathf.Repeat(y, 8f) - 4f);
            bool inside = x > zigzag && x < 127f - zigzag;
            float streak = 0.92f + (Hash(x / 3, y) - 0.5f) * 0.06f;
            return new Color(streak, streak * 0.95f, streak * 0.74f, inside ? 0.78f : 0f);
        });
        // Sırt gölgesi: ortada koyu, yanlara doğru şeffaf.
        Save("UI_GutterShadow", 64, 8, 0, (x, y) =>
        {
            float t = Mathf.Abs(x - 31.5f) / 31.5f;
            return new Color(0, 0, 0, Mathf.Pow(1f - t, 2.2f) * 0.42f);
        });

        AssetDatabase.Refresh();
        Debug.Log($"[UiSpriteGenerator] Arayüz sprite'ları üretildi ({Folder}).");
    }

    // ---- çizim yardımcıları (piksel merkezinden işaretli uzaklık; negatif = içeride) ----

    private static float RoundedBox(int x, int y, int width, int height, float radius)
    {
        float px = Mathf.Abs(x + 0.5f - width * 0.5f) - (width * 0.5f - radius - 1f);
        float py = Mathf.Abs(y + 0.5f - height * 0.5f) - (height * 0.5f - radius - 1f);
        float outside = new Vector2(Mathf.Max(px, 0f), Mathf.Max(py, 0f)).magnitude;
        return outside + Mathf.Min(Mathf.Max(px, py), 0f) - radius;
    }

    private static float Circle(int x, int y, float cx, float cy, float radius)
    {
        return new Vector2(x + 0.5f - cx, y + 0.5f - cy).magnitude - radius;
    }

    // İşaretli uzaklıktan kenar yumuşatmalı doluluk (1 piksel geçiş).
    private static float Coverage(float distance) => Mathf.Clamp01(0.5f - distance);

    private static float Hash(int x, int y)
    {
        unchecked
        {
            uint h = (uint)(x * 374761393 + y * 668265263);
            h = (h ^ (h >> 13)) * 1274126177u;
            return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
        }
    }

    private static void Save(string name, int width, int height, int border, Func<int, int, Color> pixel, TextureWrapMode wrap = TextureWrapMode.Clamp)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
            texture.SetPixel(x, y, pixel(x, y));
        texture.Apply();

        string path = $"{Folder}/{name}.png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = true;
        importer.wrapMode = wrap;
        importer.spriteBorder = new Vector4(border, border, border, border);
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }
}
