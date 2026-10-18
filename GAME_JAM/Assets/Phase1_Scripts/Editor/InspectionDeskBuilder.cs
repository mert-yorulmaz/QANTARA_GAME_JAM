using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

// BIA OS (Windows 7 teması) masaüstünü monitör canvas'ına kurar ve sahneye mesai saati ile kapı çıkışını ekler.
// Dosya paneli ve kural kitabı konumları 01_screens referans görsellerinden ölçüldü (1280x720 tasarım birimi).
public static class InspectionDeskBuilder
{
    const string Root = "Assets/Dispatch/Phase1/";
    const string DeskName = "BIA_OS";
    const float DesktopHeight = 684f;

    static TMP_FontAsset pixel, medium, semibold;
    static Win7ArtBaker.Art art;
    static RectTransform windowLayer;


    [MenuItem("Dispatch/Build Inspection Desk UI")]
    public static void Build()
    {
        pixel = FontAsset("06_fonts/DispatchPixelTR");
        medium = FontAsset("06_fonts/IBMPlexSansCondensed-Medium");
        semibold = FontAsset("06_fonts/IBMPlexSansCondensed-SemiBold");
        if (pixel == null || medium == null || semibold == null)
            return;

        var computer = Object.FindFirstObjectByType<ComputerInteraction>();
        if (computer == null || computer.computerCanvas == null)
        {
            Debug.LogError("InspectionDeskBuilder: ComputerInteraction veya computerCanvas bulunamadı.");
            return;
        }

        art = Win7ArtBaker.Bake();
        var clock = SetupShiftClock();
        var door = SetupDoor(computer, clock);

        var host = (RectTransform)computer.computerCanvas.transform;
        Selection.activeObject = null;
        foreach (var oldName in new[] { DeskName, "InspectionDesk" })
        {
            var old = host.Find(oldName);
            if (old != null)
                Object.DestroyImmediate(old.gameObject);
        }

        // Monitör canvas'ı 1920x1080; tasarım 1280x720. Kamera canvas'a arkadan baktığı için 180° döndürülür.
        var root = UIFactory.Rect(DeskName, host, 0f, 0f, 1280f, 720f);
        root.anchorMin = root.anchorMax = root.pivot = new Vector2(0.5f, 0.5f);
        root.anchoredPosition = Vector2.zero;
        root.localRotation = Quaternion.Euler(0f, 180f, 0f);
        root.localScale = Vector3.one * (host.rect.width / 1280f);

        var desktop = root.gameObject.AddComponent<BiaDesktop>();
        var desk = root.gameObject.AddComponent<InspectionDesk>();
        desk.cases = InspectionRules.DefaultCases();
        desk.clock = clock;
        desk.desktop = desktop;
        desktop.clock = clock;

        UIFactory.Image("Wallpaper", root, art.wallpaper, 0f, 0f, 1280f, 720f, true);
        desktop.iconArea = UIFactory.Rect("Icons", root, 0f, 0f, 1280f, DesktopHeight);
        // Masaüstü görev notu: klasörlerin altında, pencerelerin arkasında.
        var desktopTasks = TaskBox(UIFactory.Rect("Gadgets", root, 0f, 0f, 1280f, DesktopHeight), 10f, 384f, 250f, 14f, 1f);
        windowLayer = UIFactory.Rect("Windows", root, 0f, 0f, 1280f, DesktopHeight);
        desktop.windowLayer = windowLayer;

        // Klasör pencereleri (taskbar düğmeleriyle aynı sırada). Çerçeve yok: her klasör kendi kâğıt paneli.
        var caseWindow = Window("CaseFilesWindow", "Case Files", art.folder, 290f, 64f, 820f, 556f);
        WindowButtons(caseWindow, S("03_ui_components/rule_book/btn_close.png"), 778f, 8f, 32f);
        BuildFile(caseWindow.transform, desk);
        BuildCaseStatus(caseWindow.transform, desk);

        var ruleWindow = Window("RuleBookWindow", "Rule Book", art.ruleBookFolder, 410f, 40f, 846f, 600f);
        BuildRuleBook(ruleWindow.transform, desk, ruleWindow);

        var archiveWindow = PaperWindow("ArchiveWindow", "ARCHIVE", art.folder, 300f, 130f, 600f, 320f, out var archiveClient);
        BuildArchive(archiveClient, desk);
        var reportWindow = PaperWindow("ShiftReportWindow", "SHIFT REPORT", art.folder, 360f, 170f, 420f, 340f, out var reportClient);
        BuildReport(reportClient, desk);
        var memoWindow = PaperWindow("AgencyMemosWindow", "AGENCY MEMOS", art.folder, 420f, 100f, 540f, 400f, out var memoClient);
        BuildMemo(memoClient);

        caseWindow.title = "Case Files";
        archiveWindow.title = "Archive";
        reportWindow.title = "Shift Report";
        memoWindow.title = "Agency Memos";
        desktop.windows = new[] { caseWindow, ruleWindow, archiveWindow, reportWindow, memoWindow };
        desk.caseWindow = caseWindow;
        desk.archiveWindow = archiveWindow;
        desk.reportWindow = reportWindow;

        // Masaüstü simgeleri: solda 4 klasör, sağ üstte kural kitabı.
        desktop.icons = new[]
        {
            Icon(desktop.iconArea, "case_files", "Case Files", art.folder, caseWindow, 10f, 10f),
            Icon(desktop.iconArea, "archive", "Archive", art.folder, archiveWindow, 10f, 100f),
            Icon(desktop.iconArea, "shift_report", "Shift Report", art.folder, reportWindow, 10f, 190f),
            Icon(desktop.iconArea, "agency_memos", "Agency Memos", art.folder, memoWindow, 10f, 280f),
            Icon(desktop.iconArea, "rule_book", "Rule Book", art.ruleBookFolder, ruleWindow, 1190f, 10f)
        };

        BuildTaskbar(root, desktop);
        BuildNotification(root, desktop);
        SetupTasks(computer, clock, door, desk, caseWindow, ruleWindow, desktopTasks);

        Undo.RegisterCreatedObjectUndo(root.gameObject, "Build Inspection Desk UI");
        EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
        Selection.activeGameObject = root.gameObject;
        Debug.Log("InspectionDeskBuilder: BIA OS kuruldu -> " + host.name + "/" + DeskName);
    }

