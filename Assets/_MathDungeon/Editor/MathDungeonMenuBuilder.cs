using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using MathDungeon.Menus;

namespace MathDungeon.EditorTools
{
    /// <summary>
    /// Builds the Main Menu scene and the Pause Menu prefab from scratch.
    ///
    /// These are built by script rather than assembled by hand so they can be
    /// regenerated after a palette or copy change without anyone re-dragging
    /// forty references — and so the two screens cannot drift apart. The output
    /// is ordinary GameObjects: once generated, edit them in the Inspector like
    /// anything else. Re-running overwrites, so commit before regenerating if
    /// you have made manual changes worth keeping.
    /// </summary>
    public static class MathDungeonMenuBuilder
    {
        const string SceneDir   = "Assets/_MathDungeon/Scenes";
        const string PrefabDir  = "Assets/_MathDungeon/Prefabs";
        const string MainMenuScenePath = SceneDir + "/MainMenu.unity";
        const string PausePrefabPath   = PrefabDir + "/PauseMenu.prefab";

        // Kenney sprites the menus use, with the 9-slice borders they need.
        const string ButtonSprite = "Kenney_UIPack/Grey/button_rectangle_depth_flat.png";
        const string PanelSprite  = "Kenney_FantasyBorders/Panel/panel-000.png";
        const string SliderBg     = "Kenney_UIPack/Grey/slide_horizontal_grey.png";
        const string SliderFill   = "Kenney_UIPack/Blue/slide_horizontal_color.png";
        // slide_hangle is an arrow, not a knob — it read as a teal chevron sitting
        // on the bar. A round button face is the shape players expect to drag.
        const string HandleSprite = "Kenney_UIPack/Grey/button_round_depth_flat.png";

        // Arithmetic carved into the wall. Deliberately ASCII: the Kenney fonts
        // do not carry the Unicode maths block, and a missing glyph renders as a
        // blank box rather than falling back to something sensible.
        static readonly string[] Runes =
        {
            "7+3", "=", "12", "x-5", "9", "4/2", "+", "8=8", "6x7", "-", "3", "15"
        };

        [MenuItem("Math Dungeon/Build All Menus", priority = 0)]
        public static void BuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Directory.CreateDirectory(SceneDir);
            Directory.CreateDirectory(PrefabDir);

            PrepareSprites();
            SlateBackdropGenerator.Generate();

            BuildPauseMenuPrefab();
            BuildMainMenuScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[MathDungeon] Menus built: " + MainMenuScenePath + " and " + PausePrefabPath);
        }

        /// <summary>9-slice borders, in pixels, for the sprites the menus stretch.</summary>
        static void PrepareSprites()
        {
            // 384x128 button with a depth lip along the bottom — the bottom
            // border is larger so the lip is never stretched.
            MenuBuildKit.SetBorder(ButtonSprite, new Vector4(36f, 44f, 36f, 32f));
            // 96x96 ornate panel: a third in from each edge keeps the corners intact.
            MenuBuildKit.SetBorder(PanelSprite,  new Vector4(32f, 32f, 32f, 32f));
            MenuBuildKit.SetBorder(SliderBg,     new Vector4(16f, 12f, 16f, 12f));
            MenuBuildKit.SetBorder(SliderFill,   new Vector4(16f, 12f, 16f, 12f));
        }

        // ================================================================
        //  MAIN MENU
        // ================================================================

        static void BuildMainMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var titleFont = MenuBuildKit.EnsureFont("Kenney Future");
            var bodyFont  = MenuBuildKit.EnsureFont("Kenney Mini Square");

            // A Screen Space Overlay canvas needs no camera to render, but a
            // scene without one logs "No cameras rendering" and shows an
            // undefined background behind any non-overlay content added later.
            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = MenuBuildKit.StoneDeep;
            cam.orthographic = true;
            camGo.tag = "MainCamera";

            var eventSystem = CreateEventSystem();

            var audioGo = new GameObject("UI Audio", typeof(AudioSource));
            var audioSource = audioGo.GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.ignoreListenerPause = true;   // menu clicks work while paused

            var hover = LoadClip("tap-a");
            var click = LoadClip("click-a");

