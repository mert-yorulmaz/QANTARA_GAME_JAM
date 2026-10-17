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

                    agent.speed = 26f;
                    agent.acceleration = 45f;
                    agent.angularSpeed = 480f;
                    agent.stoppingDistance = 2.0f;
                    agent.radius = 0.9f;
                    agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance; // Sağa sola savrulup kaldırıma çıkmasını engelliyoruz
                    agent.avoidancePriority = 50;
                    agent.autoBraking = true;

                    state.agent = agent;
                }

                vehicleStates.Add(state);
            }

            Debug.Log($"<color=cyan>[A* Tester] 4 ana araç teste bağlandı ({vehicleStates.Count} araç): Akıllı Takip Mesafesi (Follow Distance) ve Sıralı Park sistemi aktif edildi.</color>");
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
            Debug.Log($"<color=yellow>[A* Tester] 🚀 Test Başlatıldı! 4 araç aynı anda {targetPosition} noktasına yola çıktı (Takip mesafesi aktif).</color>");

            // Hedef noktayı yola hizala (karadaki araçların bina üstüne çıkmaması için)
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
                    state.agent.speed = 26f;

                    // Yanal ofset VERİLMİYOR (kaldırıma taşmamaları için)
                    // Bütün araçlar yolun orta asfaltındaki hedefe yönelir.
                    // Birbirinin arkasında durma işini UpdateGroundAgentMovement içindeki kuyruk sensörü yapar.
                    state.agent.SetDestination(roadTarget);
                }
            }
        }

        private VehicleTestState GetVehicleAhead(VehicleTestState current, float maxDist, out float distToAhead)
        {
            distToAhead = float.MaxValue;
            if (current == null || current.obj == null) return null;

            Vector3 myPos = current.obj.transform.position;
            Vector3 myFwd = current.obj.transform.forward;

            VehicleTestState closestAhead = null;
            float closestDist = maxDist;

            foreach (var other in vehicleStates)
            {
                if (other == current || other.isHelicopter || other.obj == null || other.finished) continue;

                Vector3 toOther = other.obj.transform.position - myPos;
                toOther.y = 0;
                float d = toOther.magnitude;

                if (d < closestDist)
                {
                    // Aracımızın ön görüş konisi içinde mi? (~60 derece)
                    float dot = Vector3.Dot(myFwd, toOther.normalized);
                    if (dot > 0.4f)
                    {
                        closestAhead = other;
                        closestDist = d;
                    }
                }
            }

            distToAhead = closestDist;
            return closestAhead;
        }

        private void UpdateGroundAgentMovement(VehicleTestState state)
        {
            NavMeshAgent agent = state.agent;
            if (agent == null || !agent.isOnNavMesh) return;

            // Öndeki aracı kontrol et (Safe Follow Distance)
            VehicleTestState ahead = GetVehicleAhead(state, 8.0f, out float distToAhead);

            if (!state.returning)
            {
                // Hedefe doğrudan varış kontrolü
                bool reachedDestination = (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.8f);

                // YA DA öndeki araç hedefe varıp durmuşsa, biz de onun 4.5 - 5.5 metre arkasında durup sıraya giriyoruz!
                bool queuedBehindLeader = (ahead != null && (ahead.status.Contains("Hedefe vardı") || ahead.status.Contains("Sırada")) && distToAhead <= 5.2f);

                if (reachedDestination || queuedBehindLeader)
                {
                    agent.isStopped = true;
                    agent.velocity = Vector3.zero;
                    state.status = $"Hedefe vardı (Sırada bekliyor) ({Mathf.CeilToInt(waitDurationAtTarget - state.timer)}s)...";
                    state.timer += Time.deltaTime;

                    // Dönüşte kaza yapmamak için sıralı ayrılış (önce ambulans, sonra itfaiye, sonra polis)
                    float returnStagger = 0f;
                    if (state.obj == ambulance) returnStagger = 0f;
                    else if (state.obj == fireTruck) returnStagger = 1.0f;
                    else if (state.obj == policeCar) returnStagger = 2.0f;

                    if (state.timer >= waitDurationAtTarget + returnStagger)
                    {
                        state.returning = true;
                        state.timer = 0f;
                        state.status = "Üsse Geri Dönüyor (A*)...";
                        agent.ResetPath();
                        agent.isStopped = false;
                        agent.speed = 26f;
                        agent.SetDestination(state.homePosition);
                        Debug.Log($"[A* Tester] {state.obj.name} hedeften ayrıldı, kapısına geri dönüyor.");
                    }
                }
                else
                {
                    // Henüz hedefe varmadık, yoldayız. Öndeki araca göre hız ayarla:
                    if (ahead != null)
                    {
                        if (distToAhead <= 4.2f)
                        {
                            // Öndekine çok yaklaştık, dur ve bekle (asla kaldırıma kırıp sollama yapma)
                            agent.isStopped = true;
                            agent.velocity = Vector3.zero;
                        }
                        else
                        {
                            // Takip mesafesini koru, öndekinin hızına uyarla
                            agent.isStopped = false;
                            float ratio = Mathf.Clamp01((distToAhead - 4.2f) / 3.8f);
                            agent.speed = Mathf.Lerp(6f, 26f, ratio);
                        }
                    }
                    else
                    {
                        // Önü boş, tam hız ilerle
                        agent.isStopped = false;
                        agent.speed = 26f;
                    }
                }
            }
            else
            {
                // Dönüş yolundayız:
                if (ahead != null && ahead.returning && distToAhead <= 4.5f)
                {
                    agent.isStopped = true;
                    agent.velocity = Vector3.zero;
                }
                else
                {
                    agent.isStopped = false;
                    agent.speed = 26f;
                }

                // Kendi kapısına dönüş kontrolü
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.4f)
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

            // Pervaneyi gerçekçi yüksek hızda döndür
            if (state.rotorTransform != null)
            {
                state.rotorTransform.Rotate(Vector3.up, 2400f * Time.deltaTime, Space.Self);
            }

            // Çatının da üstünde ferah seyir irtifası
            float cruiseAltitude = Mathf.Max(35f, state.homePosition.y + 12f);
            float heliFlightSpeed = 36f; // Yüksek hız
            float verticalSpeed = 14f;

            if (!state.returning)
            {
                // FAZ 1: Dikey Kalkış (Önce helipadden yukarı tırman)
                if (t.position.y < cruiseAltitude - 0.5f && Vector3.Distance(new Vector3(t.position.x, 0, t.position.z), new Vector3(state.homePosition.x, 0, state.homePosition.z)) < 2f)
                {
                    state.status = "Dikey Kalkış Yapıyor (Tırmanıyor)...";
                    t.position = Vector3.MoveTowards(t.position, new Vector3(state.homePosition.x, cruiseAltitude, state.homePosition.z), verticalSpeed * Time.deltaTime);
                    return;
                }

                // FAZ 2: Hedefe Doğru Yüksek Hızda Uçuş
                Vector3 targetAtCruise = new Vector3(targetPosition.x, cruiseAltitude, targetPosition.z);
                t.position = Vector3.MoveTowards(t.position, targetAtCruise, heliFlightSpeed * Time.deltaTime);

                Vector3 moveDir = (targetAtCruise - t.position);
                moveDir.y = 0;
                if (moveDir.sqrMagnitude > 0.1f)
                {
                    // HelicopterV2 burnu +X ekseninde olduğu için -90 derece offset ekliyoruz (Burnu tam hedefe bakar!)
                    Quaternion baseRot = Quaternion.LookRotation(moveDir, Vector3.up) * Quaternion.Euler(0f, -90f, 0f);
                    t.rotation = Quaternion.Slerp(t.rotation, baseRot, 10f * Time.deltaTime);
                }

                // Hedefe varış
                if (Vector3.Distance(new Vector3(t.position.x, 0, t.position.z), new Vector3(targetPosition.x, 0, targetPosition.z)) < 2.0f)
                {
                    state.status = $"Hedefin üstünde asılı! Bekliyor ({Mathf.CeilToInt(waitDurationAtTarget - state.timer)}s)...";
                    state.timer += Time.deltaTime;

                    if (state.timer >= waitDurationAtTarget)
                    {
                        state.returning = true;
                        state.timer = 0f;
                        state.status = "Helipade Geri Dönüyor (Seyir)...";
                    }
                }
            }
            else
            {
                // FAZ 3: Helipade Geri Seyir (Seyir irtifasında)
                Vector3 aboveHelipad = new Vector3(state.homePosition.x, cruiseAltitude, state.homePosition.z);
                float distToHelipadXZ = Vector3.Distance(new Vector3(t.position.x, 0, t.position.z), new Vector3(state.homePosition.x, 0, state.homePosition.z));

                if (distToHelipadXZ > 1.2f)
                {
                    t.position = Vector3.MoveTowards(t.position, aboveHelipad, heliFlightSpeed * Time.deltaTime);

                    Vector3 returnDir = (aboveHelipad - t.position);
                    returnDir.y = 0;
                    if (returnDir.sqrMagnitude > 0.1f)
                    {
                        // Dönüş rotasında da burnu tam ileriye bakar (-90 derece offset)
                        Quaternion baseRot = Quaternion.LookRotation(returnDir, Vector3.up) * Quaternion.Euler(0f, -90f, 0f);
                        t.rotation = Quaternion.Slerp(t.rotation, baseRot, 10f * Time.deltaTime);
                    }
                }
                else
                {
                    // FAZ 4: Dikey İniş (Helipade asansör gibi yumuşakça in)
                    state.status = "Helipade Dikey İniş Yapıyor...";
                    t.position = Vector3.MoveTowards(t.position, state.homePosition, 10f * Time.deltaTime);
                    t.rotation = Quaternion.Slerp(t.rotation, state.homeRotation, 8f * Time.deltaTime);

                    if (Vector3.Distance(t.position, state.homePosition) < 0.25f)
                    {
                        t.position = state.homePosition;
                        t.rotation = state.homeRotation;
                        state.finished = true;
                        state.status = "✅ Helipade İndi ve Park Etti";
                        Debug.Log("<color=green>[A* Tester] Helikopter helipade dikey iniş yaptı ve park etti! ✅</color>");
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