    // ---------- Sahne ----------

    static ShiftClock SetupShiftClock()
    {
        var clock = Object.FindFirstObjectByType<ShiftClock>();
        if (clock == null)
        {
            var go = new GameObject("ShiftManager");
            Undo.RegisterCreatedObjectUndo(go, "Create ShiftManager");
            clock = go.AddComponent<ShiftClock>();
        }

        if (clock.audioSource == null)
        {
            clock.audioSource = clock.GetComponent<AudioSource>();
            if (clock.audioSource == null)
                clock.audioSource = clock.gameObject.AddComponent<AudioSource>();
            clock.audioSource.playOnAwake = false;
            clock.audioSource.spatialBlend = 0f;
        }
        EditorUtility.SetDirty(clock);
        return clock;
    }

    static DoorExit SetupDoor(ComputerInteraction computer, ShiftClock clock)
    {
        var door = Object.FindFirstObjectByType<SojaExiles.opencloseDoor>();
        if (door == null)
        {
            Debug.LogWarning("InspectionDeskBuilder: kapı (opencloseDoor) bulunamadı, DoorExit eklenmedi.");
            return null;
        }

        var exit = door.GetComponent<DoorExit>();
        if (exit == null)
            exit = Undo.AddComponent<DoorExit>(door.gameObject);
        exit.playerCamera = computer.playerCamera;
        exit.doorCollider = door.GetComponent<Collider>();
        exit.clock = clock;
        exit.interactionSystem = computer.interactionSystem;
        exit.door = door;

        var intro = Object.FindFirstObjectByType<IntroManager>();
        if (intro != null)
            exit.fadePanel = intro.fadePanel;

        exit.disableOnExit = new Behaviour[]
        {
            Object.FindFirstObjectByType<PlayerMovement>(),
            Object.FindFirstObjectByType<FPSCamera>()
        };
        EditorUtility.SetDirty(exit);
        return exit;
    }

    // Ekran köşesindeki görev kutusu, uyanma sahnesi ve görev takipçisi (her zaman aktif ayrı bir canvas).
    static void SetupTasks(ComputerInteraction computer, ShiftClock clock, DoorExit door, InspectionDesk desk,
        Win7Window caseWindow, Win7Window ruleWindow, TaskListView desktopTasks)
    {
        var old = GameObject.Find("GameHUD");
        if (old != null)
            Object.DestroyImmediate(old);

        var hud = new GameObject("GameHUD", typeof(RectTransform));
        hud.layer = 5;
        Undo.RegisterCreatedObjectUndo(hud, "Create GameHUD");

        // Karartma paneli (sıra 0) bu canvas'ın üstünde kalsın.
        var canvas = hud.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -1;
        var scaler = hud.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var hudTasks = TaskBox(hud.transform, 24f, 24f, 440f, 20f, 1.4f);

        var wake = hud.AddComponent<WakeUpSequence>();
        wake.playerCamera = computer.playerCamera.transform;
        var sofa = GameObject.Find("Office/Sofa_Apt_02");
        wake.sofa = sofa != null ? sofa.transform : null;
        if (sofa == null)
            Debug.LogWarning("InspectionDeskBuilder: Office/Sofa_Apt_02 bulunamadı, uyanma sahnesi atlanacak.");
        var movement = Object.FindFirstObjectByType<PlayerMovement>();
        wake.disableWhileWaking = new Behaviour[] { movement, Object.FindFirstObjectByType<FPSCamera>() };
        // Kamera kanepeden kalkarken oyuncunun kapsül gövdesinin içinden geçmesin.
        wake.hideWhileWaking = movement != null ? movement.GetComponents<Renderer>() : new Renderer[0];

        // Ses efektleri (mevcut müzik ve saat seslerinden bağımsız).
        hud.AddComponent<Sfx>();
        if (movement != null)
        {
            var steps = movement.GetComponent<Footsteps>();
            if (steps == null)
                steps = Undo.AddComponent<Footsteps>(movement.gameObject);
            steps.movement = movement;
            EditorUtility.SetDirty(steps);
        }

        var tracker = hud.AddComponent<TaskTracker>();
        tracker.wakeUp = wake;
        tracker.clock = clock;
        tracker.desk = desk;
        tracker.caseWindow = caseWindow;
        tracker.ruleBookWindow = ruleWindow;
        tracker.door = door;
        tracker.computerScreen = computer.computerCanvas;
        tracker.hudView = hudTasks;
        tracker.desktopView = desktopTasks;
    }