            var canvas = MenuBuildKit.NewCanvas("MenuCanvas", 0);
            var canvasRt = (RectTransform)canvas.transform;
            var controller = canvas.gameObject.AddComponent<MainMenuController>();

            // ---- Backdrop -------------------------------------------------
            var slate = AssetDatabase.LoadAssetAtPath<Sprite>(SlateBackdropGenerator.OutputPath);
            var backdrop = MenuBuildKit.NewImage("Backdrop", canvasRt, slate, Color.white);
            MenuBuildKit.Stretch((RectTransform)backdrop.transform);
            // Envelope, don't letterbox: the backdrop is 16:9 and the vignette
            // is baked, so cropping the edges on other aspects is harmless.
            backdrop.preserveAspect = false;

            BuildRuneField(canvasRt, bodyFont);

            // ---- Root panel ----------------------------------------------
            var root = MenuBuildKit.NewRect("Root", canvasRt);
            MenuBuildKit.Stretch(root);
            var rootGroup = root.gameObject.AddComponent<CanvasGroup>();

            var title = MenuBuildKit.CarvedText("Title", root, "MATH DUNGEON", titleFont, 96f,
                                                TextAlignmentOptions.Center, glow: true, charSpacing: 10f,
                                                faceColor: MenuBuildKit.TextBright, glowAlpha: 0.55f);
            title.anchorMin = new Vector2(0.5f, 1f);
            title.anchorMax = new Vector2(0.5f, 1f);
            title.pivot     = new Vector2(0.5f, 1f);
            title.sizeDelta = new Vector2(1560f, 150f);
            title.anchoredPosition = new Vector2(0f, -110f);

            var tagline = MenuBuildKit.CarvedText("Tagline", root, "DESCEND   .   SOLVE   .   SURVIVE",
                                                  bodyFont, 30f, TextAlignmentOptions.Center,
                                                  glow: false, charSpacing: 16f, faceColor: MenuBuildKit.TextMuted);
            tagline.anchorMin = new Vector2(0.5f, 1f);
            tagline.anchorMax = new Vector2(0.5f, 1f);
            tagline.pivot     = new Vector2(0.5f, 1f);
            tagline.sizeDelta = new Vector2(1200f, 50f);
            tagline.anchoredPosition = new Vector2(0f, -280f);

            // ---- Button column -------------------------------------------
            var column = MenuBuildKit.NewRect("Buttons", root);
            column.anchorMin = new Vector2(0.5f, 0.5f);
            column.anchorMax = new Vector2(0.5f, 0.5f);
            column.pivot     = new Vector2(0.5f, 0.5f);
            column.sizeDelta = new Vector2(480f, 480f);
            column.anchoredPosition = new Vector2(0f, -110f);

            var layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 22f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var buttonSprite = MenuBuildKit.LoadSprite(ButtonSprite);

            var play        = MenuBuildKit.MenuButton("PlayButton", column, "PLAY", titleFont, buttonSprite, 460f, 104f, 40f);
            var levelSelect = MenuBuildKit.MenuButton("LevelSelectButton", column, "LEVELS", titleFont, buttonSprite);
            var settings    = MenuBuildKit.MenuButton("SettingsButton", column, "SETTINGS", titleFont, buttonSprite);
            var quit        = MenuBuildKit.MenuButton("QuitButton", column, "QUIT", titleFont, buttonSprite);

            foreach (var b in new[] { play, levelSelect, settings, quit })
                MenuBuildKit.AddSfx(b, audioSource, hover, click);

            UnityEventTools.AddPersistentListener(play.onClick, controller.OnPlay);
            UnityEventTools.AddPersistentListener(levelSelect.onClick, controller.OnLevelSelect);
            UnityEventTools.AddPersistentListener(settings.onClick, controller.OnOpenSettings);
            UnityEventTools.AddPersistentListener(quit.onClick, controller.OnQuit);

            // ---- Settings panel ------------------------------------------
            var settingsPanel = BuildSettingsPanel(canvasRt, titleFont, bodyFont, audioSource, hover, click,
                                                   out var backButton);
            UnityEventTools.AddPersistentListener(backButton.onClick, controller.OnCloseSettings);

            var settingsGroup = settingsPanel.GetComponent<CanvasGroup>();

