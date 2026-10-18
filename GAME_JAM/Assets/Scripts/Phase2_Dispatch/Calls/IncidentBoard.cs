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
        [SerializeField] private Sprite gasMarkerSprite;
        [SerializeField] private Sprite floodMarkerSprite;
        [SerializeField] private Sprite unrestMarkerSprite;

        [Header("Afet Çeşitliliği (çıkma ağırlıkları) — yağma ana afet değil, sadece takip ihbarı")]
        [SerializeField] private float fireWeight = 25f;
        [SerializeField] private float collapseWeight = 35f;
        [SerializeField] private float gasWeight = 20f;
        [SerializeField] private float floodWeight = 20f;
        [SerializeField] private float unrestWeight = 0f;

        [Header("Takip İhbarı (deprem / selden sonra aynı bölgeden 'yağma var')")]
        [SerializeField] private Sprite unverifiedMarkerSprite;
        [Tooltip("Deprem veya selden sonra takip ihbarı gelme ihtimali")]
        [SerializeField, Range(0f, 1f)] private float followUpChance = 0.65f;
        [SerializeField] private float followUpDelayMin = 18f;
        [SerializeField] private float followUpDelayMax = 35f;
        [Tooltip("Takip ihbarlarının asılsız (scam) olma oranı")]
        [SerializeField, Range(0f, 1f)] private float scamChance = 0.35f;
        [Tooltip("Polis asılsız ihbarı bu kadar sürede doğrular ve döner (sn)")]
        [SerializeField] private float scamCheckSeconds = 6f;
        [Tooltip("'ASILSIZ İHBAR' yazısı için font (kurulum atar)")]
        [SerializeField] private TMPro.TMP_FontAsset labelFont;

        private class PendingFollowUp
        {
            public Vector3 origin;
            public int originCase;
            public int streetIndex;
            public float readyAt;
        }

        private readonly List<PendingFollowUp> pendingFollowUps = new List<PendingFollowUp>();
        private float boardTime;

        [Header("Süre (çember): olay bu sürede çözülmezse kalan yaralılar kaybedilir (sn)")]
        [SerializeField] private float fireTimeLimit = 75f;
        [SerializeField] private float collapseTimeLimit = 90f;
        [SerializeField] private float gasTimeLimit = 70f;
        [SerializeField] private float floodTimeLimit = 85f;
        [SerializeField] private float unrestTimeLimit = 60f;

        [Header("Zamanla Kayıp")]
        [Tooltip("Yardım yokken yaralı başına saniyelik ölüm oranı (tam seviyede; 6 yaralı ≈ 8 sn'de bir kayıp)")]
        [SerializeField] private float deathRatePerInjured = 0.02f;
        [Tooltip("İlk bu kadar saniye kimse ölmez (altın dakikalar)")]
        [SerializeField] private float deathGraceSeconds = 5f;
        [Tooltip("Ölüm riski bu sürede sıfırdan tam seviyeye çıkar: hızlı müdahale ödüllendirilir")]
        [SerializeField] private float deathRampSeconds = 25f;
        [Tooltip("Olay yerinde işe yarayan ekip varken ölüm oranı çarpanı")]
        [SerializeField] private float helpedDeathMultiplier = 0.3f;
        [Tooltip("Olay yerinde helikopter varken ölüm oranı çarpanı (hızlı ilk müdahale)")]
        [SerializeField] private float helicopterDeathMultiplier = 0.1f;

        [Header("Helikopter (hızlı ilk müdahaleci)")]
        [Tooltip("Helikopterin olay yerinde kalabildiği süre (su / yakıt); sonra üsse döner")]
        [SerializeField] private float helicopterEnduranceSeconds = 14f;
        [Tooltip("Yangında itfaiye rolündeki helikopterin (su bombası) iş katkısı çarpanı")]
        [SerializeField] private float helicopterWaterBombBonus = 1.5f;

        [Header("Olay Yerinde İş Süresi (1 gerekli ekiple, sn)")]
        [Tooltip("Aynı işi yapan her ek araç işi hızlandırır: 3 itfaiye = 3 kat hızlı")]
        [SerializeField] private float fireWorkSeconds = 24f;
        [SerializeField] private float collapseWorkSeconds = 30f;
        [SerializeField] private float gasWorkSeconds = 28f;
        [SerializeField] private float floodWorkSeconds = 26f;
        [SerializeField] private float unrestWorkSeconds = 18f;
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

        private static readonly string[] GasNotes =
        {
            "Faulty gas valve passed inspection. Explosion on the ground floor, fire spreading.",
            "Residents reported a gas smell for days. The boiler room exploded.",
            "Illegal gas line extension ruptured. Windows blown out on three floors."
        };

        private static readonly string[] FloodNotes =
        {
            "Building sits on a filled-in stream bed. Ground floor flooded, families on the roof.",
            "Blocked storm drains, no sandbags. Water rising fast around the entrance.",
            "Basement apartments flooded. Elderly residents cannot reach the stairs."
        };

        private static readonly string[] UnrestNotes =
        {
            "Shops looted after the tremor. Crowd blocking the street, people injured.",
            "Panic at the shelter entrance. Fights breaking out, barricade on fire.",
            "Store alarm ringing, windows smashed. Residents afraid to leave the building."
        };

        // Takip ihbarı notları: gerçek ihbarda doğrulayan ayrıntı, asılsızda şüpheli işaret var (oyuncu ayırt edebilsin)
        private static readonly string[] FollowUpRealNotes =
        {
            "Neighbors confirm smashed shop windows. A second caller reports the same street.",
            "Store owner on the line, alarm clearly audible. Two people hurt by looters.",
            "Residents of the damaged building report strangers carrying out their belongings."
        };

        private static readonly string[] FollowUpScamNotes =
        {
            "Caller refused to give a name and hung up. No other reports from the area.",
            "Laughter in the background. The caller changed the address twice.",
            "Anonymous text message only. Callback number does not exist."
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
            CallIncidentKind kind = PickKind();
            int streetIndex = UnityEngine.Random.Range(0, Streets.Length);
            int houseNumber = UnityEngine.Random.Range(2, 99);
            string street = Streets[streetIndex];
            Vector2Int injuredRange = InjuredRangeFor(kind);
            int injured = UnityEngine.Random.Range(injuredRange.x, injuredRange.y + 1);

            IncidentReport report = new IncidentReport
            {
                caseNumber = nextCaseNumber++,
                kind = kind,
                building = building,
                buildingBounds = bounds,
                sitePosition = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z),
                markerAnchor = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z),
                incidentTitle = TitleFor(kind),
                possibleUnitsText = "Possible units: " + PossibleUnitsFor(kind),
                address = $"{houseNumber} {street}",
                streetShort = ShortStreet(street),
                streetIndex = streetIndex,
                houseNumber = houseNumber,
                notes = RandomNote(kind),
                callTime = GetClockTime(),
                line = UnityEngine.Random.Range(1, 5),
                injured = injured,
                affected = injured + UnityEngine.Random.Range(2, 7),
                urgent = injured >= 5,
                timeRemaining = TimeLimitFor(kind),
                timeLimit = TimeLimitFor(kind),
                injuredRemaining = injured,
                workRequired = WorkSecondsFor(kind)
            };

            if (markerPrefab != null)
            {
                IncidentMarker marker = Instantiate(markerPrefab, report.markerAnchor, Quaternion.identity, transform);
                marker.name = $"IncidentMarker_{report.caseNumber}";
                marker.Initialize(report, MarkerFor(kind));
                report.marker = marker;
            }

            // Afet efekti: alev, duman, sarsıntı, su, alarm ışığı...
            report.effects = DisasterFx.Spawn(report);

            activeIncidents.Add(report);
            if (CityStatsManager.Instance != null) CityStatsManager.Instance.AddCiviliansAtRisk(report.injured);

            Debug.Log($"<color=orange>[Incidents] 📍 Case #{report.caseNumber}: {report.incidentTitle} @ {building.name} ({report.injured} yaralı)</color>");
            OnIncidentCreated?.Invoke(report);

            // Deprem / selden sonra aynı bölgeden "yağma var" ihbarı gelebilir
            if ((kind == CallIncidentKind.Collapse || kind == CallIncidentKind.Flood) && UnityEngine.Random.value < followUpChance)
            {
                pendingFollowUps.Add(new PendingFollowUp
                {
                    origin = report.sitePosition,
                    originCase = report.caseNumber,
                    streetIndex = streetIndex,
                    readyAt = boardTime + UnityEngine.Random.Range(followUpDelayMin, followUpDelayMax)
                });
            }
            return report;
        }

        // ==========================================
        // TAKİP İHBARI (yağma, bir kısmı asılsız)
        // ==========================================

        /// <summary>Sırası gelmiş bir takip ihbarı var ve haritada yer var.</summary>
        public bool FollowUpReady
        {
            get
            {
                if (!CanCreateIncident()) return false;
                foreach (PendingFollowUp p in pendingFollowUps)
                {
                    if (p.readyAt <= boardTime) return true;
                }
                return false;
            }
        }

        /// <summary>Sırası gelen takip ihbarını, ilk afetin yakınındaki boş binada oluşturur.</summary>
        public IncidentReport TryCreateFollowUp()
        {
            if (!CanCreateIncident()) return null;

            PendingFollowUp pending = null;
            foreach (PendingFollowUp p in pendingFollowUps)
            {
                if (p.readyAt <= boardTime) { pending = p; break; }
            }
            if (pending == null) return null;
            pendingFollowUps.Remove(pending);

            Transform building = NearestFreeBuilding(pending.origin);
            if (building == null) return null;

            Bounds bounds = CalculateBounds(building);
            bool scam = UnityEngine.Random.value < scamChance;
            int houseNumber = UnityEngine.Random.Range(2, 99);
            string street = Streets[pending.streetIndex];
            int claimed = UnityEngine.Random.Range(1, 4); // İhbarda söylenen yaralı sayısı (asılsızda gerçek değil)
            CallIncidentKind kind = CallIncidentKind.Unrest;
            string[] pool = scam ? FollowUpScamNotes : FollowUpRealNotes;

            IncidentReport report = new IncidentReport
            {
                caseNumber = nextCaseNumber++,
                kind = kind,
                isFollowUp = true,
                isScam = scam,
                originCaseNumber = pending.originCase,
                building = building,
                buildingBounds = bounds,
                sitePosition = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z),
                markerAnchor = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z),
                incidentTitle = "Looting report (unverified)",
                possibleUnitsText = "Possible units: " + PossibleUnitsFor(kind),
                address = $"{houseNumber} {street}",
                streetShort = ShortStreet(street),
                streetIndex = pending.streetIndex,
                houseNumber = houseNumber,
                notes = pool[UnityEngine.Random.Range(0, pool.Length)],
                callTime = GetClockTime(),
                line = UnityEngine.Random.Range(1, 5),
                injured = claimed,
                affected = claimed + UnityEngine.Random.Range(1, 4),
                urgent = false,
                timeRemaining = unrestTimeLimit,
                timeLimit = unrestTimeLimit,
                injuredRemaining = scam ? 0 : claimed,
                workRequired = scam ? scamCheckSeconds : unrestWorkSeconds
            };

            if (markerPrefab != null)
            {
                IncidentMarker marker = Instantiate(markerPrefab, report.markerAnchor, Quaternion.identity, transform);
                marker.name = $"IncidentMarker_{report.caseNumber}";
                marker.Initialize(report, unverifiedMarkerSprite != null ? unverifiedMarkerSprite : MarkerFor(kind));
                report.marker = marker;
            }

            // Efekt yok: doğrulanmamış ihbar haritada belli olmaz; gerçekse ekip varınca alarm / barikat çıkar
            activeIncidents.Add(report);
            if (!scam && CityStatsManager.Instance != null) CityStatsManager.Instance.AddCiviliansAtRisk(report.injured);

            Debug.Log($"<color=orange>[Incidents] 📍 Case #{report.caseNumber}: takip ihbarı (#{report.originCaseNumber} sonrası, {(scam ? "ASILSIZ" : "gerçek")}) @ {building.name}</color>");
            OnIncidentCreated?.Invoke(report);
            return report;
        }

        /// <summary>
        /// Sağ alttaki telefona olay mesajı (İngilizce anahtar, seçili dilde yazılır).
        /// Sadece ekip sevk edilmiş vakalar: oyuncu dosyayı açmadan detay görmesin, sevk ettiği ekibin başından geçenleri kaçırmasın.
        /// </summary>
        private static void Feed(IncidentReport r, FeedTone tone, string key, string englishFormat, params object[] args)
        {
            if (r.sentUnits.Count == 0) return;
            PhoneFeed.Post(tone, key, englishFormat, args);
        }

        /// <summary>Ekip adı İngilizce anahtar olarak; telefon gösterirken seçili dile çevirir.</summary>
        private static string UnitName(UnitType type)
        {
            switch (type)
            {
                case UnitType.FireEngine: return "Fire Truck";
                case UnitType.PoliceCruiser: return "Police";
                case UnitType.RescueHelicopter: return "Helicopter";
                default: return "Ambulance";
            }
        }

        private Transform NearestFreeBuilding(Vector3 origin)
        {
            Transform best = null;
            float bestDist = float.MaxValue;
            foreach (Transform b in candidateBuildings)
            {
                if (b == null) continue;
                bool used = false;
                foreach (IncidentReport r in activeIncidents)
                {
                    if (r.building == b) { used = true; break; }
                }
                if (used) continue;

                float d = (b.position - origin).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = b; }
            }
            return best;
        }

        /// <summary>Olay yerinin üstünde kısa süre yükselip sönen yazı (ASILSIZ İHBAR / YAĞMA DOĞRULANDI).</summary>
        private void ShowSiteLabel(IncidentReport r, string english, Color color)
        {
            FloatingLabel.Spawn(r.markerAnchor, Loc.T(english), color, labelFont);
        }

        // ==========================================
        // AFET KATALOĞU
        // ==========================================

        private CallIncidentKind PickKind()
        {
            float total = fireWeight + collapseWeight + gasWeight + floodWeight + unrestWeight;
            float roll = UnityEngine.Random.value * Mathf.Max(0.01f, total);
            if ((roll -= fireWeight) < 0f) return CallIncidentKind.Fire;
            if ((roll -= collapseWeight) < 0f) return CallIncidentKind.Collapse;
            if ((roll -= gasWeight) < 0f) return CallIncidentKind.GasExplosion;
            if ((roll -= floodWeight) < 0f) return CallIncidentKind.Flood;
            return unrestWeight > 0f ? CallIncidentKind.Unrest : CallIncidentKind.Collapse;
        }

        private static string TitleFor(CallIncidentKind kind)
        {
            switch (kind)
            {
                case CallIncidentKind.Fire: return "Residential fire";
                case CallIncidentKind.Collapse: return "Earthquake collapse";
                case CallIncidentKind.GasExplosion: return "Gas explosion";
                case CallIncidentKind.Flood: return "Flash flood";
                default: return "Looting & unrest";
            }
        }

        private static string PossibleUnitsFor(CallIncidentKind kind)
        {
            // Kurum renkleri sabit, isimler seçili dilde
            string fire = $"<color=#D63A2A>{Loc.T("Fire Truck")}</color>";
            string police = $"<color=#2563D9>{Loc.T("Police")}</color>";
            string ambulance = $"<mark=#2A231BE6><color=#FFFFFF> {Loc.T("Ambulance")} </color></mark>";
            string heli = $"<color=#B98A00>{Loc.T("Helicopter")}</color>";

            switch (kind)
            {
                case CallIncidentKind.Fire: return $"{fire}, {ambulance}";
                case CallIncidentKind.Collapse: return $"{ambulance}, {fire}, {police}";
                case CallIncidentKind.GasExplosion: return $"{fire}, {ambulance}, {police}";
                case CallIncidentKind.Flood: return $"{heli}, {ambulance}, {fire}";
                default: return $"{police}, {ambulance}";
            }
        }

        // ==========================================
        // YERELLEŞTİRİLMİŞ DOSYA METİNLERİ (Kaza Dosyası açılırken o anki dilde üretilir)
        // ==========================================

        private static readonly string[] StreetsTr =
        {
            "Meşe Sokak", "Sedir Caddesi", "Liman Yolu", "Ihlamur Sokak",
            "Akçaağaç Caddesi", "İstasyon Yolu", "Değirmen Sokak", "Park Sokak"
        };

        private static readonly string[] StreetsTrShort =
        {
            "MEŞE SK.", "SEDİR CD.", "LİMAN YL.", "IHLAMUR SK.",
            "AKÇAAĞAÇ CD.", "İSTASYON YL.", "DEĞİRMEN SK.", "PARK SK."
        };

        public static string LocalizedTitle(IncidentReport r) => Loc.T(r.incidentTitle);

        public static string LocalizedNotes(IncidentReport r) =>
            $"{Loc.T(r.notes)}\n<size=92%>{Loc.T("Possible units: ")}{PossibleUnitsFor(r.kind)}</size>";

        public static string LocalizedAddress(IncidentReport r) =>
            Loc.Turkish ? $"{StreetsTr[r.streetIndex]} No: {r.houseNumber}" : r.address;

        public static string LocalizedStreetShort(IncidentReport r) =>
            Loc.Turkish ? StreetsTrShort[r.streetIndex] : r.streetShort;

        private static string RandomNote(CallIncidentKind kind)
        {
            string[] pool;
            switch (kind)
            {
                case CallIncidentKind.Fire: pool = FireNotes; break;
                case CallIncidentKind.Collapse: pool = CollapseNotes; break;
                case CallIncidentKind.GasExplosion: pool = GasNotes; break;
                case CallIncidentKind.Flood: pool = FloodNotes; break;
                default: pool = UnrestNotes; break;
            }
            return pool[UnityEngine.Random.Range(0, pool.Length)];
        }

        private float TimeLimitFor(CallIncidentKind kind)
        {
            switch (kind)
            {
                case CallIncidentKind.Fire: return fireTimeLimit;
                case CallIncidentKind.Collapse: return collapseTimeLimit;
                case CallIncidentKind.GasExplosion: return gasTimeLimit;
                case CallIncidentKind.Flood: return floodTimeLimit;
                default: return unrestTimeLimit;
            }
        }

        private float WorkSecondsFor(CallIncidentKind kind)
        {
            switch (kind)
            {
                case CallIncidentKind.Fire: return fireWorkSeconds;
                case CallIncidentKind.Collapse: return collapseWorkSeconds;
                case CallIncidentKind.GasExplosion: return gasWorkSeconds;
                case CallIncidentKind.Flood: return floodWorkSeconds;
                default: return unrestWorkSeconds;
            }
        }

        private Vector2Int InjuredRangeFor(CallIncidentKind kind)
        {
            switch (kind)
            {
                case CallIncidentKind.GasExplosion: return new Vector2Int(Mathf.Max(minInjured, 4), maxInjured + 2);
                case CallIncidentKind.Unrest: return new Vector2Int(1, Mathf.Max(2, minInjured + 2));
                default: return new Vector2Int(minInjured, maxInjured);
            }
        }

        private Sprite MarkerFor(CallIncidentKind kind)
        {
            Sprite s;
            switch (kind)
            {
                case CallIncidentKind.Fire: s = fireMarkerSprite; break;
                case CallIncidentKind.Collapse: s = collapseMarkerSprite; break;
                case CallIncidentKind.GasExplosion: s = gasMarkerSprite; break;
                case CallIncidentKind.Flood: s = floodMarkerSprite; break;
                default: s = unrestMarkerSprite; break;
            }
            return s != null ? s : fireMarkerSprite;
        }

        private Transform PickFreeBuilding()
        {
            List<Transform> free = new List<Transform>();
            foreach (Transform b in candidateBuildings)
            {
                if (b == null) continue;
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
            float dt = Time.deltaTime;
            boardTime += dt;
            for (int i = activeIncidents.Count - 1; i >= 0; i--)
            {
                IncidentReport r = activeIncidents[i];
                if (r.status == CallIncidentStatus.Closed) continue;

                // Süre (çember) olay kapanana kadar işler: bekleme + yol + iş hepsi bu süreye sığmalı
                r.elapsed += dt;
                r.timeRemaining = Mathf.Max(0f, r.timeLimit - r.elapsed);

                ExpireHelicopters(r);

                // İş: olay yerindeki ekiplerin katkı toplamı (3 itfaiye = 3 kat hızlı)
                r.currentWorkRate = CurrentWorkRate(r);
                if (r.currentWorkRate > 0f)
                {
                    r.workDone += dt * r.currentWorkRate;
                    if (r.workDone >= r.workRequired)
                    {
                        ResolveIncident(r);
                        continue;
                    }
                }

                // Asılsız ihbarda kimse yok: ölüm yok, süre dolunca sessizce kapanır
                if (r.isScam)
                {
                    if (r.timeRemaining <= 0f) FailIncident(r);
                    continue;
                }

                // Zamanla kayıp: yardım gelene kadar yaralılar ölebilir
                ApplyDeaths(r, dt);
                if (r.injuredRemaining <= 0)
                {
                    Debug.Log($"<color=red>[Incidents] ☠️ Case #{r.caseNumber}: kurtarılacak kimse kalmadı ({r.deaths} kayıp).</color>");
                    Feed(r, FeedTone.Loss, "loss" + r.caseNumber, "Case #{0}: no survivors left · {1} lost", r.caseNumber, r.deaths);
                    CloseIncident(r);
                    continue;
                }

                if (r.timeRemaining <= 0f) FailIncident(r);
            }

            HandleMarkerClick();
        }

        /// <summary>Bir aracın işe katkısı: gerekli ekip 1, yardımcı 0.5; yangında su bombası atan helikopter 1.5.</summary>
        private float UnitWeight(IncidentReport r, OnSiteUnit unit)
        {
            if (unit.isHelicopter && r.HelicopterActsAsItself) return 1f; // Selde çatıdan tahliye
            float w = r.RoleWeight(unit.role);
            bool waterBomb = unit.isHelicopter && unit.role == UnitType.FireEngine &&
                             (r.kind == CallIncidentKind.Fire || r.kind == CallIncidentKind.GasExplosion);
            return waterBomb ? w * helicopterWaterBombBonus : w;
        }

        private float CurrentWorkRate(IncidentReport r)
        {
            if (r.onSiteUnits.Count == 0) return 0f;
            float rate = 0f;
            foreach (OnSiteUnit unit in r.onSiteUnits) rate += UnitWeight(r, unit);
            return rate > 0f ? rate : minimumWorkRate;
        }

        private void ApplyDeaths(IncidentReport r, float dt)
        {
            if (r.injuredRemaining <= 0 || dt <= 0f) return;

            float multiplier = 1f;
            foreach (OnSiteUnit unit in r.onSiteUnits)
            {
                if (unit.isHelicopter) { multiplier = helicopterDeathMultiplier; break; }
                if (UnitWeight(r, unit) > 0f) multiplier = helpedDeathMultiplier;
            }

            // Risk zamanla artar: ilk saniyeler güvenli, geç kalındıkça kayıplar hızlanır
            float ramp = Mathf.Clamp01((r.elapsed - deathGraceSeconds) / Mathf.Max(1f, deathRampSeconds));
            float lambda = deathRatePerInjured * r.injuredRemaining * multiplier * ramp;
            if (UnityEngine.Random.value < 1f - Mathf.Exp(-lambda * dt))
            {
                r.injuredRemaining--;
                r.deaths++;
                if (CityStatsManager.Instance != null) CityStatsManager.Instance.RegisterCasualty(1);
                Feed(r, FeedTone.Loss, "loss" + r.caseNumber, "Case #{0}: {1} lost so far, {2} still trapped", r.caseNumber, r.deaths, r.injuredRemaining);
            }
        }

        /// <summary>Helikopterin suyu / yakıtı sınırlı: olay yerinde süresi dolunca tek başına üsse döner.</summary>
        private void ExpireHelicopters(IncidentReport r)
        {
            for (int i = r.onSiteUnits.Count - 1; i >= 0; i--)
            {
                OnSiteUnit unit = r.onSiteUnits[i];
                if (!unit.isHelicopter || r.elapsed - unit.arrivedAtElapsed < helicopterEnduranceSeconds) continue;

                if (AStarMovementTester.Instance != null && unit.vehicle != null) AStarMovementTester.Instance.ReleaseVehicle(unit.vehicle);
                r.onSiteUnits.RemoveAt(i);
                Feed(r, FeedTone.Neutral, null, "Case #{0}: helicopter out of fuel, returning to base", r.caseNumber);
            }
        }

        private void FailIncident(IncidentReport r)
        {
            if (r.isScam)
            {
                Debug.Log($"<color=grey>[Incidents] Case #{r.caseNumber}: asılsız ihbar zaman aşımına uğradı (kayıp yok).</color>");
                Feed(r, FeedTone.Neutral, null, "Case #{0}: report expired, nobody called back", r.caseNumber);
                CloseIncident(r);
                return;
            }

            int lost = r.injuredRemaining;
            r.injuredRemaining = 0;
            r.deaths += lost;
            if (CityStatsManager.Instance != null && lost > 0) CityStatsManager.Instance.RegisterCasualty(lost);
            Debug.Log($"<color=red>[Incidents] ⏱️ Case #{r.caseNumber}: süre doldu. Toplam {r.deaths} kayıp.</color>");
            Feed(r, FeedTone.Loss, "loss" + r.caseNumber, "Case #{0}: time ran out · {1} lost", r.caseNumber, r.deaths);
            CloseIncident(r);
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
            // Takviye serbest: olay kapanana kadar (örn. önce helikopter, sonra itfaiye) ekip gönderilebilir
            if (report == null || report.status == CallIncidentStatus.Closed) return 0;
            if (AStarMovementTester.Instance == null || units.Count == 0) return 0;

            List<GameObject> vehicles = new List<GameObject>();
            int sent = AStarMovementTester.Instance.DispatchVehicles(report.sitePosition, units, vehicles);
            if (sent <= 0) return 0;

            if (report.status == CallIncidentStatus.Waiting) report.status = CallIncidentStatus.Responding;

            // Gerçekten yola çıkan her aracın rolü (helikopter: seçilen rol; selde kendisi)
            int heliIndex = 0;
            foreach (GameObject vehicle in vehicles)
            {
                UnitType type = AStarMovementTester.Instance.GetUnitType(vehicle);
                report.sentUnits.Add(type);
                if (type != UnitType.RescueHelicopter)
                {
                    report.sentRoles.Add(type);
                    continue;
                }

                UnitType role = (heliRoles != null && heliIndex < heliRoles.Count) ? heliRoles[heliIndex] : report.RequiredUnit;
                if (report.HelicopterActsAsItself) role = UnitType.RescueHelicopter; // Selde helikopter çatıdan tahliye yapar
                report.heliRoleByVehicle[vehicle] = role;
                report.sentRoles.Add(role);
                heliIndex++;
            }
            if (report.marker != null && report.onSiteUnits.Count == 0) report.marker.SetResponding();

            Debug.Log($"<color=cyan>[Incidents] 🚨 Case #{report.caseNumber}: {sent} birim sevk edildi.</color>");
            return sent;
        }

        /// <summary>Kaza dosyasındaki DISMISS CALL: olay yanıtsız kapanır, yaralılar kaybedilir (asılsız ihbarda ceza yok).</summary>
        public void Dismiss(IncidentReport report)
        {
            if (report == null || report.status != CallIncidentStatus.Waiting) return;

            if (report.isScam)
            {
                Debug.Log($"<color=grey>[Incidents] Case #{report.caseNumber}: asılsız ihbar reddedildi (doğru karar).</color>");
                CloseIncident(report);
                return;
            }

            if (CityStatsManager.Instance != null && report.injuredRemaining > 0) CityStatsManager.Instance.OnCallDismissed(report.injuredRemaining);
            report.deaths += report.injuredRemaining;
            report.injuredRemaining = 0;
            CloseIncident(report);
        }

        private void HandleVehicleArrived(UnitType type, Vector3 siteTarget, GameObject vehicle)
        {
            foreach (IncidentReport r in activeIncidents)
            {
                if (r.status != CallIncidentStatus.Responding && r.status != CallIncidentStatus.Working) continue;
                if ((r.sitePosition - siteTarget).sqrMagnitude > 0.01f) continue;

                // Helikopter kendisine seçilen rolün işini yapar (joker birim)
                UnitType role = type;
                if (type == UnitType.RescueHelicopter)
                {
                    role = vehicle != null && r.heliRoleByVehicle.TryGetValue(vehicle, out UnitType chosen) ? chosen : r.RequiredUnit;
                    if (r.HelicopterActsAsItself) role = UnitType.RescueHelicopter;
                }

                r.onSiteUnits.Add(new OnSiteUnit
                {
                    vehicle = vehicle,
                    role = role,
                    isHelicopter = type == UnitType.RescueHelicopter,
                    arrivedAtElapsed = r.elapsed
                });
                if (r.status == CallIncidentStatus.Responding) Feed(r, FeedTone.Info, null, "Case #{0}: first unit on scene", r.caseNumber);
                r.status = CallIncidentStatus.Working;

                // Takip ihbarı ilk ekip varınca doğrulanır: gerçekse yağma görünür hale gelir
                if (r.isFollowUp && !r.isScam && r.effects == null)
                {
                    r.effects = DisasterFx.Spawn(r);
                    ShowSiteLabel(r, "LOOTING CONFIRMED", new Color(1f, 0.55f, 0.2f));
                    Feed(r, FeedTone.Alert, null, "Case #{0}: looting confirmed · {1} injured", r.caseNumber, r.injuredRemaining);
                }
                return;
            }
        }

        private void ResolveIncident(IncidentReport report)
        {
            CityStatsManager stats = CityStatsManager.Instance;

            if (report.isScam)
            {
                // Ekip boşuna gitti: kayıp yok ama o araçlar bu süre boyunca başka yerde çalışamadı
                ShowSiteLabel(report, "FALSE ALARM", new Color(0.75f, 0.75f, 0.75f));
                Debug.Log($"<color=grey>[Incidents] Case #{report.caseNumber}: ASILSIZ İHBAR. Ekip boşuna gitti.</color>");
                Feed(report, FeedTone.Neutral, null, "Case #{0}: FALSE ALARM · units returning", report.caseNumber);
                CloseIncident(report);
                return;
            }

            int remaining = report.injuredRemaining;
            if (report.sentRoles.Contains(report.RequiredUnit))
            {
                // Doğru ekip, süre bitmeden: hayatta kalan yaralıların hepsi kurtarıldı
                if (stats != null) stats.ResolveCiviliansAtRisk(remaining);
                Debug.Log($"<color=green>[Incidents] ✅ Case #{report.caseNumber} çözüldü. {remaining} sivil kurtarıldı, {report.deaths} kayıp.</color>");
                Feed(report, FeedTone.Success, null, "Case #{0} resolved · {1} rescued, {2} lost", report.caseNumber, remaining, report.deaths);
            }
            else
            {
                // Gerekli ekip hiç gönderilmedi: kalan yaralıların yarısı kaybedilir
                int lost = Mathf.CeilToInt(remaining / 2f);
                report.deaths += lost;
                if (stats != null)
                {
                    if (lost > 0) stats.RegisterCasualty(lost);
                    stats.ResolveCiviliansAtRisk(remaining - lost);
                }
                Debug.Log($"<color=orange>[Incidents] ⚠️ Case #{report.caseNumber}: gerekli ekip ({report.RequiredUnit}) gelmedi. {lost} sivil daha kaybedildi.</color>");
                Feed(report, FeedTone.Loss, null, "Case #{0}: wrong team, no {1} sent · {2} more lost", report.caseNumber, UnitName(report.RequiredUnit), lost);
            }
            report.injuredRemaining = 0;

            CloseIncident(report);
        }

        private void CloseIncident(IncidentReport report)
        {
            report.status = CallIncidentStatus.Closed;
            activeIncidents.Remove(report);

            // Afet efekti sönerek kapanır (su çekilir, duman dağılır, binalar eski yerinde)
            if (report.effects != null)
            {
                report.effects.Stop();
                report.effects = null;
            }

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
