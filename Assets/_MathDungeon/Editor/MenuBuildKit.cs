using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace MathDungeon.EditorTools
{
    /// <summary>
    /// Shared construction helpers for the menu builders. Kept separate from the
    /// builders themselves so the main menu and the pause menu are guaranteed to
    /// use the same palette, the same carved-text treatment and the same button
    /// anatomy — the two screens have to look like one game.
    /// </summary>
    public static class MenuBuildKit
    {
        // ---- Etched Slate palette -------------------------------------
        public static readonly Color StoneDeep  = Hex("16181C");
        public static readonly Color Stone      = Hex("1E2126");
        public static readonly Color StoneLip   = Hex("343A42");  // lit top edge of a cut
        public static readonly Color Cut        = Hex("0B0D10");  // the groove itself
        public static readonly Color AccentDim  = Hex("3A6F63");
        public static readonly Color Accent     = Hex("6EE0C6");
        public static readonly Color ButtonFace = Hex("2A2F36");

        // Panels are tinted lighter than the wall so the ornate Kenney border
        // actually reads. At Stone the carving in the frame was invisible and
        // the panel looked like a plain grey rectangle.
        public static readonly Color FrameTint  = Hex("353C45");

        // Text tones. The wall is very dark, so anything meant to be READ needs
        // to sit well above the stone in value, not blend into it.
        public static readonly Color TextBright = Hex("C2CDD6");  // titles, button labels
        public static readonly Color TextMuted  = Hex("8593A0");  // taglines, slider captions
        // Runes sat at 454E58 in the first pass and read as foreground, pulling
        // the eye away from the button column. Dropped until they belong to the
        // wall rather than competing with it.
        public static readonly Color RuneFace   = Hex("2C333B");

        public const string ArtRoot  = "Assets/_MathDungeon/Art/UI/";
        public const string FontRoot = "Assets/_MathDungeon/Fonts/Kenney/";
        public const string GenRoot  = "Assets/_MathDungeon/Fonts/Generated/";

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }

        // ---- Sprites ---------------------------------------------------

        public static Sprite LoadSprite(string relativePath)
        {
            var path = ArtRoot + relativePath;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) Debug.LogError("[MathDungeon] Missing sprite: " + path);
            return sprite;
        }

        /// <summary>
        /// Sets 9-slice borders on a sprite. The shared import rules set
        /// FullRect and Sprite type but cannot guess border widths — those are
        /// per-artwork, so they are set here, once, for the handful of sprites
        /// the menus actually use.
        /// </summary>
        public static void SetBorder(string relativePath, Vector4 border)
        {
            var path = ArtRoot + relativePath;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) { Debug.LogError("[MathDungeon] No importer: " + path); return; }

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            if (settings.spriteBorder == border) return;   // don't churn reimports

            settings.spriteBorder = border;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        // ---- Fonts -----------------------------------------------------

        /// <summary>
        /// Builds a TMP font asset from a Kenney TTF. Dynamic atlas population
        /// means glyphs are rasterised on demand, so changing menu copy later
        /// does not require regenerating the atlas by hand.
        /// </summary>
        public static TMP_FontAsset EnsureFont(string ttfName, int samplingSize = 90)
        {
            var outPath = GenRoot + ttfName + " SDF.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outPath);
            if (existing != null) return existing;

            var ttf = AssetDatabase.LoadAssetAtPath<Font>(FontRoot + ttfName + ".ttf");
            if (ttf == null) { Debug.LogError("[MathDungeon] Missing font: " + ttfName + ".ttf"); return null; }

            System.IO.Directory.CreateDirectory(GenRoot);

            var fontAsset = TMP_FontAsset.CreateFontAsset(
                ttf, samplingSize, 9, GlyphRenderMode.SDFAA,
                1024, 1024, AtlasPopulationMode.Dynamic);

            fontAsset.name = ttfName + " SDF";
            AssetDatabase.CreateAsset(fontAsset, outPath);

            if (fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0)
            {
                fontAsset.atlasTextures[0].name = fontAsset.name + " Atlas";
                AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);
            }
            if (fontAsset.material != null)
            {
                fontAsset.material.name = fontAsset.name + " Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            return fontAsset;
        }

        // ---- Layout primitives -----------------------------------------

        public static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Image NewImage(string name, Transform parent, Sprite sprite, Color color,
                                     Image.Type type = Image.Type.Simple)
        {
            var rt = NewRect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.type = type;
            if (type == Image.Type.Sliced) img.pixelsPerUnitMultiplier = 1f;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>
        /// Carved text: three stacked layers reading as a chisel cut.
        ///
        /// The first attempt put a dark face on top of a light lip, which is
        /// physically what a groove looks like — and was unreadable, because
        /// dark-on-dark-stone has almost no contrast. So the face carries the
        /// legible tone and the shadow sits below-right as the depth of the cut,
        /// which is how engraved lettering actually reads at a glance.
        ///
        /// Legibility wins over literalism: a title nobody can read is not a
        /// style choice.
        /// </summary>
        /// <param name="faceColor">
        /// The readable tone. Bright for titles and button labels, dim for
        /// background runes, which are ambience rather than information.
        /// </param>
        public static RectTransform CarvedText(
            string name, Transform parent, string text, TMP_FontAsset font, float size,
            TextAlignmentOptions align = TextAlignmentOptions.Center,
            bool glow = false, float charSpacing = 6f,
            Color? faceColor = null, float glowAlpha = 0.5f)
        {
            var root = NewRect(name, parent);

            // Shadow first (behind), offset down-right: the cut's dark side.
            MakeLayer(root, "Shadow", text, font, size, align, Cut, new Vector2(3f, -3f), charSpacing);
            var face = MakeLayer(root, "Face", text, font, size, align, faceColor ?? TextBright,
                                 Vector2.zero, charSpacing);

            if (glow)
            {
                // The glow is TMP's underlay on the face itself, not a separate
                // text layer behind it. A duplicate layer is either exactly
                // covered (invisible) or scaled up to peek out — and scaling
                // pushes every glyph outward from the block's centre, so the
                // two copies stop lining up and the word reads as a
                // double-exposure. The underlay is per-glyph, so it always
                // registers.
                ApplyUnderlayGlow(face, glowAlpha);

                var pulse = face.gameObject.AddComponent<Menus.RuneGlow>();
                var so = new SerializedObject(pulse);
                so.FindProperty("maxAlpha").floatValue = glowAlpha;
                so.FindProperty("minAlpha").floatValue = glowAlpha * 0.30f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            return root;
        }

        /// <summary>
        /// Turns on TMP's underlay with zero offset, which is a glow rather than
        /// a drop shadow. Touching fontMaterial instantiates a material for this
        /// text, which is why only the handful of glowing elements get it.
        /// </summary>
        static void ApplyUnderlayGlow(TextMeshProUGUI tmp, float alpha)
        {
            var mat = tmp.fontMaterial;
            mat.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            mat.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(Accent.r, Accent.g, Accent.b, alpha));
            mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0f);
            mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, 0f);
            mat.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.40f);
            mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.55f);
        }

        static TextMeshProUGUI MakeLayer(Transform parent, string name, string text, TMP_FontAsset font,
                                         float size, TextAlignmentOptions align, Color color,
                                         Vector2 offset, float charSpacing)
        {
            var rt = NewRect(name, parent);
            Stretch(rt);
            rt.anchoredPosition = offset;

            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            if (font != null) tmp.font = font;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.characterSpacing = charSpacing;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.raycastTarget = false;
            return tmp;
        }

        /// <summary>
        /// A menu button: 9-sliced Kenney face, carved label, and colour
        /// transitions tuned so hover reads as the stone catching torchlight
        /// rather than the default washed-out tint.
        /// </summary>
        public static Button MenuButton(string name, Transform parent, string label,
                                        TMP_FontAsset font, Sprite face, float width = 460f,
                                        float height = 96f, float fontSize = 34f)
        {
            var rt = NewRect(name, parent);
            rt.sizeDelta = new Vector2(width, height);

            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = face;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1f;
            img.color = ButtonFace;
            img.raycastTarget = true;

            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor      = Color.white;
            colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
            colors.pressedColor     = new Color(0.78f, 0.78f, 0.78f, 1f);
            colors.selectedColor    = new Color(1.12f, 1.12f, 1.12f, 1f);
            colors.disabledColor    = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            colors.fadeDuration     = 0.08f;
            button.colors = colors;

            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = height;

            // Label sits slightly high: the Kenney "depth" button has a shadow
            // lip along its bottom edge, so true centring reads as too low.
            var labelRt = CarvedText("Label", rt, label, font, fontSize, TextAlignmentOptions.Center,
                                     glow: false, charSpacing: 8f);
            Stretch(labelRt);
            labelRt.offsetMin = new Vector2(0f, 8f);

            return button;
        }

        public static void AddSfx(Button button, AudioSource source, AudioClip hover, AudioClip click)
        {
            var sfx = button.gameObject.AddComponent<Menus.UIButtonSfx>();
            var so = new SerializedObject(sfx);
            so.FindProperty("source").objectReferenceValue = source;
            so.FindProperty("hoverClip").objectReferenceValue = hover;
            so.FindProperty("clickClip").objectReferenceValue = click;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static Canvas NewCanvas(string name, int sortingOrder)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            // 0.5 so the layout degrades sanely on both wider and taller screens
            // instead of only tracking width.
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;

            return canvas;
        }
    }
}