    // Görev kutusu: başlık, bakiye, para değişimi ve görev satırları. scale tüm ölçüleri büyütür.
    static TaskListView TaskBox(Transform parent, float x, float y, float width, float fontSize, float scale)
    {
        var background = UIFactory.Image("TaskBox", parent, art.hudPanel, x, y, width, 120f * scale);
        background.type = Image.Type.Sliced;
        var view = background.gameObject.AddComponent<TaskListView>();
        view.background = background.rectTransform;
        view.font = semibold;
        view.fontSize = fontSize;
        view.rowSpacing = 6f * scale;
        view.bottomPadding = 14f * scale;
        view.checkEmpty = art.checkEmpty;
        view.checkDone = art.checkDone;

        float pad = 16f * scale;
        var labelColor = UIFactory.Hex("#9FB4C4");
        UIFactory.Text("Title", background.transform, pixel, 16f * scale, Color.white, "TASKS", pad, 12f * scale, 120f * scale, 24f * scale, TextAlignmentOptions.Left);
        // Bakiye sağda; etiketi değerin hemen solunda, başlıkla çakışmaz.
        float valueWidth = 64f * scale;
        float valueX = width - pad - valueWidth;
        UIFactory.Text("BalanceLabel", background.transform, pixel, 10f * scale, labelColor, "BALANCE", valueX - 64f * scale, 16f * scale, 60f * scale, 18f * scale, TextAlignmentOptions.Right);
        view.balanceText = UIFactory.Text("Balance", background.transform, pixel, 16f * scale, Color.white, "$0", valueX, 11f * scale, valueWidth, 26f * scale, TextAlignmentOptions.Right);
        view.deltaText = UIFactory.Text("Delta", background.transform, semibold, 14f * scale, Color.white, "", valueX - 20f * scale, 34f * scale, valueWidth + 20f * scale, 18f * scale, TextAlignmentOptions.Right);
        view.deltaText.gameObject.SetActive(false);

        UIFactory.Image("Separator", background.transform, null, pad, 52f * scale, width - pad * 2f, Mathf.Max(1f, scale)).color = new Color(1f, 1f, 1f, 0.18f);
        view.rowsRoot = UIFactory.Rect("Rows", background.transform, pad, 62f * scale, width - pad * 2f, 10f);
        return view;
    }

    // ---------- Masaüstü ----------

    // Çerçevesiz klasör penceresi; içindeki herhangi bir yerden sürüklenir.
    static Win7Window Window(string name, string title, Sprite icon, float x, float y, float w, float h)
    {
        var root = UIFactory.Rect(name, windowLayer, x, y, w, h);
        var window = root.gameObject.AddComponent<Win7Window>();
        window.title = title;
        window.icon = icon;

        var drag = root.gameObject.AddComponent<WindowDragHandle>();
        drag.window = root;
        drag.bounds = windowLayer;
        return window;
    }

    // Kâğıt zeminli küçük klasör: başlık, çizgi, sağ üstte _ ve X.
    static Win7Window PaperWindow(string name, string heading, Sprite icon, float x, float y, float w, float h, out RectTransform client)
    {
        var window = Window(name, heading, icon, x, y, w, h);
        var paper = UIFactory.Image("Paper", window.transform, art.paperPanel, 0f, 0f, w, h, true);
        paper.type = Image.Type.Sliced;

        UIFactory.Image("Icon", window.transform, icon, 18f, 13f, 22f, 22f);
        UIFactory.Text("Heading", window.transform, pixel, 16f, UIFactory.Ink, heading, 48f, 12f, w - 150f, 24f, TextAlignmentOptions.Left);
        UIFactory.Image("Rule", window.transform, null, 18f, 44f, w - 36f, 2f).color = UIFactory.Ink;
        WindowButtons(window, S("03_ui_components/rule_book/btn_close.png"), w - 18f - 32f, 8f, 32f);

        client = UIFactory.Rect("Client", window.transform, 20f, 54f, w - 40f, h - 70f);
        return window;
    }

    // X (kapat) ve _ (simge durumu) düğmeleri; X'in sol tarafına _ gelir.
    static void WindowButtons(Win7Window window, Sprite closeSprite, float closeX, float y, float size)
    {
        window.closeButton = UIFactory.Button("Close", window.transform, closeSprite, closeX, y, size, size);
        window.minimizeButton = UIFactory.Button("Minimize", window.transform, art.paperMinimize, closeX - size - 6f, y, size, size);
    }

    static DesktopIcon Icon(Transform parent, string id, string label, Sprite sprite, Win7Window target, float x, float y)
    {
        var highlight = UIFactory.Image("Icon " + label, parent, art.iconHighlight, x, y, 80f, 86f, true);
        highlight.type = Image.Type.Sliced;
        var icon = highlight.gameObject.AddComponent<DesktopIcon>();
        icon.id = id;
        icon.target = target;
        icon.highlight = highlight;

        UIFactory.Image("Image", highlight.transform, sprite, 16f, 4f, 48f, 48f);
        UIFactory.Text("Shadow", highlight.transform, medium, 13f, new Color(0f, 0f, 0f, 0.75f), label, 3f, 55f, 76f, 30f, TextAlignmentOptions.Top, true);
        UIFactory.Text("Label", highlight.transform, medium, 13f, Color.white, label, 2f, 54f, 76f, 30f, TextAlignmentOptions.Top, true);
        return icon;
    }

