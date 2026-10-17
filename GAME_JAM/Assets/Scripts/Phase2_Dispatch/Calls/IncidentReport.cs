using System.Collections.Generic;
using UnityEngine;
using ZeroHour.Phase2.Data;

namespace ZeroHour.Phase2.Calls
{
    public enum CallIncidentKind
    {
        Fire,      // marker_fire     -> İtfaiye gerekir
        Collapse   // marker_collapse -> Ambulans gerekir
    }

    public enum CallIncidentStatus
    {
        Waiting,     // Rozet haritada, ekip bekleniyor
        Responding,  // Ekip yolda
        Closed       // Çözüldü / kaybedildi / reddedildi
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
        public string notes;
        public string callTime;
        public int line;

        public int affected;
        public int injured;
        public bool urgent;
        public float timeRemaining;

        public readonly List<UnitType> sentUnits = new List<UnitType>();
        public IncidentMarker marker;

        /// <summary>Olayı gerçekten çözen birim tipi.</summary>
        public UnitType RequiredUnit => kind == CallIncidentKind.Fire ? UnitType.FireEngine : UnitType.Ambulance;
    }
}
