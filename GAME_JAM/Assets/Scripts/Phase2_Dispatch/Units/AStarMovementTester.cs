using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ZeroHour.Phase2.Data;

namespace ZeroHour.Phase2.Units
{
    public class AStarMovementTester : MonoBehaviour
    {
        public static AStarMovementTester Instance { get; private set; }

        /// <summary>Bir aracın görev durumu değiştiğinde (sevk edildi / üsse döndü) tetiklenir.</summary>
        public event Action OnFleetChanged;

        /// <summary>Sevk edilen bir araç olay yerine ulaştığında tetiklenir (birim tipi, sevk hedefi, araç).</summary>
        public event Action<UnitType, Vector3, GameObject> OnVehicleArrived;

        [Header("Debug Paneli (Sol üst OnGUI + Space kısayolu)")]
        [SerializeField] private bool showDebugPanel = true;

        [Header("Hız (1 = orijinal, 0.6 = %40 daha yavaş)")]
        [SerializeField, Range(0.2f, 1f)] private float speedMultiplier = 1f;
        [Tooltip("Helikopter hız çarpanı (kara araçlarından bağımsız: helikopter hızlı ilk müdahalecidir)")]
        [SerializeField, Range(0.2f, 1.5f)] private float helicopterSpeedMultiplier = 1f;

        [Header("İstasyondan Çıkış (araçlar sevk edilince doğar, dönünce kaybolur)")]
        [Tooltip("Aynı istasyondan iki araç arasındaki çıkış aralığı (sn) — üst üste doğmasınlar")]
        [SerializeField] private float groundSpawnInterval = 2.2f;
        [SerializeField] private float helicopterSpawnInterval = 1.5f;
        [Tooltip("Açıkken araçlar sadece görevdeyken görünür (oyun modu)")]
        [SerializeField] private bool spawnOnDispatch = false;

        private readonly Dictionary<UnitType, float> lastSpawnTime = new Dictionary<UnitType, float>();

        [Header("Filo Büyüklüğü (fazlası istasyonda klonlanır; 1 = sadece sahnedeki araç)")]
        [SerializeField, Range(1, 6)] private int policeCount = 1;
        [SerializeField, Range(1, 6)] private int ambulanceCount = 1;
        [SerializeField, Range(1, 6)] private int fireTruckCount = 1;
        [SerializeField, Range(1, 6)] private int helicopterCount = 1;

        private const float BaseCruiseSpeed = 26f;    // ~95 km/s
        private const float BaseMinFollowSpeed = 6f;  // Öndeki araca yaklaşırken inilen en düşük hız
        private float CruiseSpeed => BaseCruiseSpeed * speedMultiplier;
        private float MinFollowSpeed => BaseMinFollowSpeed * speedMultiplier;

        private readonly Dictionary<GameObject, UnitType> fleetTypes = new Dictionary<GameObject, UnitType>();

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

            // Sevk sistemi: her araç kendi görev hedefini ve park kuyruğunu taşır
            public UnitType unitType;
            public bool inMission = false;
            public bool arrivedReported = false;
            public Vector3 missionTarget;
            public Vector3 missionRoadTarget;
            public List<GameObject> parkQueue;

            // Olay yerinde iş bitene kadar bekleme (sevk görevleri). Test modunda false.
            public bool holdAtSite = false;
            public bool released = false;
            public Vector3 hoverOffset;   // Aynı olaya giden helikopterler üst üste binmesin

            // Takılma kurtarıcısı: çok araçlı sıkışmalarda (3+ araçlık yol verme döngüsü) radarı kısa süre yok say
            public float stuckTime;
            public float radarBypassUntil;

            // İstasyondan sıralı çıkış: araç görünmezken sevk edilirse spawnAt anında istasyonda doğar
            public bool spawnPending;
            public float spawnAt;
        }

        private readonly List<VehicleTestState> vehicleStates = new List<VehicleTestState>();
        private bool isTestActive = false;
        private GameObject targetVisualMarker;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            AutoAssignFourUnits();

