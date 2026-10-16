using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZeroHour.Phase2.Data
{
    [System.Serializable]
    public class BuildingInspectionRecord
    {
        public string buildingId;
        public string buildingName;
        public InspectionResult result;
        public string inspectionNote; // e.g. "Gas shutoff valve inspected", "Earthquake kit missing"

        public BuildingInspectionRecord(string id, string name, InspectionResult res, string note = "")
        {
            buildingId = id;
            buildingName = name;
            result = res;
            inspectionNote = note;
        }
    }

    [System.Serializable]
    public class FleetAllocation
    {
        [Range(1, 8)] public int fireEngines = 6;
        [Range(1, 8)] public int ambulances = 5;
        [Range(1, 8)] public int policeCruisers = 5;
        [Range(0, 6)] public int helicopters = 5;
    }

    /// <summary>
    /// Bridge data container passing inspection decisions and budget from Phase 1 to Phase 2.
    /// Can be instantiated at runtime by GameManager or populated with mock data for testing.
    /// </summary>
    [CreateAssetMenu(fileName = "Phase1BridgeData", menuName = "ZeroHour/Phase 1 Bridge Data")]
    public class Phase1BridgeData : ScriptableObject
    {
        [Header("Fleet Quota (Influenced by Phase 1 Budget/Performance)")]
        public FleetAllocation fleet = new FleetAllocation();

        [Header("Building Inspection History")]
        public List<BuildingInspectionRecord> inspectedBuildings = new List<BuildingInspectionRecord>();

        public BuildingInspectionRecord GetBuildingRecord(string buildingId)
        {
            return inspectedBuildings.Find(b => b.buildingId.Equals(buildingId, StringComparison.OrdinalIgnoreCase));
        }

        public void SetRecord(string buildingId, string buildingName, InspectionResult result, string note = "")
        {
            var existing = GetBuildingRecord(buildingId);
            if (existing != null)
            {
                existing.result = result;
                existing.inspectionNote = note;
            }
            else
            {
                inspectedBuildings.Add(new BuildingInspectionRecord(buildingId, buildingName, result, note));
            }
        }

        /// <summary>
        /// Generates realistic test data for standalone Phase 2 debugging.
        /// </summary>
        public static Phase1BridgeData CreateDefaultTestData()
        {
            var data = CreateInstance<Phase1BridgeData>();
            data.fleet = new FleetAllocation
            {
                fireEngines = 2,
                ambulances = 2,
                policeCruisers = 2,
                helicopters = 1
            };

            data.inspectedBuildings.Add(new BuildingInspectionRecord("BLD_01", "Westbrook Residential", InspectionResult.PassedSafe, "Supplies verified"));
            data.inspectedBuildings.Add(new BuildingInspectionRecord("BLD_02", "Plaza Commercial Alpha", InspectionResult.WronglyApproved, "Faulty gas shutoff valve overlooked!"));
            data.inspectedBuildings.Add(new BuildingInspectionRecord("BLD_03", "Downtown Grand Hotel", InspectionResult.WronglyApproved, "Cracked support pillar ignored!"));
            data.inspectedBuildings.Add(new BuildingInspectionRecord("BLD_04", "Central Metro Bank", InspectionResult.FlaggedDefective, "Ordered night security lock"));
            data.inspectedBuildings.Add(new BuildingInspectionRecord("BLD_05", "Harbor Logistics Warehouse", InspectionResult.PassedSafe, "Fire sprinkler operational"));

            return data;
        }
    }
}