    static void BuildTaskbar(Transform root, BiaDesktop desktop)
    {
        var bar = UIFactory.Image("Taskbar", root, art.taskbar, 0f, DesktopHeight, 1280f, 36f, true).transform;
        UIFactory.Image("StartOrb", bar, art.startOrb, 4f, -6f, 44f, 44f);

        var row = UIFactory.Rect("WindowButtons", bar, 56f, 3f, 1000f, 30f);
        var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 3f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = layout.childControlHeight = false;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;

        desktop.taskButtons = new TaskbarButton[desktop.windows.Length];
        for (int i = 0; i < desktop.windows.Length; i++)
        {
            var window = desktop.windows[i];
            var button = UIFactory.Button("Task " + window.title, row, art.taskButton, 0f, 0f, 160f, 30f);
            ((Image)button.targetGraphic).type = Image.Type.Sliced;

            var task = button.gameObject.AddComponent<TaskbarButton>();
            task.button = button;
            task.background = (Image)button.targetGraphic;
            task.normal = art.taskButton;
            task.active = art.taskButtonActive;
            task.icon = UIFactory.Image("Icon", button.transform, window.icon, 8f, 7f, 16f, 16f);
            task.label = UIFactory.Text("Label", button.transform, medium, 13f, Color.white, window.title, 30f, 0f, 124f, 30f, TextAlignmentOptions.Left);
            task.label.overflowMode = TextOverflowModes.Ellipsis;
            desktop.taskButtons[i] = task;
        }

        // Sistem tepsisi: gün dilimi, saat, tarih.
        desktop.periodIcon = UIFactory.Image("PeriodIcon", bar, art.periods[0], 1100f, 10f, 16f, 16f);
        desktop.periodIcons = art.periods;
        desktop.periodText = UIFactory.Text("Period", bar, medium, 13f, Color.white, "Morning", 1120f, 0f, 60f, 36f, TextAlignmentOptions.Left);
        desktop.clockText = UIFactory.Text("Time", bar, medium, 13f, Color.white, "08:00", 1180f, 2f, 84f, 17f, TextAlignmentOptions.Center);
        desktop.dateText = UIFactory.Text("Date", bar, medium, 13f, Color.white, "10.03.2026", 1180f, 18f, 84f, 17f, TextAlignmentOptions.Center);
        desktop.showDesktopButton = UIFactory.Button("ShowDesktop", bar, art.showDesktop, 1268f, 0f, 12f, 36f);
    }

    static void BuildNotification(Transform root, BiaDesktop desktop)
    {
        var balloon = UIFactory.Image("Notification", root, art.balloon, 938f, 580f, 330f, 96f, true);
        balloon.type = Image.Type.Sliced;
        balloon.gameObject.AddComponent<CanvasGroup>();
        desktop.notification = balloon.rectTransform;

        var emblem = UIFactory.Image("Emblem", balloon.transform, AssetDatabase.LoadAssetAtPath<Sprite>(Root + "05_sprites/agency_emblem.png"), 12f, 12f, 16f, 16f);
        emblem.color = UIFactory.Navy;
        desktop.notificationTitle = UIFactory.Text("Title", balloon.transform, semibold, 15f, UIFactory.Hex("#1E395B"), "", 36f, 8f, 260f, 22f, TextAlignmentOptions.Left);
        desktop.notificationBody = UIFactory.Text("Body", balloon.transform, medium, 13f, UIFactory.Hex("#333333"), "", 36f, 32f, 282f, 56f, TextAlignmentOptions.TopLeft, true);

        var close = UIFactory.Button("Close", balloon.transform, null, 304f, 6f, 18f, 18f);
        ((Image)close.targetGraphic).color = Color.clear;
        desktop.notificationClose = close;
        UIFactory.Text("X", close.transform, semibold, 16f, UIFactory.Hex("#6D6D6D"), "×", 0f, 0f, 18f, 18f, TextAlignmentOptions.Center);
    }

    // ---------- Pencere içerikleri ----------

    // Vaka / hata durumu: dosya sekmesinin sağındaki boşlukta, _ ve X'in solunda.
    static void BuildCaseStatus(Transform window, InspectionDesk desk)
    {
        desk.caseStatus = UIFactory.Text("CaseStatus", window, semibold, 14f, Color.white, "Case 1 of 5", 390f, 12f, 230f, 24f, TextAlignmentOptions.Right);
        desk.errorStatus = UIFactory.Text("ErrorStatus", window, semibold, 14f, UIFactory.Hex("#7FE0B5"), "Errors: 0", 630f, 12f, 100f, 24f, TextAlignmentOptions.Left);
    }

    static void BuildArchive(Transform client, InspectionDesk desk)
    {
        UIFactory.Text("Columns", client, semibold, 14f, UIFactory.Label, "Case<pos=70>Building<pos=300>Verdict<pos=430>Result", 0f, 0f, 560f, 24f, TextAlignmentOptions.Left);
        UIFactory.DashedLine("Separator", client, UIFactory.Border, 0f, 26f, 560f);
        desk.archiveText = UIFactory.Text("Rows", client, medium, 15f, UIFactory.Ink, "", 0f, 34f, 560f, 210f, TextAlignmentOptions.TopLeft);
        desk.archiveText.lineSpacing = 8f;
    }

    static void BuildReport(Transform client, InspectionDesk desk)
    {
        desk.reportText = UIFactory.Text("Report", client, medium, 16f, UIFactory.Ink, "", 0f, 4f, 380f, 266f, TextAlignmentOptions.TopLeft);
    }

    // Sabah notu gövdesi (mesai dosyalar bitince biter; sonra gece ekibi ve kriz monitörü)
    public const string MemoBody =
        "Welcome back, inspector. Review every file in the Case Files folder. Your shift ends when the last file is done.\n\n" +
        "• Approve a file only if it breaks no rule.\n" +
        "• To reject a file, cite every rule it breaks.\n" +
        "• Use X-RAY SCAN on every file. Damage is not always declared.\n" +
        "• The Rule Book folder holds the full regulations (press R).\n" +
        "• Check every field and the X-RAY before you decide.\n\n" +
        "When your files are done, buy your night crew on this computer, then go to the crisis monitor on the wall.\n\n" +
        "— BIA Management";

    static void BuildMemo(Transform client)
    {
        const string memo =
            "<b>FROM:</b> Central Building Inspection Agency\n" +
            "<b>TO:</b> Inspector, Desk 4\n" +
            "<b>SUBJECT:</b> Your shift\n\n" +
            MemoBody;
        var text = UIFactory.Text("Memo", client, medium, 15f, UIFactory.Ink, memo, 0f, 4f, 500f, 326f, TextAlignmentOptions.TopLeft, true);
        // Uzun metin klasörden taşmasın: kutuya sığana kadar küçülür
        text.enableAutoSizing = true;
        text.fontSizeMax = 15f;
        text.fontSizeMin = 11f;
        text.overflowMode = TextOverflowModes.Truncate;
    }

