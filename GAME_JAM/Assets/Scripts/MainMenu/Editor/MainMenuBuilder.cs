using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.Video;
using ZeroHour.Phase2.Calls;
using ZeroHour.Phase2.UI;
using ZeroHour.Shared;

namespace ZeroHour.MainMenu.Editor
{
    /// <summary>
    /// SIGN ana menü sahnesini (Assets/Scenes/MainMenu.unity) Yağız'ın 00_main_menu tasarımından kurar.
    /// Tasarım 2560x1440 çizildi; yerleşim o piksellerle yapılır ve 1920x1080 tuvale 0.75 ölçekle sığar.
    /// Build sırası: MainMenu -> Phase1_Office -> SampleScene. Tekrar çalıştırılabilir (sahne baştan kurulur).
    /// </summary>
    public static class MainMenuBuilder
    {
        private const string ScenePath = "Assets/Scenes/MainMenu.unity";
        private const string UiDir = "Assets/UI/Dispatch_UI_Assets/";
        private const string MusicPath = "Assets/SOUNDS/sound effect_phase 1 background.mp3";
        private const string ClickPath = "Assets/SOUNDS/sound effect_mouse click.mp3";
        private const float DesignScale = 0.75f;

        private static readonly Color Ink = new Color(0.165f, 0.137f, 0.106f);
        private static readonly Color InkMuted = new Color(0.40f, 0.36f, 0.30f);
        private static readonly Color StampRed = new Color(0.72f, 0.22f, 0.18f);
        private static readonly Color Cream = new Color(0.93f, 0.89f, 0.78f);
        private static readonly Color BarCream = new Color32(0xEC, 0xDC, 0xB0, 0xFF);
        private static readonly Color TickerGray = new Color32(0x3A, 0x3A, 0x3A, 0xFF);

        private static TMP_FontAsset pixelFont;
        private static TMP_FontAsset readFont;

        [MenuItem("ZeroHour/9. Build Main Menu (SIGN)")]
        public static void Build()
        {
            if (EditorSceneManager.GetActiveScene().isDirty)
            {
                Debug.LogError("[MainMenu] Açık sahnede kaydedilmemiş değişiklik var. Önce kaydedin, sonra menüyü tekrar çalıştırın.");
                return;
            }

            pixelFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiDir + "06_fonts/DispatchPixelTR SDF.asset");
            readFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiDir + "06_fonts/IBMPlexSansCondensed-SemiBold SDF.asset");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Kamera (arayüz ekrana doğrudan çizilir; kamera sadece "kamera yok" uyarısı olmasın ve ses duyulsun diye)
            GameObject camObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camObj.tag = "MainCamera";
            Camera cam = camObj.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.cullingMask = 0;

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            // Tuval
            GameObject canvasObj = new GameObject("[SIGN_MainMenu]", typeof(RectTransform));
            canvasObj.layer = 5;
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();
            Transform root = canvasObj.transform;

            // Arka plan: ekranı doldurur (16:9 dışı ekranlarda kırpılır, boşluk kalmaz)
            Texture2D still = AssetDatabase.LoadAssetAtPath<Texture2D>(UiDir + "04_backgrounds/menu_city_night_1280x720.png");
            RawImage background = NewRaw("Background", root, still);
            RectTransform bgRt = background.rectTransform;
            bgRt.anchorMin = bgRt.anchorMax = new Vector2(0.5f, 0.5f);
            AspectRatioFitter fitter = background.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 16f / 9f;
            VideoPlayer video = background.gameObject.AddComponent<VideoPlayer>();
            video.clip = AssetDatabase.LoadAssetAtPath<VideoClip>(UiDir + "04_backgrounds/menu_background_loop_1280x720.mp4");
            video.playOnAwake = false;
            video.isLooping = true;
            video.audioOutputMode = VideoAudioOutputMode.None;

