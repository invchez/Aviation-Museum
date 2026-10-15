using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Adds the Quiz & Badges page and the Cockpit & Cutaway viewer to the app:
//   - one viewer prefab per aircraft, cut from the museum's own exhibits
//   - the Aircraft Viewer scene, registered in the build settings
//   - two new main menu buttons and the quiz page
// Idempotent: re-running replaces everything it built.
//   Unity -batchmode -quit -projectPath . -executeMethod BuildQuizAndViewer.Run
// Preview renders of the new screens (written to Library/ReviewPreview):
//   Unity -batchmode -quit -projectPath . -executeMethod BuildQuizAndViewer.Preview
public static class BuildQuizAndViewer
{
    const string MuseumScene = "Assets/Scenes/Museum.unity";
    const string MenuScene = "Assets/Scenes/Main Menu.unity";
    public const string ViewerScene = "Assets/Scenes/Aircraft Viewer.unity";
    const string PrefabFolder = "Assets/Prefabs/Viewer";
    const string GridTexturePath = "Assets/Textures/ViewerGrid.png";
    const string GroundMaterialPath = "Assets/Materials/ViewerGround.mat";
    const string SkyMaterialPath = "Assets/Materials/ViewerSky.mat";
    const string FontPath = "Assets/UI/Figtree/Figtree-VariableFont_wght SDF.asset";
    const string ButtonSpritePath = "Assets/UI/DonationButtonSettings.png";
    const string PanelSpritePath = "Assets/UI/GeneralBase.png";

    // Built objects in the main menu, removed and rebuilt on every run.
    const string QuizButtonName = "QuizButton";
    const string ViewerButtonName = "ViewerButton";
    const string QuizPageName = "Quiz Page";

    // Same order as the App Store description.
    static readonly string[] PlaneOrder =
    {
        "A-10 Thunderbolt II", "B-2 Spirit", "Eurofighter Typhoon", "F-14 Tomcat", "F-16 Fighting Falcon",
        "F-22 Raptor", "F-35 Lightning", "F/A-18 Hornet", "Dassault Rafale",
    };

    public static void Run()
    {
        List<PlaneSO> planes = LoadPlanes();
        List<ViewerAircraft> aircraft = BuildViewerPrefabs(planes);
        BuildViewerScene(aircraft);
        RegisterViewerScene();
        BuildMenu(planes);
        AssetDatabase.SaveAssets();
        Debug.Log("BuildQuizAndViewer: done.");
    }

    static List<PlaneSO> LoadPlanes()
    {
        List<PlaneSO> all = AssetDatabase.FindAssets("t:PlaneSO")
            .Select(guid => AssetDatabase.LoadAssetAtPath<PlaneSO>(AssetDatabase.GUIDToAssetPath(guid)))
            .ToList();
        return PlaneOrder.Select(name => all.First(p => p.Name == name)).ToList();
    }

    // ----------------------------------------------------------- viewer prefabs

    static List<ViewerAircraft> BuildViewerPrefabs(List<PlaneSO> planes)
    {
        Scene museum = EditorSceneManager.OpenScene(MuseumScene, OpenSceneMode.Single);
        PlaneSwitcher switcher = Object.FindObjectsByType<PlaneSwitcher>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .First(s => s.gameObject.scene == museum);
        Directory.CreateDirectory(PrefabFolder);

        List<ViewerAircraft> result = new();
        foreach (PlaneSO plane in planes)
        {
            GameObject exhibit = switcher.planePlot.SelectMany(p => p.planeData)
                .First(d => d.planeData == plane && d.planeObject != null).planeObject;
            result.Add(BuildViewerPrefab(plane, exhibit));
        }
        return result;
    }

