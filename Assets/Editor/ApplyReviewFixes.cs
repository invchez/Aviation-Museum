using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Makes the museum's features discoverable on touch devices (App Review 2.3, Sept 2026):
// a HUD with Settings / help / layout buttons, a touch tutorial, an exhibit hint, and
// floor markers in front of every exhibit. Idempotent: re-running replaces what it built.
//   Unity -batchmode -quit -projectPath . -executeMethod ApplyReviewFixes.Run
public static class ApplyReviewFixes
{
    const string MuseumScene = "Assets/Scenes/Museum.unity";
    const string FontPath = "Assets/UI/Figtree/Figtree-VariableFont_wght SDF.asset";
    const string ButtonSpritePath = "Assets/UI/DonationButtonSettings.png";
    const string PanelSpritePath = "Assets/UI/GeneralBase.png";
    const string MarkerTexturePath = "Assets/UI/ExhibitMarker.png";

    // Built objects, removed and rebuilt on every run.
    const string HudName = "HUD";
    const string TouchTutorialName = "Touch Tutorial";
    const string MarkersName = "Exhibit Markers";

    // Clear of the iPhone Dynamic Island and home indicator in landscape.
    const float SideInset = 72f;
    const float TopInset = 16f;

    static TMP_FontAsset font;
    static Sprite buttonSprite;
    static Sprite panelSprite;

    public static void Run()
    {
        Scene scene = EditorSceneManager.OpenScene(MuseumScene, OpenSceneMode.Single);

        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        // Nine-slice borders so the rounded corners stay round at any button size. Only
        // sliced Images read these; the existing Simple-mode uses of these sprites are unchanged.
        buttonSprite = LoadSlicedSprite(ButtonSpritePath, 60);
        panelSprite = LoadSlicedSprite(PanelSpritePath, 64);

        Transform mainCanvas = Find(scene, "Main Canvas");
        Transform settingsPanel = mainCanvas.Cast<Transform>().First(t => t.name == "Settings");

        RemoveBuilt(scene);

        BuildHud(mainCanvas, settingsPanel, out Button settingsButton, out Button helpButton,
                 out Button layoutButton, out TMP_Text layoutLabel);
        BuildTouchTutorial(mainCanvas, out CanvasGroup touchPanel, out Button dismissButton);
        BuildExhibitMarkers(scene);

        Wire(scene, settingsButton, helpButton, layoutButton, layoutLabel, touchPanel, dismissButton);

        SlowDayNightCycle(scene);
        FixNegativeScaleColliders(scene);
        RemoveDeadDonationButton(settingsPanel);
        LabelSettingsCloseButton(settingsPanel);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("ApplyReviewFixes: done.");
    }

    // ------------------------------------------------------------------- HUD

    static void BuildHud(Transform mainCanvas, Transform settingsPanel, out Button settingsButton,
                         out Button helpButton, out Button layoutButton, out TMP_Text layoutLabel)
    {
        RectTransform hud = NewRect(HudName, mainCanvas);
        Stretch(hud);
        // Drawn beneath the Settings panel, so an open panel covers the HUD rather
        // than having the HUD buttons sit on top of the panel's own close button.
        hud.SetSiblingIndex(settingsPanel.GetSiblingIndex());

        settingsButton = NewButton(hud, "Settings Button", "Settings", 20,
            new Vector2(1, 1), new Vector2(-SideInset, -TopInset), new Vector2(150, 46), out _);

        helpButton = NewButton(hud, "Help Button", "?", 24,
            new Vector2(1, 1), new Vector2(-SideInset - 150 - 12, -TopInset), new Vector2(46, 46), out _);

        layoutButton = NewButton(hud, "Layout Button", "Layout: Multirole Fighters", 18,
            new Vector2(0, 1), new Vector2(SideInset, -TopInset), new Vector2(320, 46), out layoutLabel);

        // First-time prompt; retires itself once any exhibit has been opened.
        RectTransform hint = NewRect("Exhibit Hint", hud);
        // Narrow enough to clear the joystick on the 800-unit-wide 13" iPad canvas, and
        // nudged right of centre for the same reason; the text wraps onto two lines.
        Anchor(hint, new Vector2(0.5f, 0), new Vector2(40, 24), new Vector2(460, 66));
        Image hintBg = hint.gameObject.AddComponent<Image>();
        hintBg.sprite = buttonSprite;
        hintBg.type = Image.Type.Sliced;
        hintBg.pixelsPerUnitMultiplier = 3.2f;
        hintBg.color = new Color(0.12f, 0.16f, 0.24f, 0.88f);
        hintBg.raycastTarget = false;
        TMP_Text hintText = NewText(hint, "Text", 18, TextAlignmentOptions.Center,
            "Walk onto a glowing marker in front of an aircraft to open its exhibit.");
        Stretch(hintText.rectTransform, 20, 6);
        hint.gameObject.AddComponent<ExhibitHint>();
    }

