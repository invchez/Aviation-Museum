using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[System.Serializable]
public class ViewerAircraft
{
    public PlaneSO plane;
    // Built by BuildQuizAndViewer: nose tip at the origin pointing +Z, wheels on y = 0,
    // scaled to real length, with a "Pilot Eye" child at the pilot's head.
    public GameObject prefab;
    public bool hasCockpitInterior = true;
}

// The Cockpit & Cutaway scene. Three ways to look at each aircraft:
//   Exterior  orbit the aircraft on a turntable
//   Cutaway   a section plane removes the near half of the airframe to show the engines,
//             tanks, radar and weapons bays inside, each numbered and explained
//   Cockpit   sit at the pilot's eye point, look around, with a head-up display
public class AircraftViewer : MonoBehaviour
{
    public enum Mode { Exterior, Cutaway, Cockpit }

    public List<ViewerAircraft> aircraft = new();
    public Camera viewCamera;
    public Transform stage;

    [Header("Rendering")]
    public Shader cutawayShader;
    public Shader ghostShader;

    [Header("UI")]
    public Canvas canvas;
    public TMP_FontAsset font;
    public Sprite buttonSprite;
    public Sprite panelSprite;
    public int menuSceneIndex = 0;

    const float SideInset = 64f;
    const float OrbitSpeed = 0.25f;
    const float LookSpeed = 0.15f;

    static readonly int CutPlaneId = Shader.PropertyToID("_AviaCutPlane");
    static readonly int CutEnabledId = Shader.PropertyToID("_AviaCutEnabled");

    public Mode CurrentMode { get; private set; } = Mode.Exterior;
    public int CurrentIndex { get; private set; }

    UiKit ui;

    // Current aircraft.
    GameObject model;
    GameObject ghost;
    GameObject parts;
    Transform pilotEye;
    ExhibitFacts facts;
    Bounds bounds;
    readonly List<(Renderer renderer, Material[] original, Material[] cutaway)> skins = new();
    readonly List<(Vector3 position, CutawayPart part)> pinTargets = new();
    readonly Dictionary<Material, Material> cutawayMaterials = new();
    Material ghostMaterial;

    // Orbit and look state.
    float yaw = 215f;
    float pitch = 14f;
    float distance;
    float cockpitYaw;
    float cockpitPitch = -4f;
    float cutDepth = 0.85f;

    // UI.
    TMP_Text titleText;
    TMP_Text subtitleText;
    readonly Dictionary<Mode, Button> modeButtons = new();
    RectTransform factsCard;
    TMP_Text factsText;
    RectTransform cutawayPanel;
    RectTransform partList;
    Slider cutSlider;
    RectTransform detailCard;
    TMP_Text detailText;
    RectTransform pinLayer;
    readonly List<(RectTransform pin, int index)> pins = new();
    RectTransform hud;
    RectTransform headingTape;
    readonly List<TMP_Text> headingLabels = new();
    RectTransform pitchLadder;
    RectTransform hudWindow;
    readonly List<(RectTransform rect, float degrees)> ladderRungs = new();
    TMP_Text cockpitNote;
    TMP_Text hint;

    // Input.
    bool dragging;
    Vector2 lastPointer;
    float lastPinch;

    void Awake()
    {
        ui = new UiKit(font, buttonSprite, panelSprite);
        ghostMaterial = new Material(ghostShader);
        BuildUi();
    }

    void Start()
    {
        ShowAircraft(0);
        SetMode(Mode.Exterior);
    }

    void OnDestroy()
    {
        Shader.SetGlobalFloat(CutEnabledId, 0);
    }

    // ------------------------------------------------------------- aircraft

    public void ShowAircraft(int index)
    {
        if (aircraft.Count == 0) return;
        CurrentIndex = (index % aircraft.Count + aircraft.Count) % aircraft.Count;
        ViewerAircraft entry = aircraft[CurrentIndex];

        if (model != null) Discard(model);
        if (ghost != null) Discard(ghost);
        if (parts != null) Discard(parts);
        skins.Clear();
        pinTargets.Clear();

        model = Instantiate(entry.prefab, stage);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        pilotEye = model.transform.Find("Pilot Eye");
        facts = ExhibitFacts.For(entry.plane.Name);

        bounds = VisibleBounds(model);
        distance = FramingSize() * 1.15f;

        foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>())
        {
            Material[] original = renderer.sharedMaterials;
            skins.Add((renderer, original, original.Select(CutawayMaterial).ToArray()));
        }

