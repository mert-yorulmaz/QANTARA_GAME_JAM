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
            // 0. Eski sahne NavMesh'ini tamamen temizle (Çimenlerin ve zeminlerin taranmasını yok et)
#pragma warning disable 0618
            UnityEditor.AI.NavMeshBuilder.ClearAllNavMeshes();
#pragma warning restore 0618

            // 1. [City_Roads_Network] ebeveyn objesini bul veya oluştur
            GameObject roadNetworkObj = GameObject.Find("[City_Roads_Network]");
            if (roadNetworkObj == null)
            {
                roadNetworkObj = new GameObject("[City_Roads_Network]");
                Undo.RegisterCreatedObjectUndo(roadNetworkObj, "Create City Roads Network");
            }

            // [City_Roads_Network] içinde yanlışlıkla çimen/bina kalmışsa onları dışarı çıkar
            Transform[] currentChildren = roadNetworkObj.GetComponentsInChildren<Transform>(true);
            foreach (var child in currentChildren)
            {
                if (child == null || child.gameObject == roadNetworkObj) continue;
                string cn = child.name.ToLower();
                if (cn.Contains("grass") || cn.Contains("nature") || cn.Contains("bush") || cn.Contains("tree") || cn.Contains("floor") || cn.Contains("building") || cn.Contains("house"))
                {
                    Undo.SetTransformParent(child, null, "Unparent Non-Road Object");
                }
            }

            // 2. Sahnede dağınık duran tüm yol parçalarını bul ve [City_Roads_Network] altına topla
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            int roadCount = 0;

            foreach (var go in allObjects)
            {
                string n = go.name.ToLower();
                // Sadece gerçek asfalt/yol parçaları
                bool isRoad = (n.StartsWith("road ") || n.Contains("road_") || n.Contains("road corner") || n.Contains("road intersection") || n.Contains("road lane") || n.Contains("road split line") || n.Contains("road t_intersection")) &&
                              !n.Contains("grass") && !n.Contains("nature") && !n.Contains("floor");

                if (isRoad)
                {
                    if (go.transform.parent != roadNetworkObj.transform)
                    {
                        Undo.SetTransformParent(go.transform, roadNetworkObj.transform, "Group Roads under [City_Roads_Network]");
                    }
                    roadCount++;
                }
                else if (n.Contains("grass") || n.Contains("house floor") || n.Contains("nature") || n.Contains("terrain"))
                {
                    // Çimen, bahçe ve doğa zeminlerine NavMeshModifier ekleyip Not Walkable & Ignore yapıyoruz
                    NavMeshModifier mod = go.GetComponent<NavMeshModifier>();
                    if (mod == null) mod = Undo.AddComponent<NavMeshModifier>(go);
                    mod.overrideArea = true;
                    mod.area = 1; // Not Walkable
                    mod.ignoreFromBuild = true;
                }
            }

            // 3. NavMeshSurface'i SADECE yolları toplayacak şekilde yapılandır (CollectObjects.Children)
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
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            Selection.activeGameObject = roadNetworkObj;

            Debug.Log($"<color=cyan>[ZeroHour] 🛣️ Haritadaki {roadCount} adet yol parçası '[City_Roads_Network]' altında toplandı. Eski sahne NavMesh'i ve çimenler temizlendi, SADECE ASFALT YOLLAR fırınlandı! Araçlar artık asfalttan asla çıkamaz.</color>");
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
