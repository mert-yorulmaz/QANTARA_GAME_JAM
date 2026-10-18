using TMPro;
using UnityEngine;
using UnityEngine.UI;

// UI parçalarını koddan kurmak için yardımcılar.
// Tüm konumlar üst-sol köşeden, tasarım çözünürlüğü (1280x720) biriminde.
public static class UIFactory
{
    public static readonly Color Paper = Hex("#ECDCB0");
    public static readonly Color Border = Hex("#B89B5E");
    public static readonly Color Ink = Hex("#2A2216");
    public static readonly Color Label = Hex("#6B5A36");
    public static readonly Color Navy = Hex("#1E3A5F");
    public static readonly Color Red = Hex("#A8241C");
    public static readonly Color Green = Hex("#2E6B34");

    public static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }

    public static RectTransform Rect(string name, Transform parent, float x, float y, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent != null ? parent.gameObject.layer : 5;

        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);
        return rt;
    }

    // Merkezden konumlanan (döndürülebilen) dikdörtgen.
    public static RectTransform CenterRect(string name, Transform parent, float cx, float cy, float w, float h, float rotation = 0f)
    {
        var rt = Rect(name, parent, cx, cy, w, h);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(cx, -cy);
        rt.localRotation = Quaternion.Euler(0f, 0f, rotation);
        return rt;
    }

    public static Image Image(string name, Transform parent, Sprite sprite, float x, float y, float w, float h, bool raycast = false)
    {
        var image = Rect(name, parent, x, y, w, h).gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = raycast;
        return image;
    }

    public static Button Button(string name, Transform parent, Sprite sprite, float x, float y, float w, float h)
    {
        var image = Image(name, parent, sprite, x, y, w, h, true);
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        var colors = button.colors;
        colors.highlightedColor = new Color(0.93f, 0.93f, 0.93f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.disabledColor = Color.white;
        button.colors = colors;
        return button;
    }

    public static TextMeshProUGUI Text(string name, Transform parent, TMP_FontAsset font, float size, Color color,
        string text, float x, float y, float w, float h, TextAlignmentOptions align, bool wrap = false)
    {
        var tmp = Rect(name, parent, x, y, w, h).gameObject.AddComponent<TextMeshProUGUI>();
        tmp.font = font;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.text = text;
        tmp.alignment = align;
        tmp.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;
        return tmp;
    }

    // Kesikli çizgi (panel üzerindeki ayraçlar gibi).
    public static RawImage DashedLine(string name, Transform parent, Color color, float x, float y, float w)
    {
        var image = Rect(name, parent, x, y, w, 1f).gameObject.AddComponent<RawImage>();
        image.texture = DashTexture();
        image.color = color;
        image.uvRect = new UnityEngine.Rect(0f, 0f, w / 8f, 1f);
        image.raycastTarget = false;
        return image;
    }

    static Texture2D dashTexture;

    static Texture2D DashTexture()
    {
        if (dashTexture != null)
            return dashTexture;

        dashTexture = new Texture2D(8, 1, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Repeat,
            hideFlags = HideFlags.DontSave
        };

        for (int x = 0; x < 8; x++)
            dashTexture.SetPixel(x, 0, x < 4 ? Color.white : Color.clear);

        dashTexture.Apply();
        return dashTexture;
    }
}
