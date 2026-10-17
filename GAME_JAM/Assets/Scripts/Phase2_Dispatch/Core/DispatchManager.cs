using System;
using System.Collections.Generic;
using UnityEngine;
using ZeroHour.Phase2.Data;
using ZeroHour.Phase2.Units;
using ZeroHour.Phase2.Incidents;

namespace ZeroHour.Phase2.Core
{
    public class DispatchManager : MonoBehaviour
    {
        public static DispatchManager Instance { get; private set; }

        [Header("Phase 1 Integration")]
        [SerializeField] private Phase1BridgeData bridgeData;

        [Header("Unit Prefabs")]
        [SerializeField] private EmergencyUnit fireEnginePrefab;
        [SerializeField] private EmergencyUnit ambulancePrefab;
        [SerializeField] private EmergencyUnit policeCruiserPrefab;
        [SerializeField] private EmergencyUnit helicopterPrefab;

        [Header("Station References in Scene")]
        [SerializeField] private UnitStation fireStation;
        [SerializeField] private UnitStation hospitalStation;
        [SerializeField] private UnitStation policeStation;
        [SerializeField] private UnitStation helipadStation;

        // Runtime collections
        private readonly List<EmergencyUnit> allUnits = new List<EmergencyUnit>();
        private readonly List<BuildingNode> allBuildings = new List<BuildingNode>();
        private BuildingNode selectedBuilding = null;

        public BuildingNode SelectedBuilding => selectedBuilding;
        public Phase1BridgeData ActiveBridgeData => bridgeData;

        public event Action<BuildingNode> OnBuildingSelected;
        public event Action OnBuildingDeselected;
        public event Action<EmergencyUnit, BuildingNode> OnUnitDispatched;
        public event Action<EmergencyUnit, UnitState> OnUnitStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Collect all buildings in scene if not manually assigned
            BuildingNode[] sceneBuildings = FindObjectsByType<BuildingNode>(FindObjectsSortMode.None);
            allBuildings.AddRange(sceneBuildings);
        }

        private void Start()
        {
            // If no bridge data attached from Phase 1, create realistic standalone fallback
            if (bridgeData == null)
            {
                bridgeData = Phase1BridgeData.CreateDefaultTestData();
                Debug.Log("[DispatchManager] No Phase 1 bridge data found. Generated standalone mock data for testing.");
            }

            ApplyPhase1DataToBuildings();
            SpawnFleet(bridgeData.fleet);

            // Sahnedeki mevcut EmergencyUnit'leri de listeye dahil et
            EmergencyUnit[] existingUnits = FindObjectsByType<EmergencyUnit>(FindObjectsSortMode.None);
            foreach (var u in existingUnits)
            {
                RegisterUnit(u);
            }
        }

        private void ApplyPhase1DataToBuildings()
        {
            if (bridgeData == null) return;

            foreach (var building in allBuildings)
            {
                var record = bridgeData.GetBuildingRecord(building.BuildingId);
                if (record != null)
                {
                    building.ApplyPhase1Record(record);
                }
            }
        }

        private void SpawnFleet(FleetAllocation allocation)
        {
            // Clear existing if any
            for (int i = 0; i < allUnits.Count; i++)
            {
                if (allUnits[i] != null) Destroy(allUnits[i].gameObject);
            }
            allUnits.Clear();

            // Spawn Fire Engines
            if (fireStation != null && fireEnginePrefab != null)
            {
                for (int i = 0; i < allocation.fireEngines; i++)
                {
                    SpawnUnit(fireEnginePrefab, fireStation, $"ENGINE-0{i + 1}", UnitType.FireEngine);
                }
            }

            // Spawn Ambulances
            if (hospitalStation != null && ambulancePrefab != null)
            {
                for (int i = 0; i < allocation.ambulances; i++)
                {
                    SpawnUnit(ambulancePrefab, hospitalStation, $"MEDIC-0{i + 1}", UnitType.Ambulance);
                }
            }

            // Spawn Police Cruisers
            if (policeStation != null && policeCruiserPrefab != null)
            {
                for (int i = 0; i < allocation.policeCruisers; i++)
                {
                    SpawnUnit(policeCruiserPrefab, policeStation, $"PATROL-0{i + 1}", UnitType.PoliceCruiser);
                }
            }

            // Spawn Helicopters
            if (helipadStation != null && helicopterPrefab != null)
            {
                for (int i = 0; i < allocation.helicopters; i++)
                {
                    SpawnUnit(helicopterPrefab, helipadStation, $"CHOPPER-0{i + 1}", UnitType.RescueHelicopter);
                }
            }
        }