            // Faz 1'de satın alınan ekip (yoksa sahnedeki varsayılan filo)
            if (ZeroHour.Shared.RunLoadout.IsSet)
            {
                policeCount = ZeroHour.Shared.RunLoadout.Police;
                ambulanceCount = ZeroHour.Shared.RunLoadout.Ambulance;
                fireTruckCount = ZeroHour.Shared.RunLoadout.FireTruck;
                helicopterCount = ZeroHour.Shared.RunLoadout.Helicopter;
            }
            CreateFleetPool();
            InitializeVehicles();
            CreateTargetMarker();

            // Oyun modu: araçlar istasyonda beklerken görünmez; sevk edilince doğar
            if (spawnOnDispatch)
            {
                foreach (var state in vehicleStates)
                {
                    if (state.obj != null) state.obj.SetActive(false);
                }
            }
        }

        // ==========================================
        // FİLO HAVUZU: Her tipten istenen sayıda araç. Hepsi aynı istasyon noktasını kullanır
        // (aynı noktada doğar, aynı noktada kaybolur); sıralı çıkış üst üste binmeyi önler.
        // ==========================================

        private void CreateFleetPool()
        {
            fleetTypes.Clear();
            if (ambulance != null) fleetTypes[ambulance] = UnitType.Ambulance;
            if (policeCar != null) fleetTypes[policeCar] = UnitType.PoliceCruiser;
            if (fireTruck != null) fleetTypes[fireTruck] = UnitType.FireEngine;
            if (helicopter != null) fleetTypes[helicopter] = UnitType.RescueHelicopter;

            AddPoolCopies(policeCar, UnitType.PoliceCruiser, policeCount);
            AddPoolCopies(ambulance, UnitType.Ambulance, ambulanceCount);
            AddPoolCopies(fireTruck, UnitType.FireEngine, fireTruckCount);
            AddPoolCopies(helicopter, UnitType.RescueHelicopter, helicopterCount);
        }

        private void AddPoolCopies(GameObject original, UnitType type, int totalCount)
        {
            if (original == null) return;
            for (int i = 1; i < totalCount; i++)
            {
                GameObject copy = Instantiate(original, original.transform.position, original.transform.rotation);
                copy.name = $"{original.name} #{i + 1}";
                fleetTypes[copy] = type;
                vehicles.Add(copy);
            }
        }

        private UnitType ResolveUnitType(GameObject v, bool isHeli)
        {
            if (fleetTypes.TryGetValue(v, out UnitType type)) return type;
            if (isHeli) return UnitType.RescueHelicopter;
            if (v == fireTruck) return UnitType.FireEngine;
            if (v == policeCar) return UnitType.PoliceCruiser;
            return UnitType.Ambulance;
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
                        if (go.transform is RectTransform) continue; // UI yazıları (örn. "Label_Ambulance") araç sanılmasın
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
                        if (go.transform is RectTransform) continue; // UI yazıları (örn. "Label_Ambulance") araç sanılmasın
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
                        if (go.transform is RectTransform) continue; // UI yazıları (örn. "Label_Ambulance") araç sanılmasın
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
                        if (go.transform is RectTransform) continue; // UI yazıları (örn. "Label_Ambulance") araç sanılmasın
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
                    isHelicopter = isHeli,
                    unitType = ResolveUnitType(v, isHeli)
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
                    if (state.unitType == UnitType.FireEngine) agent.avoidancePriority = 30;
                    else if (state.unitType == UnitType.Ambulance) agent.avoidancePriority = 40;
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

                    agent.speed = CruiseSpeed; // Acil müdahale hızı (çarpan 1'de ~95 km/s)
                    agent.acceleration = 45f * speedMultiplier;
                    agent.angularSpeed = 540f;
                    agent.stoppingDistance = 1.0f;
                    agent.radius = (state.unitType == UnitType.FireEngine) ? 1.6f : 1.2f;
                    agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
                    agent.autoBraking = true;

                    state.agent = agent;
                }

                vehicleStates.Add(state);

                // Görevdeyken yanıp sönen tepe lambası
                EmergencyLightBar lightBar = v.GetComponent<EmergencyLightBar>();
                if (lightBar == null)
                {
                    lightBar = v.AddComponent<EmergencyLightBar>();
                    lightBar.Initialize(this, state.isHelicopter);
                }

                // Görevdeyken siren / pervane sesi
                if (v.GetComponent<UnitAudio>() == null)
                    v.AddComponent<UnitAudio>().Initialize(this, state.unitType);
            }

            Debug.Log($"<color=cyan>[A* Tester] 4 ana araç teste bağlandı ({vehicleStates.Count} araç): Kazasız Konvoy Radarı ve Asfalt Cadde Park Sistemi aktif edildi.</color>");
            OnFleetChanged?.Invoke();
        }

        private void CreateTargetMarker()
        {
            if (targetVisualMarker != null) Destroy(targetVisualMarker);
            if (!showDebugPanel) return; // Oyun modunda kırmızı test diski gösterilmez

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
            if (showDebugPanel && IsSpacePressed())
            {
                StartAStarTest();
            }

            if (!isTestActive) return;

            foreach (var state in vehicleStates)
            {
                if (state.finished || state.obj == null || !state.inMission) continue;

                // İstasyondan sıralı çıkış: sırası gelen araç istasyon noktasında doğar
                if (state.spawnPending)
                {
                    if (Time.time >= state.spawnAt) SpawnAtStation(state);
                    continue;
                }

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
                BeginMission(state, targetPosition, roadTarget, parkedAtTarget);
            }

            OnFleetChanged?.Invoke();
        }

        private void BeginMission(VehicleTestState state, Vector3 target, Vector3 roadTarget, List<GameObject> queue)
        {
            state.timer = 0f;
            state.returning = false;
            state.finished = false;
            state.inMission = true;
            state.arrivedReported = false;
            state.missionTarget = target;
            state.missionRoadTarget = roadTarget;
            state.parkQueue = queue;
            state.holdAtSite = false;
            state.released = false;
            state.hoverOffset = Vector3.zero;
            state.stuckTime = 0f;
            state.radarBypassUntil = 0f;
            state.status = "Yolda (Hedefe Gidiyor...)";

            if (state.obj != null && !state.obj.activeSelf)
            {
                // Araç istasyonda görünmez bekliyor: aynı tipteki bir önceki çıkıştan sonra sırası gelince doğacak
                float interval = state.isHelicopter ? helicopterSpawnInterval : groundSpawnInterval;
                float last = lastSpawnTime.TryGetValue(state.unitType, out float t) ? t : -999f;
                state.spawnAt = Mathf.Max(Time.time, last + interval);
                lastSpawnTime[state.unitType] = state.spawnAt;
                state.spawnPending = true;
                state.status = "İstasyondan çıkmayı bekliyor...";
                return;
            }

            state.spawnPending = false;
            StartDriving(state);
        }

        private void StartDriving(VehicleTestState state)
        {
            if (!state.isHelicopter && state.agent != null && state.agent.isOnNavMesh)
            {
                state.agent.isStopped = false;
                state.agent.ResetPath();
                state.agent.speed = CruiseSpeed;

                // Bütün araçlar yolun orta asfaltındaki cadde hedefine yönelir.
                // Birbirinin arkasında durma ve mesafe koruma işini UpdateGroundAgentMovement yapar.
                state.agent.SetDestination(state.missionRoadTarget);
            }
        }

        private void SpawnAtStation(VehicleTestState state)
        {
            state.spawnPending = false;
            state.obj.transform.SetPositionAndRotation(state.homePosition, state.homeRotation);
            state.obj.SetActive(true);

            if (!state.isHelicopter && state.agent != null)
            {
                state.agent.enabled = true;
                state.agent.Warp(state.homePosition);
            }

            state.status = "Yolda (Hedefe Gidiyor...)";
            StartDriving(state);
            OnFleetChanged?.Invoke();
        }

        // ==========================================
        // SEVK (DISPATCH) API'Sİ — Kaza Dosyası buradan araç gönderir
        // ==========================================

        /// <summary>Araç şu an görevde mi? (yolda, olay yerinde veya dönüşte) — tepe lambası için.</summary>
        public bool IsVehicleOnMission(GameObject vehicle)
        {
            foreach (var s in vehicleStates)
            {
                if (s.obj == vehicle) return s.inMission;
            }
            return false;
        }

        /// <summary>Bu tipte üssünde bekleyen (görevde olmayan) araç sayısı.</summary>
        public int GetAvailableCount(UnitType type)
        {
            int count = 0;
            foreach (var s in vehicleStates)
            {
                if (s.obj != null && s.unitType == type && !s.inMission) count++;
            }
            return count;
        }

        /// <summary>Bu tipteki toplam araç sayısı.</summary>
        public int GetTotalCount(UnitType type)
        {
            int count = 0;
            foreach (var s in vehicleStates)
            {
                if (s.obj != null && s.unitType == type) count++;
            }
            return count;
        }

        /// <summary>Bu tipteki ilk aracın üs konumu (mesafe hesabı için). Araç yoksa false.</summary>
        public bool TryGetHomePosition(UnitType type, out Vector3 home)
        {
            foreach (var s in vehicleStates)
            {
                if (s.obj != null && s.unitType == type)
                {
                    home = s.homePosition;
                    return true;
                }
            }
            home = Vector3.zero;
            return false;
        }

        /// <summary>
        /// Seçilen birim tiplerinden boşta olan araçları verilen olay yerine gönderir.
        /// Hareket, konvoy mesafesi ve VTOL mantığı test moduyla birebir aynıdır.
        /// </summary>
        /// <returns>Yola çıkan araç sayısı.</returns>
        public int DispatchVehicles(Vector3 siteTarget, IList<UnitType> types, List<GameObject> dispatched = null)
        {
            Vector3 roadTarget = GetRoadTargetNear(siteTarget);

            // Takviye: aynı olay yerinde zaten araç varsa onların park kuyruğuna katıl (üst üste park etmesinler)
            List<GameObject> queue = null;
            int helisSent = 0;
            foreach (var other in vehicleStates)
            {
                if (!other.inMission || other.released || (other.missionTarget - siteTarget).sqrMagnitude > 0.01f) continue;
                if (queue == null && other.parkQueue != null) queue = other.parkQueue;
                if (other.isHelicopter) helisSent++; // Yeni helikopterler havada farklı noktada beklesin
            }
            if (queue == null) queue = new List<GameObject>();
            int sent = 0;

            foreach (UnitType type in types)
            {
                foreach (var state in vehicleStates)
                {
                    if (state.obj == null || state.unitType != type || state.inMission) continue;
                    BeginMission(state, siteTarget, roadTarget, queue);
                    state.holdAtSite = true; // İş bitene kadar olay yerinde kal; IncidentBoard.ReleaseSite ile döner

                    if (state.isHelicopter)
                    {
                        // 1. helikopter tam üstte, diğerleri çevresinde 8 m aralıkla
                        if (helisSent > 0)
                        {
                            float angle = helisSent * 2.0944f; // 120°
                            state.hoverOffset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 8f;
                        }
                        helisSent++;
                    }

                    sent++;
                    if (dispatched != null) dispatched.Add(state.obj);
                    break;
                }
            }

            if (sent > 0)
            {
                isTestActive = true;
                OnFleetChanged?.Invoke();
            }
            return sent;
        }

        /// <summary>Olay binasına en yakın asfalt (NavMesh) noktası.</summary>
        public Vector3 GetRoadTargetNear(Vector3 site)
        {
            if (NavMesh.SamplePosition(site, out NavMeshHit hit, 40.0f, NavMesh.AllAreas))
            {
                return hit.position;
            }
            if (NavMesh.SamplePosition(site, out NavMeshHit broadHit, 120.0f, NavMesh.AllAreas))
            {
                return broadHit.position;
            }
            return site;
        }

        private void ReportArrival(VehicleTestState state)
        {
            if (state.arrivedReported) return;
            state.arrivedReported = true;
            OnVehicleArrived?.Invoke(state.unitType, state.missionTarget, state.obj);
        }

        /// <summary>Aracın birim tipi.</summary>
        public UnitType GetUnitType(GameObject vehicle)
        {
            foreach (var state in vehicleStates)
            {
                if (state.obj == vehicle) return state.unitType;
            }
            return UnitType.Ambulance;
        }

        /// <summary>Bu olay yerine yolda olan (henüz varmamış) araç sayısı.</summary>
        public int CountVehiclesEnRoute(Vector3 siteTarget)
        {
            int count = 0;
            foreach (var state in vehicleStates)
            {
                if (!state.inMission || state.arrivedReported || state.released || state.returning) continue;
                if ((state.missionTarget - siteTarget).sqrMagnitude > 0.01f) continue;
                count++;
            }
            return count;
        }

        /// <summary>Tek bir aracı olay yerinden serbest bırakır (örn. suyu / yakıtı biten helikopter).</summary>
        public void ReleaseVehicle(GameObject vehicle)
        {
            foreach (var state in vehicleStates)
            {
                if (state.obj != vehicle || !state.inMission || state.released) continue;
                state.released = true;
                state.timer = 0f;
                return;
            }
        }

        /// <summary>
        /// Olay bittiğinde o olay yerindeki tüm araçları serbest bırakır; araçlar kazasız ters sırayla üsse döner.
        /// Henüz yolda olan araçlar varınca hemen dönüşe geçer.
        /// </summary>
        public void ReleaseSite(Vector3 siteTarget)
        {
            foreach (var state in vehicleStates)
            {
                if (!state.inMission || !state.holdAtSite || state.released) continue;
                if ((state.missionTarget - siteTarget).sqrMagnitude > 0.01f) continue;

                state.released = true;
                state.timer = 0f;
            }
        }

        private void FinishMission(VehicleTestState state)
        {
            state.inMission = false;

            // Oyun modu: istasyona dönen araç kaybolur (bir sonraki sevkte aynı noktada yeniden doğar)
            if (spawnOnDispatch && state.obj != null) state.obj.SetActive(false);

            OnFleetChanged?.Invoke();
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
                if (other == current || other.isHelicopter || other.obj == null || other.finished || !other.inMission) continue;
                if (other.spawnPending || !other.obj.activeInHierarchy) continue;

                // Trafik sıkışmasın: sadece (a) aynı olay yerine sıraya giren araçlar ve
                // (b) aynı yöne hareket eden araçlar için fren yapılır. Başka olay yerinde park etmiş,
                // karşıdan gelen veya duran araçların arasından NavMesh çarpışma önleyicisi geçirir.
                bool sameSiteQueue = !current.returning && !other.returning && current.parkQueue != null && other.parkQueue == current.parkQueue;
                bool movingSameWay = false;
                if (other.agent != null && other.agent.velocity.sqrMagnitude > 1f)
                {
                    Vector3 otherVel = other.agent.velocity;
                    otherVel.y = 0f;
                    movingSameWay = Vector3.Dot(myMoveDir, otherVel.normalized) > 0.3f;
                }
                if (!sameSiteQueue && !movingSameWay) continue;

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

        /// <summary>
        /// Öndeki araç için durmalı mıyım? Normalde evet. Ancak iki araç birbirine dönük duruyorsa
        /// (ikisi de diğerini "önümde" görüyor) sonsuz kilitlenme olur: listede önce gelen geçer, diğeri bekler.
        /// </summary>
        private bool ShouldYieldTo(VehicleTestState me, VehicleTestState ahead)
        {
            if (ahead == null) return false;
            if (Time.time < me.radarBypassUntil) return false; // Takılma kurtarıcısı aktif

            VehicleTestState theirAhead = GetVehicleAheadInMyPath(ahead, out _);
            if (theirAhead != me) return true;

            return vehicleStates.IndexOf(me) > vehicleStates.IndexOf(ahead);
        }

        private const float StuckBypassSeconds = 2.5f;     // Bu kadar duran araç radarı geçici olarak yok sayar
        private const float StuckBypassDuration = 2.5f;
        private const float StuckJoinQueueSeconds = 1.2f;  // Olay yerine yakın takılan araç olduğu yerde sıraya girer
        private const float StuckJoinQueueRadius = 45f;

        /// <summary>
        /// Yolda (gidiş / dönüş) hareket etmeyen aracın süresini sayar. Normal takip/fren akışını değiştirmez;
        /// sadece 2.5 sn'den uzun süren sıkışmalarda radarı 2.5 sn kapatır (NavMesh çarpışma önleyici devrede kalır).
        /// </summary>
        private void UpdateStuckRecovery(VehicleTestState state, NavMeshAgent agent, List<GameObject> queue)
        {
            bool parked = !state.returning && queue.Contains(state.obj);
            if (parked || agent.velocity.sqrMagnitude > 0.25f)
            {
                state.stuckTime = 0f;
                return;
            }

            state.stuckTime += Time.deltaTime;
            if (state.stuckTime >= StuckBypassSeconds && Time.time >= state.radarBypassUntil)
            {
                state.radarBypassUntil = Time.time + StuckBypassDuration;
                state.stuckTime = 0f;
            }
        }

        private void UpdateGroundAgentMovement(VehicleTestState state)
        {
            NavMeshAgent agent = state.agent;
            if (agent == null || !agent.isOnNavMesh) return;

            // Her görevin kendi park kuyruğu ve hedefi vardır (aynı anda farklı olay yerleri)
            List<GameObject> parkedAtTarget = state.parkQueue ?? this.parkedAtTarget;

            // Sadece hareket yönümüzün ÖNÜNDE kalan aracı kontrol et
            VehicleTestState aheadVehicle = GetVehicleAheadInMyPath(state, out float distToAhead);
            float safeDistToAhead = (aheadVehicle != null) ? GetSafeDistance(state.obj, aheadVehicle.obj) : 10.5f;

            Vector3 roadTarget = state.missionRoadTarget;
            Vector3 myPos = state.obj.transform.position;
            myPos.y = 0f;
            Vector3 targetPos = roadTarget;
            targetPos.y = 0f;
            float distToTarget = Vector3.Distance(myPos, targetPos);

            UpdateStuckRecovery(state, agent, parkedAtTarget);

            if (!state.returning)
            {
                // DİNAMİK OLAY YERİ PARK KUYRUĞU:
                // İlk kim varırsa (Ambulans, İtfaiye ya da Polis) 1. sıraya park eder.
                // 2. varan araç, 1. varanın güvenli mesafesinde (10.5m) 2. sıraya park eder.
                // 3. varan araç, 2. varanın güvenli mesafesinde (9.0m - 10.5m) 3. sıraya park eder.

                bool isAlreadyParked = parkedAtTarget.Contains(state.obj);

                // Olay yerine yakınken başka araçların arkasında takıldıysa olduğu yerde sıraya girer
                bool stuckNearSite = distToTarget <= StuckJoinQueueRadius && state.stuckTime >= StuckJoinQueueSeconds;

                if (!isAlreadyParked)
                {
                    if (parkedAtTarget.Count == 0)
                    {
                        // İlk varan araç: Hedefe doğrudan yaklaşınca 1. sırada park eder
                        if (distToTarget <= 3.5f || (!agent.pathPending && agent.remainingDistance <= 2.5f) || stuckNearSite)
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
                        if (distToLead <= safeDistToLead + 0.5f || stuckNearSite)
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
                    ReportArrival(state);
                    agent.isStopped = true;
                    agent.velocity = Vector3.zero;
                    if (state.holdAtSite && !state.released)
                    {
                        // Sevk görevi: olay bitene kadar olay yerinde çalışır (IncidentBoard serbest bırakır)
                        state.status = $"Olay yerinde çalışıyor ({parkedAtTarget.IndexOf(state.obj) + 1}. sırada)...";
                    }
                    else
                    {
                        state.status = $"Hedefe vardı ({parkedAtTarget.IndexOf(state.obj) + 1}. Sırada bekliyor) ({Mathf.CeilToInt(waitDurationAtTarget - state.timer)}s)...";
                        state.timer += Time.deltaTime;
                    }

                    // KAZASIZ TERS SIRALI AYRILIŞ:
                    // Kuyrukta en arkadaki araç (en son gelen) ilk ayrılır, çünkü arkası tamamen açıktır!
                    // En arkadaki: 0.0s gecikme, bir öndeki: 2.0s gecikme, en öndeki: 4.0s gecikme
                    int myQueueIndex = parkedAtTarget.IndexOf(state.obj);
                    int totalParked = parkedAtTarget.Count;
                    float returnStagger = 0f;

                    int reverseIndex = (totalParked - 1) - myQueueIndex;
                    if (reverseIndex < 0) reverseIndex = 0;
                    returnStagger = reverseIndex * (state.holdAtSite ? 1.2f : 2.0f); // Sevk görevlerinde daha seri ayrılış

                    // Sevk görevinde bekleme süresi işin kendisidir; serbest bırakılınca sadece sıralı ayrılış gecikmesi kalır
                    float requiredWait = state.holdAtSite ? 0.5f : waitDurationAtTarget;
                    bool mayLeave = !state.holdAtSite || state.released;

                    if (mayLeave && state.timer >= requiredWait + returnStagger)
                    {
                        state.returning = true;
                        state.timer = 0f;
                        state.status = "Üsse Geri Dönüyor (A*)...";
                        if (state.holdAtSite) parkedAtTarget.Remove(state.obj); // Takviyeler gitmiş aracı "öndeki" sanmasın
                        agent.ResetPath();
                        agent.isStopped = false;
                        agent.speed = CruiseSpeed;
                        agent.SetDestination(state.homePosition);
                        Debug.Log($"<color=cyan>[A* Tester] 🏁 {state.obj.name} hedeften ayrıldı, istasyonuna geri dönüyor.</color>");
                    }
                }
                else
                {
                    // SEYİR HALİNDE ÖNDEKİ ARACI TAKİP VE ÇARPIŞMA ÖNLEME:
                    if (aheadVehicle != null && distToAhead < safeDistToAhead + 10.0f && ShouldYieldTo(state, aheadVehicle))
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
                            agent.speed = Mathf.Lerp(MinFollowSpeed, CruiseSpeed, t);
                        }
                    }
                    else
                    {
                        // Önü tamamen boş, tam hız acil müdahale
                        agent.isStopped = false;
                        agent.speed = CruiseSpeed;
                    }
                }
            }
            else
            {
                // DÖNÜŞ YOLUNDAYIZ:
                bool yieldToAhead = ShouldYieldTo(state, aheadVehicle);
                if (aheadVehicle != null && distToAhead <= safeDistToAhead && yieldToAhead)
                {
                    // Sadece hareket yönümüzün ÖNÜNDE araç varsa dururuz (arkamızda kalan araçlar için asla durmayız!)
                    agent.isStopped = true;
                    agent.velocity = Vector3.zero;
                }
                else if (aheadVehicle != null && distToAhead < safeDistToAhead + 10.0f && yieldToAhead)
                {
                    agent.isStopped = false;
                    float t = Mathf.Clamp01((distToAhead - safeDistToAhead) / 10.0f);
                    agent.speed = Mathf.Lerp(MinFollowSpeed, CruiseSpeed, t);
                }
                else
                {
                    agent.isStopped = false;
                    agent.speed = CruiseSpeed;
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
                    FinishMission(state);
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
            float heliFlightSpeed = 36f * helicopterSpeedMultiplier; // Yollardan bağımsız, hızlı ilk müdahale
            float verticalSpeed = 14f * helicopterSpeedMultiplier;

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
                Vector3 targetPosition = state.missionTarget + state.hoverOffset;
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
                    ReportArrival(state);
                    bool working = state.holdAtSite && !state.released;
                    state.status = working
                        ? "Olay yerinde havada çalışıyor..."
                        : $"Hedefin üstünde asılı! Bekliyor ({Mathf.CeilToInt(waitDurationAtTarget - state.timer)}s)...";
                    if (!working) state.timer += Time.deltaTime;

                    float requiredHover = state.holdAtSite ? 0.5f : waitDurationAtTarget;
                    if (!working && state.timer >= requiredHover)
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
                    t.position = Vector3.MoveTowards(t.position, state.homePosition, 10f * helicopterSpeedMultiplier * Time.deltaTime);
                    t.rotation = Quaternion.Slerp(t.rotation, state.homeRotation, 8f * Time.deltaTime);

                    if (Vector3.Distance(t.position, state.homePosition) < 0.25f)
                    {
                        t.position = state.homePosition;
                        t.rotation = state.homeRotation;
                        state.finished = true;
                        state.status = "✅ Helipade İndi ve Park Etti";
                        FinishMission(state);
                        Debug.Log("<color=green>[A* Tester] Helikopter helipade dikey iniş yaptı ve park etti! ✅</color>");
                    }
                }
            }
        }

        private void OnGUI()
        {
            if (!showDebugPanel) return;

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
