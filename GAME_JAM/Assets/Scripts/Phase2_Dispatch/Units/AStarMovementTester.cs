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

            // Parça objeler yerine kesinlikle ana kök objeleri referans al
            if (ambulance != null) ambulance = ambulance.transform.root.gameObject;
            if (policeCar != null) policeCar = policeCar.transform.root.gameObject;
            if (fireTruck != null) fireTruck = fireTruck.transform.root.gameObject;
            if (helicopter != null) helicopter = helicopter.transform.root.gameObject;

            // Sadece bu 4 aracı listeye ekle
            vehicles.Clear();
            if (ambulance != null) vehicles.Add(ambulance);
            if (policeCar != null) vehicles.Add(policeCar);
            if (fireTruck != null) vehicles.Add(fireTruck);
            if (helicopter != null) vehicles.Add(helicopter);
        }

        public Vector3 GetRoadTarget()
        {
            // Olay yerindeki bina (-155.05, 0.2, 181.7) önünden geçen asfalt cadde Z = 160.0f
            Vector3 frontStreetPos = new Vector3(targetPosition.x, 0.2f, 160.0f);

            // Önce doğrudan binanın önündeki asfalt caddede sample yap
            if (NavMesh.SamplePosition(frontStreetPos, out NavMeshHit hit, 15.0f, NavMesh.AllAreas))
            {
                return hit.position;
            }

            // Bulamazsa olay yeri etrafındaki en yakın asfalta snap et
            if (NavMesh.SamplePosition(targetPosition, out NavMeshHit broadHit, 50.0f, NavMesh.AllAreas))
            {
                return broadHit.position;
            }

            return frontStreetPos;
        }

        public void InitializeVehicles()
        {
            vehicleStates.Clear();

            if (vehicles.Count == 0)
            {
                AutoAssignFourUnits();
            }

            for (int i = 0; i < vehicles.Count; i++)
            {
                var v = vehicles[i];
                if (v == null) continue;

                // Parça seçilmişse (örneğin Ambulance Body), ana root objesini al
                if (v.transform.parent != null && (v.transform.root.name.Contains("Ambulance") || v.transform.root.name.Contains("Police") || v.transform.root.name.Contains("Bus") || v.transform.root.name.Contains("Helicopter")))
                {
                    v = v.transform.root.gameObject;
                    vehicles[i] = v;
                }

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

                    agent.baseOffset = 0f; // Asfalta sıfır oturmasını sağla, asla havada kalmasın

                    // Öncelik hiyerarşisi: İtfaiye lider (30), Ambulans (40), Polis (50)
                    if (v == fireTruck) agent.avoidancePriority = 30;
                    else if (v == ambulance) agent.avoidancePriority = 40;
                    else if (v == policeCar) agent.avoidancePriority = 50;
                    else agent.avoidancePriority = 50;

                    // NavMesh üzerinde olduğundan emin ol
                    if (!agent.isOnNavMesh)
                    {
                        if (NavMesh.SamplePosition(v.transform.position, out NavMeshHit hit, 15.0f, NavMesh.AllAreas))
                        {
                            agent.Warp(hit.position);
                            state.homePosition = hit.position;
                        }
                    }

                    agent.speed = 26f; // Hızlı ve dinamik acil müdahale hızı (Yaklaşık 95 km/s)
                    agent.acceleration = 45f;
                    agent.angularSpeed = 540f;
                    agent.stoppingDistance = 1.0f;
                    agent.radius = (v == fireTruck) ? 1.6f : 1.2f;
                    agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
                    agent.autoBraking = true;

                    state.agent = agent;
                }

                vehicleStates.Add(state);
            }

            Debug.Log($"<color=cyan>[A* Tester] 4 ana araç teste bağlandı ({vehicleStates.Count} araç): Kazasız Konvoy Radarı ve Asfalt Cadde Park Sistemi aktif edildi.</color>");
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

        private readonly List<GameObject> parkedAtTarget = new List<GameObject>();

        public void StartAStarTest()
        {
            isTestActive = true;
            parkedAtTarget.Clear();
            Debug.Log($"<color=yellow>[A* Tester] 🚀 Test Başlatıldı! 4 araç aynı anda {targetPosition} olay yerine yola çıktı (Çimen engelleme & Kazasız Konvoy Takip Mesafesi aktif).</color>");

            // Karadaki araçlar için hedefi kesinlikle binanın bahçesindeki çimene değil, bina önündeki asfalt yola hizala!
            Vector3 roadTarget = GetRoadTarget();

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

                    // Bütün araçlar yolun orta asfaltındaki cadde hedefine yönelir.
                    // Birbirinin arkasında durma ve mesafe koruma işini UpdateGroundAgentMovement yapar.
                    state.agent.SetDestination(roadTarget);
                }
            }
        }

        private float GetSafeDistance(GameObject a, GameObject b)
        {
            if (a == null || b == null) return 10.5f;

            string na = a.name.ToLower();
            string nb = b.name.ToLower();

            // Otobüs (İtfaiye) 9.3 metre boyundadır! İki aracın tamponlarının birbirine girmemesi için merkezler arası en az 10.5 metre mesafe şarttır!
            if (na.Contains("bus") || nb.Contains("bus"))
            {
                return 10.5f;
            }

            // Ambulans (6.8m) veya Polis arabası (6.3m) için güvenli merkezler arası mesafe en az 9.0 metredir
            return 9.0f;
        }

        private VehicleTestState GetVehicleAheadInMyPath(VehicleTestState current, out float distToAhead)
        {
            distToAhead = float.MaxValue;
            if (current == null || current.obj == null) return null;

            Vector3 myPos = current.obj.transform.position;
            myPos.y = 0f;

            // Aracımızın gitmek istediği yön
            Vector3 myMoveDir;
            if (current.agent != null && current.agent.hasPath && current.agent.desiredVelocity.sqrMagnitude > 0.1f)
            {
                myMoveDir = current.agent.desiredVelocity;
            }
            else
            {
                myMoveDir = current.obj.transform.forward;
            }
            myMoveDir.y = 0f;

            if (myMoveDir.sqrMagnitude < 0.01f)
            {
                myMoveDir = current.obj.transform.forward;
                myMoveDir.y = 0f;
            }

            myMoveDir.Normalize();
            VehicleTestState closestAhead = null;

            foreach (var other in vehicleStates)
            {
                if (other == current || other.isHelicopter || other.obj == null || other.finished) continue;

                Vector3 otherPos = other.obj.transform.position;
                otherPos.y = 0f;

                Vector3 toOther = otherPos - myPos;
                float d = toOther.magnitude;

                // Aracın ön görüş konisinde mi? (dot > 0.25f, yani yaklaşık ön 75 derecelik açı)
                // Arkamızdaki veya ters yönde kalan araçlar için ASLA fren yapmıyoruz (Deadlock'ı çözer!)
                float dot = Vector3.Dot(myMoveDir, toOther.normalized);
                if (dot > 0.25f)
                {
                    if (d < distToAhead)
                    {
                        distToAhead = d;
                        closestAhead = other;
                    }
                }
            }

            return closestAhead;
        }

        private void UpdateGroundAgentMovement(VehicleTestState state)
        {
            NavMeshAgent agent = state.agent;
            if (agent == null || !agent.isOnNavMesh) return;

            // Sadece hareket yönümüzün ÖNÜNDE kalan aracı kontrol et
            VehicleTestState aheadVehicle = GetVehicleAheadInMyPath(state, out float distToAhead);
            float safeDistToAhead = (aheadVehicle != null) ? GetSafeDistance(state.obj, aheadVehicle.obj) : 10.5f;

            Vector3 roadTarget = GetRoadTarget();
            Vector3 myPos = state.obj.transform.position;
            myPos.y = 0f;
            Vector3 targetPos = roadTarget;
            targetPos.y = 0f;
            float distToTarget = Vector3.Distance(myPos, targetPos);

            if (!state.returning)
            {
                // DİNAMİK OLAY YERİ PARK KUYRUĞU:
                // İlk kim varırsa (Ambulans, İtfaiye ya da Polis) 1. sıraya park eder.
                // 2. varan araç, 1. varanın güvenli mesafesinde (10.5m) 2. sıraya park eder.
                // 3. varan araç, 2. varanın güvenli mesafesinde (9.0m - 10.5m) 3. sıraya park eder.

                bool isAlreadyParked = parkedAtTarget.Contains(state.obj);

                if (!isAlreadyParked)
                {
                    if (parkedAtTarget.Count == 0)
                    {
                        // İlk varan araç: Hedefe doğrudan yaklaşınca 1. sırada park eder
                        if (distToTarget <= 3.5f || (!agent.pathPending && agent.remainingDistance <= 2.5f))
                        {
                            parkedAtTarget.Add(state.obj);
                            isAlreadyParked = true;
                            agent.isStopped = true;
                            agent.velocity = Vector3.zero;
                            agent.ResetPath();
                            state.status = "Hedefe vardı (1. Sırada Park Etti)";
                        }
                    }
                    else
                    {
                        // Kuyrukta önünde bekleyen en son park etmiş araç
                        GameObject leadParkedVehicle = parkedAtTarget[parkedAtTarget.Count - 1];
                        float safeDistToLead = GetSafeDistance(state.obj, leadParkedVehicle);
                        Vector3 leadPos = leadParkedVehicle.transform.position;
                        leadPos.y = 0f;
                        float distToLead = Vector3.Distance(myPos, leadPos);

                        // Önündeki park etmiş araca safeDist (10.5m) kadar yaklaşınca nizami sıraya girip durur
                        if (distToLead <= safeDistToLead + 0.5f)
                        {
                            parkedAtTarget.Add(state.obj);
                            isAlreadyParked = true;
                            agent.isStopped = true;
                            agent.velocity = Vector3.zero;
                            agent.ResetPath();
                            state.status = $"Hedefe vardı ({parkedAtTarget.Count}. Sırada park etti)";
                        }
                    }
                }

                if (isAlreadyParked)
                {
                    agent.isStopped = true;
                    agent.velocity = Vector3.zero;
                    state.status = $"Hedefe vardı ({parkedAtTarget.IndexOf(state.obj) + 1}. Sırada bekliyor) ({Mathf.CeilToInt(waitDurationAtTarget - state.timer)}s)...";
                    state.timer += Time.deltaTime;

                    // KAZASIZ TERS SIRALI AYRILIŞ:
                    // Kuyrukta en arkadaki araç (en son gelen) ilk ayrılır, çünkü arkası tamamen açıktır!
                    // En arkadaki: 0.0s gecikme, bir öndeki: 2.0s gecikme, en öndeki: 4.0s gecikme
                    int myQueueIndex = parkedAtTarget.IndexOf(state.obj);
                    int totalParked = parkedAtTarget.Count;
                    float returnStagger = 0f;

                    int reverseIndex = (totalParked - 1) - myQueueIndex;
                    if (reverseIndex < 0) reverseIndex = 0;
                    returnStagger = reverseIndex * 2.0f;

                    if (state.timer >= waitDurationAtTarget + returnStagger)
                    {
                        state.returning = true;
                        state.timer = 0f;
                        state.status = "Üsse Geri Dönüyor (A*)...";
                        agent.ResetPath();
                        agent.isStopped = false;
                        agent.speed = 26f;
                        agent.SetDestination(state.homePosition);
                        Debug.Log($"<color=cyan>[A* Tester] 🏁 {state.obj.name} hedeften ayrıldı, istasyonuna geri dönüyor.</color>");
                    }
                }
                else
                {
                    // SEYİR HALİNDE ÖNDEKİ ARACI TAKİP VE ÇARPIŞMA ÖNLEME:
                    if (aheadVehicle != null && distToAhead < safeDistToAhead + 10.0f)
                    {
                        if (distToAhead <= safeDistToAhead)
                        {
                            // Güvenli mesafeden (10.5m) yakınsa TAM FREN (Öndekinin tamponuna asla girmez)
                            agent.isStopped = true;
                            agent.velocity = Vector3.zero;
                        }
                        else
                        {
                            // 10 metre aralıkta yüksek hızdan kademeli frenleme
                            agent.isStopped = false;
                            float t = Mathf.Clamp01((distToAhead - safeDistToAhead) / 10.0f);
                            agent.speed = Mathf.Lerp(6.0f, 26f, t);
                        }
                    }
                    else
                    {
                        // Önü tamamen boş, tam hız acil müdahale
                        agent.isStopped = false;
                        agent.speed = 26f;
                    }
                }
            }
            else
            {
                // DÖNÜŞ YOLUNDAYIZ:
                if (aheadVehicle != null && distToAhead <= safeDistToAhead)
                {
                    // Sadece hareket yönümüzün ÖNÜNDE araç varsa dururuz (arkamızda kalan araçlar için asla durmayız!)
                    agent.isStopped = true;
                    agent.velocity = Vector3.zero;
                }
                else if (aheadVehicle != null && distToAhead < safeDistToAhead + 10.0f)
                {
                    agent.isStopped = false;
                    float t = Mathf.Clamp01((distToAhead - safeDistToAhead) / 10.0f);
                    agent.speed = Mathf.Lerp(6.0f, 26f, t);
                }
                else
                {
                    agent.isStopped = false;
                    agent.speed = 26f;
                }

                // Kendi kapısına dönüş kontrolü
                Vector3 homeFlat = state.homePosition;
                homeFlat.y = 0f;
                if (!agent.pathPending && (agent.remainingDistance <= agent.stoppingDistance || Vector3.Distance(myPos, homeFlat) <= 2.5f))
                {
                    state.finished = true;
                    state.status = "✅ Kapıya Döndü ve Park Etti";
                    agent.isStopped = true;
                    agent.velocity = Vector3.zero;
                    state.obj.transform.position = state.homePosition;
                    state.obj.transform.rotation = state.homeRotation;
                    Debug.Log($"<color=green>[A* Tester] ✅ {state.obj.name} kendi istasyonuna başarıyla geri dönüp park etti!</color>");
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
