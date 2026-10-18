using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using ZeroHour.Phase2.Calls;
using ZeroHour.Phase2.Core;
using ZeroHour.Phase2.Data;
using ZeroHour.Phase2.UI;
using ZeroHour.Phase2.Units;
using Phase2Clock = ZeroHour.Phase2.Calls.ShiftClock;

namespace ZeroHour.Phase2.Editor
{
    /// <summary>
    /// Tek tıkla: Top HUD + Gelen Çağrı kartı + Kaza Dosyası + Olay Rozetleri + Taktik Kamera.
    /// Yağız'ın Dispatch_UI_Assets PNG'lerini kullanır. Tekrar çalıştırılabilir (kendi oluşturduğunu yeniden kurar).
    /// </summary>
    public static class CallSystemSceneBuilder
    {
        private const string UiDir = "Assets/UI/Dispatch_UI_Assets/03_ui_components/";
        private const string FontDir = "Assets/UI/Dispatch_UI_Assets/06_fonts/";
        private const string PrefabDir = "Assets/Prefabs/Calls";
        private const string CanvasName = "[ZeroHour_Dispatch_Canvas]";
        private const string NightMusicName = "[ZeroHour_NightMusic]";
        private const string NightMusicPath = "Assets/SOUNDS/phase 2 background.mp3";
        private const string CommandSystemName = "[ZeroHour_CommandSystem]";

        // UI PNG'leri 1280x720 tasarımın 2 katı (2560x1440) dışa aktarıldı; 1920x1080 Canvas'ta 0.75 ölçek doğal boydur
        private const float UiScale = 0.75f;

        private static readonly Color Ink = new Color(0.165f, 0.137f, 0.106f);
        private static readonly Color InkMuted = new Color(0.40f, 0.36f, 0.30f);
        private static readonly Color Cream = new Color(0.93f, 0.89f, 0.78f);
        private static readonly Color StampRed = new Color(0.72f, 0.22f, 0.18f);
        private static readonly Color HudGreen = new Color(0.47f, 0.88f, 0.69f);
        private static readonly Color HudHeader = new Color(0.62f, 0.68f, 0.78f);
        private static readonly Color HudLabel = new Color(0.92f, 0.92f, 0.92f);
        private static readonly Color DeclineRed = new Color(0.88f, 0.32f, 0.27f);
        private static readonly Color AcceptInk = new Color(0.12f, 0.12f, 0.12f);

        private static TMP_FontAsset pixelFont;
        private static TMP_FontAsset readFont;

        [MenuItem("ZeroHour/8. Build Call System + Incident File UI (Tek Tık)")]
        public static void Build()
        {
            Debug.Log("<color=yellow>[ZeroHour] 📞 Çağrı sistemi, Kaza Dosyası ve HUD kuruluyor...</color>");

            // 1. Asset hazırlığı
            Sprite unitsPanel = LoadSprite("hud_units_panel.png");
            Sprite populationPanel = LoadSprite("hud_population_panel.png");
            Sprite ringingSprite = LoadSprite("call_card_ringing.png");
            Sprite listeningSprite = LoadSprite("call_card_listening.png");
            Sprite fillSprite = LoadSprite("call_progress_fill.png");
            Sprite filePanel = LoadSprite("incident_file_panel.png");
            Sprite fireMarker = LoadSprite("marker_fire.png");
            Sprite collapseMarker = LoadSprite("marker_collapse.png");

            pixelFont = GetOrCreateFont("DispatchPixelTR.ttf", "DispatchPixelTR SDF", 64, 6);
            readFont = GetOrCreateFont("IBMPlexSansCondensed-SemiBold.ttf", "IBMPlexSansCondensed-SemiBold SDF", 90, 9);

            IncidentMarker markerPrefab = GetOrCreateMarkerPrefab(fireMarker);

            // 2. Sahne düzeni
            Camera gameCamera = SetupCameras();
            SetupFleetTester();
            EnsureEventSystem();
            DisableLegacyCanvas();
            List<Transform> stationBuildings = BuildStationBeacons(markerPrefab.GetComponent<SpriteRenderer>().sharedMaterial);

            GameObject systemObj = FindRoot(CommandSystemName);
            if (systemObj == null)
            {
                systemObj = new GameObject(CommandSystemName);
                Undo.RegisterCreatedObjectUndo(systemObj, "Create Command System");
            }

            if (Object.FindFirstObjectByType<CityStatsManager>(FindObjectsInactive.Include) == null)
            {
                Undo.AddComponent<CityStatsManager>(systemObj);
            }

            IncidentBoard board = GetOrAdd<IncidentBoard>(systemObj);
            SerializedObject boardSo = new SerializedObject(board);
            boardSo.FindProperty("markerPrefab").objectReferenceValue = markerPrefab;
            boardSo.FindProperty("fireMarkerSprite").objectReferenceValue = fireMarker;
            boardSo.FindProperty("collapseMarkerSprite").objectReferenceValue = collapseMarker;
            boardSo.FindProperty("gasMarkerSprite").objectReferenceValue = GenerateMarkerSprite("marker_gas", new Color(1f, 0.45f, 0.15f), ExplosionIcon);
            boardSo.FindProperty("floodMarkerSprite").objectReferenceValue = GenerateMarkerSprite("marker_flood", new Color(0.25f, 0.65f, 1f), WaveIcon);
            boardSo.FindProperty("unrestMarkerSprite").objectReferenceValue = GenerateMarkerSprite("marker_unrest", new Color(0.66f, 0.42f, 1f), AlarmIcon);
            // Yağma ana afet değil: sadece deprem / sel sonrası takip ihbarı (doğrulanmamış rozet, bir kısmı asılsız)
            boardSo.FindProperty("unverifiedMarkerSprite").objectReferenceValue = LoadSprite("marker_unverified.png");
            boardSo.FindProperty("labelFont").objectReferenceValue = pixelFont;
            boardSo.FindProperty("fireWeight").floatValue = 25f;
            boardSo.FindProperty("collapseWeight").floatValue = 35f;
            boardSo.FindProperty("gasWeight").floatValue = 20f;
            boardSo.FindProperty("floodWeight").floatValue = 20f;
            boardSo.FindProperty("unrestWeight").floatValue = 0f;
            boardSo.FindProperty("maxActiveIncidents").intValue = 5;
            SerializedProperty excluded = boardSo.FindProperty("excludedBuildings");
            excluded.arraySize = stationBuildings.Count;
            for (int i = 0; i < stationBuildings.Count; i++) excluded.GetArrayElementAtIndex(i).objectReferenceValue = stationBuildings[i];
            boardSo.ApplyModifiedPropertiesWithoutUndo();

            // Afet efektleri: materyaller sahnede referanslı olsun ki build'e girsin
            DisasterFxLibrary fxLibrary = GetOrAdd<DisasterFxLibrary>(systemObj);
            BuildFxMaterials(fxLibrary);
            if (gameCamera != null) GetOrAdd<CameraShake>(gameCamera.gameObject);

            AudioSource callAudio = GetOrAdd<AudioSource>(systemObj);
            callAudio.playOnAwake = false;
            callAudio.spatialBlend = 0f;
            IncomingCallController calls = GetOrAdd<IncomingCallController>(systemObj);

            // Gece vardiyası müziği: döngüde, ayarlardaki müzik seviyesiyle (MusicVolume)
            GameObject musicObj = FindRoot(NightMusicName);
            if (musicObj == null)
            {
                musicObj = new GameObject(NightMusicName);
                Undo.RegisterCreatedObjectUndo(musicObj, "Create Night Music");
            }
            AudioSource music = GetOrAdd<AudioSource>(musicObj);
            music.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(NightMusicPath);
            music.loop = true;
            music.playOnAwake = true;
            music.spatialBlend = 0f;
            music.volume = 1f;
            GetOrAdd<ZeroHour.Shared.MusicVolume>(musicObj);

            // Zorluk: çağrılar ~%55 daha sık (20–30 sn → 12–19 sn), bekleyen her olay için +4 sn
            SerializedObject callTimingSo = new SerializedObject(calls);
            callTimingSo.FindProperty("firstCallDelay").floatValue = 4f;
            callTimingSo.FindProperty("minInterval").floatValue = 12f;
            callTimingSo.FindProperty("maxInterval").floatValue = 19f;
            callTimingSo.FindProperty("extraDelayPerWaitingIncident").floatValue = 4f;
            callTimingSo.ApplyModifiedPropertiesWithoutUndo();

            // 3. Canvas
            GameObject oldCanvas = FindRoot(CanvasName);
            if (oldCanvas != null) Undo.DestroyObjectImmediate(oldCanvas);

            GameObject canvasObj = new GameObject(CanvasName, typeof(RectTransform));
            canvasObj.layer = 5;
            Undo.RegisterCreatedObjectUndo(canvasObj, "Create Dispatch Canvas");

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            BuildHud(canvasObj.transform, unitsPanel, populationPanel);
            BuildPhoneFeed(canvasObj.transform);
            BuildCallCard(canvasObj.transform, calls, callAudio, ringingSprite, listeningSprite, fillSprite);
            BuildIncidentFile(canvasObj.transform, filePanel);

            // Phase2Clock takma adı: Faz 1'de isim alanı olmayan aynı adlı ShiftClock var ve düz "ShiftClock" onu seçer
            Phase2Clock shiftClock = GetOrAdd<Phase2Clock>(systemObj);
            BuildShiftHud(canvasObj.transform, shiftClock);
            BuildTutorialHints(canvasObj.transform);
            BuildDebrief(canvasObj.transform, shiftClock);
            LocalizeStaticTexts(canvasObj.transform);

            // Ayarlar sahneye değil, her sahnede ESC ile açılan ortak prefab'a kurulur (Faz 1, Faz 2, ana menü)
            BuildGlobalSettingsPrefab();

            // 4. Kaydet
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Selection.activeGameObject = canvasObj;

            Debug.Log($"<color=green>[ZeroHour] ✅ Çağrı sistemi kuruldu! Kamera: {(gameCamera != null ? gameCamera.name : "yok")}. Play'e basın: ilk çağrı ~4 sn sonra gelir.</color>");
        }

