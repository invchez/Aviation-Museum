using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// App Store screenshot capture, run from the command line:
//   Unity -batchmode -quit -projectPath . -executeMethod CaptureStoreScreenshots.Run
//
// Renders the real scenes at exact App Store device resolutions rather than resizing
// device captures. Screen Space Overlay canvases are temporarily retargeted to the
// capture camera, and their scale factor is computed for the capture resolution,
// because CanvasScaler derives its scale from Screen.width/height, which in batch
// mode has nothing to do with the render target size.
public static class CaptureStoreScreenshots
{
    struct DeviceProfile
    {
        public string Prefix;
        public int Width;
        public int Height;
    }

    static readonly DeviceProfile[] Devices =
    {
        new DeviceProfile { Prefix = "iphone69", Width = 2868, Height = 1320 },
        new DeviceProfile { Prefix = "ipad13",   Width = 2752, Height = 2064 },
    };

    static string OutputDirectory = "screenshots/en-US";
    const string MuseumScene = "Assets/Scenes/Museum.unity";
    const string MenuScene = "Assets/Scenes/Main Menu.unity";

    // Late afternoon: the hall reads clearly but still has directional light and shadow.
    const float CaptureTimeOfDay = 16.6f;

    // Height of the player camera at spawn, used to frame exhibits at a visitor's eye level.
    static float visitorEyeHeight;

    // Cached so both device passes show the same answer, and so the worker is hit once.
    static string cachedAiQuestion;
    static string cachedAiAnswer;

    public static void Run()
    {
        Directory.CreateDirectory(OutputDirectory);

        foreach (DeviceProfile device in Devices)
        {
            CaptureMenu(device);
            CaptureMuseum(device);
        }

        Debug.Log("Screenshot capture complete.");
    }

    // Quick layout check of the museum HUD, tutorial and exhibit markers. Written outside
    // the store screenshot folder (Unity clears Temp/ on exit, so not there either).
    public static void PreviewReviewFixes()
    {
        OutputDirectory = "Library/ReviewPreview";
        Directory.CreateDirectory(OutputDirectory);

        DeviceProfile[] devices =
        {
            new DeviceProfile { Prefix = "ipadair11", Width = 2360, Height = 1640 },
            new DeviceProfile { Prefix = "iphone69", Width = 2868, Height = 1320 },
            new DeviceProfile { Prefix = "ipad13", Width = 2752, Height = 2064 },
        };

        foreach (DeviceProfile device in devices)
        {
            EditorSceneManager.OpenScene(MuseumScene, OpenSceneMode.Single);
            Scene scene = SceneManager.GetActiveScene();
            Camera camera = FindCamera(scene);
            ApplyTimeOfDay(scene);
            PrepareMuseumHud(scene);

            // The spawn view, as a visitor first sees it.
            Capture(scene, camera, device, "a_spawn");

            GameObject tutorial = FindByName(scene, "Touch Tutorial");
            tutorial.SetActive(true);
            Capture(scene, camera, device, "b_tutorial");
            tutorial.SetActive(false);

            // Behind a marker on the walkway, looking at the exhibit.
            TriggerScript trigger = Resources.FindObjectsOfTypeAll<TriggerScript>()
                .Where(t => t.gameObject.scene == scene)
                .OrderByDescending(t => t.transform.position.z)
                .First();
            trigger.RefreshPanel();
            Transform panel = PanelTransform(trigger);
            Vector3 view = ViewSide(panel);
            Vector3 eye = trigger.transform.position + view * 7f + Vector3.up * 1.2f;
            camera.transform.SetPositionAndRotation(eye,
                Quaternion.LookRotation(trigger.transform.position + Vector3.down * 0.3f - eye, Vector3.up));
            Capture(scene, camera, device, "c_marker");
        }
    }

    // ---------------------------------------------------------------- main menu

    static void CaptureMenu(DeviceProfile device)
    {
        EditorSceneManager.OpenScene(MenuScene, OpenSceneMode.Single);
        Scene scene = SceneManager.GetActiveScene();

        // Mirror what MainMenuScript does on mobile, which only runs at play time.
        SetActiveByName(scene, "Donate Button", false);
        SetActiveByName(scene, "ExitButton", false);
        SetActiveByName(scene, "AboutPanel", false);

        Capture(scene, FindCamera(scene), device, "01_main_menu");
    }

    // ------------------------------------------------------------------- museum