    static void BuildFile(Transform client, InspectionDesk desk)
    {
        var file = UIFactory.Image("InspectionFile", client, S("03_ui_components/inspection_file/inspection_file_panel.png"), 0f, 0f, 820f, 556f, true).transform;
        var labelColor = UIFactory.Label;

        desk.tabLabel = UIFactory.Text("TabLabel", file, pixel, 16f, UIFactory.Ink, "CASE #0001", 32f, 7f, 180f, 24f, TextAlignmentOptions.Left);
        UIFactory.Text("Title", file, pixel, 16f, UIFactory.Ink, "INSPECTION FILE", 29f, 47f, 300f, 24f, TextAlignmentOptions.Left);
        UIFactory.Text("Desk", file, semibold, 14f, UIFactory.Ink, "Desk 4 · Building Inspection Agency", 440f, 49f, 350f, 20f, TextAlignmentOptions.Right);

        // Panel görselinde X-RAY / REJECT / APPROVE butonları çizili; buton gizlenince kâğıt rengi örtü görünür.
        Cover(file, "XRayCover", 27f, 240f, 254f, 48f);
        Cover(file, "RejectCover", 27f, 489f, 164f, 52f);
        Cover(file, "ApproveCover", 617f, 489f, 176f, 52f);

        var content = UIFactory.Rect("Content", file, 0f, 0f, 820f, 556f);
        desk.fileContent = content.gameObject;

        // Fotoğraf kartı: bina fotoğrafı, X-RAY ızgarası ve işaretler, en üstte ataç.
        desk.photo = UIFactory.CenterRect("Photo", content, 153f, 147f, 230f, 102f, InspectionArt.CardTilt).gameObject.AddComponent<RawImage>();
        desk.photo.raycastTarget = false;
        desk.xrayGrid = UIFactory.Image("XRayGrid", content, S("03_ui_components/inspection_file/xray_overlay_grid.png"), 35f, 93f, 236f, 108f).gameObject;
        desk.xrayMarks = UIFactory.Rect("XRayMarks", content, 35f, 93f, 236f, 108f).gameObject.AddComponent<RawImage>();
        desk.xrayMarks.raycastTarget = false;
        UIFactory.Image("PaperClip", content, S("generated/paperclip.png"), 50.5f, 74f, 20f, 48f);
        desk.photoLabel = UIFactory.Text("PhotoLabel", UIFactory.CenterRect("PhotoLabelRect", content, 155f, 216f, 240f, 24f, InspectionArt.CardTilt),
            pixel, 16f, UIFactory.Ink, "FILE 01 · EXTERIOR", 0f, 0f, 240f, 24f, TextAlignmentOptions.Center);

        desk.xrayOff = S("03_ui_components/inspection_file/btn_xray_off.png");
        desk.xrayOn = S("03_ui_components/inspection_file/btn_xray_on.png");
        desk.xrayButton = UIFactory.Button("XRayButton", content, desk.xrayOff, 29f, 242f, 250f, 44f);
        desk.xrayLabel = UIFactory.Text("Label", desk.xrayButton.transform, pixel, 16f, UIFactory.Ink, "X-RAY SCAN", 72f, 0f, 170f, 44f, TextAlignmentOptions.Left);
        desk.xrayGlow = UIFactory.Image("XRayGlow", content, art.glowFrame, 20f, 233f, 268f, 62f);
        desk.xrayGlow.type = Image.Type.Sliced;
        desk.xrayGlow.color = UIFactory.Hex("#FFC107");

        // Bina bilgileri.
        string[] infoLabels = { "BUILDING", "ADDRESS", "FILED", "GROUND USE", "NOTES" };
        var infoValues = new TMP_Text[infoLabels.Length];
        for (int i = 0; i < infoLabels.Length; i++)
        {
            float cy = 101f + i * 32f;
            UIFactory.Text(infoLabels[i], content, pixel, 16f, labelColor, infoLabels[i], 306f, cy - 12f, 160f, 24f, TextAlignmentOptions.Left);
            infoValues[i] = UIFactory.Text(infoLabels[i] + "Value", content, medium, 20f, UIFactory.Ink, "-", 469f, cy - 12f, 322f, 24f, TextAlignmentOptions.Left);
        }
        desk.buildingText = infoValues[0];
        desk.addressText = infoValues[1];
        desk.filedText = infoValues[2];
        desk.groundUseText = infoValues[3];
        desk.notesText = infoValues[4];
        desk.notesText.fontSize = 18f;
        desk.notesText.textWrappingMode = TextWrappingModes.Normal;
        desk.notesText.alignment = TextAlignmentOptions.TopLeft;
        desk.notesText.rectTransform.sizeDelta = new Vector2(322f, 44f);
        desk.notesText.rectTransform.anchoredPosition = new Vector2(469f, -(229f - 11f));

        // Beyan tablosu.
        UIFactory.Text("DeclaredHeader", content, pixel, 16f, UIFactory.Ink, "DECLARED BY OWNER", 30f, 291f, 380f, 24f, TextAlignmentOptions.Left);
        UIFactory.Text("CheckHeader", content, pixel, 16f, new Color(labelColor.r, labelColor.g, labelColor.b, 0.75f), "CHECK AGAINST RULES", 440f, 291f, 350f, 24f, TextAlignmentOptions.Right);

        desk.signedText = TableRow(content, "SIGNED", 32f, 207f, 335f);
        desk.wallReportText = TableRow(content, "WALL REPORT", 32f, 207f, 370f);
        desk.bagText = TableRow(content, "BAG", 32f, 207f, 405f, true);
        desk.sealText = TableRow(content, "SEAL", 427f, 602f, 335f);
        desk.exitsText = TableRow(content, "EXITS", 427f, 602f, 370f);
        desk.attachedText = TableRow(content, "ATTACHED", 427f, 602f, 405f, true);

        // Kural kontrolü: her alanın solunda kutucuk; alana tıklamak onu ilgili kurala göre "kontrol edildi" yapar.
        desk.tickEmpty = art.paperTickEmpty;
        desk.tickDone = art.paperTickDone;
        desk.ruleChecks = new[]
        {
            RuleCheck(content, "FILED", 306f, 165f, 1, $"Rule 1.1 · Report Date: files filed before {desk.outdatedBefore} are outdated."),
            RuleCheck(content, "GROUND USE", 306f, 197f, 2, "Rule 2.2 · A shop, market, car dealer or warehouse on the ground floor needs a Wall Supplement."),
            RuleCheck(content, "SIGNED", 32f, 335f, 1, "Rule 1.2 · Seal and Signature: the owner must sign the file."),
            RuleCheck(content, "WALL REPORT", 32f, 370f, 2, "Rule 2.2 · Commercial ground floors must attach the Load-Bearing Wall Supplement."),
            RuleCheck(content, "BAG", 32f, 405f, 3, "Rule 3.3 · The emergency bag needs water, a flashlight, a first aid kit and a whistle."),
            RuleCheck(content, "SEAL", 427f, 335f, 1, "Rule 1.2 · Seal and Signature: the official agency seal is required."),
            RuleCheck(content, "EXITS", 427f, 370f, 3, "Rule 3.2 · Emergency exits must be open and accessible."),
            RuleCheck(content, "ATTACHED", 427f, 405f, 4, "Rule 4.1 · Money, gifts or envelopes are bribes. Letters are not.")
        };
        desk.checkHint = UIFactory.Text("CheckHint", content, semibold, 13f, UIFactory.Ink, "", 198f, 489f, 412f, 52f, TextAlignmentOptions.Center, true);
        desk.checkHint.lineSpacing = -6f;

        desk.summaryText = UIFactory.Text("Summary", file, pixel, 32f, UIFactory.Ink, "", 306f, 100f, 484f, 200f, TextAlignmentOptions.TopLeft, true);
        desk.summaryText.lineSpacing = 30f;
        desk.summaryText.gameObject.SetActive(false);

        // Karar butonları ve geri bildirim.
        desk.rejectButton = UIFactory.Button("RejectButton", file, S("03_ui_components/inspection_file/btn_reject.png"), 29f, 491f, 160f, 48f);
        UIFactory.Text("Label", desk.rejectButton.transform, pixel, 16f, UIFactory.Red, "REJECT", 54f, 0f, 100f, 48f, TextAlignmentOptions.Left);
        desk.approveButton = UIFactory.Button("ApproveButton", file, S("03_ui_components/inspection_file/btn_approve.png"), 619f, 491f, 172f, 48f);
        UIFactory.Text("Label", desk.approveButton.transform, pixel, 16f, Color.white, "APPROVE", 53f, 0f, 115f, 48f, TextAlignmentOptions.Left);
        desk.rejectGroup = desk.rejectButton.gameObject.AddComponent<CanvasGroup>();
        desk.approveGroup = desk.approveButton.gameObject.AddComponent<CanvasGroup>();
        desk.nextButton = UIFactory.Button("NextButton", file, S("03_ui_components/inspection_file/btn_next_file.png"), 607f, 491f, 184f, 48f);
        desk.nextLabel = UIFactory.Text("Label", desk.nextButton.transform, pixel, 16f, Color.white, "NEXT FILE >", 0f, 0f, 184f, 48f, TextAlignmentOptions.Center);
        desk.nextButton.gameObject.SetActive(false);

        desk.feedbackText = UIFactory.Text("Feedback", file, semibold, 16f, UIFactory.Green, "", 29f, 493f, 568f, 44f, TextAlignmentOptions.Left, true);
        desk.feedbackText.lineSpacing = -10f;

        // Damgalar.
        desk.approvedStamp = UIFactory.CenterRect("ApprovedStamp", file, 545f, 211f, 479f, 243f);
        UIFactory.Image("Frame", desk.approvedStamp, S("03_ui_components/inspection_file/stamp_approved.png"), 0f, 0f, 479f, 243f);
        UIFactory.Text("Text", UIFactory.CenterRect("Content", desk.approvedStamp, 239.5f, 121.5f, 420f, 80f, 10f),
            pixel, 52f, UIFactory.Hex("#2F4D70"), "APPROVED", 0f, 0f, 420f, 80f, TextAlignmentOptions.Center);
        desk.approvedStamp.gameObject.SetActive(false);

        desk.rejectedStamp = UIFactory.CenterRect("RejectedStamp", file, 548f, 223f, 477f, 264f);
        UIFactory.Image("Frame", desk.rejectedStamp, S("03_ui_components/inspection_file/stamp_rejected.png"), 0f, 0f, 477f, 264f);
        var rejectedInk = UIFactory.Hex("#B0332B");
        var rejectedContent = UIFactory.CenterRect("Content", desk.rejectedStamp, 238.5f, 132f, 420f, 130f, 10f);
        UIFactory.Text("Text", rejectedContent, pixel, 52f, rejectedInk, "REJECTED", 0f, 10f, 420f, 70f, TextAlignmentOptions.Center);
        desk.rejectedRules = UIFactory.Text("Rules", rejectedContent, pixel, 18f, rejectedInk, "RULE 2.1", 0f, 84f, 420f, 30f, TextAlignmentOptions.Center);
        desk.rejectedStamp.gameObject.SetActive(false);

        desk.rejectionSlip = BuildRejectionSlip(file);
    }

