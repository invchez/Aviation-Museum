using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Builds uGUI elements at runtime in the same style as the museum HUD, for screens whose
// content depends on data (quiz questions, badge grid, viewer part lists).
public class UiKit
{
    public static readonly Color Navy = new Color(0.07f, 0.10f, 0.17f, 0.97f);
    public static readonly Color Card = new Color(0.12f, 0.16f, 0.24f, 0.92f);
    public static readonly Color Cyan = new Color(0.45f, 0.9f, 1f, 1f);
    public static readonly Color Gold = new Color(1f, 0.78f, 0.3f, 1f);
    public static readonly Color Correct = new Color(0.3f, 0.78f, 0.45f, 1f);
    public static readonly Color Wrong = new Color(0.86f, 0.33f, 0.33f, 1f);
    public static readonly Color Muted = new Color(1f, 1f, 1f, 0.6f);

    readonly TMP_FontAsset font;
    readonly Sprite buttonSprite;
    readonly Sprite panelSprite;

    static Sprite circle;
    static Sprite ring;

    public UiKit(TMP_FontAsset font, Sprite buttonSprite, Sprite panelSprite)
    {
        this.font = font;
        this.buttonSprite = buttonSprite;
        this.panelSprite = panelSprite;
    }

    public RectTransform Rect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    public TMP_Text Text(Transform parent, string name, float size, TextAlignmentOptions alignment, string value)
    {
        RectTransform rect = Rect(name, parent);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = Color.white;
        text.text = value;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        return text;
    }

    public Image Panel(Transform parent, string name, Color colour, bool rounded = true)
    {
        RectTransform rect = Rect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        if (rounded && buttonSprite != null)
        {
            image.sprite = buttonSprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 3.2f;
        }
        image.color = colour;
        return image;
    }

    public Button Button(Transform parent, string name, string label, float fontSize, UnityAction onClick, out TMP_Text text)
    {
        RectTransform rect = Rect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = buttonSprite;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 3.2f;

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        if (onClick != null)
        {
            button.onClick.AddListener(onClick);
        }

        text = Text(rect, "Text", fontSize, TextAlignmentOptions.Center, label);
        Stretch(text.rectTransform, 10, 0);
        return button;
    }

    public Button Button(Transform parent, string name, string label, float fontSize, UnityAction onClick)
    {
        return Button(parent, name, label, fontSize, onClick, out _);
    }

    public Image PanelBackground(Transform parent, string name)
    {
        RectTransform rect = Rect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = panelSprite;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 2f;
        return image;
    }

    // A round badge: a filled disc with an optional outer ring.
    public Image Disc(Transform parent, string name, Color colour)
    {
        RectTransform rect = Rect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = CircleSprite();
        image.color = colour;
        image.raycastTarget = false;
        return image;
    }

    public Image Ring(Transform parent, string name, Color colour)
    {
        RectTransform rect = Rect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = RingSprite();
        image.color = colour;
        image.raycastTarget = false;
        return image;
    }

    // ------------------------------------------------------------------ layout

    public static void Stretch(RectTransform rect, float horizontal = 0, float vertical = 0)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(horizontal, vertical);
        rect.offsetMax = new Vector2(-horizontal, -vertical);
    }

    // Anchors to a single point; the pivot matches the anchor so position is an inset.
    public static void Anchor(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    // Fills the area between normalised anchors, inset by a margin in canvas units.
    public static void Region(RectTransform rect, Vector2 min, Vector2 max, float inset = 0)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }

    // ----------------------------------------------------------------- sprites

    static Sprite CircleSprite()
    {
        if (circle == null)
        {
            circle = BuildRadialSprite((r, edge) => Mathf.Clamp01((1f - r) / edge));
        }
        return circle;
    }

    static Sprite RingSprite()
    {
        if (ring == null)
        {
            ring = BuildRadialSprite((r, edge) => Mathf.Clamp01((1f - r) / edge) * Mathf.Clamp01((r - 0.84f) / edge));
        }
        return ring;
    }

    static Sprite BuildRadialSprite(System.Func<float, float, float> alpha)
    {
        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
        };
        float centre = (size - 1) / 2f;
        float edge = 1.5f / centre;
        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float r = Mathf.Sqrt((x - centre) * (x - centre) + (y - centre) * (y - centre)) / centre;
                pixels[y * size + x] = new Color(1, 1, 1, alpha(r, edge));
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
    }
}