        private void SpawnUnit(EmergencyUnit prefab, UnitStation station, string callsign, UnitType type)
        {
            Vector3 spawnPos = station.DispatchPoint;
            EmergencyUnit unitInstance = Instantiate(prefab, spawnPos, Quaternion.identity);
            unitInstance.Initialize(station, callsign, type);
            unitInstance.OnStateChanged += HandleUnitStateChanged;
            allUnits.Add(unitInstance);
        }

        private void HandleUnitStateChanged(EmergencyUnit unit, UnitState state)
        {
            OnUnitStateChanged?.Invoke(unit, state);
        }

        public void SelectBuilding(BuildingNode node)
        {
            selectedBuilding = node;
            OnBuildingSelected?.Invoke(node);
        }

        public void DeselectBuilding()
        {
            selectedBuilding = null;
            OnBuildingDeselected?.Invoke();
        }

        public bool DispatchUnit(UnitType type, BuildingNode targetBuilding)
        {
            if (targetBuilding == null || !targetBuilding.HasActiveIncident)
            {
                Debug.LogWarning("[DispatchManager] Cannot dispatch: Target building has no active incident.");
                return false;
            }

            EmergencyUnit idleUnit = GetIdleUnit(type);
            if (idleUnit == null)
            {
                Debug.LogWarning($"[DispatchManager] No idle units available for type: {type}");
                return false;
            }

            float workDuration = targetBuilding.CurrentIncident != null ? targetBuilding.CurrentIncident.workDuration : 8f;
            bool success = idleUnit.Dispatch(targetBuilding, workDuration);

            if (success)
            {
                targetBuilding.CurrentIncident.assignedUnits.Add(idleUnit);
                OnUnitDispatched?.Invoke(idleUnit, targetBuilding);
            }

            return success;
        }

        public EmergencyUnit GetIdleUnit(UnitType type)
        {
            for (int i = 0; i < allUnits.Count; i++)
            {
                if (allUnits[i] != null && allUnits[i].Type == type && allUnits[i].CurrentState == UnitState.IdleAtBase)
                {
                    return allUnits[i];
                }
            }
            return null;
        }

        public int GetIdleCount(UnitType type)
        {
            int count = 0;
            for (int i = 0; i < allUnits.Count; i++)
            {
                if (allUnits[i] != null && allUnits[i].Type == type && allUnits[i].CurrentState == UnitState.IdleAtBase)
                    count++;
            }
            return count;
        }

        public int GetActiveCount(UnitType type)
        {
            int count = 0;
            for (int i = 0; i < allUnits.Count; i++)
            {
                if (allUnits[i] != null && allUnits[i].Type == type && allUnits[i].CurrentState != UnitState.IdleAtBase)
                    count++;
            }
            return count;
        }

        public int GetTotalCount(UnitType type)
        {
            int count = 0;
            for (int i = 0; i < allUnits.Count; i++)
            {
                if (allUnits[i] != null && allUnits[i].Type == type)
                    count++;
            }
            return count;
        }

        public void RegisterUnit(EmergencyUnit unit)
        {
            if (unit != null && !allUnits.Contains(unit))
            {
                unit.OnStateChanged += HandleUnitStateChanged;
                allUnits.Add(unit);
                OnUnitStateChanged?.Invoke(unit, unit.CurrentState);
            }
        }

        public BuildingNode GetBuildingById(string bldId)
        {
            return allBuildings.Find(b => b.BuildingId.Equals(bldId, StringComparison.OrdinalIgnoreCase));
        }
    }
}