        // ==========================================
        // ASSET HAZIRLIĞI
        // ==========================================

        private static Sprite LoadSprite(string fileName)
        {
            return LoadSpriteAt(UiDir + fileName, false);
        }

        private static Sprite LoadSpriteAt(string path, bool pixelArt)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && pixelArt && importer.filterMode != FilterMode.Point)
            {
                // README: 05_sprites piksel simgeleri Point filtre ile import edilmeli
                importer.filterMode = FilterMode.Point;
                importer.textureType = TextureImporterType.Sprite;
                importer.SaveAndReimport();
            }
            if (importer == null)
            {
                Debug.LogWarning($"[ZeroHour] UI görseli bulunamadı: {path}");
                return null;
            }

            if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single || importer.mipmapEnabled)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static TMP_FontAsset GetOrCreateFont(string ttfName, string assetName, int samplingSize, int padding)
        {
            string assetPath = FontDir + assetName + ".asset";
            TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null) return existing;

            Font font = AssetDatabase.LoadAssetAtPath<Font>(FontDir + ttfName);
            if (font == null)
            {
                Debug.LogWarning($"[ZeroHour] Font bulunamadı: {ttfName}. Varsayılan TMP fontu kullanılacak.");
                return TMP_Settings.defaultFontAsset;
            }

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(font, samplingSize, padding, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            if (fontAsset == null) return TMP_Settings.defaultFontAsset;

            fontAsset.name = assetName;
            AssetDatabase.CreateAsset(fontAsset, assetPath);

            fontAsset.atlasTextures[0].name = assetName + " Atlas";
            AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);
            fontAsset.material.name = assetName + " Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);

            // Fontta olmayan karakterler (·, — vb.) için varsayılan TMP fontuna düş
            if (TMP_Settings.defaultFontAsset != null)
            {
                fontAsset.fallbackFontAssetTable = new List<TMP_FontAsset> { TMP_Settings.defaultFontAsset };
            }

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            return fontAsset;
        }

        private static IncidentMarker GetOrCreateMarkerPrefab(Sprite defaultSprite)
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder(PrefabDir);

            string matPath = PrefabDir + "/IncidentMarker_Unlit.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                mat = new Material(shader) { name = "IncidentMarker_Unlit" };
                AssetDatabase.CreateAsset(mat, matPath);
            }

            string prefabPath = PrefabDir + "/IncidentMarker.prefab";
            GameObject temp = new GameObject("IncidentMarker");
            SpriteRenderer sr = temp.AddComponent<SpriteRenderer>();
            sr.sharedMaterial = mat;
            sr.sprite = defaultSprite;
            SphereCollider col = temp.AddComponent<SphereCollider>();
            col.isTrigger = true;
            IncidentMarker marker = temp.AddComponent<IncidentMarker>();

            // Rozetin çevresindeki çember (sayısal geri sayım yok):
            // süre yayı saat yönünde azalır, ekip varınca aynı noktadan sarı iş yayı dolar.
            float spriteSize = defaultSprite != null ? Mathf.Max(defaultSprite.bounds.size.x, defaultSprite.bounds.size.y) : 1f;
            Sprite ringSprite = GetOrCreateRingSprite();

            GameObject ringObj = new GameObject("Ring", typeof(RectTransform));
            ringObj.transform.SetParent(temp.transform, false);
            Canvas ringCanvas = ringObj.AddComponent<Canvas>();
            ringCanvas.renderMode = RenderMode.WorldSpace;
            ringCanvas.sortingOrder = 499; // rozet ikonunun (500) arkasında, çevresinde
            RectTransform ringRt = (RectTransform)ringObj.transform;
            ringRt.sizeDelta = new Vector2(100f, 100f);
            ringRt.localPosition = Vector3.zero;
            ringRt.localScale = Vector3.one * (spriteSize * 1.55f / 100f);

            NewRingImage("Back", ringRt, ringSprite, new Color(0.05f, 0.06f, 0.08f, 0.6f), false);
            Image timeRing = NewRingImage("Time", ringRt, ringSprite, Color.white, true);
            Image workRing = NewRingImage("Work", ringRt, ringSprite, new Color(1f, 0.82f, 0.15f), true);
            workRing.fillAmount = 0f;

            // Çember "UI" katmanında: polaroid foto kamerası bu katmanı çizmez (fotoğrafı kaplamasın)
            int uiLayer = LayerMask.NameToLayer("UI");
            foreach (Transform t in ringObj.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = uiLayer;

            SerializedObject markerSo = new SerializedObject(marker);
            markerSo.FindProperty("timeRing").objectReferenceValue = timeRing;
            markerSo.FindProperty("workRing").objectReferenceValue = workRing;
            markerSo.ApplyModifiedPropertiesWithoutUndo();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(temp, prefabPath);
            Object.DestroyImmediate(temp);
            return saved.GetComponent<IncidentMarker>();
        }

        private static Image NewRingImage(string name, RectTransform parent, Sprite sprite, Color color, bool radial)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            Stretch((RectTransform)go.transform);
            Image img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            if (radial)
            {
                img.type = Image.Type.Filled;
                img.fillMethod = Image.FillMethod.Radial360;
                img.fillOrigin = (int)Image.Origin360.Top;
                img.fillClockwise = true;
                img.fillAmount = 1f;
            }
            return img;
        }

        /// <summary>Kenarı yumuşatılmış beyaz halka (rozet çemberi).</summary>
        private static Sprite GetOrCreateRingSprite()
        {
            EnsureFolder(FxDir);
            string path = FxDir + "/ui_ring.png";
            if (!System.IO.File.Exists(path))
            {
                const int Size = 128;
                const float Outer = 62f, Inner = 50f;
                Texture2D tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
                for (int y = 0; y < Size; y++)
                {
                    for (int x = 0; x < Size; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(Size * 0.5f, Size * 0.5f));
                        float a = Mathf.Clamp01(Outer - d) * Mathf.Clamp01(d - Inner);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                    }
                }
                System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
            }
            return LoadSpriteAt(path, false);
        }

        // ==========================================
        // YAĞIZ STİLİ PANEL GÖRSELLERİ (9-slice): düz Unity kutuları yerine
        // ==========================================

        /// <summary>
        /// Yağız'ın HUD panelleriyle aynı dil: koyu gri dolgu (#3A3A3A), 2 px koyu çerçeve (#1E1E1E).
        /// tan = Kaza Dosyasındaki ikon kutuları gibi yuvarlak köşeli açık kahve "çip".
        /// </summary>
        private static Sprite GetOrCreatePanelSprite(bool tanChip)
        {
            EnsureFolder(FxDir);
            string path = FxDir + (tanChip ? "/ui_chip_tan.png" : "/ui_panel_dark.png");
            if (!System.IO.File.Exists(path))
            {
                const int Size = 32;
                Texture2D tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
                Color fill = tanChip ? new Color32(214, 198, 160, 255) : new Color32(58, 58, 58, 255);
                Color border = tanChip ? new Color32(190, 172, 132, 255) : new Color32(30, 30, 30, 255);
                float radius = tanChip ? 7f : 0f;
                for (int y = 0; y < Size; y++)
                {
                    for (int x = 0; x < Size; x++)
                    {
                        // Yuvarlak köşe (sadece çip)
                        float cx = Mathf.Clamp(x + 0.5f, radius, Size - radius);
                        float cy = Mathf.Clamp(y + 0.5f, radius, Size - radius);
                        float corner = radius > 0f ? Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy)) : 0f;
                        float alpha = radius > 0f ? Mathf.Clamp01(radius - corner + 0.5f) : 1f;

                        bool edge = x < 2 || y < 2 || x >= Size - 2 || y >= Size - 2 || (radius > 0f && corner > radius - 2f);
                        Color c = edge ? border : fill;
                        c.a = alpha;
                        tex.SetPixel(x, y, c);
                    }
                }
                System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);

                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Point;
                importer.spriteBorder = tanChip ? new Vector4(9, 9, 9, 9) : new Vector4(4, 4, 4, 4);
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>Düz Image'ı Yağız stili 9-slice panele çevirir (2560 tasarım ölçeğinde çerçeve kalınlığı korunur).</summary>
        private static void StylePanel(Image img, bool tanChip)
        {
            img.sprite = GetOrCreatePanelSprite(tanChip);
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1f;
            img.color = Color.white;
        }

        // ==========================================
        // SAHNE DÜZENİ
        // ==========================================

        private static Camera SetupCameras()
        {
            // Sahnede iki MainCamera vardı. Oyunu gösteren, post-processing'li gece görünümü veren "Main Camera" kalır;
            // ikinci kamera "Main Camera (1)" silinmeden pasif yapılır (çift Audio Listener uyarısı da biter)
            GameObject gameCamObj = FindRoot("Main Camera");
            GameObject extraCamObj = FindRoot("Main Camera (1)");
            if (gameCamObj != null && extraCamObj != null)
            {
                if (!gameCamObj.activeSelf)
                {
                    Undo.RecordObject(gameCamObj, "Enable Game Camera");
                    gameCamObj.SetActive(true);
                }
                if (extraCamObj.activeSelf)
                {
                    Undo.RecordObject(extraCamObj, "Disable Extra Camera");
                    extraCamObj.SetActive(false);
                    Debug.Log("[ZeroHour] Fazladan 'Main Camera (1)' pasif yapıldı (silinmedi). Oyun kamerası: 'Main Camera'.");
                }
                TacticalCameraController stray = extraCamObj.GetComponent<TacticalCameraController>();
                if (stray != null) Undo.DestroyObjectImmediate(stray);
            }

            Camera cam = gameCamObj != null ? gameCamObj.GetComponent<Camera>() : Camera.main;
            if (cam == null) return null;

            TacticalCameraController controller = GetOrAdd<TacticalCameraController>(cam.gameObject);
            Vector3 p = cam.transform.position;
            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("panSpeed").floatValue = 70f;
            so.FindProperty("minPanLimits").vector2Value = new Vector2(p.x - 160f, p.z - 160f);
            so.FindProperty("maxPanLimits").vector2Value = new Vector2(p.x + 160f, p.z + 160f);
            so.FindProperty("zoomSpeed").floatValue = 6f;
            so.FindProperty("minZoomHeight").floatValue = Mathf.Max(30f, p.y - 90f);
            so.FindProperty("maxZoomHeight").floatValue = p.y + 110f;
            so.ApplyModifiedPropertiesWithoutUndo();
            return cam;
        }

        private static void SetupFleetTester()
        {
            AStarMovementTester tester = Object.FindFirstObjectByType<AStarMovementTester>(FindObjectsInactive.Include);
            if (tester == null)
            {
                Debug.LogWarning("[ZeroHour] AStarMovementTester bulunamadı! Önce 'ZeroHour/6. Setup A Star Movement Tester in Scene' çalıştırın.");
                return;
            }

            // Oyun modunda sol üstteki test paneli ve Space kısayolu kapalı (HUD ile çakışıyordu)
            SerializedObject so = new SerializedObject(tester);
            so.FindProperty("showDebugPanel").boolValue = false;

            // 4 aracı isimle açıkça bağla; boş kalırsa tester ada göre tahmin ediyordu
            // ("Vehicle_Ambulance" adı sahnede yok, gerçek obje "Vehicle_Ambulance_separate")
            AssignVehicle(so, "ambulance", "Vehicle_Ambulance_separate", "Vehicle_Ambulance");
            AssignVehicle(so, "policeCar", "Vehicle_Police Car_separate");
            AssignVehicle(so, "fireTruck", "Vehicle_Bus_color02_separate");
            AssignVehicle(so, "helicopter", "HelicopterV2");

            // Hız: orijinalin %72'si (%40 yavaşlatmanın üstüne %20 hızlandırma: 0.6 × 1.2);
            // filo: her kara aracından 4, helikopterden 3 (istasyonda klonlanır)
            // Zorluk: kara araçları orijinalin %55'i hızında; helikopter tam hızda (yollardan bağımsız, ~2.5 kat hızlı
            // ilk müdahaleci). Kıt filo: 3 polis, 3 ambulans, 3 itfaiye, 2 helikopter.
            // Araçlar istasyonda görünmez bekler, sevk edilince doğar, dönünce kaybolur.
            so.FindProperty("speedMultiplier").floatValue = 0.55f;
            so.FindProperty("helicopterSpeedMultiplier").floatValue = 1f;
            so.FindProperty("policeCount").intValue = 3;
            so.FindProperty("ambulanceCount").intValue = 3;
            so.FindProperty("fireTruckCount").intValue = 3;
            so.FindProperty("helicopterCount").intValue = 2;
            so.FindProperty("spawnOnDispatch").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignVehicle(SerializedObject so, string property, params string[] rootNames)
        {
            SerializedProperty prop = so.FindProperty(property);
            if (prop == null || prop.objectReferenceValue != null) return;

            foreach (string name in rootNames)
            {
                GameObject vehicle = FindRoot(name);
                if (vehicle != null)
                {
                    prop.objectReferenceValue = vehicle;
                    return;
                }
            }
            Debug.LogWarning($"[ZeroHour] Araç bulunamadı ({property}): {string.Join(", ", rootNames)}");
        }

        private static void EnsureEventSystem()
        {
            EventSystem es = Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
            if (es == null)
            {
                GameObject esObj = new GameObject("EventSystem", typeof(EventSystem));
                Undo.RegisterCreatedObjectUndo(esObj, "Create EventSystem");
                es = esObj.GetComponent<EventSystem>();
            }

            StandaloneInputModule legacy = es.GetComponent<StandaloneInputModule>();
            if (legacy != null) Undo.DestroyObjectImmediate(legacy);

            InputSystemUIInputModule module = es.GetComponent<InputSystemUIInputModule>();
            if (module == null) module = Undo.AddComponent<InputSystemUIInputModule>(es.gameObject);
            if (module.actionsAsset == null) module.AssignDefaultActions();
        }

        private static void DisableLegacyCanvas()
        {
            // Elle yerleştirilmiş eski HUD görselleri (script bağlı değil); silinmez, sadece pasif yapılır
            GameObject legacy = FindRoot("Canvas");
            if (legacy != null && legacy.transform.Find("anel_Units") != null && legacy.activeSelf)
            {
                Undo.RecordObject(legacy, "Disable Legacy Canvas");
                legacy.SetActive(false);
                Debug.Log("[ZeroHour] Eski 'Canvas' (anel_Units görselleri) pasif yapıldı, yeni HUD onun yerine geçti.");
            }
        }

        // ==========================================
        // YERELLEŞTİRME (Ayarlar → Language: ENGLISH / TÜRKÇE)
        // ==========================================

        // Kod tarafından sürekli yazılan metinler (LocalizedText bunların üstüne yazmasın)
        private static readonly HashSet<string> DynamicTextNames = new HashSet<string>
        {
            "Stamp", "CaseTab", "CallInfo", "Language", "Population", "TimeLeft", "Clock", "Stats", "Grade", "MainMenuLabel",
            "PhotoCaption", "Value_Incident", "Value_Address", "Value_Affected", "Value_Notes"
        };

        private static void LocalizeStaticTexts(Transform canvas)
        {
            int count = 0;
            foreach (TMP_Text t in canvas.GetComponentsInChildren<TMP_Text>(true))
            {
                if (DynamicTextNames.Contains(t.name) || !Loc.Has(t.text)) continue;

                LocalizedText localized = t.gameObject.AddComponent<LocalizedText>();
                SerializedObject so = new SerializedObject(localized);
                so.FindProperty("english").stringValue = t.text;
                so.ApplyModifiedPropertiesWithoutUndo();

                // Türkçe metin daha uzun olabilir: kutuya sığacak kadar küçülsün
                t.enableAutoSizing = true;
                t.fontSizeMax = t.fontSize;
                t.fontSizeMin = Mathf.Max(10f, t.fontSize * 0.55f);
                count++;
            }
            Debug.Log($"[ZeroHour] {count} sabit UI yazısı yerelleştirildi (ENGLISH / TÜRKÇE).");
        }

        // ==========================================
        // AFET EFEKT MATERYALLERİ
        // ==========================================

        private const string FxDir = PrefabDir + "/FX";

        private static void BuildFxMaterials(DisasterFxLibrary library)
        {
            EnsureFolder(FxDir);
            Texture2D soft = GetOrCreateSoftParticleTexture();

            library.additive = GetOrCreateParticleMaterial("FX_Additive", soft, true);
            library.alphaBlended = GetOrCreateParticleMaterial("FX_Alpha", soft, false);
            library.debris = GetOrCreateParticleMaterial("FX_Debris", null, false);
            library.water = GetOrCreateWaterMaterial();
            EditorUtility.SetDirty(library);
        }

        /// <summary>Yumuşak kenarlı daire (duman, alev, toz parçacığı).</summary>
        private static Texture2D GetOrCreateSoftParticleTexture()
        {
            // v2: daha dolgun kenar (duman / toz uzaktan da okunsun). Eski sürüm varsa silinir.
            string oldPath = FxDir + "/fx_soft_circle.png";
            if (System.IO.File.Exists(oldPath)) AssetDatabase.DeleteAsset(oldPath);

            string path = FxDir + "/fx_soft_circle_v2.png";
            if (!System.IO.File.Exists(path))
            {
                const int Size = 64;
                Texture2D tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
                for (int y = 0; y < Size; y++)
                {
                    for (int x = 0; x < Size; x++)
                    {
                        float dx = (x + 0.5f) / Size * 2f - 1f;
                        float dy = (y + 0.5f) / Size * 2f - 1f;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        float a = Mathf.Clamp01(1f - Mathf.Pow(d, 1.6f));
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                    }
                }
                System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);

                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Material GetOrCreateParticleMaterial(string name, Texture2D texture, bool additive)
        {
            string path = $"{FxDir}/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                mat = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }

            // URP Particles/Unlit: saydam yüzey; additive (alev) veya alfa karışımı (duman)
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", additive ? 2f : 0f);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", additive ? (float)UnityEngine.Rendering.BlendMode.One : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            mat.SetFloat("_DstBlendAlpha", additive ? (float)UnityEngine.Rendering.BlendMode.One : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            mat.SetColor("_BaseColor", Color.white);
            if (texture != null) mat.SetTexture("_BaseMap", texture);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material GetOrCreateWaterMaterial()
        {
            string path = FxDir + "/FX_FloodWater.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "FX_FloodWater" };
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            mat.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            mat.SetColor("_BaseColor", new Color(0.22f, 0.5f, 0.85f, 0.6f));
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ==========================================
        // YENİ AFET ROZETLERİ (Yağız'ın rozet çerçevesi + yeni renk halka + piksel ikon)
        // ==========================================

        private static readonly string[] ExplosionIcon =
        {
            "#...#...#.",
            ".#..#..#..",
            "..#####...",
            ".#######..",
            "#########.",
            ".#######..",
            "..#####...",
            ".#..#..#..",
            "#...#...#.",
            "..........",
        };

        private static readonly string[] WaveIcon =
        {
            "..........",
            "..##...##.",
            ".#..#.#..#",
            "#....#....",
            "..........",
            "..##...##.",
            ".#..#.#..#",
            "#....#....",
            "..........",
            "..........",
        };

        private static readonly string[] AlarmIcon =
        {
            "....##....",
            "...####...",
            "...####...",
            "...####...",
            "....##....",
            "....##....",
            "..........",
            "....##....",
            "....##....",
            "..........",
        };

        private static Sprite GenerateMarkerSprite(string name, Color ringColor, string[] icon)
        {
            string dir = PrefabDir + "/Markers";
            EnsureFolder(dir);
            string path = $"{dir}/{name}.png";

            if (!System.IO.File.Exists(path))
            {
                // Kaynak: marker_collapse (koyu daire + açık halka). Dosyadan okunur, böylece okunabilir kopya elde edilir.
                Texture2D src = new Texture2D(2, 2);
                src.LoadImage(System.IO.File.ReadAllBytes(UiDir + "marker_collapse.png"));
                int w = src.width, h = src.height;
                Vector2 c = new Vector2(w * 0.5f, h * 0.5f);
                float innerRadius = w * 0.38f;
                Color inner = src.GetPixel(w / 2, Mathf.RoundToInt(h * 0.22f)); // halkanın içindeki koyu zemin

                Texture2D dst = new Texture2D(w, h, TextureFormat.RGBA32, false);
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        Color p = src.GetPixel(x, y);
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c);
                        if (p.a > 0.01f && d <= innerRadius) p = new Color(inner.r, inner.g, inner.b, p.a);      // eski ikonu sil
                        else if (p.a > 0.01f) p = new Color(ringColor.r, ringColor.g, ringColor.b, p.a);         // halkayı boya
                        dst.SetPixel(x, y, p);
                    }
                }

                // 10x10 piksel ikon, 4 kat büyütülerek ortaya
                int cell = Mathf.Max(1, w / 22);
                int iconSize = 10 * cell;
                int ox = (w - iconSize) / 2;
                int oy = (h - iconSize) / 2;
                for (int iy = 0; iy < 10; iy++)
                {
                    for (int ix = 0; ix < 10; ix++)
                    {
                        if (icon[iy][ix] != '#') continue;
                        for (int py = 0; py < cell; py++)
                        {
                            for (int px = 0; px < cell; px++)
                            {
                                dst.SetPixel(ox + ix * cell + px, oy + (9 - iy) * cell + py, Color.white);
                            }
                        }
                    }
                }

                System.IO.File.WriteAllBytes(path, dst.EncodeToPNG());
                Object.DestroyImmediate(src);
                Object.DestroyImmediate(dst);
                AssetDatabase.ImportAsset(path);
            }

            return LoadSpriteAt(path, false);
        }

        // ==========================================
        // İSTASYON LOGOLARI (çatıda ince disk + kurum logosu)
        // ==========================================

        private struct StationDef
        {
            public string vehicleName;
            public string logoFile;
            public string label;
            public Color color;
        }

        /// <summary>
        /// Her acil durum aracının park ettiği yere en yakın binayı "istasyon binası" sayar,
        /// çatısına kurum renginde ince bir disk ve üstüne logo koyar. İstasyon binalarını döndürür
        /// (bu binalarda olay çıkmaz, böylece logo ile olay rozeti üst üste binmez).
        /// </summary>
        private static List<Transform> BuildStationBeacons(Material logoMaterial)
        {
            const string RootName = "[ZeroHour_StationBeacons]";
            const float MaxStationDistance = 15f;

            GameObject old = FindRoot(RootName);
            if (old != null) Undo.DestroyObjectImmediate(old);

            GameObject root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Create Station Beacons");

            StationDef[] defs =
            {
                new StationDef { vehicleName = "Vehicle_Police Car_separate", logoFile = "police_preview.png", label = "Police", color = new Color(0.25f, 0.50f, 1.00f) },
                new StationDef { vehicleName = "Vehicle_Ambulance_separate", logoFile = "ambulance_preview.png", label = "Ambulance", color = new Color(0.95f, 0.95f, 0.95f) },
                new StationDef { vehicleName = "Vehicle_Bus_color02_separate", logoFile = "fire_truck_preview.png", label = "FireTruck", color = new Color(0.92f, 0.25f, 0.18f) },
                new StationDef { vehicleName = "HelicopterV2", logoFile = "helicopter_preview.png", label = "Helicopter", color = new Color(1.00f, 0.80f, 0.25f) },
            };

            List<Transform> stationBuildings = new List<Transform>();

            foreach (StationDef def in defs)
            {
                GameObject vehicle = FindRoot(def.vehicleName);
                Sprite logoSprite = LoadSpriteAt("Assets/UI/Dispatch_UI_Assets/05_sprites/" + def.logoFile, true);
                if (vehicle == null || logoSprite == null)
                {
                    Debug.LogWarning($"[ZeroHour] İstasyon logosu atlandı ({def.label}): araç veya logo bulunamadı.");
                    continue;
                }

                Vector3 home = vehicle.transform.position;
                Transform building = FindNearestBuilding(home, MaxStationDistance, stationBuildings, out Bounds bounds, out float distance);

                Vector3 anchor;
                float diskDiameter;
                if (building != null)
                {
                    // Çatı ortası; helikopter gibi çatıdaki araçlarda aracın ayak seviyesi
                    anchor = new Vector3(bounds.center.x, Mathf.Max(bounds.max.y, home.y) + 0.05f, bounds.center.z);
                    diskDiameter = Mathf.Clamp(Mathf.Min(bounds.size.x, bounds.size.z) * 0.6f, 3f, 8f);
                    stationBuildings.Add(building);
                    Debug.Log($"[ZeroHour] İstasyon: {def.label} → {building.name} (araca {distance:0.0} m)");
                }
                else
                {
                    // Yakında uygun bina yoksa aracın park yerinin üstünde dur (yanlış binaya logo koyma)
                    anchor = home + Vector3.up * 6f;
                    diskDiameter = 4f;
                    Debug.LogWarning($"[ZeroHour] {def.label} için {MaxStationDistance} m içinde boş bina yok; logo araç park yerinin üstüne kondu.");
                }

                GameObject beacon = new GameObject($"Station_{def.label}");
                beacon.transform.SetParent(root.transform, false);
                beacon.transform.position = anchor;

                GameObject disk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                disk.name = "Disk";
                Object.DestroyImmediate(disk.GetComponent<Collider>()); // tıklama / NavMesh etkilenmesin
                disk.transform.SetParent(beacon.transform, false);
                disk.transform.localPosition = new Vector3(0f, 0.04f, 0f);
                disk.transform.localScale = new Vector3(diskDiameter, 0.04f, diskDiameter);
                MeshRenderer diskRenderer = disk.GetComponent<MeshRenderer>();
                diskRenderer.sharedMaterial = GetOrCreateUnlitMaterial($"StationDisk_{def.label}", def.color);
                diskRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                diskRenderer.receiveShadows = false;

                GameObject logoObj = new GameObject("Logo");
                logoObj.transform.SetParent(beacon.transform, false);
                SpriteRenderer logo = logoObj.AddComponent<SpriteRenderer>();
                logo.sprite = logoSprite;
                logo.sharedMaterial = logoMaterial;
                logo.sortingOrder = 400;

                StationBeacon component = beacon.AddComponent<StationBeacon>();
                SerializedObject so = new SerializedObject(component);
                so.FindProperty("logo").objectReferenceValue = logo;
                so.FindProperty("disk").objectReferenceValue = disk.transform;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            return stationBuildings;
        }

        private static Transform FindNearestBuilding(Vector3 point, float maxDistance, List<Transform> alreadyUsed, out Bounds bestBounds, out float bestDistance)
        {
            Transform best = null;
            bestBounds = default;
            bestDistance = float.MaxValue;

            foreach (GameObject go in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (!go.activeInHierarchy || !go.name.StartsWith("Building")) continue;
                if (alreadyUsed.Contains(go.transform)) continue; // iki kurum aynı binaya konmaz

                Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) continue;
                Bounds b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);

                Vector3 closest = b.ClosestPoint(point);
                closest.y = point.y;
                float d = Vector3.Distance(closest, point);
                if (d < bestDistance)
                {
                    best = go.transform;
                    bestBounds = b;
                    bestDistance = d;
                }
            }

            return bestDistance <= maxDistance ? best : null;
        }

        private static Material GetOrCreateUnlitMaterial(string name, Color color)
        {
            string path = $"{PrefabDir}/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                mat = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ==========================================
        // TOP HUD (UNITS + 150/150)
        // ==========================================

        private static void BuildHud(Transform canvas, Sprite unitsSprite, Sprite populationSprite)
        {
            // UNITS paneli: üst orta
            RectTransform units = NewImage("HUD_Units", canvas, unitsSprite, Color.white).rectTransform;
            units.anchorMin = units.anchorMax = new Vector2(0.5f, 1f);
            units.pivot = new Vector2(0.5f, 1f);
            units.sizeDelta = new Vector2(1064, 176);
            units.anchoredPosition = new Vector2(0f, -4.5f);
            units.localScale = Vector3.one * UiScale;

            NewText("Title", units, "UNITS", pixelFont, 40, HudHeader, 38, 14, 400, 64, TextAlignmentOptions.Left);

            string[] labels = { "Police", "Ambulance", "Fire Truck", "Helicopter" };
            float[] labelX = { 131, 348, 631, 896 };
            TMP_Text[] counts = new TMP_Text[4];
            for (int i = 0; i < 4; i++)
            {
                NewText($"Label_{labels[i]}", units, labels[i], readFont, 34, HudLabel, labelX[i], 66, labelX[i] + 200, 108, TextAlignmentOptions.Left);
                counts[i] = NewText($"Count_{labels[i]}", units, "1/1", pixelFont, 40, Color.white, labelX[i], 106, labelX[i] + 200, 150, TextAlignmentOptions.Left);
            }

            // Nüfus paneli: sağ üst
            RectTransform pop = NewImage("HUD_Population", canvas, populationSprite, Color.white).rectTransform;
            pop.anchorMin = pop.anchorMax = new Vector2(1f, 1f);
            pop.pivot = new Vector2(1f, 1f);
            pop.sizeDelta = new Vector2(378, 136);
            pop.anchoredPosition = new Vector2(-48f, -20f);
            pop.localScale = Vector3.one * UiScale;

            TMP_Text popText = NewText("Population", pop, "150/150", pixelFont, 52, HudGreen, 124, 18, 372, 118, TextAlignmentOptions.Left);
            popText.richText = true;

            DispatchHUD hud = canvas.gameObject.AddComponent<DispatchHUD>();
            SerializedObject so = new SerializedObject(hud);
            so.FindProperty("policeCountText").objectReferenceValue = counts[0];
            so.FindProperty("ambulanceCountText").objectReferenceValue = counts[1];
            so.FindProperty("fireTruckCountText").objectReferenceValue = counts[2];
            so.FindProperty("helicopterCountText").objectReferenceValue = counts[3];
            so.FindProperty("populationText").objectReferenceValue = popText;
            so.FindProperty("populationFormat").stringValue = "{0}<size=50%>/{1}</size>";
            so.FindProperty("countDisplayMode").enumValueIndex = (int)UnitCountDisplayMode.AvailableSlashTotal;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ==========================================
        // GELEN ÇAĞRI KARTI (Sol alt)
        // ==========================================

        private static void BuildCallCard(Transform canvas, IncomingCallController calls, AudioSource audio, Sprite ringingSprite, Sprite listeningSprite, Sprite fillSprite)
        {
            RectTransform card = NewRect("CallCard", canvas);
            card.anchorMin = card.anchorMax = new Vector2(0f, 0f);
            card.pivot = new Vector2(0f, 0f);
            card.sizeDelta = new Vector2(616, 254);
            card.anchoredPosition = new Vector2(14f, 16.5f);
            card.localScale = Vector3.one * UiScale;

            // --- Çalıyor ---
            Image ringing = NewImage("Ringing", card, ringingSprite, Color.white);
            Stretch(ringing.rectTransform);
            CanvasGroup ringingGroup = ringing.gameObject.AddComponent<CanvasGroup>();

            Image iconBlink = NewImage("IconBlink", ringing.transform, null, new Color(HudGreen.r, HudGreen.g, HudGreen.b, 0f));
            Place(iconBlink.rectTransform, 30, 26, 102, 98);
            iconBlink.raycastTarget = false;

            NewText("Title", ringing.transform, "INCOMING CALL", pixelFont, 40, HudGreen, 120, 30, 600, 94, TextAlignmentOptions.Left);

            Button decline = NewButton("DeclineButton", ringing.transform, 30, 152, 298, 240);
            NewText("Label", decline.transform, "DECLINE", pixelFont, 40, DeclineRed, 0, 0, 268, 88, TextAlignmentOptions.Center);

            Button accept = NewButton("AcceptButton", ringing.transform, 318, 152, 586, 240);
            NewText("Label", accept.transform, "ACCEPT", pixelFont, 40, AcceptInk, 0, 0, 268, 88, TextAlignmentOptions.Center);

            // --- Dinleniyor (3 sn) ---
            Image listening = NewImage("Listening", card, listeningSprite, Color.white);
            Stretch(listening.rectTransform);
            CanvasGroup listeningGroup = listening.gameObject.AddComponent<CanvasGroup>();

            NewText("Title", listening.transform, "ON CALL", pixelFont, 40, HudGreen, 120, 30, 600, 94, TextAlignmentOptions.Left);
            NewText("Speaking", listening.transform, "Caller is speaking...", readFont, 32, HudLabel, 240, 148, 600, 202, TextAlignmentOptions.Left);

            Image fill = NewImage("ProgressFill", listening.transform, fillSprite, Color.white);
            Place(fill.rectTransform, 32, 226, 584, 242);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0f;
            fill.raycastTarget = false;

            SerializedObject so = new SerializedObject(calls);
            so.FindProperty("ringingCard").objectReferenceValue = ringingGroup;
            so.FindProperty("listeningCard").objectReferenceValue = listeningGroup;
            so.FindProperty("acceptButton").objectReferenceValue = accept;
            so.FindProperty("declineButton").objectReferenceValue = decline;
            so.FindProperty("progressFill").objectReferenceValue = fill;
            so.FindProperty("ringingIconBlink").objectReferenceValue = iconBlink;
            so.FindProperty("audioSource").objectReferenceValue = audio;
            so.ApplyModifiedPropertiesWithoutUndo();

            ringing.gameObject.SetActive(false);
            listening.gameObject.SetActive(false);
        }

        // ==========================================
        // KAZA DOSYASI (Orta)
        // ==========================================

        private static void BuildIncidentFile(Transform canvas, Sprite panelSprite)
        {
            RectTransform holder = NewRect("IncidentFileUI", canvas);
            Stretch(holder);
            IncidentFileUI fileUI = holder.gameObject.AddComponent<IncidentFileUI>();

            RectTransform root = NewRect("Root", holder);
            Stretch(root);

            Image backdrop = NewImage("Backdrop", root, null, new Color(0.03f, 0.04f, 0.05f, 0.6f));
            Stretch(backdrop.rectTransform);
            Button backdropButton = backdrop.gameObject.AddComponent<Button>();
            backdropButton.transition = Selectable.Transition.None;

            Image panelImage = NewImage("Panel", root, panelSprite, Color.white);
            panelImage.raycastTarget = true; // Dosyaya tıklama arka plana geçip dosyayı kapatmasın
            RectTransform panel = panelImage.rectTransform;
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(1800, 1260);
            panel.anchoredPosition = Vector2.zero;
            panel.localScale = Vector3.one * UiScale;

            // Başlıklar
            TMP_Text caseTab = NewText("CaseTab", panel, "CASE #0147", pixelFont, 40, Ink, 60, 8, 450, 62, TextAlignmentOptions.Left);
            NewText("Title", panel, "DISASTER CONTROL LINE · INCIDENT FILE", pixelFont, 36, Ink, 62, 104, 1180, 160, TextAlignmentOptions.Left);
            TMP_Text callInfo = NewText("CallInfo", panel, "Call 02:14 · Line 2", readFont, 30, InkMuted, 1100, 108, 1738, 158, TextAlignmentOptions.Right);

            // Uzun damgalar ("UNVERIFIED") çerçeveden taşmasın: sığana kadar küçülür
            TMP_Text stamp = FitInBox(NewText("Stamp", panel, "URGENT", pixelFont, 50, StampRed, 1432, 180, 1732, 270, TextAlignmentOptions.Center));
            stamp.margin = new Vector4(26f, 0f, 26f, 0f);
            stamp.rectTransform.localEulerAngles = new Vector3(0f, 0f, -7.7f);

            // Bilgi satırları
            string[] infoLabels = { "INCIDENT", "ADDRESS", "AFFECTED", "NOTES" };
            float[] infoY = { 226, 299, 377, 450 };
            for (int i = 0; i < infoLabels.Length; i++)
            {
                NewText($"Label_{infoLabels[i]}", panel, infoLabels[i], pixelFont, 32, InkMuted, 699, infoY[i] - 30, 950, infoY[i] + 30, TextAlignmentOptions.Left);
            }
            TMP_Text incidentValue = NewText("Value_Incident", panel, "Residential fire", readFont, 38, Ink, 961, 196, 1410, 256, TextAlignmentOptions.Left);
            TMP_Text addressValue = NewText("Value_Address", panel, "12 Oak Street", readFont, 38, Ink, 961, 269, 1738, 329, TextAlignmentOptions.Left);
            TMP_Text affectedValue = NewText("Value_Affected", panel, "6 people · 3 injured", readFont, 38, Ink, 961, 347, 1738, 407, TextAlignmentOptions.Left);
            TMP_Text notesValue = NewText("Value_Notes", panel, "Notes", readFont, 36, Ink, 961, 424, 1738, 640, TextAlignmentOptions.TopLeft);
            notesValue.textWrappingMode = TextWrappingModes.Normal;

            // Polaroid
            RawImage photo = NewRawImage("Photo", panel);
            Place(photo.rectTransform, 73, 217, 629, 563);
            photo.rectTransform.localEulerAngles = new Vector3(0f, 0f, 1.9f);
            TMP_Text caption = NewText("PhotoCaption", panel, "OAK ST. · 02:14", pixelFont, 36, Ink, 92, 562, 632, 618, TextAlignmentOptions.Left);
            caption.rectTransform.localEulerAngles = new Vector3(0f, 0f, 1.9f);

            // Tablo başlığı
            NewText("Header_Unit", panel, "UNIT", pixelFont, 32, InkMuted, 71, 640, 400, 686, TextAlignmentOptions.Left);
            NewText("Header_Free", panel, "FREE", pixelFont, 32, InkMuted, 1222, 640, 1400, 686, TextAlignmentOptions.Left);
            NewText("Header_Send", panel, "SEND", pixelFont, 32, InkMuted, 1410, 640, 1710, 686, TextAlignmentOptions.Center);

            // Birim satırları: [-] 0 [+]
            string[] rowNames = { "Police", "Ambulance", "Fire Truck", "Helicopter" };
            UnitType[] rowTypes = { UnitType.PoliceCruiser, UnitType.Ambulance, UnitType.FireEngine, UnitType.RescueHelicopter };

            SerializedObject so = new SerializedObject(fileUI);
            SerializedProperty rows = so.FindProperty("unitRows");
            rows.arraySize = rowNames.Length;

            for (int i = 0; i < rowNames.Length; i++)
            {
                float yc = 741 + i * 106;
                NewText($"Row{i}_Name", panel, rowNames[i], readFont, 36, Ink, 163, yc - 30, 700, yc + 30, TextAlignmentOptions.Left);
                TMP_Text free = NewText($"Row{i}_Free", panel, "1", pixelFont, 40, Ink, 1222, yc - 30, 1335, yc + 30, TextAlignmentOptions.Left);

                Button minus = NewButton($"Row{i}_Minus", panel, 1425, yc - 43, 1511, yc + 43);
                NewText("Label", minus.transform, "–", pixelFont, 44, InkMuted, 0, 0, 86, 86, TextAlignmentOptions.Center);
                TMP_Text count = NewText($"Row{i}_Count", panel, "0", pixelFont, 44, Ink, 1513, yc - 30, 1606, yc + 30, TextAlignmentOptions.Center);
                Button plus = NewButton($"Row{i}_Plus", panel, 1608, yc - 43, 1696, yc + 43);
                NewText("Label", plus.transform, "+", pixelFont, 44, Cream, 0, 0, 88, 86, TextAlignmentOptions.Center);

                SerializedProperty row = rows.GetArrayElementAtIndex(i);
                row.FindPropertyRelative("unitType").enumValueIndex = (int)rowTypes[i];
                row.FindPropertyRelative("distanceText").objectReferenceValue = null; // DISTANCE sütunu kaldırıldı; olası ekipler açıklamada
                row.FindPropertyRelative("freeText").objectReferenceValue = free;
                row.FindPropertyRelative("countText").objectReferenceValue = count;
                row.FindPropertyRelative("minusButton").objectReferenceValue = minus;
                row.FindPropertyRelative("plusButton").objectReferenceValue = plus;
            }

            // Joker helikopter: seçilen her helikopter için helikopter satırında bir rol bölmesi (ambulans / polis / itfaiye)
            Sprite ambulanceIcon = LoadSpriteAt("Assets/UI/Dispatch_UI_Assets/05_sprites/ambulance_preview.png", true);
            Sprite policeIcon = LoadSpriteAt("Assets/UI/Dispatch_UI_Assets/05_sprites/police_preview.png", true);
            Sprite fireIcon = LoadSpriteAt("Assets/UI/Dispatch_UI_Assets/05_sprites/fire_truck_preview.png", true);

            SerializedProperty slots = so.FindProperty("heliRoleSlots");
            const int SlotCount = 3;
            slots.arraySize = SlotCount;
            float heliRowY = 741 + 3 * 106;
            for (int s = 0; s < SlotCount; s++)
            {
                float x0 = 430 + s * 255;
                Image slotBg = NewImage($"HeliRoleSlot{s + 1}", panel, null, new Color(0.84f, 0.77f, 0.60f, 1f));
                Place(slotBg.rectTransform, x0, heliRowY - 42, x0 + 240, heliRowY + 42);
                StylePanel(slotBg, true); // Dosyadaki ikon kutularıyla aynı yuvarlak köşeli çip
                slotBg.raycastTarget = false;

                SerializedProperty slot = slots.GetArrayElementAtIndex(s);
                slot.FindPropertyRelative("root").objectReferenceValue = slotBg.gameObject;

                (string field, string selectedField, Sprite icon, string label)[] roles =
                {
                    ("ambulanceButton", "ambulanceSelected", ambulanceIcon, "Ambulance"),
                    ("policeButton", "policeSelected", policeIcon, "Police"),
                    ("fireButton", "fireSelected", fireIcon, "Fire"),
                };

                for (int k = 0; k < roles.Length; k++)
                {
                    float bx = 8 + k * 78;
                    Button roleButton = NewButton($"Role_{roles[k].label}", slotBg.transform, bx, 7, bx + 70, 77);

                    Image selected = NewImage("Selected", roleButton.transform, null, Ink);
                    Stretch(selected.rectTransform);
                    selected.raycastTarget = false;
                    selected.enabled = false;

                    Image icon = NewImage("Icon", roleButton.transform, roles[k].icon, Color.white);
                    Place(icon.rectTransform, 11, 11, 59, 59);
                    icon.preserveAspect = true;
                    icon.raycastTarget = false;

                    slot.FindPropertyRelative(roles[k].field).objectReferenceValue = roleButton;
                    slot.FindPropertyRelative(roles[k].selectedField).objectReferenceValue = selected;
                }

                slotBg.gameObject.SetActive(false);
            }

            // Alt butonlar
            Button dismiss = NewButton("DismissButton", panel, 62, 1150, 466, 1246);
            FitInBox(NewText("Label", dismiss.transform, "DISMISS CALL", pixelFont, 40, Ink, 0, 0, 404, 96, TextAlignmentOptions.Center));

            Button dispatch = NewButton("DispatchButton", panel, 1406, 1146, 1738, 1250);
            Image dispatchFill = NewImage("ActiveFill", dispatch.transform, null, Ink);
            Stretch(dispatchFill.rectTransform);
            dispatchFill.raycastTarget = false;
            TMP_Text dispatchLabel = FitInBox(NewText("Label", dispatch.transform, "DISPATCH", pixelFont, 40, Cream, 0, 0, 332, 104, TextAlignmentOptions.Center));

            so.FindProperty("root").objectReferenceValue = root.gameObject;
            so.FindProperty("backdropButton").objectReferenceValue = backdropButton;
            so.FindProperty("caseTabText").objectReferenceValue = caseTab;
            so.FindProperty("callInfoText").objectReferenceValue = callInfo;
            so.FindProperty("stampText").objectReferenceValue = stamp;
            so.FindProperty("incidentValueText").objectReferenceValue = incidentValue;
            so.FindProperty("addressValueText").objectReferenceValue = addressValue;
            so.FindProperty("affectedValueText").objectReferenceValue = affectedValue;
            so.FindProperty("notesValueText").objectReferenceValue = notesValue;
            so.FindProperty("photoImage").objectReferenceValue = photo;
            so.FindProperty("photoCaptionText").objectReferenceValue = caption;
            so.FindProperty("dismissButton").objectReferenceValue = dismiss;
            so.FindProperty("dispatchButton").objectReferenceValue = dispatch;
            so.FindProperty("dispatchActiveFill").objectReferenceValue = dispatchFill;
            so.FindProperty("dispatchLabel").objectReferenceValue = dispatchLabel;
            so.ApplyModifiedPropertiesWithoutUndo();

            root.gameObject.SetActive(false);
        }

        // ==========================================
        // VARDİYA SAYACI (sol üst) + AYARLAR DİŞLİSİ
        // ==========================================

        private static readonly Color HudPanelColor = new Color(0.227f, 0.227f, 0.227f, 1f);
        private static readonly Color HudBorderColor = new Color(0.09f, 0.09f, 0.09f, 1f);
        private static readonly Color SettingsBlue = new Color(0.31f, 0.76f, 0.97f, 1f);
        private static readonly Color ToggleTextIdle = new Color(0.55f, 0.65f, 0.78f, 1f);

        private static void BuildShiftHud(Transform canvas, Phase2Clock clock)
        {
            Image panel = NewImage("HUD_Shift", canvas, null, HudPanelColor);
            panel.raycastTarget = false;
            StylePanel(panel, false);
            RectTransform rt = panel.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            const float ShiftPanelWidth = 440f;
            rt.sizeDelta = new Vector2(ShiftPanelWidth, 136);
            rt.anchoredPosition = new Vector2(20f, -20f);
            rt.localScale = Vector3.one * UiScale;

            NewText("Title", rt, "NIGHT SHIFT", pixelFont, 28, HudHeader, 24, 12, 420, 48, TextAlignmentOptions.Left);
            TMP_Text timeLeft = NewText("TimeLeft", rt, "3:00", pixelFont, 64, HudGreen, 24, 48, 240, 124, TextAlignmentOptions.Left);
            TMP_Text clockText = NewText("Clock", rt, "02:00 AM", readFont, 32, HudLabel, 250, 66, 420, 112, TextAlignmentOptions.Right);

            // Ayarlar dişlisi: vardiya panelinin sağında
            Image gearBg = NewImage("SettingsButton", canvas, null, HudPanelColor);
            StylePanel(gearBg, false);
            RectTransform gearRt = gearBg.rectTransform;
            gearRt.anchorMin = gearRt.anchorMax = new Vector2(0f, 1f);
            gearRt.pivot = new Vector2(0f, 1f);
            gearRt.sizeDelta = new Vector2(136, 136);
            gearRt.anchoredPosition = new Vector2(20f + ShiftPanelWidth * UiScale + 12f, -20f);
            gearRt.localScale = Vector3.one * UiScale;
            Button gearButton = gearBg.gameObject.AddComponent<Button>();
            gearButton.targetGraphic = gearBg;
            Image gearIcon = NewImage("Icon", gearRt, LoadSpriteAt("Assets/UI/Dispatch_UI_Assets/05_sprites/gear_preview.png", true), Color.white);
            Place(gearIcon.rectTransform, 28, 28, 108, 108);
            gearIcon.preserveAspect = true;
            gearIcon.raycastTarget = false;
            gearBg.gameObject.AddComponent<OpenSettingsButton>(); // Ortak ayarlar paneli sahnede değil, referansla bağlanamaz

            // HUD öğeleri en altta çizilsin: Kaza Dosyası, karne ve ayarlar bunların üstünde kalır
            gearBg.transform.SetAsFirstSibling();
            panel.transform.SetAsFirstSibling();

            SerializedObject so = new SerializedObject(clock);
            so.FindProperty("shiftDurationSeconds").floatValue = 180f;
            so.FindProperty("timeLeftText").objectReferenceValue = timeLeft;
            so.FindProperty("clockText").objectReferenceValue = clockText;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ==========================================
        // TELEFON (sağ alt): olay günlüğü mesaj olarak düşer
        // ==========================================

        private static void BuildPhoneFeed(Transform canvas)
        {
            const float Width = 340f, Height = 500f;

            // Gövde: Yağız stili koyu çerçeve
            Image body = NewImage("PhoneFeed", canvas, null, HudPanelColor);
            body.raycastTarget = false;
            StylePanel(body, false);
            RectTransform rt = body.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.sizeDelta = new Vector2(Width, Height);
            rt.anchoredPosition = new Vector2(-20f, 16f);

            // Durum çubuğu: saat + sinyal çubukları, ortada hoparlör yarığı
            TMP_Text clock = NewText("StatusClock", rt, "02:00", readFont, 18, HudLabel, 22, 12, 120, 38, TextAlignmentOptions.Left);
            Image speaker = NewImage("Speaker", rt, null, HudBorderColor);
            speaker.raycastTarget = false;
            Place(speaker.rectTransform, Width * 0.5f - 34f, 20, Width * 0.5f + 34f, 28);
            for (int i = 0; i < 4; i++)
            {
                Image signal = NewImage("Signal" + i, rt, null, HudLabel);
                signal.raycastTarget = false;
                float x0 = Width - 64f + i * 9f;
                Place(signal.rectTransform, x0, 32f - 5f - i * 4f, x0 + 6f, 32f);
            }

            NewText("Header", rt, "MESSAGES", pixelFont, 24, HudHeader, 22, 44, Width - 22, 76, TextAlignmentOptions.Left);
            Image headerLine = NewImage("HeaderLine", rt, null, HudBorderColor);
            headerLine.raycastTarget = false;
            Place(headerLine.rectTransform, 16, 80, Width - 16, 83);

            // Ekran: taşan eski mesajlar üstten kırpılır
            Image screen = NewImage("Screen", rt, null, new Color(0.11f, 0.115f, 0.12f, 1f));
            screen.raycastTarget = false;
            Place(screen.rectTransform, 14, 88, Width - 14, Height - 34);
            screen.gameObject.AddComponent<RectMask2D>();

            RectTransform content = NewRect("Content", screen.transform);
            Stretch(content); // Mesaj balonlarını PhoneFeed alttan yukarı elle dizer

            // Ana ekran çubuğu
            Image homeBar = NewImage("HomeBar", rt, null, new Color(0.62f, 0.68f, 0.78f, 0.6f));
            homeBar.raycastTarget = false;
            Place(homeBar.rectTransform, Width * 0.5f - 50f, Height - 21, Width * 0.5f + 50f, Height - 16);

            AudioSource audio = body.gameObject.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 0f;

            PhoneFeed feed = body.gameObject.AddComponent<PhoneFeed>();
            SerializedObject so = new SerializedObject(feed);
            so.FindProperty("content").objectReferenceValue = content;
            so.FindProperty("font").objectReferenceValue = readFont;
            so.FindProperty("statusClockText").objectReferenceValue = clock;
            so.FindProperty("audioSource").objectReferenceValue = audio;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ==========================================
        // İPUCU BANDI (alt orta; Kaza Dosyasını örtmez)
        // ==========================================

        private static void BuildTutorialHints(Transform canvas)
        {
            Image bg = NewImage("TutorialHints", canvas, null, HudPanelColor);
            bg.raycastTarget = false;
            StylePanel(bg, false);
            RectTransform rt = bg.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(1000f, 50f);
            rt.anchoredPosition = new Vector2(80f, 10f); // Sol alttaki çağrı kartından uzak dursun

            CanvasGroup group = bg.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            TextMeshProUGUI text = NewText("Hint", rt, "", readFont, 26, HudLabel, 0, 0, 1000, 50, TextAlignmentOptions.Center);
            text.enableAutoSizing = true;
            text.fontSizeMax = 26f;
            text.fontSizeMin = 16f;

            TutorialHints hints = bg.gameObject.AddComponent<TutorialHints>();
            SerializedObject so = new SerializedObject(hints);
            so.FindProperty("banner").objectReferenceValue = group;
            so.FindProperty("hintText").objectReferenceValue = text;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ==========================================
        // VARDİYA SONU KARNESİ
        // ==========================================

        private static void BuildDebrief(Transform canvas, Phase2Clock clock)
        {
            RectTransform root = NewRect("ShiftDebrief", canvas);
            Stretch(root);

            Image backdrop = NewImage("Backdrop", root, null, new Color(0.02f, 0.03f, 0.04f, 0.78f));
            Stretch(backdrop.rectTransform);

            Image panel = NewImage("Panel", root, null, HudPanelColor);
            StylePanel(panel, false);
            panel.raycastTarget = true;
            RectTransform rt = panel.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(1120, 760);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one * UiScale;

            // Ayarlar paneli gibi başlık ve alt bant ayırıcı çizgileri
            Image headerLine = NewImage("HeaderLine", rt, null, HudBorderColor);
            Place(headerLine.rectTransform, 0, 128, 1120, 132);
            headerLine.raycastTarget = false;
            Image footerLine = NewImage("FooterLine", rt, null, HudBorderColor);
            Place(footerLine.rectTransform, 0, 588, 1120, 592);
            footerLine.raycastTarget = false;

            NewText("Title", rt, "SHIFT OVER · 05:00 AM", pixelFont, 44, HudHeader, 42, 36, 1078, 100, TextAlignmentOptions.Left);
            TMP_Text stats = NewText("Stats", rt, "Survivors 150 / 150", pixelFont, 40, HudLabel, 42, 160, 720, 560, TextAlignmentOptions.TopLeft);
            stats.lineSpacing = 70f; // Piksel fontun satır yüksekliği dar; satırlar birbirine binmesin
            NewText("GradeLabel", rt, "GRADE", pixelFont, 36, HudHeader, 760, 150, 1078, 200, TextAlignmentOptions.Center);
            TMP_Text grade = NewText("Grade", rt, "S", pixelFont, 220, HudGreen, 760, 200, 1078, 500, TextAlignmentOptions.Center);

            Image againBg = NewImage("PlayAgainButton", rt, null, SettingsBlue);
            Place(againBg.rectTransform, 718, 620, 1078, 708);
            Button again = againBg.gameObject.AddComponent<Button>();
            again.targetGraphic = againBg;
            NewText("Label", againBg.transform, "GO TO SLEEP", pixelFont, 40, AcceptInk, 0, 0, 360, 88, TextAlignmentOptions.Center);

            // Ana menüye dönüş: vardiya HUD'ındaki dişli gibi koyu çerçeveli panel düğmesi
            Image menuBg = NewImage("MainMenuButton", rt, null, HudPanelColor);
            StylePanel(menuBg, false);
            Place(menuBg.rectTransform, 42, 620, 402, 708);
            Button menu = menuBg.gameObject.AddComponent<Button>();
            menu.targetGraphic = menuBg;
            FitInBox(NewText("Label", menuBg.transform, "MAIN MENU", pixelFont, 40, HudLabel, 0, 0, 360, 88, TextAlignmentOptions.Center));

            SerializedObject so = new SerializedObject(clock);
            so.FindProperty("debriefRoot").objectReferenceValue = root.gameObject;
            so.FindProperty("debriefStatsText").objectReferenceValue = stats;
            so.FindProperty("debriefGradeText").objectReferenceValue = grade;
            so.FindProperty("playAgainButton").objectReferenceValue = again;
            so.FindProperty("mainMenuButton").objectReferenceValue = menu;
            so.ApplyModifiedPropertiesWithoutUndo();

            root.gameObject.SetActive(false);
        }

        // ==========================================
        // AYARLAR (Yağız'ın settings_panel_dark tasarımı → mevcut SettingsUI)
        // ==========================================

        private const string GlobalSettingsPrefabPath = "Assets/Resources/GlobalSettings.prefab";

        /// <summary>Kendi Canvas'ı olan ayarlar panelini prefab olarak kaydeder; SettingsUI onu oyun açılınca yükler.</summary>
        private static void BuildGlobalSettingsPrefab()
        {
            GameObject root = new GameObject("GlobalSettings", typeof(RectTransform));
            root.layer = 5;
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // Her iki fazın UI'ının da üstünde
            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();

            BuildSettings(root.transform);
            LocalizeStaticTexts(root.transform);

            EnsureFolder("Assets/Resources");
            PrefabUtility.SaveAsPrefabAsset(root, GlobalSettingsPrefabPath);
            Object.DestroyImmediate(root);
        }

        private static SettingsUI BuildSettings(Transform canvas)
        {
            const string PartsDir = "Assets/UI/Dispatch_UI_Assets/03_ui_components/settings_parts_dark/";
            Sprite panelSprite = LoadSprite("settings_panel_dark.png");
            Sprite blockFilled = LoadSpriteAt(PartsDir + "volume_block_filled.png", false);
            Sprite blockEmpty = LoadSpriteAt(PartsDir + "volume_block_empty.png", false);
            Sprite toggleHalf = LoadSpriteAt(PartsDir + "toggle_selected_half.png", false);

            RectTransform holder = NewRect("SettingsUI", canvas);
            Stretch(holder);
            SettingsUI settings = holder.gameObject.AddComponent<SettingsUI>();

            RectTransform root = NewRect("Root", holder);
            Stretch(root);
            Image backdrop = NewImage("Backdrop", root, null, new Color(0.02f, 0.03f, 0.04f, 0.6f));
            Stretch(backdrop.rectTransform);

            Image panelImage = NewImage("Panel", root, panelSprite, Color.white);
            panelImage.raycastTarget = true;
            RectTransform panel = panelImage.rectTransform;
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(1120, 854);
            panel.anchoredPosition = Vector2.zero;
            panel.localScale = Vector3.one * UiScale;

            NewText("Title", panel, "SETTINGS", pixelFont, 40, HudHeader, 42, 40, 600, 92, TextAlignmentOptions.Left);
            Button closeX = NewButton("CloseX", panel, 1018, 30, 1090, 102);

            string[] labels = { "Sound", "Music", "Language", "Fullscreen" };
            float[] rowY = { 208, 346, 484, 622 };
            for (int i = 0; i < labels.Length; i++)
            {
                NewText($"Label_{labels[i]}", panel, labels[i], readFont, 34, HudLabel, 136, rowY[i] - 28, 520, rowY[i] + 28, TextAlignmentOptions.Left);
            }

            (Slider soundSlider, TMP_Text soundPercent) = BuildVolumeRow(panel, "Sound", rowY[0], blockFilled, blockEmpty);
            (Slider musicSlider, TMP_Text musicPercent) = BuildVolumeRow(panel, "Music", rowY[1], blockFilled, blockEmpty);

            Button prevLang = NewButton("LanguagePrev", panel, 630, 448, 702, 520);
            NewText("Label", prevLang.transform, "<", pixelFont, 36, HudLabel, 0, 0, 72, 72, TextAlignmentOptions.Center);
            Button nextLang = NewButton("LanguageNext", panel, 1006, 448, 1078, 520);
            NewText("Label", nextLang.transform, ">", pixelFont, 36, HudLabel, 0, 0, 72, 72, TextAlignmentOptions.Center);
            TMP_Text languageText = NewText("Language", panel, "ENGLISH", pixelFont, 44, Color.white, 702, 450, 1006, 518, TextAlignmentOptions.Center);

            // Tam ekran OFF / ON: seçili tarafın dolgusu (toggle_selected_half) SettingsUI tarafından açılıp kapanır
            FitInBox(NewText("OffIdle", panel, "OFF", pixelFont, 40, ToggleTextIdle, 788, 587, 932, 658, TextAlignmentOptions.Center));
            FitInBox(NewText("OnIdle", panel, "ON", pixelFont, 40, ToggleTextIdle, 932, 587, 1076, 658, TextAlignmentOptions.Center));
            Image offHighlight = NewImage("OffSelected", panel, toggleHalf, Color.white);
            Place(offHighlight.rectTransform, 788, 586, 932, 660);
            offHighlight.raycastTarget = false;
            FitInBox(NewText("Label", offHighlight.transform, "OFF", pixelFont, 40, AcceptInk, 0, 0, 144, 74, TextAlignmentOptions.Center));
            Image onHighlight = NewImage("OnSelected", panel, toggleHalf, Color.white);
            Place(onHighlight.rectTransform, 932, 586, 1076, 660);
            onHighlight.raycastTarget = false;
            FitInBox(NewText("Label", onHighlight.transform, "ON", pixelFont, 40, AcceptInk, 0, 0, 144, 74, TextAlignmentOptions.Center));
            Button offButton = NewButton("FullscreenOff", panel, 788, 587, 932, 658);
            Button onButton = NewButton("FullscreenOn", panel, 932, 587, 1076, 658);

            Button defaults = NewButton("DefaultsButton", panel, 42, 728, 342, 816);
            FitInBox(NewText("Label", defaults.transform, "DEFAULTS", pixelFont, 40, HudLabel, 0, 0, 300, 88, TextAlignmentOptions.Center));
            // Ana menüye dönüş: VARSAYILAN ile GERİ arasında, ince çerçeveli (çerçeve dört ince çizgi; hazır Outline efekti değil)
            Button mainMenu = NewButton("MainMenuButton", panel, 396, 728, 796, 816);
            float[][] edges = { new[] { 0f, 0f, 400f, 3f }, new[] { 0f, 85f, 400f, 88f }, new[] { 0f, 0f, 3f, 88f }, new[] { 397f, 0f, 400f, 88f } };
            foreach (float[] e in edges)
            {
                Image line = NewImage("Frame", mainMenu.transform, null, HudHeader);
                Place(line.rectTransform, e[0], e[1], e[2], e[3]);
                line.raycastTarget = false;
            }
            TMP_Text mainMenuLabel = FitInBox(NewText("MainMenuLabel", mainMenu.transform, "MAIN MENU", pixelFont, 40, HudLabel, 0, 0, 400, 88, TextAlignmentOptions.Center));

            Button back = NewButton("BackButton", panel, 850, 728, 1078, 816);
            FitInBox(NewText("Label", back.transform, "BACK", pixelFont, 40, AcceptInk, 0, 0, 228, 88, TextAlignmentOptions.Center));

            SerializedObject so = new SerializedObject(settings);
            so.FindProperty("settingsPanelRoot").objectReferenceValue = root.gameObject;
            so.FindProperty("soundSlider").objectReferenceValue = soundSlider;
            so.FindProperty("soundPercentText").objectReferenceValue = soundPercent;
            so.FindProperty("musicSlider").objectReferenceValue = musicSlider;
            so.FindProperty("musicPercentText").objectReferenceValue = musicPercent;
            so.FindProperty("languageText").objectReferenceValue = languageText;
            so.FindProperty("prevLanguageButton").objectReferenceValue = prevLang;
            so.FindProperty("nextLanguageButton").objectReferenceValue = nextLang;
            so.FindProperty("fullscreenOffButton").objectReferenceValue = offButton;
            so.FindProperty("fullscreenOnButton").objectReferenceValue = onButton;
            so.FindProperty("fullscreenOffHighlight").objectReferenceValue = offHighlight;
            so.FindProperty("fullscreenOnHighlight").objectReferenceValue = onHighlight;
            so.FindProperty("defaultsButton").objectReferenceValue = defaults;
            so.FindProperty("backButton").objectReferenceValue = back;
            so.FindProperty("closeXButton").objectReferenceValue = closeX;
            so.FindProperty("mainMenuButton").objectReferenceValue = mainMenu;
            so.FindProperty("mainMenuLabel").objectReferenceValue = mainMenuLabel;
            so.ApplyModifiedPropertiesWithoutUndo();

            root.gameObject.SetActive(false);
            return settings;
        }

        private static (Slider, TMP_Text) BuildVolumeRow(RectTransform panel, string name, float rowY, Sprite filled, Sprite empty)
        {
            const int BlockCount = 10;
            const float BlockWidth = 32f, BlockGap = 6f, BlockHeight = 50f;
            float x0 = 500f; // "100%" yazısına yer kalsın (çubuk 874'te biter)
            float width = BlockCount * BlockWidth + (BlockCount - 1) * BlockGap;

            // Görünmez Slider: tıklama / sürükleme alanı blok çubuğunun tamamı
            Image hit = NewImage($"{name}Slider", panel, null, new Color(1f, 1f, 1f, 0f));
            Place(hit.rectTransform, x0, rowY - BlockHeight * 0.5f, x0 + width, rowY + BlockHeight * 0.5f);
            hit.raycastTarget = true;

            RectTransform fillArea = NewRect("FillArea", hit.rectTransform);
            Stretch(fillArea);
            Image fill = NewImage("Fill", fillArea, null, new Color(1f, 1f, 1f, 0f));
            Stretch(fill.rectTransform);
            fill.raycastTarget = false;

            Slider slider = hit.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.targetGraphic = hit;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.transition = Selectable.Transition.None;
            Navigation nav = slider.navigation;
            nav.mode = Navigation.Mode.None;
            slider.navigation = nav;

            Image[] blocks = new Image[BlockCount];
            for (int i = 0; i < BlockCount; i++)
            {
                float bx = i * (BlockWidth + BlockGap);
                blocks[i] = NewImage($"Block{i + 1}", hit.rectTransform, empty, Color.white);
                Place(blocks[i].rectTransform, bx, 0f, bx + BlockWidth, BlockHeight);
                blocks[i].raycastTarget = false;
            }

            VolumeBlockBar bar = hit.gameObject.AddComponent<VolumeBlockBar>();
            SerializedObject so = new SerializedObject(bar);
            SerializedProperty blockProp = so.FindProperty("blocks");
            blockProp.arraySize = BlockCount;
            for (int i = 0; i < BlockCount; i++) blockProp.GetArrayElementAtIndex(i).objectReferenceValue = blocks[i];
            so.FindProperty("filledSprite").objectReferenceValue = filled;
            so.FindProperty("emptySprite").objectReferenceValue = empty;
            so.ApplyModifiedPropertiesWithoutUndo();

            TMP_Text percent = NewText($"{name}Percent", panel, "70%", pixelFont, 44, Color.white, 896, rowY - 30, 1078, rowY + 30, TextAlignmentOptions.Right);
            return (slider, percent);
        }

        // ==========================================
        // UI YARDIMCILARI (koordinatlar: ebeveynin sol üst köşesinden piksel)
        // ==========================================

        private static RectTransform NewRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

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
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        private static Image NewImage(string name, Transform parent, Sprite sprite, Color color)
        {
            RectTransform rt = NewRect(name, parent);
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = sprite == null || name == "Backdrop";
            return img;
        }

        private static RawImage NewRawImage(string name, Transform parent)
        {
            RectTransform rt = NewRect(name, parent);
            RawImage img = rt.gameObject.AddComponent<RawImage>();
            img.color = Color.white;
            img.raycastTarget = false;
            return img;
        }

        private static TextMeshProUGUI NewText(string name, Transform parent, string text, TMP_FontAsset font, float size, Color color,
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

        /// <summary>Türkçe metin daha uzun olabilir ("KAPALI", "VARSAYILAN"): kenarlara değmeden sığana kadar küçülür.</summary>
        private static TMP_Text FitInBox(TMP_Text tmp)
        {
            tmp.enableAutoSizing = true;
            tmp.fontSizeMax = tmp.fontSize;
            tmp.fontSizeMin = Mathf.Min(24f, tmp.fontSize);
            tmp.margin = new Vector4(12f, 0f, 12f, 0f);
            return tmp;
        }

        private static Button NewButton(string name, Transform parent, float x0, float y0, float x1, float y1)
        {
            RectTransform rt = NewRect(name, parent);
            Place(rt, x0, y0, x1, y1);
            Image hit = rt.gameObject.AddComponent<Image>();
            hit.color = Color.white;
            hit.raycastTarget = true;

            Button button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = hit;
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0f);
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.12f);
            colors.pressedColor = new Color(0f, 0f, 0f, 0.18f);
            colors.selectedColor = new Color(1f, 1f, 1f, 0f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0f);
            colors.colorMultiplier = 1f;
            button.colors = colors;
            return button;
        }

        // ==========================================
        // GENEL YARDIMCILAR
        // ==========================================

        private static GameObject FindRoot(string name)
        {
            foreach (GameObject go in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (go.name == name) return go;
            }
            return null;
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            T existing = go.GetComponent<T>();
            return existing != null ? existing : Undo.AddComponent<T>(go);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
