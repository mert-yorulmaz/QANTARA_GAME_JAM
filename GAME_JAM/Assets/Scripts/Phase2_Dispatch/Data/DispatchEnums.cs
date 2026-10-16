using System;

namespace ZeroHour.Phase2.Data
{
    public enum UnitType
    {
        FireEngine,       // Heavy rescue & firefighting
        Ambulance,        // Medical evacuation & casualty triage
        PoliceCruiser,    // Perimeter lockdown & looting suppression
        RescueHelicopter  // Rooftop evacuation & fly-over rubble
    }

    public enum UnitState
    {
        IdleAtBase,
        EnRouteToIncident,
        WorkingAtSite,
        ReturningToBase
    }

    public enum IncidentType
    {
        BuildingFire,          // Requires Fire Engine
        GasLeakExplosion,      // Severe: Requires Fire Engine + Ambulance
        StructuralCollapse,    // Earthquake trapped victims: Ambulance + Police
        LootingAndUnrest,      // Civil disorder: Police Cruiser
        MedicalEmergency,      // Severe injuries: Ambulance
        RooftopStranded        // Collapsed access: Rescue Helicopter ONLY
    }

    public enum IncidentSeverity
    {
        Low,       // 1 unit, fast resolve
        Medium,    // 1-2 units, standard danger
        Critical   // 2+ units, rapid casualty increase if unattended
    }

    public enum InspectionResult
    {
        PassedSafe,        // Inspected correctly in Phase 1 -> Minor or no incident
        FlaggedDefective,  // Flagged by player in Phase 1 -> Evacuated, minimal casualties
        WronglyApproved,   // Player made mistake in Phase 1 -> Major disaster point!
        Unchecked          // Player ran out of time in Phase 1 -> High risk
    }
}