        // The removed half of the airframe, drawn as an X-ray outline in cutaway mode.
        ghost = Instantiate(entry.prefab, stage);
        ghost.name = "Ghost";
        foreach (Renderer renderer in ghost.GetComponentsInChildren<Renderer>())
        {
            renderer.sharedMaterials = Enumerable.Repeat(ghostMaterial, renderer.sharedMaterials.Length).ToArray();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        parts = BuildParts(model.transform);

        titleText.text = facts?.DisplayName ?? entry.plane.Name;
        factsText.text = FactsSummary(entry.plane);
        BuildPartList();
        HideDetail();

        // Keep the mode, but fall back from the cockpit if this aircraft has no interior.
        SetMode(CurrentMode == Mode.Cockpit && !entry.hasCockpitInterior ? Mode.Exterior : CurrentMode);
    }

    string FactsSummary(PlaneSO plane)
    {
        if (facts == null) return plane.Information;
        string crew = facts.Crew == 1 ? "1 pilot" : $"{facts.Crew} crew";
        return $"<b>Crew</b>  {crew}\n<b>Length</b>  {facts.LengthMetres:0.0} m\n<b>Engines</b>  {facts.Engines}\n<b>Gun</b>  {facts.Gun}";
    }

    Material CutawayMaterial(Material original)
    {
        if (original == null) return null;
        if (cutawayMaterials.TryGetValue(original, out Material existing)) return existing;

        Material material = new Material(cutawayShader) { name = $"{original.name} (Cutaway)" };
        if (original.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", original.GetTexture("_BaseMap"));
            material.SetTextureScale("_BaseMap", original.GetTextureScale("_BaseMap"));
            material.SetTextureOffset("_BaseMap", original.GetTextureOffset("_BaseMap"));
        }
        material.SetColor("_SectionColor", new Color(0.20f, 0.23f, 0.29f));
        if (original.HasProperty("_BaseColor"))
        {
            Color colour = original.GetColor("_BaseColor");
            colour.a = 1;
            material.SetColor("_BaseColor", colour);
        }
        cutawayMaterials[original] = material;
        return material;
    }

    GameObject BuildParts(Transform aircraftRoot)
    {
        GameObject root = new GameObject("Cutaway Parts");
        root.transform.SetParent(aircraftRoot, false);
        if (facts == null) return root;

        foreach (CutawayPart part in facts.Parts)
        {
            AddPart(root.transform, part, part.Position);
            if (part.Mirror)
            {
                AddPart(root.transform, part, new Vector3(-part.Position.x, part.Position.y, part.Position.z));
            }
            pinTargets.Add((ToLocal(part.Position), part));
        }
        return root;
    }

    static Vector3 ToLocal(Vector3 position) => new Vector3(position.x, position.y, -position.z);

