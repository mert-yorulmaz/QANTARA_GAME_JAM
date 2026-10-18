using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Masaüstü simgesi: tıklayınca seçilir, çift tıklayınca penceresini açar, sürüklenip bırakıldığı yer kaydedilir.
public class DesktopIcon : MonoBehaviour, IPointerDownHandler, IPointerClickHandler,
    IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Tooltip("Konum kaydı için benzersiz anahtar.")]
    public string id;
    public Win7Window target;
    public Image highlight;

    const float DoubleClickTime = 0.4f;

    BiaDesktop desktop;
    RectTransform rect;
    Vector2 dragOffset;
    bool selected;
    bool hovered;
    bool dragged;
    float lastClickTime = -1f;

    string KeyX => $"BIAOS.Icon.{id}.x";
    string KeyY => $"BIAOS.Icon.{id}.y";

    public void Init(BiaDesktop owner)
    {
        desktop = owner;
        rect = (RectTransform)transform;

        if (PlayerPrefs.HasKey(KeyX) && PlayerPrefs.HasKey(KeyY))
            rect.anchoredPosition = Clamp(new Vector2(PlayerPrefs.GetFloat(KeyX), PlayerPrefs.GetFloat(KeyY)));

        Refresh();
    }

    public void SetSelected(bool value)
    {
        selected = value;
        Refresh();
    }

    void Refresh()
    {
        var c = highlight.color;
        c.a = selected ? 1f : hovered ? 0.55f : 0f;
        highlight.color = c;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
        Refresh();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        Refresh();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        dragged = false;
        desktop.SelectIcon(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (dragged)
            return;

        if (Time.unscaledTime - lastClickTime <= DoubleClickTime)
        {
            lastClickTime = -1f;
            if (target != null)
                target.Open();
        }
        else
        {
            lastClickTime = Time.unscaledTime;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragged = true;
        rect.SetAsLastSibling();
        if (TryLocal(eventData, out Vector2 local))
            dragOffset = rect.anchoredPosition - local;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (TryLocal(eventData, out Vector2 local))
            rect.anchoredPosition = Clamp(local + dragOffset);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        PlayerPrefs.SetFloat(KeyX, rect.anchoredPosition.x);
        PlayerPrefs.SetFloat(KeyY, rect.anchoredPosition.y);
        PlayerPrefs.Save();
    }

    Vector2 Clamp(Vector2 position)
    {
        var area = desktop.iconArea.rect;
        position.x = Mathf.Clamp(position.x, 0f, area.width - rect.sizeDelta.x);
        position.y = Mathf.Clamp(position.y, -(area.height - rect.sizeDelta.y), 0f);
        return position;
    }

    bool TryLocal(PointerEventData eventData, out Vector2 local)
    {
        var area = desktop.iconArea;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, eventData.position, eventData.pressEventCamera, out local))
            return false;

        local -= new Vector2(-area.pivot.x * area.rect.width, (1f - area.pivot.y) * area.rect.height);
        return true;
    }
}
