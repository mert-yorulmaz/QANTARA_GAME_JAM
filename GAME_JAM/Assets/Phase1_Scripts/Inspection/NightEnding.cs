using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ZeroHour.Phase2.Calls;
using ZeroHour.Shared;

// Oyunun kapanışı: gece vardiyası bitince ofis sahnesi bu modda açılır. Kamera kriz monitöründen geri çekilir,
// oyuncu kanepeye gidip E ile yatar, gözler kapanır (ekran kararır), teşekkür ve jenerik gelir, ana menüye dönülür.
// Sahneye elle eklenmez; GameEnding.Pending açıkken ofis yüklenince kendini kurar.
public class NightEnding : MonoBehaviour
{
    const string OfficeScene = "Phase1_Office";
    const string MainMenuScene = "MainMenu";
    const float SofaReach = 2.4f;

    CrisisMonitorExit monitor;
    WakeUpSequence wake;
    Camera cam;
    Transform body;
    float spawnYaw;
    float listenerVolume = 1f;

    CanvasGroup objective;
    Image blackout;
    CanvasGroup thanks;
    CanvasGroup credits;
    bool showingPrompt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Hook()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // sceneLoaded, sahnedeki Start'lardan önce gelir: uyanma sahnesi ve monitör girişi daha başlamadan kapatılır.
    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!GameEnding.Pending || scene.name != OfficeScene)
            return;

        GameEnding.Pending = false;
        new GameObject("[NightEnding]").AddComponent<NightEnding>().Setup();
    }

    void Setup()
    {
        monitor = FindFirstObjectByType<CrisisMonitorExit>();
        wake = FindFirstObjectByType<WakeUpSequence>();
        if (wake != null)
            wake.enabled = false;

        if (monitor != null)
        {
            monitor.enabled = false;
            cam = monitor.playerCamera;
            SetControls(false);
            foreach (var go in monitor.hideOnEnter)
                if (go != null)
                    go.SetActive(false);
        }

        foreach (var door in FindObjectsByType<DoorExit>(FindObjectsSortMode.None))
            door.enabled = false;
        DisableComputer();
        HideTasks();

        if (cam != null)
        {
            var fps = cam.GetComponent<FPSCamera>();
            body = fps != null && fps.playerBody != null ? fps.playerBody : cam.transform.parent;
            spawnYaw = cam.transform.eulerAngles.y;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        BuildUI();
    }

    IEnumerator Start()
    {
        if (cam != null && body != null && monitor != null && monitor.screen != null)
            yield return LeaveMonitor();

        if (cam != null && body != null && wake != null && wake.sofa != null)
        {
            yield return WaitForSleep();
            yield return LieDown();
        }

        yield return Credits();
    }

    // ---------------- 1. Monitörden çıkış ----------------

    IEnumerator LeaveMonitor()
    {
        Transform screen = monitor.screen.transform;
        Bounds b = monitor.screen.bounds;
        Vector3 normal = -screen.forward;
        if (Vector3.Dot(normal, cam.transform.position - b.center) < 0f)
            normal = -normal;

        // Oyuncu monitörün önünde, ona bakarak durur
        Vector3 flat = Vector3.ProjectOnPlane(normal, Vector3.up).normalized;
        Vector3 standPos = b.center + flat * 2.6f;
        standPos.y = body.position.y;
        Teleport(standPos, Quaternion.LookRotation(-flat, Vector3.up));

        Transform camT = cam.transform;
        Vector3 localPos = camT.localPosition;
        camT.localRotation = Quaternion.identity;
        Vector3 endPos = camT.position;
        Quaternion endRot = camT.rotation;

        // Ekranı dolduran kamera konumu (monitöre girişin tersi)
        float vHalf = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
        float hHalf = Mathf.Atan(Mathf.Tan(vHalf) * cam.aspect);
        float distance = Mathf.Min(screen.lossyScale.y * 0.5f / Mathf.Tan(vHalf), screen.lossyScale.x * 0.5f / Mathf.Tan(hHalf)) * monitor.fillFactor;
        Vector3 startPos = b.center + normal * Mathf.Max(0.05f, distance);
        Quaternion startRot = Quaternion.LookRotation(-normal, screen.up);

        SetBodyVisible(false);
        if (monitor.screenOn != null)
            monitor.screen.sharedMaterial = monitor.screenOn;
        camT.SetPositionAndRotation(startPos, startRot);

        yield return new WaitForSeconds(1.0f);

        const float duration = 2.2f;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / duration);
            camT.SetPositionAndRotation(Vector3.Lerp(startPos, endPos, k), Quaternion.Slerp(startRot, endRot, k));
            yield return null;
        }
        camT.localPosition = localPos;
        camT.localRotation = Quaternion.identity;

        // Gece bitti: kriz monitörü kapanır
        yield return new WaitForSeconds(0.4f);
        if (monitor.screenOff != null)
            monitor.screen.sharedMaterial = monitor.screenOff;
        Sfx.Play(SfxId.Click);

        SetBodyVisible(true);
        SetControls(true);
        DisableComputer();
        HideTasks();
    }

    // ---------------- 2. Kanepeye git ----------------

    IEnumerator WaitForSleep()
    {
        yield return Fade(objective, 0f, 1f, 0.6f);

        while (true)
        {
            if (!GamePause.IsMenuOpen && Keyboard.current != null)
            {
                bool near = NearSofa();
                if (near)
                {
                    ShowPrompt(Loc.T("[E] Sleep"));
                    if (Keyboard.current.eKey.wasPressedThisFrame)
                        break;
                }
                else
                {
                    HidePrompt();
                }

                // Sandalye alanının "[E] Otur" yazısı çıkmasın: bilgisayar kapalı
                var interaction = monitor.interactionSystem;
                if (!near && interaction != null && interaction.InChairZone && interaction.interactionPanel != null && interaction.interactionPanel.activeSelf)
                    interaction.HidePrompt();
            }
            yield return null;
        }

        HidePrompt();
        StartCoroutine(Fade(objective, objective.alpha, 0f, 0.4f));
    }

    bool NearSofa()
    {
        Vector3 center = SofaBounds().center;
        Vector3 offset = body.position - center;
        offset.y = 0f;
        if (offset.magnitude <= SofaReach)
            return true;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        return Physics.Raycast(ray, out RaycastHit hit, 4f) && hit.transform.IsChildOf(wake.sofa);
    }

    // ---------------- 3. Yat, gözler kapanır ----------------

    IEnumerator LieDown()
    {
        SetControls(false);
        SetBodyVisible(false);
        Sfx.Play(SfxId.ChairSit);

        // Uyanma sahnesindeki yatış pozu (WakeUpSequence ile aynı)
        Bounds sofa = SofaBounds();
        Vector3 sofaCenter = new Vector3(sofa.center.x, sofa.min.y + sofa.size.y * 0.6f, sofa.center.z);
        Vector3 lyingPos = sofaCenter + wake.headOffset;
        Quaternion lyingRot = Quaternion.Euler(wake.lyingEuler) * Quaternion.Euler(0f, spawnYaw, 0f);

        Transform camT = cam.transform;
        Vector3 startPos = camT.position;
        Quaternion startRot = camT.rotation;
        const float duration = 2.4f;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / duration);
            float drop = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t / duration - 0.25f) / 0.75f));
            Vector3 pos = Vector3.Lerp(startPos, lyingPos, k);
            pos.y = Mathf.Lerp(startPos.y, lyingPos.y, drop);
            camT.SetPositionAndRotation(pos, Quaternion.Slerp(startRot, lyingRot, k));
            yield return null;
        }

        yield return new WaitForSeconds(0.8f);
        Sfx.Play(SfxId.WakeBreath);

        // Göz kırpar gibi yavaşça kararır; sesler de kısılır
        listenerVolume = AudioListener.volume;
        const float close = 2.8f;
        for (float t = 0f; t < close; t += Time.deltaTime)
        {
            float k = t / close;
            SetBlack(Mathf.SmoothStep(0f, 1f, k));
            AudioListener.volume = listenerVolume * (1f - k);
            yield return null;
        }
        SetBlack(1f);
        AudioListener.volume = 0f;
    }

    // ---------------- 4. Teşekkür + jenerik ----------------

    IEnumerator Credits()
    {
        if (blackout.color.a < 1f)
        {
            listenerVolume = AudioListener.volume;
            for (float t = 0f; t < 1f; t += Time.deltaTime)
            {
                SetBlack(t);
                yield return null;
            }
            SetBlack(1f);
        }

        yield return new WaitForSeconds(0.8f);
        yield return Fade(thanks, 0f, 1f, 1.2f);
        yield return Hold(3f);
        yield return Fade(thanks, thanks.alpha, 0f, 0.9f);

        yield return Fade(credits, 0f, 1f, 1.2f);
        yield return Hold(5f);
        yield return Fade(credits, credits.alpha, 0f, 1.2f);
        yield return new WaitForSeconds(0.5f);

        AudioListener.volume = listenerVolume;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(MainMenuScene);
    }

    // Bir tuşa / tıklamaya basılırsa bekleme kısalır (ESC ayarlar için kalır)
    IEnumerator Hold(float seconds)
    {
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            if (t > 0.5f && SkipPressed())
                yield break;
            yield return null;
        }
    }

    static bool SkipPressed()
    {
        if (GamePause.IsMenuOpen)
            return false;
        var kb = Keyboard.current;
        if (kb != null && kb.anyKey.wasPressedThisFrame && !kb.escapeKey.wasPressedThisFrame)
            return true;
        var mouse = Mouse.current;
        return mouse != null && mouse.leftButton.wasPressedThisFrame;
    }

    // ---------------- Yardımcılar ----------------

    void SetControls(bool on)
    {
        if (monitor == null)
            return;
        foreach (var behaviour in monitor.disableOnEnter)
            if (behaviour != null)
                behaviour.enabled = on;
    }

    // Görev kutusu (TASKS / BALANCE) mesai içindir; TaskTracker kutuyu her karede açtığı için bileşen kapatılır.
    // SetControls(true) onu geri açabildiği için her seferinde tekrar çağrılır.
    static void HideTasks()
    {
        foreach (var tracker in FindObjectsByType<TaskTracker>(FindObjectsSortMode.None))
        {
            tracker.enabled = false;
            if (tracker.hudView != null)
                tracker.hudView.gameObject.SetActive(false);
        }
    }

    // Mesai bitti: bilgisayara oturulmaz
    static void DisableComputer()
    {
        foreach (var computer in FindObjectsByType<ComputerInteraction>(FindObjectsSortMode.None))
        {
            if (computer.computerCanvas != null)
                computer.computerCanvas.SetActive(false);
            computer.enabled = false;
        }
    }

    void SetBodyVisible(bool visible)
    {
        if (wake == null)
            return;
        foreach (var r in wake.hideWhileWaking)
            if (r != null)
                r.enabled = visible;
    }

    void Teleport(Vector3 position, Quaternion rotation)
    {
        var controller = body.GetComponent<CharacterController>();
        if (controller != null)
            controller.enabled = false;
        body.SetPositionAndRotation(position, rotation);
        if (controller != null)
            controller.enabled = true;
    }

    Bounds SofaBounds()
    {
        var renderers = wake.sofa.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(wake.sofa.position, Vector3.one);
        Bounds bounds = renderers[0].bounds;
        foreach (var r in renderers)
            bounds.Encapsulate(r.bounds);
        return bounds;
    }

    void ShowPrompt(string message)
    {
        var interaction = monitor.interactionSystem;
        if (interaction == null || interaction.interactionPanel == null)
            return;
        if (interaction.interactionText != null)
            interaction.interactionText.text = message;
        interaction.interactionPanel.SetActive(true);
        showingPrompt = true;
    }

    void HidePrompt()
    {
        if (!showingPrompt)
            return;
        showingPrompt = false;
        var interaction = monitor.interactionSystem;
        if (interaction != null && interaction.interactionPanel != null)
            interaction.interactionPanel.SetActive(false);
    }

    void SetBlack(float alpha)
    {
        blackout.color = new Color(0f, 0f, 0f, alpha);
    }

    static IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
    {
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            group.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        group.alpha = to;
    }

    // ---------------- Arayüz ----------------

    void BuildUI()
    {
        var canvasObj = new GameObject("NightEndingCanvas", typeof(RectTransform));
        canvasObj.transform.SetParent(transform, false);
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // Oyunun piksel fontu (görev kutusunun başlığıyla aynı)
        TMP_FontAsset font = null;
        var taskView = FindFirstObjectByType<TaskListView>(FindObjectsInactive.Include);
        if (taskView != null)
        {
            var taskText = taskView.GetComponentInChildren<TMP_Text>(true);
            if (taskText != null)
                font = taskText.font;
        }
        if (font == null)
            font = TMP_Settings.defaultFontAsset;

        // Görev: mesai bitti, kanepeye yat
        objective = Group("Objective", canvasObj.transform, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(1500f, 76f));
        var band = objective.gameObject.AddComponent<Image>();
        band.color = new Color(0.03f, 0.04f, 0.06f, 0.72f);
        band.raycastTarget = false;
        Label(objective.transform, font, Loc.T("Your shift is over. Lie down on the couch and get some sleep."), 32f, Color.white, Vector2.zero, new Vector2(1460f, 76f));

        blackout = new GameObject("Blackout", typeof(RectTransform)).AddComponent<Image>();
        blackout.transform.SetParent(canvasObj.transform, false);
        var bt = blackout.rectTransform;
        bt.anchorMin = Vector2.zero;
        bt.anchorMax = Vector2.one;
        bt.offsetMin = bt.offsetMax = Vector2.zero;
        blackout.raycastTarget = false;
        SetBlack(0f);

        var dim = new Color(0.72f, 0.75f, 0.8f);
        thanks = Group("Thanks", canvasObj.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1600f, 200f));
        Label(thanks.transform, font, Loc.T("THANKS FOR PLAYING"), 84f, Color.white, Vector2.zero, new Vector2(1600f, 140f));

        credits = Group("Credits", canvasObj.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1600f, 700f));
        Label(credits.transform, font, "SIGN", 120f, Color.white, new Vector2(0f, 220f), new Vector2(1600f, 150f));
        Label(credits.transform, font, Loc.T("DISASTER CONTROL LINE"), 34f, dim, new Vector2(0f, 120f), new Vector2(1600f, 50f));
        Label(credits.transform, font, Loc.T("Developed by"), 32f, dim, new Vector2(0f, -10f), new Vector2(1600f, 50f));
        Label(credits.transform, font, "QANTARA TEAM", 76f, Color.white, new Vector2(0f, -85f), new Vector2(1600f, 100f));
        Label(credits.transform, font, "Civil Game Jam 2026", 30f, dim, new Vector2(0f, -220f), new Vector2(1600f, 50f));
    }

    static CanvasGroup Group(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, anchor.y);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        var group = go.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        return group;
    }

    static void Label(Transform parent, TMP_FontAsset font, string text, float size, Color color, Vector2 position, Vector2 box)
    {
        var go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (font != null)
            tmp.font = font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = size * 0.5f;
        tmp.fontSizeMax = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        var rt = tmp.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = box;
    }
}