    static ViewerAircraft BuildViewerPrefab(PlaneSO plane, GameObject exhibit)
    {
        ExhibitFacts facts = ExhibitFacts.For(plane.Name);

        // A plain copy of the exhibit, with the same world rotation and scale. In the
        // museum every aircraft's nose points along world +X.
        GameObject model = Object.Instantiate(exhibit);
        model.name = exhibit.name;
        model.SetActive(true);
        foreach (Transform child in model.GetComponentsInChildren<Transform>(true)) child.gameObject.SetActive(true);
        foreach (Collider collider in model.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
        foreach (MonoBehaviour behaviour in model.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(behaviour);
        foreach (Light light in model.GetComponentsInChildren<Light>(true)) Object.DestroyImmediate(light);

        GameObject root = new GameObject($"Viewer {plane.Name}");
        GameObject pivot = new GameObject("Model");
        pivot.transform.SetParent(root.transform, false);

        // Turn the nose from +X to +Z around the model's centre.
        Bounds bounds = VisibleBounds(model);
        pivot.transform.position = bounds.center;
        model.transform.SetParent(pivot.transform, true);
        pivot.transform.rotation = Quaternion.Euler(0, -90, 0);

        // Real length in metres, so eye heights and system sizes are true to life.
        bounds = VisibleBounds(model);
        float scale = facts != null ? facts.LengthMetres / bounds.size.z : 1f;
        pivot.transform.localScale = Vector3.one * scale;

        // Nose tip at the origin, on the centreline, wheels on the ground.
        bounds = VisibleBounds(model);
        pivot.transform.position -= new Vector3(bounds.center.x, bounds.min.y, bounds.max.z);

        Renderer cockpit = FindRenderer(model, r => r.name.ToLowerInvariant().Contains("cockpit"), r => r.bounds.max.z);
        Renderer canopy = FindRenderer(model, r => IsCanopy(r.name), r => r.bounds.max.z);
        Transform eye = new GameObject("Pilot Eye").transform;
        eye.SetParent(root.transform, false);
        eye.position = PilotEye(plane.Name, cockpit, canopy, VisibleBounds(model));
        Debug.Log($"BuildQuizAndViewer: {plane.Name} cockpit={cockpit?.name} canopy={canopy?.name} eye={eye.position:F2}");

        // Named after the asset: display names such as "F/A-18 Hornet" are not valid file names.
        string path = $"{PrefabFolder}/{plane.name}.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        // Read before the model is destroyed: a destroyed Renderer compares equal to null.
        bool hasCockpit = cockpit != null;
        Object.DestroyImmediate(root);
        Object.DestroyImmediate(model);

        return new ViewerAircraft { plane = plane, prefab = prefab, hasCockpitInterior = hasCockpit };
    }

    static bool IsCanopy(string name)
    {
        string lower = name.ToLowerInvariant();
        return lower.Contains("canopy") || lower.StartsWith("vehicle#blister1");
    }

    // Where the cockpit meshes sit differs between these models, so the estimate below is
    // corrected per aircraft from the rendered view: (metres further aft, metres up).
    static readonly Dictionary<string, Vector2> EyeCorrections = new()
    {
        ["F-14 Tomcat"] = new Vector2(0.9f, 0f),
        ["F-16 Fighting Falcon"] = new Vector2(-0.3f, 0f),
        ["Dassault Rafale"] = new Vector2(-0.3f, 0.1f),
    };

    // The pilot's eyes sit a little aft of the middle of the cockpit tub, about two thirds of
    // the way up the canopy. The F-14's front cockpit is the pilot's; the RIO sits behind.
    static Vector3 PilotEye(string planeName, Renderer cockpit, Renderer canopy, Bounds overall)
    {
        Bounds tub = cockpit != null ? cockpit.bounds : canopy != null ? canopy.bounds : overall;
        Bounds glass = canopy != null ? canopy.bounds : tub;
        float z = tub.center.z - tub.size.z * 0.12f;
        float y = glass.min.y + glass.size.y * 0.62f;
        Vector2 correction = EyeCorrections.GetValueOrDefault(planeName);
        return new Vector3(0, y + correction.y, z - correction.x);
    }

    static Renderer FindRenderer(GameObject root, System.Func<Renderer, bool> match, System.Func<Renderer, float> order)
    {
        return root.GetComponentsInChildren<Renderer>(true)
            .Where(r => r.bounds.size.magnitude > 0.3f && match(r))
            .OrderByDescending(order)
            .FirstOrDefault();
    }

    static Bounds VisibleBounds(GameObject root)
    {
        // Some models carry empty meshes far from the aircraft; skip anything without size.
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true).Where(r => r.bounds.size.sqrMagnitude > 1e-4f).ToArray();
        Bounds result = renderers[0].bounds;
        foreach (Renderer renderer in renderers) result.Encapsulate(renderer.bounds);
        return result;
    }

    // ------------------------------------------------------------ viewer scene

    static void BuildViewerScene(List<ViewerAircraft> aircraft)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.fieldOfView = 40;
        camera.farClipPlane = 600;
        cameraObject.AddComponent<UniversalAdditionalCameraData>();
        cameraObject.AddComponent<AudioListener>();

        GameObject sun = new GameObject("Sun");
        Light light = sun.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.3f;
        light.color = new Color(1f, 0.96f, 0.9f);
        light.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(48, 150, 0);

        RenderSettings.skybox = EnsureSkyMaterial();
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.55f, 0.62f, 0.75f);
        RenderSettings.ambientEquatorColor = new Color(0.42f, 0.44f, 0.48f);
        RenderSettings.ambientGroundColor = new Color(0.18f, 0.19f, 0.21f);
        RenderSettings.sun = light;

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ramp";
        Object.DestroyImmediate(ground.GetComponent<Collider>());
        ground.transform.localScale = new Vector3(40, 1, 40);
        ground.GetComponent<Renderer>().sharedMaterial = EnsureGroundMaterial();

