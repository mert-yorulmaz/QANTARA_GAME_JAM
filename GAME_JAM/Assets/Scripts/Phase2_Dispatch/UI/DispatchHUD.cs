using UnityEngine;
using TMPro;
using ZeroHour.Phase2.Core;
using ZeroHour.Phase2.Data;
using ZeroHour.Phase2.Units;

namespace ZeroHour.Phase2.UI
{
    public class DispatchHUD : MonoBehaviour
    {
        [Header("Top Header - City Statistics")]
        [SerializeField] private TextMeshProUGUI populationText;
        [SerializeField] private TextMeshProUGUI atRiskText;
        [SerializeField] private TextMeshProUGUI casualtiesText;
        [SerializeField] private TextMeshProUGUI rescuedText;
        [SerializeField] private TextMeshProUGUI clockText;

        [Header("Bottom Fleet Dock")]
        [SerializeField] private TextMeshProUGUI fireEngineDockText;
        [SerializeField] private TextMeshProUGUI ambulanceDockText;
        [SerializeField] private TextMeshProUGUI policeDockText;
        [SerializeField] private TextMeshProUGUI helicopterDockText;

        private void Start()
        {
            if (CityStatsManager.Instance != null)
            {
                CityStatsManager.Instance.OnStatsUpdated += RefreshCityStats;
            }

            if (DispatchManager.Instance != null)
            {
                DispatchManager.Instance.OnUnitStateChanged += HandleUnitChanged;
            }

            RefreshCityStats();
            RefreshFleetDock();
        }

        private void OnDestroy()
        {
            if (CityStatsManager.Instance != null)
            {
                CityStatsManager.Instance.OnStatsUpdated -= RefreshCityStats;
            }

            if (DispatchManager.Instance != null)
            {
                DispatchManager.Instance.OnUnitStateChanged -= HandleUnitChanged;
            }
        }

        private void Update()
        {
            // Update simulated clock from ShiftTimelineController
            if (ShiftTimelineController.Instance != null && clockText != null)
            {
                clockText.text = ShiftTimelineController.Instance.GetSimulatedClockTime();
            }
        }

        private void HandleUnitChanged(EmergencyUnit unit, UnitState state)
        {
            RefreshFleetDock();
        }

        public void RefreshCityStats()
        {
            if (CityStatsManager.Instance == null) return;

            // Matching user sketch: [ 👤 150 / 150 ]
            if (populationText != null)
                populationText.text = $"{CityStatsManager.Instance.CurrentSurvivors} / {CityStatsManager.Instance.InitialPopulation}";

            if (atRiskText != null)
                atRiskText.text = $"AT RISK: {CityStatsManager.Instance.CiviliansAtRisk}";

            if (casualtiesText != null)
                casualtiesText.text = $"CASUALTIES: {CityStatsManager.Instance.TotalCasualties}";

            if (rescuedText != null)
                rescuedText.text = $"RESCUED: {CityStatsManager.Instance.TotalRescued}";
        }

        public void RefreshFleetDock()
        {
            if (DispatchManager.Instance == null) return;

            // Matching user sketch: [ Police 4/5 ] [ Ambulance 5/5 ] [ Fire 6/6 ] [ Helicopter 5/5 ]
            if (policeDockText != null)
            {
                int idle = DispatchManager.Instance.GetIdleCount(UnitType.PoliceCruiser);
                int total = DispatchManager.Instance.GetTotalCount(UnitType.PoliceCruiser);
                policeDockText.text = $"Police {idle}/{total}";
            }

            if (ambulanceDockText != null)
            {
                int idle = DispatchManager.Instance.GetIdleCount(UnitType.Ambulance);
                int total = DispatchManager.Instance.GetTotalCount(UnitType.Ambulance);
                ambulanceDockText.text = $"Ambulance {idle}/{total}";
            }

            if (fireEngineDockText != null)
            {
                int idle = DispatchManager.Instance.GetIdleCount(UnitType.FireEngine);
                int total = DispatchManager.Instance.GetTotalCount(UnitType.FireEngine);
                fireEngineDockText.text = $"Fire {idle}/{total}";
            }

            if (helicopterDockText != null)
            {
                int idle = DispatchManager.Instance.GetIdleCount(UnitType.RescueHelicopter);
                int total = DispatchManager.Instance.GetTotalCount(UnitType.RescueHelicopter);
                helicopterDockText.text = $"Helicopter {idle}/{total}";
            }
        }
    }
}
