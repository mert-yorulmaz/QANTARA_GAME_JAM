using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// BIA OS masaüstü (Windows 7 teması): simgeler, pencereler, görev çubuğu, saat ve bildirimler.
public class BiaDesktop : MonoBehaviour
{
    public ShiftClock clock;
    public RectTransform iconArea;
    public RectTransform windowLayer;

    [Header("Desktop")]
    public DesktopIcon[] icons;
    [Tooltip("taskButtons ile aynı sırada.")]
    public Win7Window[] windows;

    [Header("Taskbar")]
    public TaskbarButton[] taskButtons;
    public Button showDesktopButton;
    public TMP_Text clockText;
    public TMP_Text dateText;
    public TMP_Text periodText;
    public Image periodIcon;
    [Tooltip("Sabah, öğle, öğleden sonra, akşam.")]
    public Sprite[] periodIcons;

    [Header("Notification")]
    public RectTransform notification;
    public TMP_Text notificationTitle;
    public TMP_Text notificationBody;
    public Button notificationClose;
    public float notificationSeconds = 6f;

    Win7Window focused;
    DesktopIcon selectedIcon;
    Coroutine notificationRoutine;
    string pendingTitle;
    string pendingBody;
    readonly List<RaycastResult> raycastResults = new List<RaycastResult>();
    int shownPeriod = -1;

    void Awake()
    {
        foreach (var window in windows)
            window.Init(this);

        foreach (var icon in icons)
            icon.Init(this);

        for (int i = 0; i < taskButtons.Length; i++)
        {
            int index = i;
            taskButtons[i].button.onClick.AddListener(() => TaskbarClicked(windows[index]));
            taskButtons[i].gameObject.SetActive(false);
            windows[i].Opened += () => taskButtons[index].transform.SetAsLastSibling();
        }

        showDesktopButton.onClick.AddListener(ShowDesktop);
        notificationClose.onClick.AddListener(HideNotification);
        notification.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        // Masa kapalıyken gelen son bildirim oturunca gösterilir.
        if (pendingTitle != null)
        {
            Notify(pendingTitle, pendingBody);
            pendingTitle = pendingBody = null;
        }
    }

    void Update()
    {
        clockText.text = clock.TimeText;
        dateText.text = clock.DateText;

        int period = clock.Period;
        if (period != shownPeriod)
        {
            shownPeriod = period;
            periodText.text = clock.PeriodName;
            if (period < periodIcons.Length)
                periodIcon.sprite = periodIcons[period];
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            HandlePointerDown(Mouse.current.position.ReadValue());
    }

    // Pencerenin içindeki herhangi bir yere tıklamak onu öne getirir; boş masaüstüne tıklamak seçimi kaldırır.
    void HandlePointerDown(Vector2 screenPosition)
    {
        if (EventSystem.current == null)
            return;

        var pointer = new PointerEventData(EventSystem.current) { position = screenPosition };
        raycastResults.Clear();
        EventSystem.current.RaycastAll(pointer, raycastResults);
        if (raycastResults.Count == 0)
            return;

        var hit = raycastResults[0].gameObject;
        if (!hit.transform.IsChildOf(transform))
            return;

        Sfx.Play(SfxId.Click);

        var window = hit.GetComponentInParent<Win7Window>();
        if (window != null && window != focused)
            Focus(window);

        if (hit.GetComponentInParent<DesktopIcon>() == null)
            SelectIcon(null);
    }

    public void Focus(Win7Window window)
    {
        focused = window;
        window.transform.SetAsLastSibling();
        RefreshWindows();
    }

    public void WindowStateChanged(Win7Window window)
    {
        if (focused == window && !window.IsVisible)
            focused = TopVisibleWindow();
        RefreshWindows();
    }

    Win7Window TopVisibleWindow()
    {
        for (int i = windowLayer.childCount - 1; i >= 0; i--)
        {
            var window = windowLayer.GetChild(i).GetComponent<Win7Window>();
            if (window != null && window.IsVisible)
                return window;
        }
        return null;
    }

    void RefreshWindows()
    {
        for (int i = 0; i < windows.Length; i++)
        {
            var window = windows[i];
            bool isFocused = window == focused && window.IsVisible;
            taskButtons[i].gameObject.SetActive(window.IsOpen);
            taskButtons[i].SetActiveLook(isFocused);
        }
    }

    // Win7 davranışı: küçültülmüş ya da arkadaki pencereyi öne getirir, öndekini küçültür.
    void TaskbarClicked(Win7Window window)
    {
        if (window.IsVisible && window == focused)
            window.Minimize();
        else
            window.Open();
    }

    void ShowDesktop()
    {
        foreach (var window in windows)
            if (window.IsVisible)
                window.Minimize();
    }

    public void SelectIcon(DesktopIcon icon)
    {
        if (selectedIcon == icon)
            return;

        if (selectedIcon != null)
            selectedIcon.SetSelected(false);
        selectedIcon = icon;
        if (selectedIcon != null)
            selectedIcon.SetSelected(true);
    }

    public void Notify(string title, string body)
    {
        if (!isActiveAndEnabled)
        {
            pendingTitle = title;
            pendingBody = body;
            return;
        }

        Sfx.Play(SfxId.Notify);
        notificationTitle.text = title;
        notificationBody.text = body;
        notification.gameObject.SetActive(true);
        notification.SetAsLastSibling();

        if (notificationRoutine != null)
            StopCoroutine(notificationRoutine);
        notificationRoutine = StartCoroutine(NotificationRoutine());
    }

    IEnumerator NotificationRoutine()
    {
        var group = notification.GetComponent<CanvasGroup>();
        for (float t = 0f; t < 0.25f; t += Time.deltaTime)
        {
            group.alpha = t / 0.25f;
            yield return null;
        }
        group.alpha = 1f;

        yield return new WaitForSeconds(notificationSeconds);

        for (float t = 0f; t < 0.4f; t += Time.deltaTime)
        {
            group.alpha = 1f - t / 0.4f;
            yield return null;
        }
        HideNotification();
    }

    void HideNotification()
    {
        if (notificationRoutine != null)
            StopCoroutine(notificationRoutine);
        notificationRoutine = null;
        notification.gameObject.SetActive(false);
    }
}
