using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Pencerenin sağ alt köşesindeki tutamaç: sürükleyince pencere büyür / küçülür.
// Pencere içeriği sabit yerleşimli olduğu için en ve boy birlikte (oran korunarak) değişir; yazılar bozulmaz.
// Pencere masaüstünün dışına taşmaz. Win7Window.Init her pencereye otomatik ekler.
public class WindowResizeHandle : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    public RectTransform window;
    public RectTransform bounds;
    public float minScale = 0.6f;
    public float maxScale = 1.6f;

    const float GripSize = 26f;
    static Sprite gripSprite;

    public static WindowResizeHandle Attach(RectTransform window, RectTransform bounds)
    {
        var go = new GameObject("ResizeGrip", typeof(RectTransform));
        go.layer = window.gameObject.layer;
        var rt = (RectTransform)go.transform;
        rt.SetParent(window, false);
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.sizeDelta = new Vector2(GripSize, GripSize);
        rt.anchoredPosition = new Vector2(-3f, 3f);

        var image = go.AddComponent<Image>();
        image.sprite = GripSprite();
        image.color = new Color(UIFactory.Ink.r, UIFactory.Ink.g, UIFactory.Ink.b, 0.85f);
        image.raycastTarget = true;

        var handle = go.AddComponent<WindowResizeHandle>();
        handle.window = window;
        handle.bounds = bounds;
        return handle;
    }

    void LateUpdate()
    {
        // İçerik sonradan eklense de tutamaç hep en üstte kalsın
        if (transform.GetSiblingIndex() != transform.parent.childCount - 1)
            transform.SetAsLastSibling();
    }

    public void OnBeginDrag(PointerEventData eventData) { }

    public void OnDrag(PointerEventData eventData)
    {
        if (window == null || bounds == null)
            return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(bounds, eventData.position, eventData.pressEventCamera, out Vector2 local))
            return;

        // bounds sol üst köşesine göre işaretçi konumu (pencere anchoredPosition ile aynı uzay)
        local -= new Vector2(-bounds.pivot.x * bounds.rect.width, (1f - bounds.pivot.y) * bounds.rect.height);

        Vector2 topLeft = window.anchoredPosition;
        Vector2 size = window.sizeDelta;
        float scaleX = (local.x - topLeft.x) / Mathf.Max(1f, size.x);
        float scaleY = (topLeft.y - local.y) / Mathf.Max(1f, size.y);
        float scale = (scaleX + scaleY) * 0.5f;

        // Masaüstünün dışına taşmasın
        float fitX = (bounds.rect.width - topLeft.x) / Mathf.Max(1f, size.x);
        float fitY = (bounds.rect.height + topLeft.y) / Mathf.Max(1f, size.y);
        scale = Mathf.Clamp(scale, minScale, Mathf.Min(maxScale, fitX, fitY));

        window.localScale = new Vector3(scale, scale, 1f);
    }

    // Köşede üç çapraz çizgi (Windows 7 boyutlandırma tutamacı)
    static Sprite GripSprite()
    {
        if (gripSprite != null)
            return gripSprite;

        const int size = 16;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        var clear = new Color(1f, 1f, 1f, 0f);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                // Sağ alt köşeye paralel çizgiler: x - y sabit (doku y'si aşağıdan yukarı)
                int d = x - y;
                bool line = (d == 3 || d == 4 || d == 8 || d == 9 || d == 13 || d == 14) && x >= y;
                tex.SetPixel(x, y, line ? Color.white : clear);
            }
        tex.Apply();
        gripSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return gripSprite;
    }
}
