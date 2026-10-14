using UnityEditor;
using UnityEngine;

// Preserve atlas coordinates and alpha; Unity's default NPOT resize would
// otherwise move every region boundary in these generated sprite sheets.
public sealed class PaintedArtImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Resources/Painted/")) return;
        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Default;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 4096;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        if(assetPath.EndsWith("/CityAsphalt.png"))
        {
            importer.mipmapEnabled=true;
            importer.wrapMode=TextureWrapMode.Repeat;
            importer.filterMode=FilterMode.Trilinear;
            importer.anisoLevel=8;
        }
    }
}

