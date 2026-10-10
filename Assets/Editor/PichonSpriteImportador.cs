using UnityEditor;
using UnityEngine;

/// <summary>
/// Configura automaticamente los sprites del Pichoncito heroe al importarlos
/// desde Assets/Sprites/PichonHeroe/ (Sprite 2D, 100 PPU, pivote abajo-centro,
/// sin compresion y con transparencia).
/// </summary>
public class PichonSpriteImportador : AssetPostprocessor
{
    private const string CarpetaObjetivo = "Assets/Sprites/PichonHeroe/";

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(CarpetaObjetivo))
        {
            return;
        }

        TextureImporter importador = assetImporter as TextureImporter;
        if (importador == null)
        {
            return;
        }

        importador.textureType = TextureImporterType.Sprite;
        importador.spriteImportMode = SpriteImportMode.Single;
        importador.spritePixelsPerUnit = 100f;
        importador.filterMode = FilterMode.Bilinear;
        importador.textureCompression = TextureImporterCompression.Uncompressed;
        importador.alphaIsTransparency = true;
        importador.mipmapEnabled = false;

        TextureImporterSettings ajustes = new TextureImporterSettings();
        importador.ReadTextureSettings(ajustes);
        ajustes.spriteAlignment = (int)SpriteAlignment.Custom;
        ajustes.spritePivot = new Vector2(0.5f, 0f);
        importador.SetTextureSettings(ajustes);
    }
}