    // Solda kutucuk (tik aç/kapat) + alan adının üstünde görünmez bağlantı (kural kitabını açar).
    static InspectionDesk.RuleCheck RuleCheck(Transform parent, string field, float labelX, float cy, int page, string hint)
    {
        var tickButton = UIFactory.Button("Tick " + field, parent, art.paperTickEmpty, labelX - 18f, cy - 7f, 14f, 14f);
        // Küçük kutucuğa tıklamayı kolaylaştır.
        tickButton.targetGraphic.raycastPadding = new Vector4(-4f, -6f, -3f, -6f);

        var linkArea = UIFactory.Image("RuleLink " + field, parent, null, labelX, cy - 13f, 150f, 26f, true);
        linkArea.color = Color.clear;
        var link = linkArea.gameObject.AddComponent<Button>();

        // Üzerine gelince alan adı laciverte döner: kural kitabına bağlantı olduğu belli olsun.
        var label = parent.Find(field);
        link.targetGraphic = label != null ? label.GetComponent<Graphic>() : linkArea;
        var colors = link.colors;
        colors.highlightedColor = new Color(0.45f, 0.62f, 1f, 1f);
        colors.pressedColor = new Color(0.3f, 0.45f, 0.9f, 1f);
        colors.selectedColor = Color.white;
        link.colors = colors;

        return new InspectionDesk.RuleCheck
        {
            field = field, hint = hint, ruleBookPage = page,
            tickButton = tickButton, labelButton = link, tick = (Image)tickButton.targetGraphic
        };
    }