    void AddPart(Transform parent, CutawayPart part, Vector3 position)
    {
        PrimitiveType type = part.Shape switch
        {
            PartShape.Engine or PartShape.Gun => PrimitiveType.Cylinder,
            PartShape.Radar => PrimitiveType.Cylinder,
            PartShape.Tank => PrimitiveType.Capsule,
            _ => PrimitiveType.Cube,
        };
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = part.Label;
        Discard(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = ToLocal(position);

        // Size is (width, height, length) in metres. Unity's cylinder and capsule run
        // along Y and are 2 units tall, so lay them along the fuselage.
        Vector3 size = part.Size;
        switch (type)
        {
            case PrimitiveType.Cylinder when part.Shape == PartShape.Radar:
                go.transform.localRotation = Quaternion.Euler(90, 0, 0);
                go.transform.localScale = new Vector3(size.x, size.z * 0.5f, size.y);
                break;
            case PrimitiveType.Cylinder:
            case PrimitiveType.Capsule:
                go.transform.localRotation = Quaternion.Euler(90, 0, 0);
                go.transform.localScale = new Vector3(size.x, size.z * 0.5f, size.y);
                break;
            default:
                go.transform.localScale = size;
                break;
        }

        Material material = new Material(cutawayShader);
        material.SetColor("_BaseColor", PartColour(part.Shape));
        material.SetFloat("_IgnoreCut", 1);
        material.SetFloat("_Emission", 0.35f);
        go.GetComponent<Renderer>().sharedMaterial = material;
    }

    static Color PartColour(PartShape shape) => shape switch
    {
        PartShape.Engine => new Color(0.95f, 0.6f, 0.25f),
        PartShape.Tank => new Color(0.3f, 0.6f, 0.95f),
        PartShape.Radar => new Color(0.85f, 0.85f, 0.9f),
        PartShape.Seat => new Color(0.9f, 0.3f, 0.3f),
        PartShape.Gun => new Color(0.55f, 0.58f, 0.62f),
        _ => new Color(0.45f, 0.85f, 0.55f),
    };

    // Edit-mode safe, so the store screenshot tools can drive this screen outside play mode.
    static void Discard(Object target)
    {
        if (Application.isPlaying) Destroy(target);
        else DestroyImmediate(target);
    }

    static Bounds VisibleBounds(GameObject root)
    {
        // Some models carry empty meshes far from the aircraft; skip anything without size.
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>().Where(r => r.bounds.size.sqrMagnitude > 1e-4f).ToArray();
        Bounds result = renderers.Length > 0 ? renderers[0].bounds : new Bounds(root.transform.position, Vector3.one);
        foreach (Renderer renderer in renderers) result.Encapsulate(renderer.bounds);
        return result;
    }

    // ------------------------------------------------------------------ modes

    public void SetMode(Mode mode)
    {
        if (mode == Mode.Cockpit && !aircraft[CurrentIndex].hasCockpitInterior)
        {
            mode = Mode.Exterior;
        }
        CurrentMode = mode;

        bool cut = mode == Mode.Cutaway;
        foreach (var skin in skins)
        {
            if (skin.renderer != null) skin.renderer.sharedMaterials = cut ? skin.cutaway : skin.original;
        }
        ghost.SetActive(cut);
        parts.SetActive(cut);
        Shader.SetGlobalFloat(CutEnabledId, cut ? 1 : 0);

        factsCard.gameObject.SetActive(mode == Mode.Exterior);
        cutawayPanel.gameObject.SetActive(cut);
        pinLayer.gameObject.SetActive(cut);
        hud.gameObject.SetActive(mode == Mode.Cockpit);
        if (!cut) HideDetail();

        viewCamera.fieldOfView = mode == Mode.Cockpit ? 62f : 40f;
        viewCamera.nearClipPlane = mode == Mode.Cockpit ? 0.03f : 0.3f;
        if (mode == Mode.Cockpit)
        {
            cockpitYaw = 0;
            cockpitPitch = -4f;
        }

        subtitleText.text = mode switch
        {
            Mode.Cutaway => "Cutaway — tap a number to learn what is inside",
            Mode.Cockpit => "Cockpit — drag to look around the pilot's seat",
            _ => "Drag to turn the aircraft, pinch to zoom",
        };

        foreach (var pair in modeButtons)
        {
            bool selected = pair.Key == mode;
            pair.Value.targetGraphic.color = selected ? UiKit.Cyan : Color.white;
            pair.Value.GetComponentInChildren<TMP_Text>().color = selected ? new Color(0.07f, 0.1f, 0.17f) : Color.white;
        }
        Button cockpitButton = modeButtons[Mode.Cockpit];
        cockpitButton.interactable = aircraft[CurrentIndex].hasCockpitInterior;
        cockpitNote.gameObject.SetActive(!aircraft[CurrentIndex].hasCockpitInterior && mode == Mode.Exterior);

        UpdateCamera();
    }

    // ------------------------------------------------------------------ frame

    void Update()
    {
        HandleInput();
        UpdateCamera();
        if (CurrentMode == Mode.Cutaway) UpdateCutPlane();
    }

    void LateUpdate()
    {
        if (CurrentMode == Mode.Cutaway) UpdatePins();
        if (CurrentMode == Mode.Cockpit) UpdateHud();
    }

    public void UpdateCamera()
    {
        if (model == null) return;

        if (CurrentMode == Mode.Cockpit && pilotEye != null)
        {
            Quaternion look = pilotEye.rotation * Quaternion.Euler(-cockpitPitch, cockpitYaw, 0);
            viewCamera.transform.SetPositionAndRotation(pilotEye.position, look);
            return;
        }

        Vector3 target = bounds.center;
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        viewCamera.transform.SetPositionAndRotation(target - rotation * Vector3.forward * distance, rotation);
    }

    public void UpdateCutPlane()
    {
        // A vertical plane along the fuselage, removing the half nearer the camera. The
        // slider moves it from just grazing the skin to slightly past the centreline.
        Vector3 axis = stage.right;
        float side = Mathf.Sign(Vector3.Dot(viewCamera.transform.position - stage.position, axis));
        Vector3 normal = axis * (side == 0 ? 1 : side);
        float halfWidth = FuselageHalfWidth();
        float offset = Mathf.Lerp(halfWidth, -0.15f * halfWidth, cutDepth);
        float d = Vector3.Dot(stage.position, normal) + offset;
        Shader.SetGlobalVector(CutPlaneId, new Vector4(normal.x, normal.y, normal.z, d));
    }

    float FuselageHalfWidth()
    {
        // Roughly the fuselage, not the wings: a fraction of the length suits every jet here.
        return (facts?.LengthMetres ?? 15f) * 0.09f;
    }

    // ------------------------------------------------------------------ input

    void HandleInput()
    {
        if (Touchscreen.current != null && HandleTouch()) return;
        HandleMouse();
    }

    bool HandleTouch()
    {
        List<TouchControl> active = Touchscreen.current.touches.Where(t => t.isInProgress).ToList();
        if (active.Count == 0)
        {
            dragging = false;
            lastPinch = 0;
            return false;
        }

        if (active.Count >= 2)
        {
            float pinch = Vector2.Distance(active[0].position.ReadValue(), active[1].position.ReadValue());
            if (lastPinch > 0 && CurrentMode != Mode.Cockpit)
            {
                Zoom(lastPinch / pinch);
            }
            lastPinch = pinch;
            dragging = false;
            return true;
        }

        lastPinch = 0;
        TouchControl touch = active[0];
        Vector2 position = touch.position.ReadValue();
        if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
        {
            dragging = !OverUi(position);
            lastPointer = position;
            return true;
        }

        if (dragging)
        {
            Drag(position - lastPointer);
        }
        lastPointer = position;
        return true;
    }

    void HandleMouse()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 position = mouse.position.ReadValue();
        if (mouse.leftButton.wasPressedThisFrame)
        {
            dragging = !OverUi(position);
        }
        else if (mouse.leftButton.isPressed && dragging)
        {
            Drag(position - lastPointer);
        }
        else if (!mouse.leftButton.isPressed)
        {
            dragging = false;
        }
        lastPointer = position;

        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f && CurrentMode != Mode.Cockpit && !OverUi(position))
        {
            Zoom(scroll > 0 ? 0.9f : 1.1f);
        }
    }

    void Drag(Vector2 delta)
    {
        // Screen pixels to degrees, independent of device resolution.
        float scale = 1000f / Mathf.Max(1, Screen.height);
        if (CurrentMode == Mode.Cockpit)
        {
            cockpitYaw = Mathf.Clamp(cockpitYaw - delta.x * LookSpeed * scale, -150, 150);
            cockpitPitch = Mathf.Clamp(cockpitPitch - delta.y * LookSpeed * scale, -60, 45);
        }
        else
        {
            yaw += delta.x * OrbitSpeed * scale;
            pitch = Mathf.Clamp(pitch - delta.y * OrbitSpeed * scale, -5, 80);
        }
    }

    // Length matters more than span here: framing the B-2's 52 m wingspan would leave
    // the aircraft itself tiny.
    float FramingSize()
    {
        return Mathf.Max(bounds.size.z, bounds.size.x * 0.55f);
    }

    void Zoom(float factor)
    {
        float size = FramingSize();
        distance = Mathf.Clamp(distance * factor, size * 0.45f, size * 2.2f);
    }

    static bool OverUi(Vector2 screenPosition)
    {
        if (EventSystem.current == null) return false;
        PointerEventData data = new PointerEventData(EventSystem.current) { position = screenPosition };
        List<RaycastResult> hits = new();
        EventSystem.current.RaycastAll(data, hits);
        return hits.Count > 0;
    }

    // --------------------------------------------------------------- cutaway UI

    void BuildPartList()
    {
        foreach (Transform child in partList.Cast<Transform>().ToList()) Discard(child.gameObject);
        foreach (var pin in pins) Discard(pin.pin.gameObject);
        pins.Clear();

        const float rowHeight = 30f;
        for (int i = 0; i < pinTargets.Count; i++)
        {
            int index = i;
            CutawayPart part = pinTargets[i].part;

            Button row = ui.Button(partList, $"Part {i + 1}", "", 14, () => ShowDetail(index), out TMP_Text label);
            row.targetGraphic.color = new Color(1, 1, 1, 0.08f);
            label.text = $"<color=#73E6FF><b>{i + 1}</b></color>   {part.Label}";
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            RectTransform rect = (RectTransform)row.transform;
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, -i * (rowHeight + 4));
            rect.sizeDelta = new Vector2(0, rowHeight);

            Button pinButton = ui.Button(pinLayer, $"Pin {i + 1}", $"{i + 1}", 14, () => ShowDetail(index), out TMP_Text pinLabel);
            Image pinImage = (Image)pinButton.targetGraphic;
            pinImage.sprite = null;
            Image disc = ui.Disc(pinButton.transform, "Disc", UiKit.Cyan);
            UiKit.Stretch(disc.rectTransform);
            disc.transform.SetAsFirstSibling();
            pinImage.color = new Color(0, 0, 0, 0);
            pinButton.targetGraphic = disc;
            pinLabel.color = new Color(0.07f, 0.1f, 0.17f);
            pinLabel.fontStyle = FontStyles.Bold;
            UiKit.Stretch(pinLabel.rectTransform);
            RectTransform pinRect = (RectTransform)pinButton.transform;
            pinRect.anchorMin = pinRect.anchorMax = Vector2.zero;
            pinRect.pivot = new Vector2(0.5f, 0.5f);
            pinRect.sizeDelta = new Vector2(26, 26);
            pins.Add((pinRect, i));
        }

        float listHeight = pinTargets.Count * (rowHeight + 4);
        cutawayPanel.sizeDelta = new Vector2(cutawayPanel.sizeDelta.x, listHeight + 96);
    }

    public void UpdatePins()
    {
        // From the canvas' own pixel rect, not Screen, so offscreen renders agree with the device.
        Vector2 canvasSize = canvas.pixelRect.size / canvas.scaleFactor;
        foreach (var (pin, index) in pins)
        {
            Vector3 world = model.transform.TransformPoint(pinTargets[index].position);
            Vector3 viewport = viewCamera.WorldToViewportPoint(world);
            bool visible = viewport.z > 0;
            pin.gameObject.SetActive(visible);
            if (!visible) continue;
            // Canvas units, whatever the scale factor.
            pin.anchoredPosition = new Vector2(viewport.x * canvasSize.x, viewport.y * canvasSize.y);
        }
    }

    public void ShowDetail(int index)
    {
        CutawayPart part = pinTargets[index].part;
        detailText.text = $"<color=#73E6FF><b>{index + 1}  {part.Label}</b></color>\n<size=85%>{part.Detail}</size>";
        detailCard.gameObject.SetActive(true);
    }

    void HideDetail()
    {
        if (detailCard != null) detailCard.gameObject.SetActive(false);
    }

    // -------------------------------------------------------------------- HUD

    public void UpdateHud()
    {
        // Conformal symbology: one degree of view is the same number of canvas units as
        // one degree of the world, so the horizon bar sits on the real horizon.
        Vector2 canvasSize = canvas.pixelRect.size / canvas.scaleFactor;
        float unitsPerDegree = canvasSize.y / viewCamera.fieldOfView;
        float horizontalFov = 2 * Mathf.Atan(Mathf.Tan(viewCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * viewCamera.aspect) * Mathf.Rad2Deg;
        float unitsPerDegreeX = canvasSize.x / horizontalFov;

        float heading = Mathf.Repeat(cockpitYaw, 360);
        for (int i = 0; i < headingLabels.Count; i++)
        {
            float mark = Mathf.Round(heading / 10f) * 10f + (i - headingLabels.Count / 2) * 10f;
            TMP_Text label = headingLabels[i];
            int shown = (int)Mathf.Repeat(mark, 360);
            label.text = shown % 30 == 0 ? (shown / 10).ToString("00") : "|";
            float x = (mark - heading) * unitsPerDegreeX;
            label.gameObject.SetActive(Mathf.Abs(x) < headingTape.rect.width * 0.5f - 12);
            label.rectTransform.anchoredPosition = new Vector2(x, 0);
        }

        float halfHeight = hudWindow.rect.height * 0.5f;
        foreach (var (rect, degrees) in ladderRungs)
        {
            float y = (degrees - cockpitPitch) * unitsPerDegree;
            rect.gameObject.SetActive(Mathf.Abs(y) < halfHeight - 8);
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
        }
    }

    // --------------------------------------------------------------- UI build

    void BuildUi()
    {
        Transform root = canvas.transform;

        // Top bar: back to the menu, aircraft picker.
        Button back = ui.Button(root, "Menu Button", "← Menu", 18, () => SceneManager.LoadScene(menuSceneIndex));
        UiKit.Anchor((RectTransform)back.transform, new Vector2(0, 1), new Vector2(SideInset, -14), new Vector2(120, 42));

        RectTransform picker = ui.Rect("Aircraft Picker", root);
        // 380 wide so the left arrow clears the Menu button on a 4:3 iPad canvas (800 units).
        UiKit.Anchor(picker, new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(380, 66));
        Button previous = ui.Button(picker, "Previous", "←", 22, () => ShowAircraft(CurrentIndex - 1));
        UiKit.Anchor((RectTransform)previous.transform, new Vector2(0, 0.5f), new Vector2(0, 4), new Vector2(52, 42));
        Button next = ui.Button(picker, "Next", "→", 22, () => ShowAircraft(CurrentIndex + 1));
        UiKit.Anchor((RectTransform)next.transform, new Vector2(1, 0.5f), new Vector2(0, 4), new Vector2(52, 42));
        titleText = ui.Text(picker, "Title", 24, TextAlignmentOptions.Center, "");
        titleText.fontStyle = FontStyles.Bold;
        titleText.textWrappingMode = TextWrappingModes.NoWrap;
        titleText.enableAutoSizing = true;
        titleText.fontSizeMin = 15;
        titleText.fontSizeMax = 24;
        titleText.rectTransform.anchorMin = new Vector2(0, 0.4f);
        titleText.rectTransform.anchorMax = new Vector2(1, 1);
        titleText.rectTransform.offsetMin = new Vector2(56, 0);
        titleText.rectTransform.offsetMax = new Vector2(-56, 0);
        subtitleText = ui.Text(picker, "Subtitle", 13, TextAlignmentOptions.Center, "");
        subtitleText.color = new Color(1, 1, 1, 0.75f);
        subtitleText.rectTransform.anchorMin = new Vector2(0, 0);
        subtitleText.rectTransform.anchorMax = new Vector2(1, 0.4f);
        subtitleText.rectTransform.offsetMin = new Vector2(-40, 0);
        subtitleText.rectTransform.offsetMax = new Vector2(40, 0);

        // Mode switch, bottom centre.
        RectTransform modes = ui.Rect("Modes", root);
        UiKit.Anchor(modes, new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(420, 46));
        string[] labels = { "Exterior", "Cutaway", "Cockpit" };
        for (int i = 0; i < 3; i++)
        {
            Mode mode = (Mode)i;
            Button button = ui.Button(modes, $"{labels[i]} Button", labels[i], 18, () => SetMode(mode));
            UiKit.Region((RectTransform)button.transform, new Vector2(i / 3f, 0), new Vector2((i + 1) / 3f, 1), 4);
            modeButtons[mode] = button;
        }

        cockpitNote = ui.Text(root, "Cockpit Note", 13, TextAlignmentOptions.Center,
            "This model has no cockpit interior, so the cockpit view is unavailable.");
        cockpitNote.color = new Color(1, 1, 1, 0.7f);
        UiKit.Anchor(cockpitNote.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 62), new Vector2(520, 22));

        // Exterior: a fact card on the left.
        factsCard = ui.Panel(root, "Facts Card", UiKit.Card).rectTransform;
        UiKit.Anchor(factsCard, new Vector2(0, 0), new Vector2(SideInset, 70), new Vector2(250, 150));
        factsText = ui.Text(factsCard, "Facts", 13, TextAlignmentOptions.TopLeft, "");
        factsText.lineSpacing = 6;
        UiKit.Stretch(factsText.rectTransform, 14, 12);

        // Cutaway: numbered pins over the model, a key on the left, a section slider.
        pinLayer = ui.Rect("Pins", root);
        UiKit.Stretch(pinLayer);
        pinLayer.SetSiblingIndex(0);

        cutawayPanel = ui.Panel(root, "Cutaway Key", UiKit.Card).rectTransform;
        UiKit.Anchor(cutawayPanel, new Vector2(0, 1), new Vector2(SideInset, -70), new Vector2(210, 300));
        TMP_Text keyTitle = ui.Text(cutawayPanel, "Key Title", 15, TextAlignmentOptions.TopLeft, "<b>Inside the aircraft</b>");
        keyTitle.rectTransform.anchorMin = new Vector2(0, 1);
        keyTitle.rectTransform.anchorMax = new Vector2(1, 1);
        keyTitle.rectTransform.pivot = new Vector2(0.5f, 1);
        keyTitle.rectTransform.anchoredPosition = new Vector2(0, -10);
        keyTitle.rectTransform.sizeDelta = new Vector2(-24, 22);
        partList = ui.Rect("Parts", cutawayPanel);
        partList.anchorMin = new Vector2(0, 0);
        partList.anchorMax = new Vector2(1, 1);
        partList.offsetMin = new Vector2(8, 60);
        partList.offsetMax = new Vector2(-8, -38);

        TMP_Text sliderLabel = ui.Text(cutawayPanel, "Section Label", 12, TextAlignmentOptions.BottomLeft, "Section depth");
        sliderLabel.color = new Color(1, 1, 1, 0.7f);
        sliderLabel.rectTransform.anchorMin = new Vector2(0, 0);
        sliderLabel.rectTransform.anchorMax = new Vector2(1, 0);
        sliderLabel.rectTransform.pivot = new Vector2(0.5f, 0);
        sliderLabel.rectTransform.anchoredPosition = new Vector2(0, 34);
        sliderLabel.rectTransform.sizeDelta = new Vector2(-24, 18);
        cutSlider = BuildSlider(cutawayPanel);
        cutSlider.value = cutDepth;
        cutSlider.onValueChanged.AddListener(v => cutDepth = v);

        detailCard = ui.Panel(root, "Part Detail", UiKit.Card).rectTransform;
        UiKit.Anchor(detailCard, new Vector2(1, 1), new Vector2(-SideInset, -70), new Vector2(280, 150));
        detailText = ui.Text(detailCard, "Detail", 16, TextAlignmentOptions.TopLeft, "");
        detailText.enableAutoSizing = true;
        detailText.fontSizeMin = 10;
        detailText.fontSizeMax = 16;
        UiKit.Stretch(detailText.rectTransform, 14, 12);
        Button closeDetail = ui.Button(detailCard, "Close", "×", 18, HideDetail);
        UiKit.Anchor((RectTransform)closeDetail.transform, new Vector2(1, 1), new Vector2(-6, -6), new Vector2(30, 30));

        BuildHud(root);
    }

    Slider BuildSlider(RectTransform parent)
    {
        RectTransform rect = ui.Rect("Section Slider", parent);
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(0.5f, 0);
        rect.anchoredPosition = new Vector2(0, 10);
        rect.sizeDelta = new Vector2(-28, 22);

        Image track = ui.Panel(rect, "Track", new Color(1, 1, 1, 0.2f)).GetComponent<Image>();
        track.rectTransform.anchorMin = new Vector2(0, 0.5f);
        track.rectTransform.anchorMax = new Vector2(1, 0.5f);
        track.rectTransform.sizeDelta = new Vector2(0, 6);
        track.sprite = null;

        RectTransform handleArea = ui.Rect("Handle Area", rect);
        UiKit.Stretch(handleArea, 10, 0);
        Image handle = ui.Disc(handleArea, "Handle", UiKit.Cyan);
        handle.raycastTarget = true;
        handle.rectTransform.sizeDelta = new Vector2(22, 0);

        Slider slider = rect.gameObject.AddComponent<Slider>();
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight;
        slider.navigation = new Navigation { mode = Navigation.Mode.None };
        // The slider needs a raycast target across its whole width to be draggable.
        Image hit = rect.gameObject.AddComponent<Image>();
        hit.color = new Color(0, 0, 0, 0);
        return slider;
    }

    void BuildHud(Transform root)
    {
        Color green = new Color(0.35f, 1f, 0.45f, 0.95f);

        hud = ui.Rect("HUD", root);
        UiKit.Stretch(hud);
        hud.SetSiblingIndex(1);

        RectTransform window = hudWindow = ui.Rect("HUD Window", hud);
        UiKit.Anchor(window, new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(360, 300));
        window.pivot = new Vector2(0.5f, 0.5f);
        window.gameObject.AddComponent<RectMask2D>();

        // Pitch ladder: a rung every 10 degrees, placed each frame by UpdateHud.
        pitchLadder = ui.Rect("Pitch Ladder", window);
        UiKit.Stretch(pitchLadder);
        for (int angle = -30; angle <= 30; angle += 10)
        {
            if (angle == 0) continue;
            foreach (float x in new[] { -70f, 70f })
            {
                Image line = ui.Panel(pitchLadder, $"Rung {angle}", green, rounded: false);
                UiKit.Anchor(line.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(x, 0), new Vector2(60, 2));
                line.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                line.raycastTarget = false;
                ladderRungs.Add((line.rectTransform, angle));
            }
            TMP_Text number = ui.Text(pitchLadder, $"Label {angle}", 12, TextAlignmentOptions.Center, Mathf.Abs(angle).ToString());
            number.color = green;
            UiKit.Anchor(number.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(116, 0), new Vector2(30, 16));
            number.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            ladderRungs.Add((number.rectTransform, angle));
        }
        Image horizon = ui.Panel(pitchLadder, "Horizon", green, rounded: false);
        UiKit.Anchor(horizon.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(280, 2));
        horizon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        horizon.raycastTarget = false;
        ladderRungs.Add((horizon.rectTransform, 0));

        // Boresight cross, fixed in the middle.
        TMP_Text boresight = ui.Text(window, "Boresight", 26, TextAlignmentOptions.Center, "–^–");
        boresight.color = green;
        UiKit.Anchor(boresight.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(80, 30));
        boresight.rectTransform.pivot = new Vector2(0.5f, 0.5f);

        // Heading tape across the top of the HUD window.
        headingTape = ui.Rect("Heading Tape", window);
        UiKit.Anchor(headingTape, new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(300, 20));
        headingTape.pivot = new Vector2(0.5f, 1);
        headingTape.gameObject.AddComponent<RectMask2D>();
        for (int i = 0; i < 7; i++)
        {
            TMP_Text label = ui.Text(headingTape, $"Heading {i}", 14, TextAlignmentOptions.Center, "");
            label.color = green;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            UiKit.Anchor(label.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30, 20));
            label.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            headingLabels.Add(label);
        }
        TMP_Text caret = ui.Text(window, "Heading Caret", 12, TextAlignmentOptions.Center, "^");
        caret.color = green;
        UiKit.Anchor(caret.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -26), new Vector2(20, 14));

        // Parked on the ramp: zero airspeed, ground altitude.
        TMP_Text speed = ui.Text(window, "Airspeed", 14, TextAlignmentOptions.Center, "0\nKT");
        speed.color = green;
        UiKit.Anchor(speed.rectTransform, new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(46, 40));
        TMP_Text altitude = ui.Text(window, "Altitude", 14, TextAlignmentOptions.Center, "GND\nALT");
        altitude.color = green;
        UiKit.Anchor(altitude.rectTransform, new Vector2(1, 0.5f), new Vector2(-8, 0), new Vector2(46, 40));

        hint = ui.Text(hud, "Cockpit Hint", 13, TextAlignmentOptions.Center,
            "Parked on the ramp. The head-up display shows heading, pitch, airspeed and altitude.");
        hint.color = new Color(1, 1, 1, 0.8f);
        UiKit.Anchor(hint.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 64), new Vector2(560, 22));
    }
}
