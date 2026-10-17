using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using ZeroHour.Phase2.Core;
using ZeroHour.Phase2.Data;
using ZeroHour.Phase2.Units;
using ZeroHour.Phase2.Incidents;

namespace ZeroHour.Phase2.Editor
{
    public static class EmergencySystemSetup
    {
        private const string PrefabDir = "Assets/Prefabs/EmergencyUnits";

        [MenuItem("ZeroHour/4. One-Click Setup Dispatch System and Stations")]
        [MenuItem("Tools/ZeroHour/Setup Dispatch System")]
        public static void SetupCompleteDispatchSystem()
        {
            Debug.Log("<color=yellow>[ZeroHour] 🚀 Acil Durum & Dispatch Sistemi Kurulumu Başlatılıyor...</color>");

            // 1. Klasör yapısını oluştur
            EnsureFolderStructure();

            // 2. Birim Prefablarını oluştur / kontrol et
            EmergencyUnit ambulancePrefab = GetOrCreateUnitPrefab(
                "Assets/SimplePoly City - Low Poly Assets/Prefab/Vehicles/Vehicle with Static Wheels/Vehicle_Ambulance.prefab",
                "Ambulance_Unit.prefab",
                UnitType.Ambulance,
                "AMB",
                12f,
                20
            );

            EmergencyUnit policePrefab = GetOrCreateUnitPrefab(
                "Assets/SimplePoly City - Low Poly Assets/Prefab/Vehicles/Vehicle with Static Wheels/Vehicle_Police Car.prefab",
                "Police_Unit.prefab",
                UnitType.PoliceCruiser,
                "POL",
                14f,
                30
            );

            EmergencyUnit firePrefab = GetOrCreateUnitPrefab(
                "Assets/SimplePoly City - Low Poly Assets/Prefab/Vehicles/Vehicle with Static Wheels/Vehicle_Truck_color01.prefab",
                "Fire_Unit.prefab",
                UnitType.FireEngine,
                "FIRE",
                10f,
                10
            );

            EmergencyUnit heliPrefab = GetOrCreateHeliPrefab(
                "Assets/Alstra Infinite/Planes LowPoly/Prefabs/Version 1.2/HelicopterV1.prefab",
                "Helicopter_Unit.prefab",
                UnitType.RescueHelicopter,
                "HELI",
                18f,
                10f
            );

            // 3. İstasyonları Sahnede Kur (Hastane, Polis, İtfaiye, Helipad)
            UnitStation hospitalStation = GetOrCreateStation("Station_Hospital", new Vector3(-345.6f, 0.2f, 74.2f), UnitType.Ambulance, "Şehir Hastanesi Acil Servis");
            UnitStation policeStation = GetOrCreateStation("Station_Police", new Vector3(-250.8f, 0.2f, 64.0f), UnitType.PoliceCruiser, "İlçe Emniyet Müdürlüğü");
            UnitStation fireStation = GetOrCreateStation("Station_Fire", new Vector3(-188.2f, 0.2f, 89.0f), UnitType.FireEngine, "Merkez İtfaiye İstasyonu");
            UnitStation helipadStation = GetOrCreateStation("Station_Helipad", new Vector3(-222.5f, 0.2f, 90.4f), UnitType.RescueHelicopter, "Stadyum Helikopter Pisti");

            // 4. [ZeroHour_CommandSystem] ve DispatchManager Yapılandırması
            GameObject systemObj = GameObject.Find("[ZeroHour_CommandSystem]");
            if (systemObj == null)
            {
                systemObj = new GameObject("[ZeroHour_CommandSystem]");
                Undo.RegisterCreatedObjectUndo(systemObj, "Create Command System");
            }

            CityStatsManager statsManager = systemObj.GetComponent<CityStatsManager>();
            if (statsManager == null)
            {
                statsManager = Undo.AddComponent<CityStatsManager>(systemObj);
            }

            DispatchManager dispatchManager = systemObj.GetComponent<DispatchManager>();
            if (dispatchManager == null)
            {
                dispatchManager = Undo.AddComponent<DispatchManager>(systemObj);
            }

            ShiftTimelineController timeline = systemObj.GetComponent<ShiftTimelineController>();
            if (timeline == null)
            {
                timeline = Undo.AddComponent<ShiftTimelineController>(systemObj);
            }

            // DispatchManager Serileştirilmiş Alanlarını Bağla
            SerializedObject so = new SerializedObject(dispatchManager);
            SetObjectReference(so, "ambulancePrefab", ambulancePrefab);
            SetObjectReference(so, "policeCruiserPrefab", policePrefab);
            SetObjectReference(so, "fireEnginePrefab", firePrefab);
            SetObjectReference(so, "helicopterPrefab", heliPrefab);

            SetObjectReference(so, "hospitalStation", hospitalStation);
            SetObjectReference(so, "policeStation", policeStation);
            SetObjectReference(so, "fireStation", fireStation);
            SetObjectReference(so, "helipadStation", helipadStation);
            so.ApplyModifiedProperties();

            // 5. Kriz Binalarına BuildingNode Bağla
            SetupTargetBuildings();

            // 6. NavMeshSurface Kontrolü ve Fırınlama
            CityNavMeshBaker.BakeNavMesh();

            // 7. Park halindeki araçlara NavMeshObstacle ekle
            CityNavMeshBaker.SetupParkedCarsAsObstacles();

            Selection.activeGameObject = systemObj;
            Debug.Log("<color=green>[ZeroHour] ✅ TÜM SİSTEM BAŞARIYLA KURULDU! Artık araçlar kendi binalarından kalkıp olay yerine gidecek ve işleri bitince geri dönecek!</color>");
        }

