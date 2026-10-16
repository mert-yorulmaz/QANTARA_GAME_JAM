using System.Collections.Generic;
using UnityEngine;
using ZeroHour.Phase2.Data;

namespace ZeroHour.Phase2.Units
{
    public class UnitStation : MonoBehaviour
    {
        [Header("Station Profile")]
        [SerializeField] private UnitType stationType;
        [SerializeField] private string stationName = "Emergency Station";
        [SerializeField] private Transform dispatchExitPoint;

        [Header("Visuals (2.5D Hologram Marker)")]
        [SerializeField] private Color stationAccentColor = Color.cyan;
        [SerializeField] private GameObject stationIconMarker;

        private readonly List<EmergencyUnit> assignedUnits = new List<EmergencyUnit>();

        public UnitType StationType => stationType;
        public string StationName => stationName;
        public Vector3 DispatchPoint => dispatchExitPoint != null ? dispatchExitPoint.position : transform.position;

        public void RegisterUnit(EmergencyUnit unit)
        {
            if (!assignedUnits.Contains(unit))
            {
                assignedUnits.Add(unit);
            }
        }

        public void UnregisterUnit(EmergencyUnit unit)
        {
            assignedUnits.Remove(unit);
        }

        public int AvailableUnitCount()
        {
            int count = 0;
            for (int i = 0; i < assignedUnits.Count; i++)
            {
                if (assignedUnits[i] != null && assignedUnits[i].CurrentState == UnitState.IdleAtBase)
                    count++;
            }
            return count;
        }

        public EmergencyUnit GetFirstIdleUnit()
        {
            for (int i = 0; i < assignedUnits.Count; i++)
            {
                if (assignedUnits[i] != null && assignedUnits[i].CurrentState == UnitState.IdleAtBase)
                    return assignedUnits[i];
            }
            return null;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = stationAccentColor;
            Gizmos.DrawWireCube(transform.position, new Vector3(2f, 0.5f, 2f));
            Gizmos.DrawLine(transform.position, DispatchPoint);
        }
    }
}
