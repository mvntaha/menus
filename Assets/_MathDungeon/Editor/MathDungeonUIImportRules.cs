using UnityEditor;
using UnityEngine;

namespace MathDungeon.EditorTools
{
    /// <summary>
    /// Applies UI-appropriate import settings to every texture under
    /// Assets/_MathDungeon/Art/UI. Unity's default for a URP 3D template is
    /// Texture Type "Default" with mipmaps on, which is wrong for canvas art:
    /// sprites come out blurry and cost memory they do not need.
    ///
    /// This runs on import, so it also covers anything dropped in later — and it
    /// travels with the folder when _MathDungeon is copied into another project.
    /// </summary>
    public class MathDungeonUIImportRules : AssetPostprocessor
    {
        const string UIRoot = "Assets/_MathDungeon/Art/UI/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(UIRoot, System.StringComparison.Ordinal))
                return;

            var importer = (TextureImporter)assetImporter;

            // Only stamp defaults on a first import. Once the asset exists, a
            // deliberate change in the Inspector (9-slice borders, a custom
            // pivot) must survive reimports.
            if (!importer.importSettingsMissing)
                return;

            // Everything goes through TextureImporterSettings in one
            // read-mutate-write, so the settings-only fields (spriteMeshType,
            // the physics-shape flag) cannot be clobbered by a later write.
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);

            settings.textureType         = TextureImporterType.Sprite;
            settings.textureShape        = TextureImporterShape.Texture2D;
            settings.spriteMode          = (int)SpriteImportMode.Single;
            settings.alphaIsTransparency = true;
            settings.mipmapEnabled       = false;   // canvas art is never minified
            settings.wrapMode            = TextureWrapMode.Clamp;
            settings.filterMode          = FilterMode.Bilinear;
            settings.readable            = false;

            // FullRect is required for Image types Sliced / Tiled / Filled;
            // Tight would break every 9-sliced button and panel. Set it
            // explicitly rather than leaning on it being the enum's zero value.
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;

            importer.SetTextureSettings(settings);

            importer.maxTextureSize = 2048;

            // Uncompressed: these are small 2x UI pieces, and DXT blocking is
            // clearly visible on the soft borders and gradients Kenney uses.
            importer.textureCompression  = TextureImporterCompression.Uncompressed;
            importer.crunchedCompression = false;
        }
    }
}
