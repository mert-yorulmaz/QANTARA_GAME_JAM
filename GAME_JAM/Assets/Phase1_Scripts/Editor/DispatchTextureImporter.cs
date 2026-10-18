using UnityEditor;

// Dispatch UI paketindeki görselleri README'deki ayarlarla içe aktarır.
public class DispatchTextureImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Dispatch/"))
            return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
        importer.maxTextureSize = 4096;

        // 10x10 piksel simgeler: Point filtre, 10 PPU.
        if (assetPath.Contains("/05_sprites/"))
        {
            importer.filterMode = UnityEngine.FilterMode.Point;
            importer.spritePixelsPerUnit = 10;
        }
        else
        {
            importer.filterMode = UnityEngine.FilterMode.Bilinear;
        }
    }
}