    // -------------------------------------------------------------- tutorial

    static void BuildTouchTutorial(Transform mainCanvas, out CanvasGroup touchPanel, out Button dismissButton)
    {
        RectTransform root = NewRect(TouchTutorialName, mainCanvas);
        Stretch(root);
        root.SetAsLastSibling();
        touchPanel = root.gameObject.AddComponent<CanvasGroup>();

        // Dim the scene and swallow touches so the joystick can't be dragged underneath.
        Image dimmer = root.gameObject.AddComponent<Image>();
        dimmer.color = new Color(0, 0, 0, 0.55f);

        // Fixed size in canvas units. The Main Canvas is ~1021x470 on a 6.9" iPhone and
        // ~800x600 on a 13" iPad, so 700x430 fits every supported screen with a margin.
        RectTransform card = NewRect("Card", root);
        Anchor(card, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700, 430));
        Image cardBg = card.gameObject.AddComponent<Image>();
        cardBg.sprite = panelSprite;
        cardBg.type = Image.Type.Sliced;
        cardBg.pixelsPerUnitMultiplier = 2f;
        cardBg.color = new Color(1, 1, 1, 0.97f);

        TMP_Text title = NewText(card, "Title", 30, TextAlignmentOptions.TopLeft, "Welcome to AVIA Museum");
        AnchorTop(title.rectTransform, 22, 40);

        // A fixed size rather than auto-sizing: auto-size resolves against the rect at the
        // moment the mesh is built and settled on its minimum, leaving tiny text.
        TMP_Text body = NewText(card, "Body", 20, TextAlignmentOptions.TopLeft,
            "<b>Move</b>  Drag the joystick in the bottom-left corner.\n" +
            "<b>Look around</b>  Swipe on the right side of the screen.\n" +
            "<b>Open an exhibit</b>  Walk onto a glowing marker in front of an aircraft to see its " +
            "plaque and specifications, and to ask the AI museum guide a question.\n" +
            "<b>Switch layouts</b>  Tap Layout at the top left to change the aircraft on display.\n" +
            "<b>Day and night</b>  The light changes through the day. Turn it on or off in Settings.");
        body.lineSpacing = 4;
        body.paragraphSpacing = 8;
        RectTransform bodyRect = body.rectTransform;
        bodyRect.anchorMin = new Vector2(0, 0);
        bodyRect.anchorMax = new Vector2(1, 1);
        bodyRect.offsetMin = new Vector2(32, 102);
        bodyRect.offsetMax = new Vector2(-32, -72);

        dismissButton = NewButton(card, "Got It Button", "Got it", 20,
            new Vector2(0.5f, 0), new Vector2(0, 44), new Vector2(180, 46), out _);
        ((RectTransform)dismissButton.transform).pivot = new Vector2(0.5f, 0);

        TMP_Text footnote = NewText(card, "Footnote", 13, TextAlignmentOptions.Center,
            "Tap ? at the top of the screen to see this again.");
        RectTransform footRect = footnote.rectTransform;
        footRect.anchorMin = new Vector2(0, 0);
        footRect.anchorMax = new Vector2(1, 0);
        footRect.pivot = new Vector2(0.5f, 0);
        footRect.anchoredPosition = new Vector2(0, 14);
        footRect.sizeDelta = new Vector2(-40, 22);
        footnote.color = new Color(1, 1, 1, 0.7f);