        GameObject stage = new GameObject("Stage");

        // UI
        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform));
        canvasObject.layer = LayerMask.NameToLayer("UI");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800, 600);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<InputSystemUIInputModule>();

        GameObject viewerObject = new GameObject("Aircraft Viewer");
        AircraftViewer viewer = viewerObject.AddComponent<AircraftViewer>();
        viewer.aircraft = aircraft;
        viewer.viewCamera = camera;
        viewer.stage = stage.transform;
        viewer.cutawayShader = Shader.Find("AVIA/Cutaway");
        viewer.ghostShader = Shader.Find("AVIA/CutawayGhost");
        viewer.canvas = canvas;
        viewer.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        viewer.buttonSprite = LoadSprite(ButtonSpritePath);
        viewer.panelSprite = LoadSprite(PanelSpritePath);
        viewer.menuSceneIndex = 0;

        EditorSceneManager.SaveScene(scene, ViewerScene);
    }

    static Material EnsureSkyMaterial()
    {
        Material sky = AssetDatabase.LoadAssetAtPath<Material>(SkyMaterialPath);
        if (sky == null)
        {
            sky = new Material(Shader.Find("Skybox/Procedural"));
            AssetDatabase.CreateAsset(sky, SkyMaterialPath);
        }
        sky.SetFloat("_SunSize", 0.03f);
        sky.SetFloat("_AtmosphereThickness", 0.85f);
        sky.SetColor("_SkyTint", new Color(0.45f, 0.55f, 0.75f));
        sky.SetColor("_GroundColor", new Color(0.22f, 0.24f, 0.28f));
        sky.SetFloat("_Exposure", 1.1f);
        EditorUtility.SetDirty(sky);
        return sky;
    }

    static Material EnsureGroundMaterial()
    {
        if (!File.Exists(GridTexturePath))
        {
            // Concrete ramp slabs: a faint seam every slab, darker than the slab itself.
            const int size = 256;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool seam = x < 3 || y < 3;
                    float noise = Mathf.PerlinNoise(x * 0.07f, y * 0.07f) * 0.05f;
                    float v = seam ? 0.30f : 0.46f + noise;
                    texture.SetPixel(x, y, new Color(v, v * 1.01f, v * 1.04f, 1));
                }
            }
            texture.Apply();
            File.WriteAllBytes(GridTexturePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(GridTexturePath);
        }

        Material ground = AssetDatabase.LoadAssetAtPath<Material>(GroundMaterialPath);
        if (ground == null)
        {
            ground = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(ground, GroundMaterialPath);
        }
        ground.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(GridTexturePath));
        // 400 m plane, 5 m slabs.
        ground.SetTextureScale("_BaseMap", new Vector2(80, 80));
        ground.SetColor("_BaseColor", new Color(0.62f, 0.64f, 0.68f));
        ground.SetFloat("_Smoothness", 0.15f);
        EditorUtility.SetDirty(ground);
        return ground;
    }

    static void RegisterViewerScene()
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.Where(s => s.path != ViewerScene).ToList();
        // Directly after the museum, so it is build index 2.
        int museum = scenes.FindIndex(s => s.path == MuseumScene);
        scenes.Insert(museum + 1, new EditorBuildSettingsScene(ViewerScene, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    public static int ViewerBuildIndex()
    {
        return EditorBuildSettings.scenes.Where(s => s.enabled).ToList().FindIndex(s => s.path == ViewerScene);
    }

    // -------------------------------------------------------------------- menu

    static void BuildMenu(List<PlaneSO> planes)
    {
        Scene scene = EditorSceneManager.OpenScene(MenuScene, OpenSceneMode.Single);
        Transform canvas = Find(scene, "Canvas");
        Transform startButton = Find(scene, "StartButton");
        Transform aboutButton = Find(scene, "AboutButton");

        foreach (string name in new[] { QuizButtonName, ViewerButtonName, QuizPageName })
        {
            Transform existing = FindOrNull(scene, name);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);
        }

        // Quiz page, above the menu and About panel but under the scene fade.
        GameObject pageObject = new GameObject(QuizPageName, typeof(RectTransform));
        pageObject.layer = LayerMask.NameToLayer("UI");
        RectTransform page = (RectTransform)pageObject.transform;
        page.SetParent(canvas, false);
        page.anchorMin = Vector2.zero;
        page.anchorMax = Vector2.one;
        page.offsetMin = Vector2.zero;
        page.offsetMax = Vector2.zero;
        page.SetSiblingIndex(Find(scene, "FadeOut").GetSiblingIndex());
        Image background = pageObject.AddComponent<Image>();
        Image menuBackground = Find(scene, "MainPanel").GetComponent<Image>();
        background.sprite = menuBackground.sprite;
        background.type = menuBackground.type;
        background.color = menuBackground.color;
        QuizPage quiz = pageObject.AddComponent<QuizPage>();
        quiz.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        quiz.buttonSprite = LoadSprite(ButtonSpritePath);
        quiz.panelSprite = LoadSprite(PanelSpritePath);
        quiz.planes = planes;
        pageObject.SetActive(false);

        Button quizButton = CloneMenuButton(aboutButton, QuizButtonName, "Quiz & Badges");
        UnityEventTools.AddPersistentListener(quizButton.onClick, quiz.Open);

        Button viewerButton = CloneMenuButton(aboutButton, ViewerButtonName, "Cockpit & Cutaway");
        SceneSwitcher switcher = Find(scene, "FadeOut").GetComponent<SceneSwitcher>();
        UnityEventTools.AddIntPersistentListener(viewerButton.onClick, switcher.SwitchScene, ViewerBuildIndex());

        // Four buttons in the right-hand column: Enter, Quiz, Cockpit, About.
        Transform[] column = { startButton, quizButton.transform, viewerButton.transform, aboutButton };
        const float top = 0.84f, height = 0.13f, gap = 0.035f;
        for (int i = 0; i < column.Length; i++)
        {
            RectTransform rect = (RectTransform)column[i];
            float max = top - i * (height + gap);
            rect.anchorMin = new Vector2(rect.anchorMin.x, max - height);
            rect.anchorMax = new Vector2(rect.anchorMax.x, max);
            rect.SetSiblingIndex(i);
            FitLabel(rect);
        }

        // Desktop-only Exit sits below the column. The Donate button's handler no longer
        // exists and it overlapped the column, so it stays off on every platform.
        Transform exit = FindOrNull(scene, "ExitButton");
        if (exit != null)
        {
            RectTransform rect = (RectTransform)exit;
            rect.anchorMin = new Vector2(rect.anchorMin.x, 0.03f);
            rect.anchorMax = new Vector2(rect.anchorMax.x, 0.15f);
            FitLabel(rect);
        }
        Transform donate = FindOrNull(scene, "Donate Button");
        if (donate != null) donate.gameObject.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    static Button CloneMenuButton(Transform template, string name, string label)
    {
        GameObject clone = Object.Instantiate(template.gameObject, template.parent);
        clone.name = name;
        Button button = clone.GetComponent<Button>();
        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
        {
            UnityEventTools.RemovePersistentListener(button.onClick, i);
        }
        clone.GetComponentInChildren<TMP_Text>(true).text = label;
        return button;
    }

    // The labels were sized for taller buttons; fill the button and shrink to fit.
    static void FitLabel(RectTransform button)
    {
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        RectTransform rect = text.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(24, 6);
        rect.offsetMax = new Vector2(-24, -6);
        text.enableAutoSizing = true;
        text.fontSizeMax = Mathf.Max(text.fontSize, 28);
        text.fontSizeMin = 14;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.alignment = TextAlignmentOptions.Center;
        EditorUtility.SetDirty(text);
    }

    // ---------------------------------------------------------------- helpers

    static Sprite LoadSprite(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
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

    // ---------------------------------------------------------------- preview

    struct Device
    {
        public string Prefix;
        public int Width;
        public int Height;
    }

    static readonly Device[] PreviewDevices =
    {
        new Device { Prefix = "iphone69", Width = 2868, Height = 1320 },
        new Device { Prefix = "ipad13", Width = 2752, Height = 2064 },
    };

    const string PreviewFolder = "Library/ReviewPreview";

    public static void Preview()
    {
        Directory.CreateDirectory(PreviewFolder);
        string[] only = System.Environment.GetCommandLineArgs().SkipWhile(a => a != "-previewOnly").Skip(1).Take(1).ToArray();
        string filter = only.Length > 0 ? only[0] : "";

        foreach (Device device in PreviewDevices)
        {
            if (filter == "" || filter == "menu") PreviewMenu(device);
            if (filter == "" || filter == "viewer") PreviewViewer(device, null);
            if (filter.StartsWith("plane:")) PreviewViewer(device, filter.Substring(6));
        }
    }

    static void PreviewMenu(Device device)
    {
        EditorSceneManager.OpenScene(MenuScene, OpenSceneMode.Single);
        Scene scene = SceneManager.GetActiveScene();
        Camera camera = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).First();
        FindOrNull(scene, "ExitButton")?.gameObject.SetActive(false);
        FindOrNull(scene, "AboutPanel")?.gameObject.SetActive(false);
        Capture(scene, camera, device, "menu_1_main", null);

        // Sample progress so the badge grid shows every tile state.
        PlayerPrefs.SetInt("QuizBest_F-22 Raptor", 5);
        PlayerPrefs.SetInt("QuizBest_B-2 Spirit", 4);
        PlayerPrefs.SetInt("QuizBest_F-14 Tomcat", 2);
        PlayerPrefs.SetInt("Visited_F-22 Raptor", 1);
        PlayerPrefs.SetInt("Visited_Eurofighter Typhoon", 1);
        try
        {
            QuizPage quiz = Object.FindObjectsByType<QuizPage>(FindObjectsInactive.Include, FindObjectsSortMode.None).First();
            Invoke(quiz, "Awake");
            quiz.Open();
            Capture(scene, camera, device, "menu_2_badges", null);

            Invoke(quiz, "StartQuiz", quiz.planes.First(p => p.Name == "F-16 Fighting Falcon"));
            Capture(scene, camera, device, "menu_3_question", null);

            // Answer wrong, to show both the wrong and the correct highlight.
            Button wrong = quiz.GetComponentsInChildren<Button>().Where(b => b.name.StartsWith("Option")).First();
            wrong.onClick.Invoke();
            Capture(scene, camera, device, "menu_4_answered", null);

            // Jump to the result screen with a passing score.
            typeof(QuizPage).GetField("score", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(quiz, 4);
            Invoke(quiz, "ShowResult");
            Capture(scene, camera, device, "menu_5_result", null);
        }
        finally
        {
            foreach (string key in new[] { "QuizBest_F-22 Raptor", "QuizBest_B-2 Spirit", "QuizBest_F-14 Tomcat",
                                           "QuizBest_F-16 Fighting Falcon", "Visited_F-22 Raptor", "Visited_Eurofighter Typhoon" })
            {
                PlayerPrefs.DeleteKey(key);
            }
            PlayerPrefs.Save();
        }
    }

    static void PreviewViewer(Device device, string onlyPlane)
    {
        EditorSceneManager.OpenScene(ViewerScene, OpenSceneMode.Single);
        Scene scene = SceneManager.GetActiveScene();
        DynamicGI.UpdateEnvironment();
        AircraftViewer viewer = Object.FindObjectsByType<AircraftViewer>(FindObjectsSortMode.None).First();
        Invoke(viewer, "Awake");

        for (int i = 0; i < viewer.aircraft.Count; i++)
        {
            string name = viewer.aircraft[i].plane.Name;
            if (onlyPlane != null && viewer.aircraft[i].plane.name != onlyPlane) continue;
            if (onlyPlane == null && device.Prefix != "iphone69" && viewer.aircraft[i].plane.name != "F-22 Raptor") continue;

            viewer.ShowAircraft(i);
            string tag = $"viewer_{i}_{viewer.aircraft[i].plane.name.Replace(" ", "")}";
            viewer.SetMode(AircraftViewer.Mode.Exterior);
            Capture(scene, viewer.viewCamera, device, $"{tag}_a_exterior", null);
            viewer.SetMode(AircraftViewer.Mode.Cutaway);
            Capture(scene, viewer.viewCamera, device, $"{tag}_b_cutaway", () =>
            {
                viewer.UpdateCutPlane();
                viewer.UpdatePins();
            });
            // The part detail card, as it appears when a number is tapped.
            viewer.ShowDetail(0);
            Capture(scene, viewer.viewCamera, device, $"{tag}_b2_detail", () =>
            {
                viewer.UpdateCutPlane();
                viewer.UpdatePins();
            });

            if (viewer.aircraft[i].hasCockpitInterior)
            {
                viewer.SetMode(AircraftViewer.Mode.Cockpit);
                Capture(scene, viewer.viewCamera, device, $"{tag}_c_cockpit", viewer.UpdateHud);
            }
        }
    }

    static void Invoke(object target, string method, params object[] args)
    {
        target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
            .Invoke(target, args);
    }

    // Renders at an exact device resolution; overlay canvases are retargeted to the camera
    // with the scale factor CanvasScaler would pick for that resolution.
    static void Capture(Scene scene, Camera camera, Device device, string shot, System.Action beforeRender)
    {
        List<(Canvas canvas, CanvasScaler scaler)> restore = new();
        foreach (Canvas canvas in Resources.FindObjectsOfTypeAll<Canvas>()
                     .Where(c => c.gameObject.scene == scene && c.renderMode == RenderMode.ScreenSpaceOverlay && c.isRootCanvas))
        {
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = Mathf.Max(0.35f, camera.nearClipPlane * 2f);
            if (scaler != null)
            {
                scaler.enabled = false;
                float logWidth = Mathf.Log(device.Width / scaler.referenceResolution.x, 2);
                float logHeight = Mathf.Log(device.Height / scaler.referenceResolution.y, 2);
                canvas.scaleFactor = Mathf.Pow(2, Mathf.Lerp(logWidth, logHeight, scaler.matchWidthOrHeight));
            }
            restore.Add((canvas, scaler));
        }

        RenderTexture target = new RenderTexture(device.Width, device.Height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
        RenderTexture previousActive = RenderTexture.active;
        try
        {
            camera.targetTexture = target;
            Canvas.ForceUpdateCanvases();
            beforeRender?.Invoke();
            Canvas.ForceUpdateCanvases();
            camera.Render();

            RenderTexture.active = target;
            Texture2D image = new Texture2D(device.Width, device.Height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, device.Width, device.Height), 0, 0);
            image.Apply();
            File.WriteAllBytes(Path.Combine(PreviewFolder, $"{device.Prefix}_{shot}.png"), image.EncodeToPNG());
            Object.DestroyImmediate(image);
        }
        finally
        {
            RenderTexture.active = previousActive;
            camera.targetTexture = null;
            target.Release();
            Object.DestroyImmediate(target);
            foreach (var (canvas, scaler) in restore)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.worldCamera = null;
                if (scaler != null) scaler.enabled = true;
            }
        }
    }
}