    static void CaptureMuseum(DeviceProfile device)
    {
        EditorSceneManager.OpenScene(MuseumScene, OpenSceneMode.Single);
        Scene scene = SceneManager.GetActiveScene();

        Camera camera = FindCamera(scene);
        visitorEyeHeight = camera.transform.position.y;
        ApplyTimeOfDay(scene);
        PrepareMuseumHud(scene);

        TriggerScript[] triggers = Resources.FindObjectsOfTypeAll<TriggerScript>()
            .Where(t => t.gameObject.scene == scene)
            .OrderBy(t => t.transform.position.z)
            .ToArray();

        foreach (TriggerScript trigger in triggers)
        {
            trigger.RefreshPanel();
            HideAllPanels(trigger);
        }

        // Wide establishing shot down the exhibit row, from the visitor walkway.
        FrameHall(camera, triggers);
        Capture(scene, camera, device, "02_exhibit_hall");

        // Eurofighter plaque, Rafale specifications, F-14 AI guide.
        CaptureExhibit(scene, camera, device, triggers, 0, "InfoPanel", "03_exhibit_plaque");
        CaptureExhibit(scene, camera, device, triggers, 1, "StatPanel", "04_specifications");
        CaptureExhibit(scene, camera, device, triggers, 2, "AIPanel", "05_ai_guide");
    }

    static void ApplyTimeOfDay(Scene scene)
    {
        foreach (DayNightCycle cycle in Resources.FindObjectsOfTypeAll<DayNightCycle>()
                     .Where(c => c.gameObject.scene == scene))
        {
            cycle.timeOfDay = CaptureTimeOfDay;
            cycle.autoCycle = false;

            // ApplyTimeOfDay is private and normally driven by OnValidate/Update.
            typeof(DayNightCycle)
                .GetMethod("ApplyTimeOfDay", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(cycle, null);
        }
    }

    static void PrepareMuseumHud(Scene scene)
    {
        // Only the joystick the player actually drives is enabled at runtime, and only
        // on mobile; activating every object named "Joystick" would stack spare rigs.
        foreach (PlayerScript player in Resources.FindObjectsOfTypeAll<PlayerScript>()
                     .Where(p => p.gameObject.scene == scene))
        {
            if (player.movementJoystick != null)
            {
                player.movementJoystick.gameObject.SetActive(true);
            }
        }

        // Overlays that must not cover the exhibits. The layout switcher only appears
        // when the visitor walks into its trigger, and its buttons are spawned at Start.
        SetActiveByName(scene, "Settings", false);
        SetActiveByName(scene, "Tutorial Panel", false);
        SetActiveByName(scene, "Layout Switcher", false);
    }

    // Exhibit canvases all face the same way; the visitor walkway is on the far side
    // of the panel from its transform forward, which is where the Player spawns.
    static Vector3 ViewSide(Transform panel)
    {
        return -panel.forward;
    }

    static void FrameHall(Camera camera, TriggerScript[] triggers)
    {
        Transform first = PanelTransform(triggers.First());
        Transform last = PanelTransform(triggers.Last());

        Vector3 view = ViewSide(first);
        Vector3 along = (last.position - first.position).normalized;

        // Stand on the walkway just past the last exhibit and look back down the row.
        Vector3 position = last.position + view * 4.2f + along * 5.0f;
        position.y = first.position.y + 0.55f;

        Vector3 lookAt = Vector3.Lerp(first.position, last.position, 0.35f) - view * 2.0f;
        lookAt.y = first.position.y - 0.35f;

        camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(lookAt - position, Vector3.up));
    }

    static void CaptureExhibit(Scene scene, Camera camera, DeviceProfile device,
                               TriggerScript[] triggers, int index, string panelName, string shotName)
    {
        TriggerScript trigger = triggers[Mathf.Clamp(index, 0, triggers.Length - 1)];

        foreach (TriggerScript other in triggers)
        {
            HideAllPanels(other);
        }

        GameObject panel = PanelByName(trigger, panelName);
        if (panel == null)
        {
            Debug.LogError($"Panel {panelName} missing on {trigger.name}");
            return;
        }

        panel.SetActive(true);
        ForceVisible(panel);

        // The first-time hint retires as soon as an exhibit opens, so it never shares the
        // screen with an exhibit panel in the real app.
        SetActiveByName(scene, "Exhibit Hint", false);

        if (panelName == "AIPanel")
        {
            FillAiTranscript(trigger, panel);
        }

        Transform target = PanelTransform(trigger);
        Vector3 view = ViewSide(target);

        // Stand on the exhibit's floor marker at eye height and face the panel, which is
        // exactly where the app's auto-turn leaves a visitor who steps onto the marker.
        Vector3 walkway = view;
        walkway.y = 0f;
        walkway.Normalize();
        // Stand on the marker ring, toward its walkway side, so the whole panel and the
        // aircraft either side of it are in view.
        Vector3 position = trigger.transform.position + walkway * 2.2f;
        position.y = visitorEyeHeight;

        // Aim at the panel's visual centre. The Information, Statistics and AI panels are
        // drawn well below the exhibit canvas origin.
        RectTransform panelRect = (RectTransform)panel.transform;
        Vector3 panelCentre = panelRect.TransformPoint(panelRect.rect.center);

        camera.transform.SetPositionAndRotation(
            position, Quaternion.LookRotation(panelCentre - position, Vector3.up));

        Capture(scene, camera, device, shotName);
    }

