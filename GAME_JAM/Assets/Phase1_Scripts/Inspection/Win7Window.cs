using System;
using UnityEngine;
using UnityEngine.UI;

// Masaüstünde açılan klasör penceresi: sürüklenir, simge durumuna küçülür, X ile kapanır.
public class Win7Window : MonoBehaviour
{
    public string title;
    public Sprite icon;
    public Button minimizeButton;
    public Button closeButton;

    public event Action Opened;
    public event Action Closed;

    public bool IsOpen { get; private set; }
    public bool IsMinimized { get; private set; }
    public bool IsVisible => IsOpen && !IsMinimized;

    BiaDesktop desktop;

    public void Init(BiaDesktop owner)
    {
        desktop = owner;
        minimizeButton.onClick.AddListener(Minimize);
        closeButton.onClick.AddListener(Close);

        // Pencere içeriği (ör. Case Files'taki dosya paneli) düğmelerin üstüne binip tıklamayı yutmasın
        minimizeButton.transform.SetAsLastSibling();
        closeButton.transform.SetAsLastSibling();

        // Sağ alt köşeden boyutlandırma (sahneyi yeniden kurmadan her pencereye eklenir)
        var drag = GetComponent<WindowDragHandle>();
        if (drag != null && GetComponentInChildren<WindowResizeHandle>(true) == null)
            WindowResizeHandle.Attach((RectTransform)transform, drag.bounds);

        gameObject.SetActive(false);
    }

    public void Open()
    {
        bool firstOpen = !IsOpen;
        if (firstOpen || IsMinimized)
            Sfx.Play(SfxId.FolderOpen);
        IsOpen = true;
        IsMinimized = false;
        gameObject.SetActive(true);

        if (firstOpen)
            Opened?.Invoke();

        desktop.Focus(this);
    }

    public void Minimize()
    {
        if (!IsOpen)
            return;

        IsMinimized = true;
        gameObject.SetActive(false);
        Sfx.Play(SfxId.Minimize);
        desktop.WindowStateChanged(this);
    }

    public void Close()
    {
        if (!IsOpen)
            return;

        IsOpen = false;
        IsMinimized = false;
        gameObject.SetActive(false);
        Sfx.Play(SfxId.FolderClose);
        Closed?.Invoke();
        desktop.WindowStateChanged(this);
    }
}