            // Geniş ekranlarda üst bant ve alt çubuk kenarlara kadar uzasın
            Image topStrip = NewImage("TopStrip", root, TickerGray);
            Anchor(topStrip.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), 82f * DesignScale);
            Image bottomStrip = NewImage("BottomStrip", root, BarCream);
            Anchor(bottomStrip.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), (1440f - 1221f) * DesignScale);

            // Tasarım alanı: 2560x1440 piksel koordinatı, ekran ortasında 0.75 ölçek
            RectTransform design = NewRect("Design", root);
            design.anchorMin = design.anchorMax = new Vector2(0.5f, 0.5f);
            design.pivot = new Vector2(0.5f, 0.5f);
            design.sizeDelta = new Vector2(2560f, 1440f);
            design.localScale = Vector3.one * DesignScale;

            // --- Üst "CANLI" haber bandı ---
            RawImage ticker = NewRaw("LiveTicker", design, AssetDatabase.LoadAssetAtPath<Texture2D>(UiDir + "03_ui_components/menu_live_ticker.png"));
            Place(ticker.rectTransform, 0, 0, 2560, 82);
            Localized(Fit(NewText("Live", design, "LIVE", pixelFont, 46, Color.white, 0, 0, 164, 80, TextAlignmentOptions.Center), 14f));

            RectTransform viewport = NewRect("TickerViewport", design);
            Place(viewport, 176, 0, 2560, 80);
            viewport.gameObject.AddComponent<RectMask2D>();
            TMP_Text tickerText = NewText("TickerText", viewport, "", readFont, 36, Color.white, 0, 0, 2400, 80, TextAlignmentOptions.MidlineLeft);
            RectTransform tickerRt = tickerText.rectTransform;
            tickerRt.anchorMin = tickerRt.anchorMax = new Vector2(0f, 0.5f);
            tickerRt.pivot = new Vector2(0f, 0.5f);
            tickerRt.anchoredPosition = Vector2.zero;

            // --- Alt vardiya dosyası çubuğu (sekme yazısı yok) ---
            RawImage bar = NewRaw("ShiftFileBar", design, AssetDatabase.LoadAssetAtPath<Texture2D>(UiDir + "03_ui_components/menu_bottom_bar_shift_file.png"));
            Place(bar.rectTransform, 0, 1127, 2560, 1440);

            NewText("Title", design, "SIGN", pixelFont, 112, Ink, 51, 1226, 563, 1338, TextAlignmentOptions.BottomLeft);
            Localized(Fit(NewText("Subtitle", design, "DISASTER CONTROL LINE", pixelFont, 44, InkMuted, 51, 1362, 563, 1410, TextAlignmentOptions.Left)));

            InfoBlock(design, "LINES", "4 open", 746, 905, 905);      // 4 hat (kesikli ayırıcılara değmesin)
            InfoBlock(design, "UNITS", "11 ready", 1048, 1205, 1205);  // 3 polis + 3 ambulans + 3 itfaiye + 2 helikopter
            InfoBlock(design, "CITY", "150 residents", 1356, 1490, 1700); // Etiket damgaya değmesin

            TMP_Text stamp = Localized(Fit(NewText("Stamp", design, "ON DUTY", pixelFont, 52, StampRed, 1530, 1238, 1755, 1316, TextAlignmentOptions.Center), 18f));
            stamp.rectTransform.localEulerAngles = new Vector3(0f, 0f, 7f); // Çubuktaki damga çerçevesi gibi eğik

            Button settingsButton = HitButton("SettingsButton", design, 1807, 1278, 1911, 1382);
            settingsButton.gameObject.AddComponent<OpenSettingsButton>();
            Button quitButton = HitButton("QuitButton", design, 1932, 1278, 2035, 1382);
            Button startButton = HitButton("StartShiftButton", design, 2056, 1278, 2511, 1382);
            Localized(Fit(NewText("Label", startButton.transform, "START SHIFT", pixelFont, 44, Cream, 110, 0, 430, 104, TextAlignmentOptions.Center)));

            // --- Geçiş: siyahtan açılır, VARDİYAYA BAŞLA ile kararır ---
            Image fade = NewImage("Fade", root, Color.black);
            Stretch(fade.rectTransform);
            CanvasGroup fadeGroup = fade.gameObject.AddComponent<CanvasGroup>();
            fadeGroup.blocksRaycasts = false;

            // --- Ses: Faz 1'in arka plan müziği + tıklama sesi ---
            GameObject musicObj = new GameObject("MenuMusic", typeof(AudioSource));
            AudioSource musicSource = musicObj.GetComponent<AudioSource>();
            musicSource.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(MusicPath);
            musicSource.loop = true;
            musicSource.playOnAwake = true;
            musicSource.spatialBlend = 0f;
            MusicVolume music = musicObj.AddComponent<MusicVolume>();

            GameObject sfxObj = new GameObject("MenuSfx", typeof(AudioSource));
            AudioSource sfx = sfxObj.GetComponent<AudioSource>();
            sfx.playOnAwake = false;
            sfx.spatialBlend = 0f;

            MainMenuUI ui = canvasObj.AddComponent<MainMenuUI>();
            SerializedObject so = new SerializedObject(ui);
            so.FindProperty("background").objectReferenceValue = background;
            so.FindProperty("fallbackImage").objectReferenceValue = still;
            so.FindProperty("video").objectReferenceValue = video;
            so.FindProperty("tickerText").objectReferenceValue = tickerText;
            so.FindProperty("startButton").objectReferenceValue = startButton;
            so.FindProperty("quitButton").objectReferenceValue = quitButton;
            SerializedProperty clicks = so.FindProperty("clickButtons");
            Button[] clickList = { startButton, quitButton, settingsButton };
            clicks.arraySize = clickList.Length;
            for (int i = 0; i < clickList.Length; i++) clicks.GetArrayElementAtIndex(i).objectReferenceValue = clickList[i];
            so.FindProperty("sfxSource").objectReferenceValue = sfx;
            so.FindProperty("clickClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(ClickPath);
            so.FindProperty("music").objectReferenceValue = music;
            so.FindProperty("fadeOverlay").objectReferenceValue = fadeGroup;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            SetBuildOrder();
            PlayerSettings.productName = "SIGN";

            Debug.Log("<color=green>[MainMenu] ✅ SIGN ana menüsü kuruldu: " + ScenePath + ". Build sırası: MainMenu → Phase1_Office → SampleScene.</color>");
        }

        private static void SetBuildOrder()
        {
            string[] order = { ScenePath, "Assets/Scenes/Phase1_Office.unity", "Assets/Scenes/SampleScene.unity" };
            var scenes = new List<EditorBuildSettingsScene>();
            foreach (string path in order) scenes.Add(new EditorBuildSettingsScene(path, true));
            foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
                if (System.Array.IndexOf(order, existing.path) < 0) scenes.Add(existing);
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // LINES / UNITS / CITY: üstte etiket, altta değer (ikonlar çubuk görselinde)
        private static void InfoBlock(Transform parent, string label, string value, float x0, float labelX1, float valueX1)
        {
            Localized(Fit(NewText("Label_" + label, parent, label, pixelFont, 38, InkMuted, x0, 1288, labelX1, 1328, TextAlignmentOptions.Left)));
            Localized(Fit(NewText("Value_" + label, parent, value, readFont, 36, Ink, x0, 1330, valueX1, 1370, TextAlignmentOptions.Left)));
        }

        // ---------------- Yardımcılar ----------------

        private static RectTransform NewRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Tasarım pikseliyle yerleştirir: (x0,y0) sol üst, (x1,y1) sağ alt köşe.</summary>
        private static void Place(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(x1 - x0, y1 - y0);
            rt.anchoredPosition = new Vector2((x0 + x1) * 0.5f, -(y0 + y1) * 0.5f);
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static void Anchor(RectTransform rt, Vector2 min, Vector2 max, float height)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = new Vector2(0.5f, min.y);
            rt.sizeDelta = new Vector2(0f, height);
            rt.anchoredPosition = Vector2.zero;
        }

        private static Image NewImage(string name, Transform parent, Color color)
        {
            Image img = NewRect(name, parent).gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = name == "Fade";
            return img;
        }

        private static RawImage NewRaw(string name, Transform parent, Texture texture)
        {
            RawImage img = NewRect(name, parent).gameObject.AddComponent<RawImage>();
            img.texture = texture;
            img.raycastTarget = false;
            return img;
        }

        private static TMP_Text NewText(string name, Transform parent, string text, TMP_FontAsset font, float size, Color color,
            float x0, float y0, float x1, float y1, TextAlignmentOptions alignment)
        {
            RectTransform rt = NewRect(name, parent);
            Place(rt, x0, y0, x1, y1);
            TextMeshProUGUI tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.raycastTarget = false;
            return tmp;
        }

        /// <summary>Türkçe metin daha uzun olabilir: kutuya sığana kadar küçülür.</summary>
        private static TMP_Text Fit(TMP_Text tmp, float sideMargin = 0f)
        {
            tmp.enableAutoSizing = true;
            tmp.fontSizeMax = tmp.fontSize;
            tmp.fontSizeMin = Mathf.Min(20f, tmp.fontSize);
            tmp.margin = new Vector4(sideMargin, 0f, sideMargin, 0f);
            return tmp;
        }

        /// <summary>Sabit yazıyı seçili dile bağlar (İngilizce kaynak metin anahtar olur).</summary>
        private static TMP_Text Localized(TMP_Text tmp)
        {
            LocalizedText loc = tmp.gameObject.AddComponent<LocalizedText>();
            SerializedObject so = new SerializedObject(loc);
            so.FindProperty("english").stringValue = tmp.text;
            so.ApplyModifiedPropertiesWithoutUndo();
            return tmp;
        }

        // Düğme görselleri çubukta çizili: üstüne görünmez, üzerine gelince hafif parlayan tıklama alanı
        private static Button HitButton(string name, Transform parent, float x0, float y0, float x1, float y1)
        {
            RectTransform rt = NewRect(name, parent);
            Place(rt, x0, y0, x1, y1);
            Image hit = rt.gameObject.AddComponent<Image>();
            hit.color = Color.white;
            Button button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = hit;
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0f);
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.14f);
            colors.pressedColor = new Color(0f, 0f, 0f, 0.18f);
            colors.selectedColor = new Color(1f, 1f, 1f, 0f);
            colors.colorMultiplier = 1f;
            button.colors = colors;
            Navigation nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;
            return button;
        }
    }
}
