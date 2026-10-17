using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace ZeroHour.Phase2.Units
{
    public class AStarMovementTester : MonoBehaviour
    {
        [Header("Test Target Position")]
        [SerializeField] private Vector3 targetPosition = new Vector3(-155.0499f, 0.2f, 181.7f);
        [SerializeField] private float waitDurationAtTarget = 3.0f;

        [Header("4 Spesifik Acil Durum Aracı")]
        [SerializeField] private GameObject ambulance;
        [SerializeField] private GameObject policeCar;
        [SerializeField] private GameObject fireTruck;
        [SerializeField] private GameObject helicopter;

        [Header("Aktif Test Listesi")]
        [SerializeField] private List<GameObject> vehicles = new List<GameObject>();

        private class VehicleTestState
        {
            public GameObject obj;
            public NavMeshAgent agent;
            public Vector3 homePosition;
            public Quaternion homeRotation;
            public bool isHelicopter;
            public string status = "Beklemede (Kapıda)";
            public float timer = 0f;
            public bool returning = false;
            public bool finished = false;
            public Transform rotorTransform;
        }

        private readonly List<VehicleTestState> vehicleStates = new List<VehicleTestState>();
        private bool isTestActive = false;
        private GameObject targetVisualMarker;

        private void Start()
        {
            AutoAssignFourUnits();
            InitializeVehicles();
            CreateTargetMarker();
        }

        public void AutoAssignFourUnits()
        {
            // 1. Helikopter: HelicopterV2 (Asla helipad değil!)
            if (helicopter == null)
            {
                helicopter = GameObject.Find("HelicopterV2");
                if (helicopter == null)
                {
                    foreach (var go in FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                    {
                        string n = go.name.ToLower();
                        if (n.Contains("helicopter") && !n.Contains("helipad") && !n.Contains("roof"))
                        {
                            helicopter = go;
                            break;
                        }
                    }
                }
            }

            // 2. İtfaiye: Vehicle_Bus_color02_separate
            if (fireTruck == null)
            {
                fireTruck = GameObject.Find("Vehicle_Bus_color02_separate");
                if (fireTruck == null)
                {
                    foreach (var go in FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                    {
                        string n = go.name.ToLower();
                        if (n.Contains("bus_color02") || (n.Contains("bus") && n.Contains("separate")))
                        {
                            fireTruck = go;
                            break;
                        }
                    }
                }
            }

            // 3. Polis: Vehicle_Police Car_separate
            if (policeCar == null)
            {
                policeCar = GameObject.Find("Vehicle_Police Car_separate");
                if (policeCar == null)
                {
                    foreach (var go in FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                    {
                        string n = go.name.ToLower();
                        if (n.Contains("police") && n.Contains("separate"))
                        {
                            policeCar = go;
                            break;
                        }
                    }
                }
            }

            // 4. Ambulans: Vehicle_Ambulance
            if (ambulance == null)
            {
                ambulance = GameObject.Find("Vehicle_Ambulance");
                if (ambulance == null)
                {
                    foreach (var go in FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                    {
                        string n = go.name.ToLower();
                        if (n.Contains("ambulance"))
                        {
                            ambulance = go;
                            break;
                        }
                    }
                }
            }

            // Sadece bu 4 aracı listeye ekle
            vehicles.Clear();
            if (ambulance != null) vehicles.Add(ambulance);
            if (policeCar != null) vehicles.Add(policeCar);
            if (fireTruck != null) vehicles.Add(fireTruck);
            if (helicopter != null) vehicles.Add(helicopter);
        }

        public void InitializeVehicles()
        {
            vehicleStates.Clear();

            if (vehicles.Count == 0)
            {
                AutoAssignFourUnits();
            }

            foreach (var v in vehicles)
            {
                if (v == null) continue;

                // Sadece gerçek helikopter uçabilir, helipad asla helikopter olamaz
                bool isHeli = v.name.ToLower().Contains("helicopter") && !v.name.ToLower().Contains("helipad");

                VehicleTestState state = new VehicleTestState
                {
                    obj = v,
                    homePosition = v.transform.position,
                    homeRotation = v.transform.rotation,
                    isHelicopter = isHeli
                };

                // Helikopter pervanesini bul
                if (state.isHelicopter)
                {
                    Transform rotor = null;
                    foreach (Transform child in v.GetComponentsInChildren<Transform>())
                    {
                        string cn = child.name.ToLower();
                        if (cn.Contains("rotor") || cn.Contains("prop") || cn.Contains("blade") || cn.Contains("wing"))
                        {
                            rotor = child;
                            break;
                        }
                    }
                    state.rotorTransform = rotor;
                }
                else
                {
                    // Karadaki araç için NavMeshAgent ayarla
                    NavMeshAgent agent = v.GetComponent<NavMeshAgent>();
                    if (agent == null) agent = v.AddComponent<NavMeshAgent>();

                    // NavMesh üzerinde olduğundan emin ol
                    if (!agent.isOnNavMesh)
                    {
                        if (NavMesh.SamplePosition(v.transform.position, out NavMeshHit hit, 8.0f, NavMesh.AllAreas))
                        {
                            agent.Warp(hit.position);
                            state.homePosition = hit.position;
                        }
                    }

                    agent.speed = 14f;
                    agent.acceleration = 28f;
                    agent.angularSpeed = 400f;
                    agent.stoppingDistance = 1.8f;
                    agent.radius = 0.8f;
                    agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
                    agent.avoidancePriority = 20;

                    state.agent = agent;
                }

                vehicleStates.Add(state);
            }

            Debug.Log($"<color=cyan>[A* Tester] Sadece 4 ana araç teste bağlandı ({vehicleStates.Count} araç): Ambulans, Polis, İtfaiye ve HelicopterV2.</color>");
        }

        private void CreateTargetMarker()
        {
            if (targetVisualMarker != null) Destroy(targetVisualMarker);

            targetVisualMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            targetVisualMarker.name = "AStar_Test_Target_Marker";
            targetVisualMarker.transform.position = targetPosition;
            targetVisualMarker.transform.localScale = new Vector3(2.5f, 0.05f, 2.5f);

            Collider col = targetVisualMarker.GetComponent<Collider>();
            if (col != null) Destroy(col);

            MeshRenderer rend = targetVisualMarker.GetComponent<MeshRenderer>();
            if (rend != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(1f, 0.2f, 0.1f, 0.8f);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(1f, 0.2f, 0.1f) * 3f);
                rend.material = mat;
            }
        }

        private void Update()
        {
            if (IsSpacePressed())
            {
                StartAStarTest();
            }

            if (!isTestActive) return;

            foreach (var state in vehicleStates)
            {
                if (state.finished || state.obj == null) continue;

                if (state.isHelicopter)
                {
                    UpdateHelicopterMovement(state);
                }
                else
                {
                    UpdateGroundAgentMovement(state);
                }
            }
        }

        private bool IsSpacePressed()
        {
            try
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null && kb.spaceKey.wasPressedThisFrame) return true;
            }
            catch { }

            try
            {
                if (Input.GetKeyDown(KeyCode.Space)) return true;
            }
            catch { }

            return false;
        }

        public void StartAStarTest()
        {
            isTestActive = true;
            Debug.Log($"<color=yellow>[A* Tester] 🚀 Test Başlatıldı! 4 araç {targetPosition} noktasına A* ile yola çıktı.</color>");

            // Hedef noktayı yola hizala (karadaki araçların bina üstüne çıkmaya çalışmaması için)
            Vector3 roadTarget = targetPosition;
            if (NavMesh.SamplePosition(targetPosition, out NavMeshHit roadHit, 15.0f, NavMesh.AllAreas))
            {
                roadTarget = roadHit.position;
            }

            foreach (var state in vehicleStates)
            {
                state.timer = 0f;
                state.returning = false;
                state.finished = false;
                state.status = "Yolda (Hedefe Gidiyor...)";

                if (!state.isHelicopter && state.agent != null && state.agent.isOnNavMesh)
                {
                    state.agent.isStopped = false;
                    state.agent.ResetPath();
                    state.agent.SetDestination(roadTarget);
                }
            }
        }

        private void UpdateGroundAgentMovement(VehicleTestState state)
        {
            NavMeshAgent agent = state.agent;
            if (agent == null || !agent.isOnNavMesh) return;

            if (!state.returning)
            {
                // Hedefe varış kontrolü
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.6f)
                {
                    state.status = $"Hedefe vardı! Bekliyor ({Mathf.CeilToInt(waitDurationAtTarget - state.timer)}s)...";
                    state.timer += Time.deltaTime;

                    if (state.timer >= waitDurationAtTarget)
                    {
                        // Geri dönüşü başlat
                        state.returning = true;
                        state.timer = 0f;
                        state.status = "Üsse Geri Dönüyor (A*)...";
                        agent.ResetPath();
                        agent.SetDestination(state.homePosition);
                        Debug.Log($"[A* Tester] {state.obj.name} hedeften ayrıldı, kapısına geri dönüyor.");
                    }
                }
            }
            else
            {
                // Kendi kapısına dönüş kontrolü
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.3f)
                {
                    state.finished = true;
                    state.status = "✅ Kapıya Döndü ve Park Etti";
                    agent.isStopped = true;
                    state.obj.transform.rotation = state.homeRotation;
                    Debug.Log($"<color=green>[A* Tester] {state.obj.name} kendi istasyonuna başarıyla geri dönüp park etti! ✅</color>");
                }
            }
        }

        private void UpdateHelicopterMovement(VehicleTestState state)
        {
            Transform t = state.obj.transform;

            // Pervaneyi döndür
            if (state.rotorTransform != null)
            {
                state.rotorTransform.Rotate(Vector3.up, 1400f * Time.deltaTime, Space.Self);
            }

            float flightAltitude = 12f;
            Vector3 target = state.returning ? state.homePosition : targetPosition;
            Vector3 aerialTarget = target;
            aerialTarget.y = flightAltitude;

            // Havada ilerle
            t.position = Vector3.MoveTowards(t.position, aerialTarget, 18f * Time.deltaTime);

            Vector3 dir = aerialTarget - t.position;
            dir.y = 0;
            if (dir.sqrMagnitude > 0.05f)
            {
                Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);
                t.rotation = Quaternion.Slerp(t.rotation, rot, 8f * Time.deltaTime);
            }

            if (Vector3.Distance(new Vector3(t.position.x, 0, t.position.z), new Vector3(target.x, 0, target.z)) < 1.5f)
            {
                if (!state.returning)
                {
                    state.status = $"Helikopter hedefin üzerinde havada! ({Mathf.CeilToInt(waitDurationAtTarget - state.timer)}s)...";
                    state.timer += Time.deltaTime;
                    if (state.timer >= waitDurationAtTarget)
                    {
                        state.returning = true;
                        state.timer = 0f;
                        state.status = "Helipade Geri Dönüyor...";
                    }
                }
                else
                {
                    // Helipade iniş
                    t.position = Vector3.MoveTowards(t.position, state.homePosition, 8f * Time.deltaTime);
                    if (Vector3.Distance(t.position, state.homePosition) < 0.3f)
                    {
                        state.finished = true;
                        state.status = "✅ Helipade İndi ve Park Etti";
                        t.rotation = state.homeRotation;
                        Debug.Log("<color=green>[A* Tester] Helikopter helipade başarıyla iniş yaptı! ✅</color>");
                    }
                }
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(25, 25, 420, 240), GUI.skin.box);
            GUILayout.Label("<b><size=16>🚨 ZERO HOUR: A* HAREKET TESTİ</size></b>");
            GUILayout.Label($"Hedef: <color=yellow>{targetPosition}</color>");
            GUILayout.Space(6);

            GUI.backgroundColor = isTestActive ? Color.yellow : Color.green;
            if (GUILayout.Button(isTestActive ? "🔄 TESTİ YENİDEN BAŞLAT" : "▶️ 4 ARACI HEDEFE GÖNDER (TIKLA VEYA SPACE)", GUILayout.Height(40)))
            {
                StartAStarTest();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(8);
            for (int i = 0; i < vehicleStates.Count; i++)
            {
                var s = vehicleStates[i];
                if (s.obj != null)
                {
                    GUILayout.Label($"• <b>{s.obj.name}:</b> {s.status}");
                }
            }

            GUILayout.EndArea();
        }
    }
}
