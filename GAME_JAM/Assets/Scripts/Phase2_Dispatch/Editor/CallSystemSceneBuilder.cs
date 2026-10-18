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
            boardSo.FindProperty("unattendedTimeout").floatValue = 45f;
            boardSo.FindProperty("respondingTimeout").floatValue = 60f;
            boardSo.FindProperty("maxActiveIncidents").intValue = 3;
            SerializedProperty excluded = boardSo.FindProperty("excludedBuildings");
            excluded.arraySize = stationBuildings.Count;
            for (int i = 0; i < stationBuildings.Count; i++) excluded.GetArrayElementAtIndex(i).objectReferenceValue = stationBuildings[i];
            boardSo.ApplyModifiedPropertiesWithoutUndo();

            AudioSource callAudio = GetOrAdd<AudioSource>(systemObj);
            callAudio.playOnAwake = false;
            callAudio.spatialBlend = 0f;
            IncomingCallController calls = GetOrAdd<IncomingCallController>(systemObj);

            // Çağrılar üst üste gelmesin: 20–30 sn arayla, bekleyen her olay için +6 sn
            SerializedObject callTimingSo = new SerializedObject(calls);
            callTimingSo.FindProperty("firstCallDelay").floatValue = 4f;
            callTimingSo.FindProperty("minInterval").floatValue = 20f;
            callTimingSo.FindProperty("maxInterval").floatValue = 30f;
            callTimingSo.FindProperty("extraDelayPerWaitingIncident").floatValue = 6f;
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
            BuildCallCard(canvasObj.transform, calls, callAudio, ringingSprite, listeningSprite, fillSprite);
            BuildIncidentFile(canvasObj.transform, filePanel);

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

            // Rozetin üstündeki geri sayım ("0:45" / "EN ROUTE"). Altta olursa çatının içinde kalıp görünmüyordu.
            float spriteSize = defaultSprite != null ? Mathf.Max(defaultSprite.bounds.size.x, defaultSprite.bounds.size.y) : 1f;
            GameObject countdownObj = new GameObject("Countdown");
            countdownObj.transform.SetParent(temp.transform, false);
            countdownObj.transform.localPosition = new Vector3(0f, spriteSize * 0.95f, 0f);
            TextMeshPro countdown = countdownObj.AddComponent<TextMeshPro>();
            if (pixelFont != null) countdown.font = pixelFont;
            countdown.text = "0:45";
            countdown.fontSize = 5.2f * spriteSize;
            countdown.alignment = TextAlignmentOptions.Center;
            countdown.textWrappingMode = TextWrappingModes.NoWrap;
            countdown.rectTransform.sizeDelta = new Vector2(spriteSize * 3f, spriteSize * 0.8f);
            countdownObj.GetComponent<MeshRenderer>().sortingOrder = 501;

            SerializedObject markerSo = new SerializedObject(marker);
            markerSo.FindProperty("countdownText").objectReferenceValue = countdown;
            markerSo.ApplyModifiedPropertiesWithoutUndo();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(temp, prefabPath);
            Object.DestroyImmediate(temp);
            return saved.GetComponent<IncidentMarker>();
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
            NewText("Title", panel, "DAMAGE CONTROL LINE · INCIDENT FILE", pixelFont, 36, Ink, 62, 104, 1180, 160, TextAlignmentOptions.Left);
            TMP_Text callInfo = NewText("CallInfo", panel, "Call 02:14 · Line 2", readFont, 30, InkMuted, 1100, 108, 1738, 158, TextAlignmentOptions.Right);

            TMP_Text stamp = NewText("Stamp", panel, "URGENT", pixelFont, 50, StampRed, 1432, 180, 1732, 270, TextAlignmentOptions.Center);
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

            // Alt butonlar
            Button dismiss = NewButton("DismissButton", panel, 62, 1150, 466, 1246);
            NewText("Label", dismiss.transform, "DISMISS CALL", pixelFont, 40, Ink, 0, 0, 404, 96, TextAlignmentOptions.Center);

            Button dispatch = NewButton("DispatchButton", panel, 1406, 1146, 1738, 1250);
            Image dispatchFill = NewImage("ActiveFill", dispatch.transform, null, Ink);
            Stretch(dispatchFill.rectTransform);
            dispatchFill.raycastTarget = false;
            TMP_Text dispatchLabel = NewText("Label", dispatch.transform, "DISPATCH", pixelFont, 40, Cream, 0, 0, 332, 104, TextAlignmentOptions.Center);

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
