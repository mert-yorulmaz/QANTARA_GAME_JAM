using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ZeroHour.Phase2.Core;

namespace ZeroHour.Phase2.UI
{
    public class DispatchTerminalUI : MonoBehaviour
    {
        [Header("Terminal Elements")]
        [SerializeField] private GameObject terminalPanel;
        [SerializeField] private TextMeshProUGUI terminalLogText;
        [SerializeField] private Button engageShiftButton;
        [SerializeField] private float typewriterSpeed = 0.02f;

        [Header("Terminal Content (English)")]
        [TextArea(8, 14)]
        [SerializeField] private string briefingMessage =
            ">>> [CITY CRISIS COMMAND - C4I TERMINAL v2.4]\n" +
            ">>> SYSTEM TIME: 01:58:20 AM // NIGHT PROTOCOL ACTIVE\n" +
            ">>> WARNING: Subterranean seismic tremors detected across Sectors 1-5.\n" +
            ">>> CRITICAL DIRECTIVES:\n" +
            "    1. Triage incoming 911 distress calls on the 2.5D grid.\n" +
            "    2. Structural hazards correlate with daytime inspection reports.\n" +
            "    3. Deploy Police Cruisers to quell blackout looting.\n" +
            "    4. Deploy Rescue Helicopters for collapsed roads & rooftop survivors.\n" +
            ">>> OBJECTIVE: Maintain city survival rate above 70%.\n" +
            ">>> STANDBY FOR INITIAL SHOCK...";

        private Coroutine typewriterCoroutine;

        private void Start()
        {
            if (engageShiftButton != null)
            {
                engageShiftButton.onClick.AddListener(OnEngageClicked);
                engageShiftButton.interactable = false;
            }

            OpenBriefing();
        }

        public void OpenBriefing()
        {
            if (terminalPanel != null) terminalPanel.SetActive(true);

            if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = StartCoroutine(TypewriterRoutine());
        }

        private IEnumerator TypewriterRoutine()
        {
            if (terminalLogText != null)
            {
                terminalLogText.text = "";
                for (int i = 0; i < briefingMessage.Length; i++)
                {
                    terminalLogText.text += briefingMessage[i];
                    yield return new WaitForSeconds(typewriterSpeed);
                }
            }

            if (engageShiftButton != null)
            {
                engageShiftButton.interactable = true;
            }
        }

        private void OnEngageClicked()
        {
            if (terminalPanel != null) terminalPanel.SetActive(false);

            if (ShiftTimelineController.Instance != null)
            {
                ShiftTimelineController.Instance.StartEmergencyShift();
            }
        }
    }
}