    static void Cover(Transform parent, string name, float x, float y, float w, float h)
    {
        UIFactory.Image(name, parent, null, x, y, w, h).color = UIFactory.Paper;
    }

    static TMP_Text TableRow(Transform parent, string label, float labelX, float valueX, float cy, bool multiLine = false)
    {
        UIFactory.Text(label, parent, pixel, 16f, UIFactory.Label, label, labelX, cy - 12f, 170f, 24f, TextAlignmentOptions.Left);
        var value = UIFactory.Text(label + "Value", parent, medium, 18f, UIFactory.Ink, "-", valueX, cy - 12f, 188f, 24f,
            multiLine ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.Left, multiLine);
        if (multiLine)
        {
            value.rectTransform.anchoredPosition = new Vector2(valueX, -(cy - 11f));
            value.rectTransform.sizeDelta = new Vector2(188f, 44f);
            value.lineSpacing = -8f;
        }
        return value;
    }

    static RejectionSlip BuildRejectionSlip(Transform file)
    {
        var slipImage = UIFactory.Image("RejectionSlip", file, S("03_ui_components/rejection_slip/rejection_slip_panel.png"), 21f, 294f, 778f, 246f, true);
        var slipTransform = slipImage.transform;
        var slip = slipImage.gameObject.AddComponent<RejectionSlip>();

        UIFactory.Text("Title", slipTransform, pixel, 16f, UIFactory.Red, "REJECTION SLIP", 22f, 15f, 300f, 24f, TextAlignmentOptions.Left);
        UIFactory.Text("Hint", slipTransform, semibold, 14f, UIFactory.Red, "Mark every rule this file breaks.", 405f, 17f, 350f, 20f, TextAlignmentOptions.Right);

        slip.chipNormal = S("03_ui_components/rejection_slip/chip_normal.png");
        slip.chipSelected = S("03_ui_components/rejection_slip/chip_selected.png");

        int count = InspectionRules.All.Length;
        slip.chips = new Button[count];
        slip.chipNumbers = new TMP_Text[count];
        slip.chipLabels = new TMP_Text[count];
        float[] columns = { 21.8f, 207.4f, 393f, 578.6f };
        float[] rows = { 59.5f, 119.5f };
        for (int i = 0; i < count; i++)
        {
            var rule = InspectionRules.All[i];
            var chip = UIFactory.Button("Chip " + InspectionRules.Code(rule), slipTransform, slip.chipNormal, columns[i % 4], rows[i / 4], 177f, 52f);
            slip.chips[i] = chip;
            slip.chipNumbers[i] = UIFactory.Text("Number", chip.transform, pixel, 20f, UIFactory.Ink, InspectionRules.Code(rule), 12f, 0f, 44f, 52f, TextAlignmentOptions.Left);
            slip.chipLabels[i] = UIFactory.Text("Label", chip.transform, semibold, 15f, UIFactory.Ink, InspectionRules.ChipLabel(rule), 56f, 0f, 115f, 52f, TextAlignmentOptions.Left, true);
            slip.chipLabels[i].lineSpacing = -6f;
        }

        slip.cancelButton = UIFactory.Button("CancelButton", slipTransform, S("03_ui_components/rejection_slip/btn_cancel.png"), 21.8f, 183.7f, 118f, 44f);
        UIFactory.Text("Label", slip.cancelButton.transform, pixel, 16f, UIFactory.Ink, "CANCEL", 0f, 0f, 118f, 44f, TextAlignmentOptions.Center);

        slip.ruleBookLink = UIFactory.Button("CheckRuleBook", slipTransform, S("03_ui_components/rejection_slip/link_check_rule_book.png"), 149f, 182.8f, 146f, 44f);
        var link = UIFactory.Text("Label", slip.ruleBookLink.transform, semibold, 14f, UIFactory.Ink, "Check rule book", 38f, 0f, 108f, 44f, TextAlignmentOptions.Left);
        link.fontStyle = FontStyles.Underline;

        slip.citingText = UIFactory.Text("Citing", slipTransform, semibold, 14f, UIFactory.Red, "", 290f, 195f, 191f, 20f, TextAlignmentOptions.Right);

        slip.confirmDisabled = S("03_ui_components/rejection_slip/btn_confirm_disabled.png");
        slip.confirmEnabled = S("03_ui_components/rejection_slip/btn_confirm_enabled.png");
        slip.confirmButton = UIFactory.Button("ConfirmButton", slipTransform, slip.confirmDisabled, 491.5f, 183.7f, 264f, 44f);
        slip.confirmLabel = UIFactory.Text("Label", slip.confirmButton.transform, pixel, 16f, Color.white, "CONFIRM REJECTION", 0f, 0f, 264f, 44f, TextAlignmentOptions.Center);

        slipImage.gameObject.SetActive(false);
        return slip;
    }

