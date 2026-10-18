using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using ZeroHour.Phase2.Core;
using ZeroHour.Phase2.Data;
using ZeroHour.Phase2.Units;

namespace ZeroHour.Phase2.Calls
{
    /// <summary>
    /// Haritadaki aktif olayları yönetir: olay binası seçer, rozet çıkarır, süre sayar,
    /// rozet tıklamasını yakalar, sevk/ret sonucunu nüfusa işler.
    /// </summary>
    public class IncidentBoard : MonoBehaviour
    {
        public static IncidentBoard Instance { get; private set; }

        [Header("Rozet")]
        [SerializeField] private IncidentMarker markerPrefab;
        [SerializeField] private Sprite fireMarkerSprite;
        [SerializeField] private Sprite collapseMarkerSprite;

        [Header("Olay Kuralları")]
        [Tooltip("Ekip gönderilmezse bu süre sonunda yaralılar kaybedilir ve rozet kalkar (sn)")]
        [SerializeField] private float unattendedTimeout = 45f;
        [Tooltip("Ekip yola çıktıktan sonra bu süre içinde varamazsa olay yine de kapanır (rozet takılı kalmasın)")]
        [SerializeField] private float respondingTimeout = 90f;

        [Header("Olay Yerinde İş Süresi (1 gerekli ekiple, sn)")]
        [Tooltip("Aynı işi yapan her ek araç işi hızlandırır: 3 itfaiye = 3 kat hızlı")]
        [SerializeField] private float fireWorkSeconds = 24f;
        [SerializeField] private float collapseWorkSeconds = 30f;
        [Tooltip("İşe katkısı olmayan ekipler gönderilse bile iş bu en düşük hızla ilerler")]
        [SerializeField] private float minimumWorkRate = 0.25f;
        [SerializeField] private int minInjured = 2;
        [SerializeField] private int maxInjured = 8;
        [SerializeField] private int maxActiveIncidents = 3;
        [Tooltip("Olay adayı bina kök objelerinin ad öneki")]
        [SerializeField] private string buildingNamePrefix = "Building";
        [Tooltip("Olay çıkmayacak binalar (istasyon binaları: üstlerinde kurum logosu var)")]
        [SerializeField] private List<Transform> excludedBuildings = new List<Transform>();

        [Header("Vardiya Saati")]
        [Tooltip("Gerçek 1 saniye = oyunda kaç dakika")]
        [SerializeField] private float gameMinutesPerSecond = 1f;
        [SerializeField] private int shiftStartHour = 2;

        private readonly List<IncidentReport> activeIncidents = new List<IncidentReport>();
        private readonly List<Transform> candidateBuildings = new List<Transform>();
        private int nextCaseNumber = 147;

        public IReadOnlyList<IncidentReport> ActiveIncidents => activeIncidents;

        public event Action<IncidentReport> OnIncidentCreated;
        public event Action<IncidentReport> OnIncidentClosed;

        // Kurum renkleri: itfaiye kırmızı, polis mavi, ambulans beyaz (krem kağıtta okunsun diye koyu zemin)
        private const string FireTruckTag = "<color=#D63A2A>Fire Truck</color>";
        private const string PoliceTag = "<color=#2563D9>Police</color>";
        private const string AmbulanceTag = "<mark=#2A231BE6><color=#FFFFFF> Ambulance </color></mark>";

        public int WaitingCount
        {
            get
            {
                int count = 0;
                foreach (IncidentReport r in activeIncidents)
                {
                    if (r.status == CallIncidentStatus.Waiting) count++;
                }
                return count;
            }
        }

        private static readonly string[] Streets =
        {
            "Oak Street", "Cedar Avenue", "Harbor Road", "Linden Street",
            "Maple Avenue", "Station Road", "Mill Lane", "Park Street"
        };

        private static readonly string[] FireNotes =
        {
            "Gas shut-off valve was never installed. Fire may spread to the house next door.",
            "Kitchen fire spreading upward. Residents smell gas on the stairs.",
            "Electrical panel caught fire in the basement. Smoke on every floor."
        };

