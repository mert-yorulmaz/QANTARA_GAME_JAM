using System.Collections.Generic;
using UnityEngine;
using ZeroHour.Phase2.Data;

namespace ZeroHour.Phase2.Calls
{
    public enum CallIncidentKind
    {
        Fire,          // marker_fire     -> İtfaiye gerekir
        Collapse,      // marker_collapse -> Deprem çökmesi: Ambulans gerekir
        GasExplosion,  // marker_gas      -> İtfaiye gerekir
        Flood,         // marker_flood    -> Sel: çatıdan tahliye, Helikopter gerekir
        Unrest         // Sadece takip ihbarı: deprem / selden sonra aynı bölgeden "yağma var" (marker_unverified) -> Polis
    }

    public enum CallIncidentStatus
    {
        Waiting,     // Rozet haritada, ekip bekleniyor
        Responding,  // Ekip yolda
        Working,     // En az bir ekip olay yerinde, iş ilerliyor
        Closed       // Çözüldü / kaybedildi / reddedildi
    }

    /// <summary>Olay yerine varmış bir araç (helikopterin suyu / yakıtı sınırlıdır).</summary>
    public class OnSiteUnit
    {
        public GameObject vehicle;
        public UnitType role;
        public bool isHelicopter;
        public float arrivedAtElapsed;
    }

    /// <summary>
    /// Dinlenen bir çağrıdan doğan olay kaydı (Kaza Dosyası'nın verisi).
    /// </summary>
    public class IncidentReport
    {
        public int caseNumber;
        public CallIncidentKind kind;
        public CallIncidentStatus status = CallIncidentStatus.Waiting;

        public Transform building;
        public Bounds buildingBounds;
        public Vector3 sitePosition;   // Binanın zemin merkezi (sevk hedefi)
        public Vector3 markerAnchor;   // Rozetin asılı durduğu çatı üstü nokta

        public string incidentTitle;
        public string address;
        public string streetShort;
        public int streetIndex;
        public int houseNumber;
        public string notes;
        public string possibleUnitsText; // Renkli "olası ekipler" satırı (Kaza Dosyası açıklamasında)
        public string callTime;
        public int line;

        public int affected;
        public int injured;
        public bool urgent;

        // Takip ihbarı: deprem / selden sonra aynı bölgeden gelen, doğrulanmamış ihbar (bir kısmı asılsız)
        public bool isFollowUp;
        public bool isScam;
        public int originCaseNumber;
        public float timeRemaining;
        public float timeLimit;

        public readonly List<UnitType> sentUnits = new List<UnitType>();   // Gönderilen araç tipleri (helikopter = RescueHelicopter)
        public readonly List<UnitType> sentRoles = new List<UnitType>();   // Yapılacak iş: helikopterler seçilen rolde sayılır
        public readonly Dictionary<GameObject, UnitType> heliRoleByVehicle = new Dictionary<GameObject, UnitType>(); // Her helikopterin rolü
        public readonly List<OnSiteUnit> onSiteUnits = new List<OnSiteUnit>(); // Olay yerinde çalışan araçlar

        // Süre ve kayıplar: süre olay kapanana kadar işler; yardım gelene kadar yaralılar ölebilir
        public float elapsed;
        public int injuredRemaining;
        public int deaths;

        // Olay yerinde iş: workRequired saniye × (1 gerekli ekip). Ekip sayısı arttıkça hızlanır.
        public float workRequired;
        public float workDone;
        public float currentWorkRate;

        public IncidentMarker marker;
        public DisasterFx effects;

        /// <summary>Olayı gerçekten çözen birim tipi.</summary>
        public UnitType RequiredUnit
        {
            get
            {
                switch (kind)
                {
                    case CallIncidentKind.Fire: return UnitType.FireEngine;
                    case CallIncidentKind.GasExplosion: return UnitType.FireEngine;
                    case CallIncidentKind.Flood: return UnitType.RescueHelicopter;
                    case CallIncidentKind.Unrest: return UnitType.PoliceCruiser;
                    default: return UnitType.Ambulance;
                }
            }
        }

        /// <summary>İş hızı katkısı: gerekli ekip 1, olası (yardımcı) ekip 0.5, ilgisiz ekip 0.</summary>
        public float RoleWeight(UnitType role)
        {
            if (role == RequiredUnit) return 1f;
            switch (kind)
            {
                case CallIncidentKind.Fire:
                    return role == UnitType.Ambulance ? 0.5f : 0f;
                case CallIncidentKind.Collapse:
                    return (role == UnitType.FireEngine || role == UnitType.PoliceCruiser) ? 0.5f : 0f;
                case CallIncidentKind.GasExplosion:
                    return (role == UnitType.Ambulance || role == UnitType.PoliceCruiser) ? 0.5f : 0f;
                case CallIncidentKind.Flood:
                    return (role == UnitType.Ambulance || role == UnitType.FireEngine) ? 0.5f : 0f;
                case CallIncidentKind.Unrest:
                    return role == UnitType.Ambulance ? 0.5f : 0f;
                default:
                    return 0f;
            }
        }

        /// <summary>Selde helikopter rolünden bağımsız olarak kendisi (çatıdan tahliye) sayılır.</summary>
        public bool HelicopterActsAsItself => RequiredUnit == UnitType.RescueHelicopter;

        /// <summary>Kalan iş süresi (sn), mevcut hızla.</summary>
        public float WorkSecondsLeft => currentWorkRate > 0.001f ? Mathf.Max(0f, workRequired - workDone) / currentWorkRate : Mathf.Max(0f, workRequired - workDone);
    }
}
