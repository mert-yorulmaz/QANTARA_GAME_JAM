using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ZeroHour.Phase2.Core;

namespace ZeroHour.Phase2.UI
{
    public class ShiftDebriefUI : MonoBehaviour
    {
        [Header("Debrief Panel Root")]
        [SerializeField] private GameObject debriefPanel;

        [Header("Statistics Fields")]
        [SerializeField] private TextMeshProUGUI survivalRateText;
        [SerializeField] private TextMeshProUGUI totalRescuedText;
        [SerializeField] private TextMeshProUGUI totalCasualtiesText;
        [SerializeField] private TextMeshProUGUI inspectionImpactText;
        [SerializeField] private TextMeshProUGUI operationGradeText;

        [Header("Action Buttons")]
        [SerializeField] private Button nextDayButton;

        private void Start()
        {
            if (debriefPanel != null) debriefPanel.SetActive(false);

            if (nextDayButton != null)
            {
                nextDayButton.onClick.AddListener(OnNextDayClicked);
            }

            if (ShiftTimelineController.Instance != null)
            {
                ShiftTimelineController.Instance.OnShiftEnded += ShowDebrief;
            }
        }

        private void OnDestroy()
        {
            if (ShiftTimelineController.Instance != null)
            {
                ShiftTimelineController.Instance.OnShiftEnded -= ShowDebrief;
            }
        }

        public void ShowDebrief()
        {
            if (debriefPanel != null) debriefPanel.SetActive(true);

            if (CityStatsManager.Instance != null)
            {
                float rate = CityStatsManager.Instance.SurvivalRatePercent;
                if (survivalRateText != null)
                    survivalRateText.text = $"CITY SURVIVAL RATE: {rate:F1}%";

                if (totalRescuedText != null)
                    totalRescuedText.text = $"RESCUED CITIZENS: {CityStatsManager.Instance.TotalRescued:N0}";

                if (totalCasualtiesText != null)
                    totalCasualtiesText.text = $"CONFIRMED CASUALTIES: {CityStatsManager.Instance.TotalCasualties:N0}";

                if (operationGradeText != null)
                {
                    string grade = "F";
                    if (rate >= 95f) grade = "GRADE S - HEROIC DISPATCH";
                    else if (rate >= 85f) grade = "GRADE A - EFFICIENT TRIAGE";
                    else if (rate >= 70f) grade = "GRADE B - ACCEPTABLE CASUALTIES";
                    else if (rate >= 50f) grade = "GRADE C - HIGH LOSSES";
                    else grade = "GRADE F - CATASTROPHIC FAILURE";

                    operationGradeText.text = grade;
                }
            }

            if (inspectionImpactText != null)
            {
                inspectionImpactText.text = "DAYTIME INSPECTION REPORT EVALUATED:\nHazards mitigated through verified building codes.";
            }
        }

        private void OnNextDayClicked()
        {
            Debug.Log("[ShiftDebriefUI] Proceeding to Day 2 or Inspection Phase...");
            // Hooks into Yazılımcı 3's GameManager scene transition or reload
        }
    }
}
