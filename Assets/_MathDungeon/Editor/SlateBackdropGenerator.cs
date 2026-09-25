using System.IO;
using UnityEditor;
using UnityEngine;

namespace MathDungeon.EditorTools
{
    /// <summary>
    /// Generates the "Etched Slate" backdrop as a PNG asset.
    ///
    /// Procedural rather than hand-painted for two reasons: the Kenney packs
    /// ship UI widgets, not wall textures, and a generator can be re-run at a
    /// different resolution or palette without redoing art. The seed is fixed,
    /// so regenerating produces a byte-identical image and does not churn git.
    /// </summary>
    public static class SlateBackdropGenerator
    {
        public const string OutputPath = "Assets/_MathDungeon/Art/UI/Generated/slate_backdrop.png";

        const int Width  = 1920;
        const int Height = 1080;
        const int Seed   = 20260925;

        // Course = one horizontal row of stone blocks.
        const int CourseHeight = 150;
        const int MinBlockWidth = 210;
        const int MaxBlockWidth = 330;
        const int SeamWidth = 4;

        static readonly Color BaseStone = new Color32(0x1E, 0x21, 0x26, 0xFF);
        static readonly Color SeamDark  = new Color32(0x0A, 0x0B, 0x0D, 0xFF);

        [MenuItem("Math Dungeon/Regenerate Slate Backdrop")]
        public static void Generate()
        {
            var rng = new System.Random(Seed);
            var px = new Color[Width * Height];

            // --- Per-block bookkeeping ------------------------------------
            // blockId lets the chisel pass know where a block's edges are
            // without re-deriving the layout per pixel.
            var blockTint = new float[Width * Height];
            var edgeTop   = new bool[Width * Height];
            var edgeBottom= new bool[Width * Height];
            var isSeam    = new bool[Width * Height];

            int courses = Mathf.CeilToInt((float)Height / CourseHeight);
            for (int c = 0; c < courses; c++)
            {
                int y0 = c * CourseHeight;
                int y1 = Mathf.Min(y0 + CourseHeight, Height);

                // Stagger every other course so seams never line up vertically.
                int x = -rng.Next(0, MaxBlockWidth);
                while (x < Width)
                {
                    int w = rng.Next(MinBlockWidth, MaxBlockWidth);
                    float tint = 1f + (float)(rng.NextDouble() - 0.5) * 0.22f;

                    int bx0 = x, bx1 = Mathf.Min(x + w, Width);
                    for (int yy = y0; yy < y1; yy++)
                    {
                        for (int xx = Mathf.Max(bx0, 0); xx < bx1; xx++)
                        {
                            int i = yy * Width + xx;
                            blockTint[i] = tint;

                            bool seam = xx < bx0 + SeamWidth || xx >= bx1 - SeamWidth
                                     || yy < y0 + SeamWidth || yy >= y1 - SeamWidth;
                            isSeam[i] = seam;

                            // A chiselled block catches light on its top lip and
                            // falls into shadow at its base.
                            edgeTop[i]    = yy >= y0 + SeamWidth && yy < y0 + SeamWidth + 5;
                            edgeBottom[i] = yy < y1 - SeamWidth && yy >= y1 - SeamWidth - 7;
                        }
                    }
                    x += w;
                }
            }

            // --- Shade ----------------------------------------------------
            float cx = Width * 0.5f, cy = Height * 0.5f;
            float maxDist = Mathf.Sqrt(cx * cx + cy * cy);

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int i = y * Width + x;
                    Color col = BaseStone * blockTint[i];
                    col.a = 1f;

                    // Two octaves of grain keep the stone from reading as flat
                    // vinyl without adding visible tiling.
                    float grain =
                        (Mathf.PerlinNoise(x * 0.015f + 11.3f, y * 0.015f + 7.7f) - 0.5f) * 0.16f +
                        (Mathf.PerlinNoise(x * 0.11f  + 3.1f,  y * 0.11f  + 5.9f) - 0.5f) * 0.07f;
                    col.r += grain; col.g += grain; col.b += grain;

                    if (edgeTop[i])    { col.r += 0.035f; col.g += 0.038f; col.b += 0.042f; }
                    if (edgeBottom[i]) { col.r -= 0.030f; col.g -= 0.030f; col.b -= 0.032f; }
                    if (isSeam[i])     col = Color.Lerp(col, SeamDark, 0.82f);

                    // Baked vignette. Cheaper than a runtime overlay and it means
                    // the backdrop is a single draw call.
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / maxDist;
                    // Floor raised from the first pass: at 0.34 the corners went
                    // to near-black and the corner runes disappeared into them.
                    float vig = Mathf.Lerp(1f, 0.46f, Mathf.SmoothStep(0.34f, 1f, d));
                    col.r *= vig; col.g *= vig; col.b *= vig;

                    col.a = 1f;
                    px[i] = col;
                }
            }

            var tex = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            tex.SetPixels(px);
            tex.Apply();

            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
            File.WriteAllBytes(OutputPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(OutputPath, ImportAssetOptions.ForceUpdate);

            // The shared UI import rules cap textures at 2048 and turn off
            // mipmaps, which is right for widgets. A full-screen backdrop wants
            // the same treatment but must not be downscaled below 1920.
            var importer = (TextureImporter)AssetImporter.GetAtPath(OutputPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048;
            // CompressedHQ (BC7) rather than the default: a full-screen baked
            // vignette is a long smooth gradient, and standard DXT bands on it
            // visibly. Uncompressed would be 8 MB of VRAM for one backdrop.
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();

            Debug.Log($"[MathDungeon] Slate backdrop written to {OutputPath} ({Width}x{Height}).");
        }
    }
}