        private static readonly string[] CollapseNotes =
        {
            "Cracked columns were reported at inspection. Building partly collapsed, people trapped.",
            "Unsecured shelves and wardrobes fell over. Residents trapped inside.",
            "A load-bearing column was removed downstairs. Upper floors came down."
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (AStarMovementTester.Instance != null) AStarMovementTester.Instance.OnVehicleArrived -= HandleVehicleArrived;
        }

        private void Start()
        {
            CollectCandidateBuildings();

            if (AStarMovementTester.Instance != null)
            {
                AStarMovementTester.Instance.OnVehicleArrived += HandleVehicleArrived;
            }
        }

        private void CollectCandidateBuildings()
        {
            candidateBuildings.Clear();
            foreach (Transform t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (t.parent != null || !t.gameObject.activeInHierarchy) continue;
                if (!t.name.StartsWith(buildingNamePrefix)) continue;
                if (t.GetComponentInChildren<Renderer>() == null) continue;
                if (excludedBuildings.Contains(t)) continue;
                candidateBuildings.Add(t);
            }
        }

        // ==========================================
        // OLAY OLUŞTURMA
        // ==========================================

        public bool CanCreateIncident()
        {
            return activeIncidents.Count < maxActiveIncidents && activeIncidents.Count < candidateBuildings.Count;
        }

        public string GetClockTime()
        {
            if (ShiftClock.Instance != null) return ShiftClock.Instance.GetClockTime();

            int totalMinutes = shiftStartHour * 60 + Mathf.FloorToInt(Time.timeSinceLevelLoad * gameMinutesPerSecond);
            return $"{(totalMinutes / 60) % 24:00}:{totalMinutes % 60:00}";
        }

        public IncidentReport CreateRandomIncident()
        {
            if (!CanCreateIncident()) return null;

            Transform building = PickFreeBuilding();
            if (building == null) return null;

            Bounds bounds = CalculateBounds(building);
            CallIncidentKind kind = UnityEngine.Random.value < 0.5f ? CallIncidentKind.Fire : CallIncidentKind.Collapse;
            string street = Streets[UnityEngine.Random.Range(0, Streets.Length)];
            int injured = UnityEngine.Random.Range(minInjured, maxInjured + 1);

            IncidentReport report = new IncidentReport
            {
                caseNumber = nextCaseNumber++,
                kind = kind,
                building = building,
                buildingBounds = bounds,
                sitePosition = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z),
                markerAnchor = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z),
                incidentTitle = kind == CallIncidentKind.Fire ? "Residential fire" : "Earthquake collapse",
                possibleUnitsText = kind == CallIncidentKind.Fire
                    ? $"Possible units: {FireTruckTag}, {AmbulanceTag}"
                    : $"Possible units: {AmbulanceTag}, {FireTruckTag}, {PoliceTag}",
                address = $"{UnityEngine.Random.Range(2, 99)} {street}",
                streetShort = ShortStreet(street),
                notes = kind == CallIncidentKind.Fire
                    ? FireNotes[UnityEngine.Random.Range(0, FireNotes.Length)]
                    : CollapseNotes[UnityEngine.Random.Range(0, CollapseNotes.Length)],
                callTime = GetClockTime(),
                line = UnityEngine.Random.Range(1, 5),
                injured = injured,
                affected = injured + UnityEngine.Random.Range(2, 7),
                urgent = injured >= 5,
                timeRemaining = unattendedTimeout,
                timeLimit = unattendedTimeout,
                workRequired = kind == CallIncidentKind.Fire ? fireWorkSeconds : collapseWorkSeconds
            };

            if (markerPrefab != null)
            {
                IncidentMarker marker = Instantiate(markerPrefab, report.markerAnchor, Quaternion.identity, transform);
                marker.name = $"IncidentMarker_{report.caseNumber}";
                marker.Initialize(report, kind == CallIncidentKind.Fire ? fireMarkerSprite : collapseMarkerSprite);
                report.marker = marker;
            }

            activeIncidents.Add(report);
            if (CityStatsManager.Instance != null) CityStatsManager.Instance.AddCiviliansAtRisk(report.injured);

