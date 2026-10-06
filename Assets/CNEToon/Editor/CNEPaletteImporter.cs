using UnityEditor;
using UnityEngine;

// Palet dokularının içe aktarma ayarlarını sabitler: palet küçük renk karelerinden oluşur; mipmap, sıkıştırma ya da
// tekrar eden sarma komşu renkleri birbirine karıştırır ve renk doğruluğu bozulur.
// Kapsam: yolu "/Palettes/" içeren dokular. Adı "_Prop" ile biten dosya özellik maskesidir (linear), diğerleri
// renk paletidir (sRGB).
public class CNEPaletteImporter : AssetPostprocessor
{
    private const string PaletteFolderMarker = "/Palettes/";
    private const string PropMapSuffix = "_Prop";

    private void OnPreprocessTexture()
    {
        if (!assetPath.Replace('\\', '/').Contains(PaletteFolderMarker))
            return;

        var importer = (TextureImporter)assetImporter;
        string fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath);

        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = !fileName.EndsWith(PropMapSuffix);
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.npotScale = TextureImporterNPOTScale.None;
    }
}
