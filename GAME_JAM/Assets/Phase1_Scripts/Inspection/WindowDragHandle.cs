using UnityEngine;
using UnityEngine.EventSystems;

// Pencereyi sürükler; pencere masaüstünün (monitörün) tamamen içinde kalır.
public class WindowDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    public RectTransform window;
    public RectTransform bounds;

    Vector2 offset;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (TryLocal(eventData, out Vector2 local))
            offset = window.anchoredPosition - local;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!TryLocal(eventData, out Vector2 local))
            return;

        window.anchoredPosition = Clamp(local + offset);
    }

    public Vector2 Clamp(Vector2 position)
    {
        // Boyutlandırılmış pencerenin ekrandaki gerçek boyutu
        Vector2 size = Vector2.Scale(window.sizeDelta, window.localScale);
        position.x = Mathf.Clamp(position.x, 0f, Mathf.Max(0f, bounds.rect.width - size.x));
        position.y = Mathf.Clamp(position.y, -Mathf.Max(0f, bounds.rect.height - size.y), 0f);
        return position;
    }

    bool TryLocal(PointerEventData eventData, out Vector2 local)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                bounds, eventData.position, eventData.pressEventCamera, out local))
            return false;

        // bounds pivotu üst-sol olmalı; anchoredPosition ile aynı uzay.
        local -= new Vector2(-bounds.pivot.x * bounds.rect.width, (1f - bounds.pivot.y) * bounds.rect.height);
        return true;
    }
}
