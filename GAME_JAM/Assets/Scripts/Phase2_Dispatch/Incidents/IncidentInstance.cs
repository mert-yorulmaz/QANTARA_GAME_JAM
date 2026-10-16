using System;
using System.Collections.Generic;
using UnityEngine;
using ZeroHour.Phase2.Data;
using ZeroHour.Phase2.Units;

namespace ZeroHour.Phase2.Incidents
{
    [System.Serializable]
    public class IncidentInstance
    {
        public string incidentId;
        public IncidentType type;
        public IncidentSeverity severity;
        public string title;
        public string description;

        [Header("Triage & Requirements")]
        public List<UnitType> requiredUnitTypes = new List<UnitType>();
        public int civiliansAtRisk = 10;
        public float workDuration = 10f;
        public float urgencyTimer = 25f; // Time before casualties start dying
        public float casualtyTickInterval = 3f;

        [Header("False Alarm / Intelligence")]
        public bool isFalseAlarm = false; // Player might dispatch units to a panicked false report!
        public string falseAlarmReport = "FALSE ALARM: Area surveyed. No active threat detected. Units RTB.";

        // Runtime dynamic state
        public List<EmergencyUnit> assignedUnits = new List<EmergencyUnit>();
        public bool isBeingTreated = false;
        public bool isResolved = false;
        public float elapsedUrgency = 0f;
        public float lastCasualtyTick = 0f;

        public IncidentInstance(
            string id,
            IncidentType incType,
            IncidentSeverity sev,
            string incTitle,
            string incDesc,
            int civilians,
            float duration,
            bool falseAlarm = false,
            params UnitType[] reqUnits)
        {
            incidentId = id;
            type = incType;
            severity = sev;
            title = incTitle;
            description = incDesc;
            civiliansAtRisk = civilians;
            workDuration = duration;
            isFalseAlarm = falseAlarm;
            requiredUnitTypes.AddRange(reqUnits);
            assignedUnits = new List<EmergencyUnit>();
        }

        public bool AreRequirementsMet()
        {
            if (isFalseAlarm) return assignedUnits.Count > 0;
            if (requiredUnitTypes.Count == 0) return true;

            // Check if every required unit type is satisfied (Helicopter can also substitute as aerial suppression/triage)
            foreach (var req in requiredUnitTypes)
            {
                bool satisfied = assignedUnits.Exists(u => u.Type == req || (u.Type == UnitType.RescueHelicopter && req != UnitType.PoliceCruiser));
                if (!satisfied) return false;
            }
            return true;
        }

        public bool CanAssignUnit(EmergencyUnit unit)
        {
            if (unit == null || isResolved) return false;
            if (assignedUnits.Contains(unit)) return false;

            // False alarms accept any dispatched unit to investigate
            if (isFalseAlarm) return assignedUnits.Count == 0;

            // Helicopter can always be dispatched as rapid first responder
            if (unit.Type == UnitType.RescueHelicopter) return true;

            int neededCount = 0;
            for (int i = 0; i < requiredUnitTypes.Count; i++)
            {
                if (requiredUnitTypes[i] == unit.Type) neededCount++;
            }

            int currentCount = 0;
            for (int i = 0; i < assignedUnits.Count; i++)
            {
                if (assignedUnits[i].Type == unit.Type) currentCount++;
            }

            return currentCount < neededCount;
        }
    }
}