    // The transcript is filled from a real call to the live worker so the screenshot
    // shows an answer the app actually produces, not invented marketing copy.
    static void FillAiTranscript(TriggerScript trigger, GameObject panel)
    {
        PlaneSO plane = GetField<PlaneSO>(trigger, "Plane");
        AiTextReciever receiver = panel.GetComponentInParent<AiTextReciever>(true)
                                  ?? GetField<AiTextReciever>(trigger, "aiTextReciever");

        TMP_InputField transcript = receiver != null
            ? GetPrivateOrPublic<TMP_InputField>(receiver, "responseField")
            : panel.GetComponentsInChildren<TMP_InputField>(true).FirstOrDefault();

        if (transcript == null)
        {
            Debug.LogError("Could not find the AI transcript field.");
            return;
        }

        string question = $"What was the {plane?.Name ?? "aircraft"} designed to do?";

        if (cachedAiAnswer == null)
        {
            cachedAiQuestion = question;
            cachedAiAnswer = AskWorker(plane, question);
        }

        if (cachedAiAnswer == null)
        {
            Debug.LogError("Worker call failed; leaving the AI panel transcript empty.");
            return;
        }

        transcript.text =
            "AI: Hi, what can I help you with?\n\n" +
            $"User: {cachedAiQuestion}\n\n" +
            $"AI: {cachedAiAnswer}";
        transcript.ForceLabelUpdate();

        // The app keeps the transcript scrolled to the latest line; do the same here.
        Canvas.ForceUpdateCanvases();
        transcript.textComponent.ForceMeshUpdate();
        float overflow = transcript.textComponent.preferredHeight - transcript.textViewport.rect.height;
        if (overflow > 0f)
        {
            RectTransform textRect = transcript.textComponent.rectTransform;
            textRect.anchoredPosition = new Vector2(textRect.anchoredPosition.x, overflow);
        }
    }

    static string AskWorker(PlaneSO plane, string question)
    {
        string prompt =
            "You are an aviation museum assistant helping a visitor.\n" +
            "Active aircraft context:\n" +
            $"- Name: {plane?.Name}\n" +
            $"- Manufacturer: {plane?.Manufacturer}\n" +
            $"- Top Speed: {plane?.TSpeed}\n" +
            $"- Wing Span: {plane?.WingSpan}\n" +
            $"- Details: {plane?.Information}\n\n" +
            $"Visitor question: {question}\n\n" +
            "Instructions:\n- Answer in under 100 words.";

        try
        {
            using HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.Add("x-app-secret", "test_secret");

            string body = JsonUtility.ToJson(new WorkerRequest { message = prompt });
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            string json = client
                .PostAsync("https://spring-poetry-3bc5.alexli010972.workers.dev/playeraiquestion", content)
                .GetAwaiter().GetResult()
                .Content.ReadAsStringAsync().GetAwaiter().GetResult();

            var parsed = JsonUtility.FromJson<AiTextReciever.Choices>(json);
            string answer = parsed?.choices != null && parsed.choices.Length > 0
                ? parsed.choices[0]?.message?.content
                : null;

            Debug.Log($"Worker answer: {answer}");
            return answer;
        }
        catch (Exception e)
        {
            Debug.LogError($"Worker request failed: {e.Message}");
            return null;
        }
    }

    [Serializable]
    class WorkerRequest
    {
        public string message;
    }

    static void HideAllPanels(TriggerScript trigger)
    {
        foreach (string field in new[] { "StarterMain", "AiPanel", "StatisticsPanel", "InformationPanel" })
        {
            GameObject panel = GetField<GameObject>(trigger, field);
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }
    }

    static GameObject PanelByName(TriggerScript trigger, string panelName)
    {
        return panelName switch
        {
            "InfoPanel" => GetField<GameObject>(trigger, "InformationPanel"),
            "StatPanel" => GetField<GameObject>(trigger, "StatisticsPanel"),
            "AIPanel" => GetField<GameObject>(trigger, "AiPanel"),
            _ => GetField<GameObject>(trigger, "StarterMain"),
        };
    }

    static Transform PanelTransform(TriggerScript trigger)
    {
        Canvas canvas = GetField<Canvas>(trigger, "Canvas");
        return canvas != null ? canvas.transform : trigger.transform;
    }