        private static void EnsureFolderStructure()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }
            if (!AssetDatabase.IsValidFolder(PrefabDir))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "EmergencyUnits");
            }
        }

        private static EmergencyUnit GetOrCreateUnitPrefab(string sourcePath, string prefabName, UnitType type, string callsign, float speed, int avoidancePriority)
        {
            string targetPath = $"{PrefabDir}/{prefabName}";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(targetPath);
            if (existing != null)
            {
                return existing.GetComponent<EmergencyUnit>();
            }

            GameObject sourceModel = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (sourceModel == null)
            {
                Debug.LogWarning($"[ZeroHour] Kaynak model bulunamadı: {sourcePath}");
                return null;
            }

            GameObject instance = Object.Instantiate(sourceModel);
            instance.name = prefabName.Replace(".prefab", "");

            // NavMeshAgent ekle ve ayarla
            NavMeshAgent agent = instance.GetComponent<NavMeshAgent>();
            if (agent == null) agent = instance.AddComponent<NavMeshAgent>();
            agent.speed = speed;
            agent.acceleration = 20f;
            agent.angularSpeed = 360f;
            agent.radius = 0.65f;
            agent.height = 1.4f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            agent.avoidancePriority = avoidancePriority;
            agent.autoBraking = true;

            // EmergencyUnit ekle ve ayarla
            EmergencyUnit unit = instance.GetComponent<EmergencyUnit>();
            if (unit == null) unit = instance.AddComponent<EmergencyUnit>();

            SerializedObject so = new SerializedObject(unit);
            so.FindProperty("unitType").enumValueIndex = (int)type;
            so.FindProperty("unitCallsign").stringValue = callsign;
            so.FindProperty("travelSpeed").floatValue = speed;
            so.ApplyModifiedProperties();

            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(instance, targetPath);
            Object.DestroyImmediate(instance);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return savedPrefab.GetComponent<EmergencyUnit>();
        }

        private static EmergencyUnit GetOrCreateHeliPrefab(string sourcePath, string prefabName, UnitType type, string callsign, float speed, float altitude)
        {
            string targetPath = $"{PrefabDir}/{prefabName}";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(targetPath);
            if (existing != null)
            {
                return existing.GetComponent<EmergencyUnit>();
            }

            GameObject sourceModel = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (sourceModel == null)
            {
                Debug.LogWarning($"[ZeroHour] Helikopter kaynak modeli bulunamadı: {sourcePath}");
                return null;
            }

            GameObject instance = Object.Instantiate(sourceModel);
            instance.name = prefabName.Replace(".prefab", "");

            // Helikopter karada değil havada gideceği için NavMeshAgent gerekmez
            NavMeshAgent agent = instance.GetComponent<NavMeshAgent>();
            if (agent != null) Object.DestroyImmediate(agent);

            EmergencyUnit unit = instance.GetComponent<EmergencyUnit>();
            if (unit == null) unit = instance.AddComponent<EmergencyUnit>();

            SerializedObject so = new SerializedObject(unit);
            so.FindProperty("unitType").enumValueIndex = (int)type;
            so.FindProperty("unitCallsign").stringValue = callsign;
            so.FindProperty("travelSpeed").floatValue = speed;
            so.FindProperty("flightAltitude").floatValue = altitude;
            so.ApplyModifiedProperties();

            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(instance, targetPath);
            Object.DestroyImmediate(instance);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return savedPrefab.GetComponent<EmergencyUnit>();
        }

        private static UnitStation GetOrCreateStation(string stationObjName, Vector3 position, UnitType type, string displayName)
        {
            GameObject obj = GameObject.Find(stationObjName);
            if (obj == null)
            {
                obj = new GameObject(stationObjName);
                obj.transform.position = position;
                Undo.RegisterCreatedObjectUndo(obj, $"Create {stationObjName}");
            }

            UnitStation station = obj.GetComponent<UnitStation>();
            if (station == null)
            {
                station = Undo.AddComponent<UnitStation>(obj);
            }

            SerializedObject so = new SerializedObject(station);
            so.FindProperty("stationType").enumValueIndex = (int)type;
            so.FindProperty("stationName").stringValue = displayName;
            so.FindProperty("dispatchExitPoint").objectReferenceValue = obj.transform;
            so.ApplyModifiedProperties();

            return station;
        }

        private static void SetupTargetBuildings()
        {
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            int count = 0;

            foreach (var go in allObjects)
            {
                if (go.name.Contains("Building_House_04") || go.name.Contains("Building_House_01") || go.name.Contains("Building_Residential"))
                {
                    BuildingNode node = go.GetComponent<BuildingNode>();
                    if (node == null)
                    {
                        node = Undo.AddComponent<BuildingNode>(go);
                    }

                    Collider col = go.GetComponent<Collider>();
                    if (col == null)
                    {
                        BoxCollider box = Undo.AddComponent<BoxCollider>(go);
                        Renderer rend = go.GetComponentInChildren<Renderer>();
                        if (rend != null)
                        {
                            box.center = go.transform.InverseTransformPoint(rend.bounds.center);
                            box.size = rend.bounds.size;
                        }
                    }

                    SerializedObject so = new SerializedObject(node);
                    so.FindProperty("buildingId").stringValue = go.name;
                    so.FindProperty("buildingName").stringValue = go.name.Replace("Building_", "").Replace("_color03", "");
                    so.FindProperty("totalCivilians").intValue = 25;
                    so.ApplyModifiedProperties();

                    count++;
                }
            }

            Debug.Log($"[ZeroHour] {count} adet konut binasına BuildingNode ve Tıklanabilir Collider bağlandı.");
        }

        private static void SetObjectReference(SerializedObject so, string propertyName, Object targetObj)
        {
            SerializedProperty prop = so.FindProperty(propertyName);
            if (prop != null)
            {
                prop.objectReferenceValue = targetObj;
            }
            else
            {
                Debug.LogWarning($"[ZeroHour] Property '{propertyName}' bulunamadı.");
            }
        }
    }
}