        root.gameObject.SetActive(false);
    }

    // --------------------------------------------------------------- markers

    static void BuildExhibitMarkers(Scene scene)
    {
        Sprite ring = EnsureMarkerSprite();
        GameObject root = new GameObject(MarkersName);
        SceneManager.MoveGameObjectToScene(root, scene);
        Physics.SyncTransforms();

        foreach (TriggerScript trigger in Object.FindObjectsByType<TriggerScript>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                     .Where(t => t.gameObject.scene == scene))
        {
            Transform panel = trigger.Canvas != null ? trigger.Canvas.transform : trigger.transform;

            // The exhibit panel faces the walkway (away from its transform forward). Put
            // the marker inside the trigger zone, on the walkway side, so a visitor who
            // steps onto it is facing the panel when it appears.
            Vector3 walkway = -panel.forward;
            walkway.y = 0;
            walkway.Normalize();
            Vector3 position = trigger.transform.position + walkway * 1.8f;
            position.y = FloorHeight(position, trigger.transform.position.y - 0.45f) + 0.03f;

            GameObject marker = new GameObject($"Marker ({trigger.name})", typeof(RectTransform));
            marker.transform.SetParent(root.transform, false);
            marker.transform.SetPositionAndRotation(position, Quaternion.Euler(90, 0, 0));
            marker.transform.localScale = Vector3.one * 0.01f;

            Canvas canvas = marker.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            ((RectTransform)marker.transform).sizeDelta = new Vector2(300, 300);

            GameObject imageObject = new GameObject("Ring", typeof(RectTransform));
            imageObject.transform.SetParent(marker.transform, false);
            Stretch((RectTransform)imageObject.transform);
            Image image = imageObject.AddComponent<Image>();
            image.sprite = ring;
            image.color = new Color(0.45f, 0.9f, 1f, 1f);
            image.raycastTarget = false;
        }
    }

    static float FloorHeight(Vector3 at, float fallback)
    {
        Vector3 origin = new Vector3(at.x, at.y + 3f, at.z);
        foreach (RaycastHit hit in Physics.RaycastAll(origin, Vector3.down, 10f, ~0, QueryTriggerInteraction.Ignore)
                     .OrderBy(h => h.distance))
        {
            return hit.point.y;
        }
        return fallback;
    }

    static Sprite EnsureMarkerSprite()
    {
        {
            const int size = 512;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float centre = (size - 1) / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float r = Mathf.Sqrt((x - centre) * (x - centre) + (y - centre) * (y - centre)) / centre;
                    // Solid outer ring with soft edges, plus a faint fill so it reads as a spot to stand on.
                    float ring = Mathf.Clamp01(1f - Mathf.Abs(r - 0.85f) / 0.10f);
                    float fill = r < 0.76f ? 0.24f : 0f;
                    float alpha = Mathf.Max(ring, fill) * (r <= 1f ? 1f : 0f);
                    texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
            }
            texture.Apply();
            File.WriteAllBytes(MarkerTexturePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(MarkerTexturePath);
        }

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(MarkerTexturePath);
        // Setting textureType alone leaves spriteImportMode at None, which produces no Sprite
        // sub-asset; an Image with no sprite then draws a solid rectangle.
        if (importer.textureType != TextureImporterType.Sprite || !importer.alphaIsTransparency ||
            importer.spriteImportMode != SpriteImportMode.Single)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(MarkerTexturePath);
    }

    // ---------------------------------------------------------------- wiring

    static void Wire(Scene scene, Button settingsButton, Button helpButton, Button layoutButton,
                     TMP_Text layoutLabel, CanvasGroup touchPanel, Button dismissButton)
    {
        SettingsScript settings = FindComponent<SettingsScript>(scene);
        settings.openButton = settingsButton;
        EditorUtility.SetDirty(settings);

        PlaneSwitcher switcher = FindComponent<PlaneSwitcher>(scene);
        switcher.hudLayoutButton = layoutButton;
        switcher.hudLayoutLabel = layoutLabel;
        EditorUtility.SetDirty(switcher);

        TutorialScript tutorial = FindComponent<TutorialScript>(scene);
        tutorial.touchPanel = touchPanel;
        tutorial.dismissButton = dismissButton;
        tutorial.helpButton = helpButton;
        EditorUtility.SetDirty(tutorial);
    }

    // ------------------------------------------------------------ scene fixes

    static void SlowDayNightCycle(Scene scene)
    {
        // A full day every 30 seconds flickered the hall; three minutes reads as a day passing.
        foreach (DayNightCycle cycle in Object.FindObjectsByType<DayNightCycle>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                     .Where(c => c.gameObject.scene == scene))
        {
            cycle.dayLengthInMinutes = 3f;
            EditorUtility.SetDirty(cycle);
        }
    }

    static void FixNegativeScaleColliders(Scene scene)
    {
        // A centred box is symmetric, so mirroring it changes nothing; making the scale
        // positive keeps the same collision shape and clears Unity's negative-scale warnings.
        foreach (BoxCollider box in Object.FindObjectsByType<BoxCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                     .Where(b => b.gameObject.scene == scene))
        {
            Vector3 scale = box.transform.localScale;
            if ((scale.x >= 0 && scale.y >= 0 && scale.z >= 0) || box.center != Vector3.zero)
            {
                continue;
            }

            box.transform.localScale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            EditorUtility.SetDirty(box.transform);
            Debug.Log($"ApplyReviewFixes: positive scale on {box.name}");
        }
    }

    // The panel's close button had no label or icon at all, just a blank pill.
    static void LabelSettingsCloseButton(Transform settingsPanel)
    {
        Transform close = settingsPanel.GetComponentsInChildren<Transform>(true).First(t => t.name == "Close Button");
        Transform existing = close.Find("Close Label");
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        TMP_Text label = NewText(close, "Close Label", 28, TextAlignmentOptions.Center, "X");
        Stretch(label.rectTransform);
    }

    static void RemoveDeadDonationButton(Transform settingsPanel)
    {
        Transform donation = settingsPanel.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Donation");
        if (donation != null)
        {
            Object.DestroyImmediate(donation.gameObject);
        }
    }

    static void RemoveBuilt(Scene scene)
    {
        foreach (string name in new[] { HudName, TouchTutorialName, MarkersName })
        {
            Transform existing = FindOrNull(scene, name);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }
        }
    }

    // ---------------------------------------------------------------- helpers

    static Button NewButton(Transform parent, string name, string label, float fontSize,
                            Vector2 anchor, Vector2 position, Vector2 size, out TMP_Text text)
    {
        RectTransform rect = NewRect(name, parent);
        Anchor(rect, anchor, position, size);

        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = buttonSprite;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 3.2f;

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.navigation = new Navigation { mode = Navigation.Mode.None };

        text = NewText(rect, "Text", fontSize, TextAlignmentOptions.Center, label);
        Stretch(text.rectTransform, 10, 0);
        return button;
    }

    static TMP_Text NewText(Transform parent, string name, float fontSize, TextAlignmentOptions alignment, string value)
    {
        RectTransform rect = NewRect(name, parent);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.text = value;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        return text;
    }

    static RectTransform NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    // Anchors to a single point; the pivot matches the anchor so position is an inset.
    static void Anchor(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    static void AnchorTop(RectTransform rect, float top, float height)
    {
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.anchoredPosition = new Vector2(0, -top);
        rect.sizeDelta = new Vector2(-56, height);
    }

    static void Stretch(RectTransform rect, float horizontal = 0, float vertical = 0)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(horizontal, vertical);
        rect.offsetMax = new Vector2(-horizontal, -vertical);
    }

    static Sprite LoadSlicedSprite(string path, float border)
    {
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        Vector4 wanted = new Vector4(border, border, border, border);
        if (importer.spriteBorder != wanted)
        {
            importer.spriteBorder = wanted;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
    }

    static T FindComponent<T>(Scene scene) where T : Component
    {
        return Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .First(c => c.gameObject.scene == scene);
    }

    static Transform Find(Scene scene, string name)
    {
        return FindOrNull(scene, name) ?? throw new System.Exception($"'{name}' not found in {scene.path}");
    }

    static Transform FindOrNull(Scene scene, string name)
    {
        return scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(t => t.name == name);
    }
}