    static void BuildRuleBook(Transform client, InspectionDesk desk, Win7Window window)
    {
        var bookRoot = UIFactory.Rect("Book", client, 0f, 4f, 792f, 592f);
        var book = bookRoot.gameObject.AddComponent<RuleBook>();
        book.window = window;
        book.pages = RuleBook.DefaultPages(desk.outdatedBefore);
        book.pixelFont = pixel;
        book.bodyFont = medium;
        book.boldFont = semibold;
        book.badgeNavy = S("03_ui_components/rule_book/rule_badge_navy.png");
        book.badgeRed = S("03_ui_components/rule_book/rule_badge_red.png");
        book.verdictTag = S("03_ui_components/rule_book/verdict_tag_reject.png");

        // Sekmeler kitabın arkasında kalır.
        book.tabs = new Button[5];
        book.tabLabels = new TMP_Text[5];
        book.tabActive = new Sprite[5];
        book.tabInactive = new Sprite[5];
        for (int i = 0; i < 5; i++)
        {
            book.tabActive[i] = S($"03_ui_components/rule_book/tab_{i + 1}_active.png");
            book.tabInactive[i] = S($"03_ui_components/rule_book/tab_{i + 1}_inactive.png");
            book.tabs[i] = UIFactory.Button($"Tab {i + 1}", bookRoot, book.tabInactive[i], 768f, 70f + i * 60f, 70f, 52f);
            book.tabLabels[i] = UIFactory.Text("Number", book.tabs[i].transform, pixel, 16f, UIFactory.Paper, (i + 1).ToString(), 22f, 0f, 30f, 52f, TextAlignmentOptions.Center);
        }

        var panel = UIFactory.Image("Panel", bookRoot, S("03_ui_components/rule_book/rule_book_panel.png"), 0f, 0f, 792f, 592f, true).transform;
        UIFactory.Text("Title", panel, pixel, 16f, UIFactory.Ink, "REGULATIONS HANDBOOK", 97f, 24f, 500f, 24f, TextAlignmentOptions.Left);
        UIFactory.Text("Subtitle", panel, medium, 15f, UIFactory.Label, "Central Building Inspection Agency", 97f, 49f, 500f, 20f, TextAlignmentOptions.Left);

        book.closeButton = UIFactory.Button("CloseButton", panel, S("03_ui_components/rule_book/btn_close.png"), 705f, 29f, 40f, 40f);
        window.closeButton = book.closeButton;
        window.minimizeButton = UIFactory.Button("MinimizeButton", panel, art.paperMinimize, 659f, 29f, 40f, 40f);

        book.chapterLabel = UIFactory.Text("ChapterLabel", panel, pixel, 16f, UIFactory.Red, "CHAPTER 2", 36f, 97f, 500f, 24f, TextAlignmentOptions.Left);
        book.chapterTitle = UIFactory.Text("ChapterTitle", panel, semibold, 24f, UIFactory.Ink, "", 35f, 118f, 700f, 32f, TextAlignmentOptions.Left);
        book.entriesRoot = UIFactory.Rect("Entries", panel, 35f, 161f, 710f, 350f);

        book.pageIndicator = UIFactory.Text("PageIndicator", panel, pixel, 16f, UIFactory.Label, "PAGE 1 / 5", 296f, 527f, 200f, 24f, TextAlignmentOptions.Center);
        book.prevButton = UIFactory.Button("PrevButton", panel, S("03_ui_components/rule_book/btn_prev.png"), 35f, 519f, 110f, 40f);
        UIFactory.Text("Label", book.prevButton.transform, pixel, 16f, UIFactory.Ink, "< PREV", 0f, 0f, 110f, 40f, TextAlignmentOptions.Center);
        book.nextButton = UIFactory.Button("NextButton", panel, S("03_ui_components/rule_book/btn_next.png"), 635f, 519f, 110f, 40f);
        UIFactory.Text("Label", book.nextButton.transform, pixel, 16f, UIFactory.Ink, "NEXT >", 0f, 0f, 110f, 40f, TextAlignmentOptions.Center);

        desk.ruleBook = book;
    }

    // ---------- Asset yardımcıları ----------

    static Sprite S(string relativePath)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + relativePath);
        if (sprite == null)
            Debug.LogError("InspectionDeskBuilder: sprite bulunamadı -> " + Root + relativePath);
        return sprite;
    }

    static TMP_FontAsset FontAsset(string relativePath)
    {
        string assetPath = Root + relativePath + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (existing != null)
            return existing;

        var font = AssetDatabase.LoadAssetAtPath<Font>(Root + relativePath + ".ttf");
        if (font == null)
        {
            Debug.LogError("InspectionDeskBuilder: font bulunamadı -> " + Root + relativePath + ".ttf");
            return null;
        }

        var asset = TMP_FontAsset.CreateFontAsset(font, 48, 6, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, false);
        asset.name = Path.GetFileName(relativePath);
        AssetDatabase.CreateAsset(asset, assetPath);

        asset.atlasTextures[0].name = asset.name + " Atlas";
        AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
        asset.material.name = asset.name + " Material";
        AssetDatabase.AddObjectToAsset(asset.material, asset);

        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        return asset;
    }
}