            // ---- Version stamp -------------------------------------------
            var version = MenuBuildKit.CarvedText("Version", canvasRt, Application.version, bodyFont, 20f,
                                                  TextAlignmentOptions.BottomRight, glow: false, charSpacing: 4f, faceColor: MenuBuildKit.TextMuted);
            version.anchorMin = new Vector2(1f, 0f);
            version.anchorMax = new Vector2(1f, 0f);
            version.pivot     = new Vector2(1f, 0f);
            version.sizeDelta = new Vector2(320f, 40f);
            version.anchoredPosition = new Vector2(-28f, 20f);

            // ---- Wire the controller -------------------------------------
            var so = new SerializedObject(controller);
            so.FindProperty("rootPanel").objectReferenceValue = rootGroup;
            so.FindProperty("settingsPanel").objectReferenceValue = settingsGroup;
            so.FindProperty("quitButton").objectReferenceValue = quit.gameObject;
            so.ApplyModifiedPropertiesWithoutUndo();

            // First selected control, so keyboard and gamepad navigation has a
            // starting point instead of nothing being focused.
            if (eventSystem != null) eventSystem.firstSelectedGameObject = play.gameObject;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, MainMenuScenePath);
            AddSceneToBuildSettings(MainMenuScenePath);
        }

        static void BuildRuneField(RectTransform parent, TMP_FontAsset font)
        {
            var field = MenuBuildKit.NewRect("RuneField", parent);
            MenuBuildKit.Stretch(field);

            // Fixed seed: the rune scatter is part of the art direction, so it
            // must not reshuffle every time the menus are rebuilt.
            var rng = new System.Random(4242);

            // Kept clear of the centre column where the title and buttons sit,
            // so nothing competes with the things players need to read.
            var slots = new List<Vector2>
            {
                new Vector2(-780f,  330f), new Vector2(-640f,  -60f), new Vector2(-820f, -350f),
                new Vector2(-560f,  180f), new Vector2(-700f,  -230f), new Vector2(-520f, -420f),
                new Vector2( 780f,  300f), new Vector2( 640f,  -40f), new Vector2( 820f, -330f),
                new Vector2( 560f,  200f), new Vector2( 700f,  -250f), new Vector2( 520f, -430f),
            };

            for (int i = 0; i < Runes.Length && i < slots.Count; i++)
            {
                float size = 44f + (float)rng.NextDouble() * 38f;
                var rune = MenuBuildKit.CarvedText("Rune_" + i, field, Runes[i], font, size,
                                                   TextAlignmentOptions.Center, glow: true, charSpacing: 4f,
                                                   faceColor: MenuBuildKit.RuneFace, glowAlpha: 0.22f);
                rune.anchorMin = new Vector2(0.5f, 0.5f);
                rune.anchorMax = new Vector2(0.5f, 0.5f);
                rune.pivot     = new Vector2(0.5f, 0.5f);
                rune.sizeDelta = new Vector2(280f, 120f);
                rune.anchoredPosition = slots[i];
                rune.localRotation = Quaternion.Euler(0f, 0f, (float)(rng.NextDouble() - 0.5) * 14f);
            }
        }

        static RectTransform BuildSettingsPanel(RectTransform parent, TMP_FontAsset titleFont,
                                                TMP_FontAsset bodyFont, AudioSource audioSource,
                                                AudioClip hover, AudioClip click, out Button backButton)
        {
            var panel = MenuBuildKit.NewRect("SettingsPanel", parent);
            MenuBuildKit.Stretch(panel);
            var group = panel.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            // Dim behind the panel. This one DOES take raycasts, so clicks can't
            // reach the buttons underneath while settings are open.
            var dim = MenuBuildKit.NewImage("Dim", panel, null, new Color(0f, 0f, 0f, 0.55f));
            MenuBuildKit.Stretch((RectTransform)dim.transform);
            dim.raycastTarget = true;

            var frame = MenuBuildKit.NewImage("Frame", panel, MenuBuildKit.LoadSprite(PanelSprite),
                                              MenuBuildKit.FrameTint, Image.Type.Sliced);
            var frameRt = (RectTransform)frame.transform;
            frameRt.anchorMin = new Vector2(0.5f, 0.5f);
            frameRt.anchorMax = new Vector2(0.5f, 0.5f);
            frameRt.sizeDelta = new Vector2(760f, 560f);
            frame.raycastTarget = true;

            var heading = MenuBuildKit.CarvedText("Heading", frameRt, "SETTINGS", titleFont, 56f,
                                                   TextAlignmentOptions.Center, glow: true, charSpacing: 12f);
            heading.anchorMin = new Vector2(0.5f, 1f);
            heading.anchorMax = new Vector2(0.5f, 1f);
            heading.pivot     = new Vector2(0.5f, 1f);
            heading.sizeDelta = new Vector2(600f, 80f);
            heading.anchoredPosition = new Vector2(0f, -56f);

            BuildSlider("MusicVolume", frameRt, "MUSIC", bodyFont, new Vector2(0f, 60f));
            BuildSlider("SfxVolume",   frameRt, "SOUND", bodyFont, new Vector2(0f, -40f));

            backButton = MenuBuildKit.MenuButton("BackButton", frameRt, "BACK", titleFont,
                                                 MenuBuildKit.LoadSprite(ButtonSprite), 300f, 88f, 32f);
            var backRt = (RectTransform)backButton.transform;
            backRt.anchorMin = new Vector2(0.5f, 0f);
            backRt.anchorMax = new Vector2(0.5f, 0f);
            backRt.pivot     = new Vector2(0.5f, 0f);
            backRt.anchoredPosition = new Vector2(0f, 48f);
            MenuBuildKit.AddSfx(backButton, audioSource, hover, click);

            return panel;
        }

        static Slider BuildSlider(string name, Transform parent, string label, TMP_FontAsset font, Vector2 position)
        {
            var row = MenuBuildKit.NewRect(name, parent);
            row.anchorMin = new Vector2(0.5f, 0.5f);
            row.anchorMax = new Vector2(0.5f, 0.5f);
            row.sizeDelta = new Vector2(600f, 80f);
            row.anchoredPosition = position;

            var caption = MenuBuildKit.CarvedText("Label", row, label, font, 26f,
                                                   TextAlignmentOptions.Left, glow: false, charSpacing: 8f, faceColor: MenuBuildKit.TextMuted);
            caption.anchorMin = new Vector2(0f, 0.5f);
            caption.anchorMax = new Vector2(0f, 0.5f);
            caption.pivot     = new Vector2(0f, 0.5f);
            caption.sizeDelta = new Vector2(200f, 40f);
            caption.anchoredPosition = new Vector2(10f, 0f);

            var sliderRt = MenuBuildKit.NewRect("Slider", row);
            sliderRt.anchorMin = new Vector2(1f, 0.5f);
            sliderRt.anchorMax = new Vector2(1f, 0.5f);
            sliderRt.pivot     = new Vector2(1f, 0.5f);
            sliderRt.sizeDelta = new Vector2(340f, 40f);
            sliderRt.anchoredPosition = new Vector2(-10f, 0f);

            var slider = sliderRt.gameObject.AddComponent<Slider>();

            var bg = MenuBuildKit.NewImage("Background", sliderRt, MenuBuildKit.LoadSprite(SliderBg),
                                           MenuBuildKit.Hex("15181C"), Image.Type.Sliced);
            MenuBuildKit.Stretch((RectTransform)bg.transform);

            var fillArea = MenuBuildKit.NewRect("Fill Area", sliderRt);
            MenuBuildKit.Stretch(fillArea);
            fillArea.offsetMin = new Vector2(6f, 0f);
            fillArea.offsetMax = new Vector2(-6f, 0f);

            var fill = MenuBuildKit.NewImage("Fill", fillArea, MenuBuildKit.LoadSprite(SliderFill),
                                             MenuBuildKit.AccentDim, Image.Type.Sliced);
            var fillRt = (RectTransform)fill.transform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(0f, 1f);
            fillRt.sizeDelta = new Vector2(20f, 0f);

            var handleArea = MenuBuildKit.NewRect("Handle Slide Area", sliderRt);
            MenuBuildKit.Stretch(handleArea);
            handleArea.offsetMin = new Vector2(10f, 0f);
            handleArea.offsetMax = new Vector2(-10f, 0f);

            var handle = MenuBuildKit.NewImage("Handle", handleArea, MenuBuildKit.LoadSprite(HandleSprite),
                                               MenuBuildKit.Accent);
            var handleRt = (RectTransform)handle.transform;
            handleRt.sizeDelta = new Vector2(44f, 44f);
            handle.raycastTarget = true;

            slider.fillRect = fillRt;
            slider.handleRect = handleRt;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.8f;

            return slider;
        }

        // ================================================================
        //  PAUSE MENU
        // ================================================================

        static void BuildPauseMenuPrefab()
        {
            var titleFont = MenuBuildKit.EnsureFont("Kenney Future");
            var bodyFont  = MenuBuildKit.EnsureFont("Kenney Mini Square");

            // sortingOrder 100: the pause menu must sit above whatever HUD the
            // gameplay scene already has.
            var canvas = MenuBuildKit.NewCanvas("PauseMenu", 100);
            var canvasRt = (RectTransform)canvas.transform;
            var controller = canvas.gameObject.AddComponent<PauseMenuController>();

            var audioSource = canvas.gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.ignoreListenerPause = true;   // audible while the game is paused

            var hover = LoadClip("tap-a");
            var click = LoadClip("click-a");

            var panel = MenuBuildKit.NewRect("Panel", canvasRt);
            MenuBuildKit.Stretch(panel);

            var dim = MenuBuildKit.NewImage("Dim", panel, null, new Color(0.02f, 0.03f, 0.04f, 0.78f));
            MenuBuildKit.Stretch((RectTransform)dim.transform);
            dim.raycastTarget = true;   // swallow clicks meant for the game behind

            // ---- Pause root ----------------------------------------------
            var root = MenuBuildKit.NewRect("Root", panel);
            MenuBuildKit.Stretch(root);
            var rootGroup = root.gameObject.AddComponent<CanvasGroup>();

            var frame = MenuBuildKit.NewImage("Frame", root, MenuBuildKit.LoadSprite(PanelSprite),
                                              MenuBuildKit.FrameTint, Image.Type.Sliced);
            var frameRt = (RectTransform)frame.transform;
            frameRt.anchorMin = new Vector2(0.5f, 0.5f);
            frameRt.anchorMax = new Vector2(0.5f, 0.5f);
            frameRt.sizeDelta = new Vector2(620f, 680f);

            var heading = MenuBuildKit.CarvedText("Heading", frameRt, "PAUSED", titleFont, 72f,
                                                   TextAlignmentOptions.Center, glow: true, charSpacing: 14f);
            heading.anchorMin = new Vector2(0.5f, 1f);
            heading.anchorMax = new Vector2(0.5f, 1f);
            heading.pivot     = new Vector2(0.5f, 1f);
            heading.sizeDelta = new Vector2(520f, 100f);
            heading.anchoredPosition = new Vector2(0f, -60f);

            var column = MenuBuildKit.NewRect("Buttons", frameRt);
            column.anchorMin = new Vector2(0.5f, 0.5f);
            column.anchorMax = new Vector2(0.5f, 0.5f);
            column.pivot     = new Vector2(0.5f, 0.5f);
            column.sizeDelta = new Vector2(440f, 440f);
            column.anchoredPosition = new Vector2(0f, -40f);

            var layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var buttonSprite = MenuBuildKit.LoadSprite(ButtonSprite);
            var resume   = MenuBuildKit.MenuButton("ResumeButton",   column, "RESUME",    titleFont, buttonSprite, 420f, 92f, 34f);
            var restart  = MenuBuildKit.MenuButton("RestartButton",  column, "RESTART",   titleFont, buttonSprite, 420f, 92f, 34f);
            var settings = MenuBuildKit.MenuButton("SettingsButton", column, "SETTINGS",  titleFont, buttonSprite, 420f, 92f, 34f);
            var toMenu   = MenuBuildKit.MenuButton("MainMenuButton", column, "MAIN MENU", titleFont, buttonSprite, 420f, 92f, 30f);

            foreach (var b in new[] { resume, restart, settings, toMenu })
                MenuBuildKit.AddSfx(b, audioSource, hover, click);

            UnityEventTools.AddPersistentListener(resume.onClick,   controller.Resume);
            UnityEventTools.AddPersistentListener(restart.onClick,  controller.Restart);
            UnityEventTools.AddPersistentListener(settings.onClick, controller.OnOpenSettings);
            UnityEventTools.AddPersistentListener(toMenu.onClick,   controller.ToMainMenu);

            // ---- Settings sub-panel --------------------------------------
            var settingsPanel = MenuBuildKit.NewRect("SettingsPanel", panel);
            MenuBuildKit.Stretch(settingsPanel);
            var settingsGroup = settingsPanel.gameObject.AddComponent<CanvasGroup>();
            settingsGroup.alpha = 0f;
            settingsGroup.interactable = false;
            settingsGroup.blocksRaycasts = false;

            var sFrame = MenuBuildKit.NewImage("Frame", settingsPanel, MenuBuildKit.LoadSprite(PanelSprite),
                                               MenuBuildKit.FrameTint, Image.Type.Sliced);
            var sFrameRt = (RectTransform)sFrame.transform;
            sFrameRt.anchorMin = new Vector2(0.5f, 0.5f);
            sFrameRt.anchorMax = new Vector2(0.5f, 0.5f);
            sFrameRt.sizeDelta = new Vector2(700f, 480f);
            sFrame.raycastTarget = true;

            var sHeading = MenuBuildKit.CarvedText("Heading", sFrameRt, "SETTINGS", titleFont, 48f,
                                                    TextAlignmentOptions.Center, glow: true, charSpacing: 12f);
            sHeading.anchorMin = new Vector2(0.5f, 1f);
            sHeading.anchorMax = new Vector2(0.5f, 1f);
            sHeading.pivot     = new Vector2(0.5f, 1f);
            sHeading.sizeDelta = new Vector2(560f, 70f);
            sHeading.anchoredPosition = new Vector2(0f, -46f);

            BuildSlider("MusicVolume", sFrameRt, "MUSIC", bodyFont, new Vector2(0f, 40f));
            BuildSlider("SfxVolume",   sFrameRt, "SOUND", bodyFont, new Vector2(0f, -50f));

            var sBack = MenuBuildKit.MenuButton("BackButton", sFrameRt, "BACK", titleFont, buttonSprite, 280f, 84f, 30f);
            var sBackRt = (RectTransform)sBack.transform;
            sBackRt.anchorMin = new Vector2(0.5f, 0f);
            sBackRt.anchorMax = new Vector2(0.5f, 0f);
            sBackRt.pivot     = new Vector2(0.5f, 0f);
            sBackRt.anchoredPosition = new Vector2(0f, 40f);
            MenuBuildKit.AddSfx(sBack, audioSource, hover, click);
            UnityEventTools.AddPersistentListener(sBack.onClick, controller.OnCloseSettings);

            // ---- Wire and save -------------------------------------------
            var so = new SerializedObject(controller);
            so.FindProperty("pausePanel").objectReferenceValue = panel.gameObject;
            so.FindProperty("pauseRoot").objectReferenceValue = rootGroup;
            so.FindProperty("settingsPanel").objectReferenceValue = settingsGroup;
            so.ApplyModifiedPropertiesWithoutUndo();

            panel.gameObject.SetActive(false);   // starts hidden; the controller shows it

            Directory.CreateDirectory(PrefabDir);
            PrefabUtility.SaveAsPrefabAsset(canvas.gameObject, PausePrefabPath);
            Object.DestroyImmediate(canvas.gameObject);
        }

        // ================================================================
        //  Helpers
        // ================================================================

        static UnityEngine.EventSystems.EventSystem CreateEventSystem()
        {
            var go = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            go.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
            return go.GetComponent<UnityEngine.EventSystems.EventSystem>();
        }

        static AudioClip LoadClip(string name)
        {
            var path = "Assets/_MathDungeon/Audio/UI/" + name + ".ogg";
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) Debug.LogWarning("[MathDungeon] Missing audio clip: " + path);
            return clip;
        }

        static void AddSceneToBuildSettings(string scenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(s => s.path == scenePath)) return;

            // Index 0: the main menu is the entry point of the build.
            scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
