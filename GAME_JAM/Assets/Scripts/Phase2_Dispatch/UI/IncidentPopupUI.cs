using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ZeroHour.Phase2.Core;
using ZeroHour.Phase2.Data;
using ZeroHour.Phase2.Incidents;

namespace ZeroHour.Phase2.UI
{
    public class IncidentPopupUI : MonoBehaviour
    {
        [Header("Popup Panel Root")]
        [SerializeField] private GameObject popupRoot;

        [Header("Text Fields")]
        [SerializeField] private TextMeshProUGUI buildingTitleText;
        [SerializeField] private TextMeshProUGUI incidentTitleText;
        [SerializeField] private TextMeshProUGUI incidentDescriptionText;
        [SerializeField] private TextMeshProUGUI severityBadgeText;
        [SerializeField] private TextMeshProUGUI civiliansAtRiskText;
        [SerializeField] private TextMeshProUGUI requiredUnitsText;

        [Header("Dispatch Action Buttons")]
        [SerializeField] private Button sendFireButton;
        [SerializeField] private Button sendAmbulanceButton;
        [SerializeField] private Button sendPoliceButton;
        [SerializeField] private Button sendHelicopterButton;
        [SerializeField] private Button closeButton;

        private BuildingNode currentSelectedBuilding = null;

        private void Start()
        {
            if (popupRoot != null) popupRoot.SetActive(false);

            if (sendFireButton != null) sendFireButton.onClick.AddListener(() => OnDispatchClicked(UnitType.FireEngine));
            if (sendAmbulanceButton != null) sendAmbulanceButton.onClick.AddListener(() => OnDispatchClicked(UnitType.Ambulance));
            if (sendPoliceButton != null) sendPoliceButton.onClick.AddListener(() => OnDispatchClicked(UnitType.PoliceCruiser));
            if (sendHelicopterButton != null) sendHelicopterButton.onClick.AddListener(() => OnDispatchClicked(UnitType.RescueHelicopter));
            if (closeButton != null) closeButton.onClick.AddListener(ClosePopup);

            if (DispatchManager.Instance != null)
            {
                DispatchManager.Instance.OnBuildingSelected += HandleBuildingSelected;
                DispatchManager.Instance.OnBuildingDeselected += ClosePopup;
            }
        }

        private void OnDestroy()
        {
            if (DispatchManager.Instance != null)
            {
                DispatchManager.Instance.OnBuildingSelected -= HandleBuildingSelected;
                DispatchManager.Instance.OnBuildingDeselected -= ClosePopup;
            }
        }

        private void HandleBuildingSelected(BuildingNode building)
        {
            if (building == null || !building.HasActiveIncident)
            {
                ClosePopup();
                return;
            }

            currentSelectedBuilding = building;
            RefreshPopupContent();
            if (popupRoot != null) popupRoot.SetActive(true);
        }

        public void RefreshPopupContent()
        {
            if (currentSelectedBuilding == null || !currentSelectedBuilding.HasActiveIncident)
            {
                ClosePopup();
                return;
            }

            var incident = currentSelectedBuilding.CurrentIncident;

            if (buildingTitleText != null)
                buildingTitleText.text = currentSelectedBuilding.BuildingName.ToUpper();

            if (incidentTitleText != null)
                incidentTitleText.text = incident.title;

            if (incidentDescriptionText != null)
                incidentDescriptionText.text = incident.description;

            if (severityBadgeText != null)
                severityBadgeText.text = $"SEVERITY: {incident.severity.ToString().ToUpper()}";

            if (civiliansAtRiskText != null)
                civiliansAtRiskText.text = $"CIVILIANS AT RISK: {incident.civiliansAtRisk}";

            if (requiredUnitsText != null)
            {
                string reqs = "REQUIRED: ";
                for (int i = 0; i < incident.requiredUnitTypes.Count; i++)
                {
                    reqs += incident.requiredUnitTypes[i].ToString();
                    if (i < incident.requiredUnitTypes.Count - 1) reqs += ", ";
                }
                requiredUnitsText.text = reqs;
            }

            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            if (DispatchManager.Instance == null || currentSelectedBuilding == null) return;

            var incident = currentSelectedBuilding.CurrentIncident;
            if (incident == null) return;

            if (sendFireButton != null)
                sendFireButton.interactable = DispatchManager.Instance.GetIdleCount(UnitType.FireEngine) > 0;

            if (sendAmbulanceButton != null)
                sendAmbulanceButton.interactable = DispatchManager.Instance.GetIdleCount(UnitType.Ambulance) > 0;

            if (sendPoliceButton != null)
                sendPoliceButton.interactable = DispatchManager.Instance.GetIdleCount(UnitType.PoliceCruiser) > 0;

            if (sendHelicopterButton != null)
                sendHelicopterButton.interactable = DispatchManager.Instance.GetIdleCount(UnitType.RescueHelicopter) > 0;
        }

        private void OnDispatchClicked(UnitType type)
        {
            if (DispatchManager.Instance != null && currentSelectedBuilding != null)
            {
                bool success = DispatchManager.Instance.DispatchUnit(type, currentSelectedBuilding);
                if (success)
                {
                    RefreshPopupContent();
                }
            }
        }

        public void ClosePopup()
        {
            currentSelectedBuilding = null;
            if (popupRoot != null) popupRoot.SetActive(false);
        }
    }
}
