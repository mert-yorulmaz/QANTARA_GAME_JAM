using UnityEditor;
using UnityEngine;
using Unity.AI.Navigation;
using UnityEngine.AI;

namespace ZeroHour.Phase2.Editor
{
    public static class CityNavMeshBaker
    {
        [MenuItem("ZeroHour/3. Bake City NavMesh (A Star Roads Only)")]
        [MenuItem("Tools/ZeroHour/Bake Roads Only NavMesh")]
        public static void BakeNavMesh()
        {
            // 1. [City_Roads_Network] ebeveyn objesini bul veya oluştur
            GameObject roadNetworkObj = GameObject.Find("[City_Roads_Network]");
            if (roadNetworkObj == null)
            {
                roadNetworkObj = new GameObject("[City_Roads_Network]");
                Undo.RegisterCreatedObjectUndo(roadNetworkObj, "Create City Roads Network");
            }

            // 2. Sahnede dağınık duran tüm yol parçalarını (Road ...) bul ve bu ebeveynin içine topla
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            int roadCount = 0;

            foreach (var go in allObjects)
            {
                if (go.name.StartsWith("Road ") || go.name.Contains("Road_") || go.name.Contains("Road Corner") || go.name.Contains("Road Intersection") || go.name.Contains("Road Lane"))
                {
                    if (go.transform.parent != roadNetworkObj.transform)
                    {
                        Undo.SetTransformParent(go.transform, roadNetworkObj.transform, "Group Roads under [City_Roads_Network]");
                    }
                    roadCount++;
                }
            }

            // 3. NavMeshSurface'i doğrudan sadece yolları toplayacak şekilde yapılandır (CollectObjects.Children)
            NavMeshSurface surface = roadNetworkObj.GetComponent<NavMeshSurface>();
            if (surface == null)
            {
                surface = Undo.AddComponent<NavMeshSurface>(roadNetworkObj);
            }

            surface.collectObjects = CollectObjects.Children; // SADECE BU OBJENİN ÇOCUKLARI (YOLLAR) BAKELENİR!
            surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
            surface.defaultArea = 0; // Walkable (Sürülebilir Yol)

            // 4. Sadece yolları fırınla
            surface.BuildNavMesh();
            EditorUtility.SetDirty(surface);
            Selection.activeGameObject = roadNetworkObj;

            Debug.Log($"<color=cyan>[ZeroHour] 🛣️ Haritadaki {roadCount} adet yol parçası '[City_Roads_Network]' altında toplandı ve SADECE YOLLAR NavMesh olarak fırınlandı! Araçlar artık asfalttan asla çıkamaz.</color>");
        }

        [MenuItem("ZeroHour/Setup Parked Cars As NavMesh Obstacles")]
        public static void SetupParkedCarsAsObstacles()
        {
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            int obstacleCount = 0;

            foreach (var go in allObjects)
            {
                string name = go.name.ToLower();
                if (name.StartsWith("vehicle_") || name.StartsWith("car_") || name.Contains("taxi") || name.Contains("suv") || name.Contains("bus") || name.Contains("truck"))
                {
                    // Acil durum araçlarını hariç tut
                    if (go.GetComponent<ZeroHour.Phase2.Units.EmergencyUnit>() != null) continue;

                    NavMeshObstacle obstacle = go.GetComponent<NavMeshObstacle>();
                    if (obstacle == null)
                    {
                        obstacle = Undo.AddComponent<NavMeshObstacle>(go);
                    }

                    obstacle.carving = true;
                    obstacle.carveOnlyStationary = true;
                    obstacle.shape = NavMeshObstacleShape.Box;

                    Renderer rend = go.GetComponentInChildren<Renderer>();
                    if (rend != null)
                    {
                        obstacle.size = rend.bounds.size;
                    }

                    EditorUtility.SetDirty(go);
                    obstacleCount++;
                }
            }

            Debug.Log($"<color=cyan>[ZeroHour] Şehirdeki {obstacleCount} adet araca NavMeshObstacle (Carving) eklendi!</color>");
        }

        [MenuItem("ZeroHour/Carve All Buildings As Solid Obstacles")]
        public static void CarveAllBuildingsAsObstacles()
        {
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            int bldCount = 0;

            foreach (var go in allObjects)
            {
                string name = go.name.ToLower();
                // Bütün konut, dükkan, stadyum ve binaları bul
                if (name.StartsWith("building_") || name.Contains("house_") || name.Contains("shop") || name.Contains("stadium") || name.Contains("pizza") || name.Contains("bakery"))
                {
                    NavMeshObstacle obstacle = go.GetComponent<NavMeshObstacle>();
                    if (obstacle == null)
                    {
                        obstacle = Undo.AddComponent<NavMeshObstacle>(go);
                    }

                    obstacle.carving = true;
                    obstacle.carveOnlyStationary = true;
                    obstacle.shape = NavMeshObstacleShape.Box;

                    Renderer rend = go.GetComponentInChildren<Renderer>();
                    if (rend != null)
                    {
                        obstacle.center = go.transform.InverseTransformPoint(rend.bounds.center);
                        obstacle.size = rend.bounds.size;
                    }

                    EditorUtility.SetDirty(go);
                    bldCount++;
                }
            }

            Debug.Log($"<color=cyan>[ZeroHour] 🏢 Şehirdeki {bldCount} adet binaya NavMeshObstacle (Carving) eklendi! Araçlar artık binaların içinden geçemez.</color>");
        }

        [MenuItem("ZeroHour/6. Setup A Star Movement Tester in Scene")]
        public static void SetupAStarMovementTester()
        {
            // Önce tüm binaları katı duvar yap
            CarveAllBuildingsAsObstacles();

            GameObject testerObj = GameObject.Find("[ZeroHour_AStarTester]");
            if (testerObj == null)
            {
                testerObj = new GameObject("[ZeroHour_AStarTester]");
                Undo.RegisterCreatedObjectUndo(testerObj, "Create A* Tester");
            }

            ZeroHour.Phase2.Units.AStarMovementTester tester = testerObj.GetComponent<ZeroHour.Phase2.Units.AStarMovementTester>();
            if (tester == null)
            {
                tester = Undo.AddComponent<ZeroHour.Phase2.Units.AStarMovementTester>(testerObj);
            }

            tester.AutoAssignFourUnits();
            tester.InitializeVehicles();
            Selection.activeGameObject = testerObj;
            Debug.Log("<color=green>[ZeroHour] 🚀 A* Movement Tester güncellendi! Binalar katı duvar yapıldı. 4 araç (HelicopterV2, Bus, Ambulance, Police) bağlandı!</color>");
        }
    }
}