    static void ForceVisible(GameObject panel)
    {
        // Panels sit at alpha 0 in the saved scene because their fade-in is animated.
        // Sample the fade-in clip at its last frame so every element ends up exactly as
        // it does in the app, translucent backgrounds included.
        Animator animator = panel.GetComponent<Animator>();
        AnimationClip fadeIn = animator != null && animator.runtimeAnimatorController != null
            ? animator.runtimeAnimatorController.animationClips.FirstOrDefault(c => c.name.Contains("FadeIn"))
            : null;

        if (fadeIn != null)
        {
            fadeIn.SampleAnimation(panel, fadeIn.length);
        }
        else
        {
            foreach (CanvasGroup group in panel.GetComponentsInChildren<CanvasGroup>(true))
            {
                group.alpha = 1f;
            }
        }

        if (animator != null)
        {
            animator.enabled = false;
        }
    }

    // ------------------------------------------------------------------ capture

    static void Capture(Scene scene, Camera camera, DeviceProfile device, string shotName)
    {
        if (camera == null)
        {
            Debug.LogError($"No camera for {shotName}; skipping.");
            return;
        }

        List<(Canvas canvas, RenderMode mode, Camera cam, bool scalerEnabled, float scale)> restore = new();

        foreach (Canvas canvas in Resources.FindObjectsOfTypeAll<Canvas>())
        {
            if (canvas.gameObject.scene != scene || canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                continue;
            }

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            restore.Add((canvas, canvas.renderMode, canvas.worldCamera,
                         scaler != null && scaler.enabled, canvas.scaleFactor));

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = Mathf.Max(0.35f, camera.nearClipPlane * 2f);

            if (scaler != null)
            {
                // Pin the scale factor to what this canvas would use at the capture
                // resolution; CanvasScaler itself would compute it from Screen.*.
                // Disable first: CanvasScaler.OnDisable resets the scale factor to 1.
                scaler.enabled = false;
                canvas.scaleFactor = ScaleFactorFor(scaler, device);
            }
        }

        RenderTexture target = new RenderTexture(device.Width, device.Height, 24, RenderTextureFormat.ARGB32)
        {
            antiAliasing = 4,
        };

        RenderTexture previousTarget = camera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;

        try
        {
            camera.targetTexture = target;
            Canvas.ForceUpdateCanvases();
            camera.Render();

            RenderTexture.active = target;
            Texture2D image = new Texture2D(device.Width, device.Height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, device.Width, device.Height), 0, 0);
            image.Apply();

            string path = Path.Combine(OutputDirectory, $"{device.Prefix}_{shotName}.png");
            File.WriteAllBytes(path, image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image);

            Debug.Log($"Wrote {path} ({device.Width}x{device.Height})");
        }
        finally
        {
            RenderTexture.active = previousActive;
            camera.targetTexture = previousTarget;
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);

            foreach (var entry in restore)
            {
                entry.canvas.renderMode = entry.mode;
                entry.canvas.worldCamera = entry.cam;
                entry.canvas.scaleFactor = entry.scale;
                CanvasScaler scaler = entry.canvas.GetComponent<CanvasScaler>();
                if (scaler != null)
                {
                    scaler.enabled = entry.scalerEnabled;
                }
            }
        }
    }

    static float ScaleFactorFor(CanvasScaler scaler, DeviceProfile device)
    {
        if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
        {
            return scaler.scaleFactor;
        }

        Vector2 reference = scaler.referenceResolution;
        float logWidth = Mathf.Log(device.Width / reference.x, 2f);
        float logHeight = Mathf.Log(device.Height / reference.y, 2f);

        switch (scaler.screenMatchMode)
        {
            case CanvasScaler.ScreenMatchMode.Expand:
                return Mathf.Pow(2f, Mathf.Min(logWidth, logHeight));
            case CanvasScaler.ScreenMatchMode.Shrink:
                return Mathf.Pow(2f, Mathf.Max(logWidth, logHeight));
            default:
                return Mathf.Pow(2f, Mathf.Lerp(logWidth, logHeight, scaler.matchWidthOrHeight));
        }
    }

    // ------------------------------------------------------------------ helpers

    static Camera FindCamera(Scene scene)
    {
        Camera[] cameras = Resources.FindObjectsOfTypeAll<Camera>()
            .Where(c => c.gameObject.scene == scene && c.enabled)
            .ToArray();

        return cameras.FirstOrDefault(c => c.CompareTag("MainCamera")) ?? cameras.FirstOrDefault();
    }

    static GameObject FindByName(Scene scene, string name)
    {
        return scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(t => t.name == name)?.gameObject;
    }

    static void SetActiveByName(Scene scene, string name, bool active)
    {
        GameObject target = FindByName(scene, name);
        if (target != null)
        {
            target.SetActive(active);
        }
    }

    static T GetField<T>(TriggerScript trigger, string fieldName) where T : class
    {
        return typeof(TriggerScript).GetField(fieldName)?.GetValue(trigger) as T;
    }

    static T GetPrivateOrPublic<T>(object instance, string fieldName) where T : class
    {
        return instance.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance)?.GetValue(instance) as T;
    }
}