            Debug.Log($"<color=orange>[Incidents] 📍 Case #{report.caseNumber}: {report.incidentTitle} @ {building.name} ({report.injured} yaralı)</color>");
            OnIncidentCreated?.Invoke(report);
            return report;
        }

        private Transform PickFreeBuilding()
        {
            List<Transform> free = new List<Transform>();
            AStarMovementTester fleet = AStarMovementTester.Instance;
            foreach (Transform b in candidateBuildings)
            {
                if (b == null) continue;
                if (fleet != null && fleet.IsHeliRoof(b)) continue; // Çatısında helikopter park eden bina
                bool used = false;
                foreach (IncidentReport r in activeIncidents)
                {
                    if (r.building == b) { used = true; break; }
                }
                if (!used) free.Add(b);
            }
            return free.Count > 0 ? free[UnityEngine.Random.Range(0, free.Count)] : null;
        }

        private static Bounds CalculateBounds(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static string ShortStreet(string street)
        {
            string[] parts = street.Split(' ');
            string suffix = parts[1] == "Street" ? "ST." : parts[1] == "Avenue" ? "AVE." : parts[1] == "Road" ? "RD." : "LN.";
            return $"{parts[0].ToUpperInvariant()} {suffix}";
        }

        // ==========================================
        // OYUN DÖNGÜSÜ: Süre + Rozet Tıklama
        // ==========================================

        private void Update()
        {
            for (int i = activeIncidents.Count - 1; i >= 0; i--)
            {
                IncidentReport r = activeIncidents[i];

                if (r.status == CallIncidentStatus.Responding)
                {
                    // Güvence: ekip yolda takılırsa rozet sonsuza kadar kalmasın
                    r.respondingTime += Time.deltaTime;
                    if (r.respondingTime >= respondingTimeout) ResolveIncident(r);
                    continue;
                }

                if (r.status == CallIncidentStatus.Working)
                {
                    // İş hızı = olay yerindeki ekiplerin katkı toplamı (3 itfaiye = 3 kat hızlı)
                    float rate = 0f;
                    foreach (UnitType role in r.onSiteRoles) rate += r.RoleWeight(role);
                    r.currentWorkRate = Mathf.Max(minimumWorkRate, rate);
                    r.workDone += Time.deltaTime * r.currentWorkRate;
                    if (r.workDone >= r.workRequired) ResolveIncident(r);
                    continue;
                }
                if (r.status != CallIncidentStatus.Waiting) continue;

                r.timeRemaining -= Time.deltaTime;
                if (r.timeRemaining <= 0f)
                {
                    // Ekip gönderilmedi: yaralılar kaybedildi
                    if (CityStatsManager.Instance != null) CityStatsManager.Instance.RegisterCasualty(r.injured);
                    Debug.Log($"<color=red>[Incidents] ⏱️ Case #{r.caseNumber} zamanında müdahale görmedi. {r.injured} sivil kaybedildi.</color>");
                    CloseIncident(r);
                }
            }

            HandleMarkerClick();
        }

        private void HandleMarkerClick()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
            if (IncidentFileUI.Instance != null && IncidentFileUI.Instance.IsOpen) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
            RaycastHit[] hits = Physics.RaycastAll(ray, 5000f, ~0, QueryTriggerInteraction.Collide);

            IncidentMarker closest = null;
            float closestDist = float.MaxValue;
            foreach (RaycastHit hit in hits)
            {
                IncidentMarker marker = hit.collider.GetComponentInParent<IncidentMarker>();
                if (marker != null && hit.distance < closestDist)
                {
                    closest = marker;
                    closestDist = hit.distance;
                }
            }

            if (closest != null) OpenIncidentFile(closest.Report);
        }

        public void OpenIncidentFile(IncidentReport report)
        {
            if (report == null || IncidentFileUI.Instance == null) return;
            IncidentFileUI.Instance.Open(report);
        }

        // ==========================================
        // SEVK / RET / SONUÇ
        // ==========================================

        /// <summary>Seçilen birimleri olay yerine gönderir. Yola çıkan araç sayısını döndürür.</summary>
        public int Dispatch(IncidentReport report, IList<UnitType> units)
        {
            return Dispatch(report, units, null);
        }

        /// <param name="heliRoles">Her helikopterin rolü (ambulans / polis / itfaiye). Boşsa helikopter gerekli ekip rolünü alır.</param>
        public int Dispatch(IncidentReport report, IList<UnitType> units, IList<UnitType> heliRoles)
        {
            if (report == null || report.status != CallIncidentStatus.Waiting) return 0;
            if (AStarMovementTester.Instance == null || units.Count == 0) return 0;

            int sent = AStarMovementTester.Instance.DispatchVehicles(report.sitePosition, units);
            if (sent <= 0) return 0;

            report.status = CallIncidentStatus.Responding;
            report.sentUnits.AddRange(units);

            int heliIndex = 0;
            foreach (UnitType unit in units)
            {
                if (unit != UnitType.RescueHelicopter)
                {
                    report.sentRoles.Add(unit);
                    continue;
                }

                UnitType role = (heliRoles != null && heliIndex < heliRoles.Count) ? heliRoles[heliIndex] : report.RequiredUnit;
                report.heliRoles.Add(role);
                report.sentRoles.Add(role);
                heliIndex++;
            }
            if (report.marker != null) report.marker.SetResponding();

            Debug.Log($"<color=cyan>[Incidents] 🚨 Case #{report.caseNumber}: {sent} birim sevk edildi.</color>");
            return sent;
        }

        /// <summary>Kaza dosyasındaki DISMISS CALL: olay yanıtsız kapanır, yaralılar kaybedilir.</summary>
        public void Dismiss(IncidentReport report)
        {
            if (report == null || report.status != CallIncidentStatus.Waiting) return;

            if (CityStatsManager.Instance != null) CityStatsManager.Instance.OnCallDismissed(report.injured);
            CloseIncident(report);
        }

        private void HandleVehicleArrived(UnitType type, Vector3 siteTarget)
        {
            foreach (IncidentReport r in activeIncidents)
            {
                if (r.status != CallIncidentStatus.Responding && r.status != CallIncidentStatus.Working) continue;
                if ((r.sitePosition - siteTarget).sqrMagnitude > 0.01f) continue;

                // Helikopter kendisine seçilen rolün işini yapar (joker birim)
                UnitType role = type;
                if (type == UnitType.RescueHelicopter)
                {
                    role = r.helisArrived < r.heliRoles.Count ? r.heliRoles[r.helisArrived] : r.RequiredUnit;
                    r.helisArrived++;
                }

                r.onSiteRoles.Add(role);
                r.status = CallIncidentStatus.Working;
                return;
            }
        }

        private void ResolveIncident(IncidentReport report)
        {
            CityStatsManager stats = CityStatsManager.Instance;

            if (report.sentRoles.Contains(report.RequiredUnit))
            {
                // Doğru ekip: herkes kurtarıldı
                if (stats != null) stats.ResolveCiviliansAtRisk(report.injured);
                Debug.Log($"<color=green>[Incidents] ✅ Case #{report.caseNumber} çözüldü. {report.injured} sivil kurtarıldı.</color>");
            }
            else
            {
                // Yanlış ekip: yaralıların yarısı kaybedilir
                int lost = Mathf.CeilToInt(report.injured / 2f);
                if (stats != null)
                {
                    stats.RegisterCasualty(lost);
                    stats.ResolveCiviliansAtRisk(report.injured - lost);
                }
                Debug.Log($"<color=orange>[Incidents] ⚠️ Case #{report.caseNumber}: yanlış ekip ({report.RequiredUnit} gerekiyordu). {lost} sivil kaybedildi.</color>");
            }

            CloseIncident(report);
        }

        private void CloseIncident(IncidentReport report)
        {
            report.status = CallIncidentStatus.Closed;
            activeIncidents.Remove(report);

            // Olay yerinde bekleyen / yolda olan araçlar üsse dönsün
            if (AStarMovementTester.Instance != null) AStarMovementTester.Instance.ReleaseSite(report.sitePosition);

            if (report.marker != null)
            {
                Destroy(report.marker.gameObject);
                report.marker = null;
            }

            OnIncidentClosed?.Invoke(report);
        }
    }
}
