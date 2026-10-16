using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZeroHour.Phase2.Data;
using ZeroHour.Phase2.Incidents;

namespace ZeroHour.Phase2.Core
{
    [System.Serializable]
    public class ScriptedShiftEvent
    {
        public float timeOffsetSeconds;
        public string targetBuildingId;
        public IncidentType incidentType;
        public IncidentSeverity severity;
        public string eventTitle;
        [TextArea(2, 4)] public string eventDescription;
        public UnitType[] requiredUnits;
        public int civiliansAtRisk = 15;
        public float workDuration = 10f;
        public float tremorShakeIntensity = 0f;

        [HideInInspector] public bool isExecuted = false;
    }

    public class ShiftTimelineController : MonoBehaviour
    {
        public static ShiftTimelineController Instance { get; private set; }

        [Header("Shift Time Configuration")]
        [SerializeField] private float totalShiftDurationSeconds = 150f;
        [SerializeField] private int startHour = 2; // 02:00 AM
        [SerializeField] private int endHour = 5;   // 05:00 AM

        [Header("Scripted Events Timeline")]
        [SerializeField] private List<ScriptedShiftEvent> shiftEvents = new List<ScriptedShiftEvent>();

        [Header("Camera Shake Hook")]
        [SerializeField] private Camera mainCamera;

        // Runtime states
        private bool isShiftActive = false;
        private float currentShiftTimer = 0f;
        private Coroutine cameraShakeCoroutine;

        public bool IsShiftActive => isShiftActive;
        public float CurrentShiftTimer => currentShiftTimer;
        public float RemainingTime => Mathf.Max(0f, totalShiftDurationSeconds - currentShiftTimer);
        public float ShiftProgressNormalized => Mathf.Clamp01(currentShiftTimer / totalShiftDurationSeconds);

        public event Action OnShiftStarted;
        public event Action<ScriptedShiftEvent> OnEventTriggered;
        public event Action<float> OnCameraShake; // Shake intensity for audio/FX
        public event Action OnShiftEnded;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            PopulateDefaultScriptedTimelineIfEmpty();
        }

        public void StartEmergencyShift()
        {
            currentShiftTimer = 0f;
            isShiftActive = true;

            for (int i = 0; i < shiftEvents.Count; i++)
            {
                shiftEvents[i].isExecuted = false;
            }

            OnShiftStarted?.Invoke();
            Debug.Log("[ShiftTimelineController] Emergency Shift engaged! 02:00 AM grid online.");
        }

        private void Update()
        {
            if (!isShiftActive) return;

            currentShiftTimer += Time.deltaTime;

            // Check events
            for (int i = 0; i < shiftEvents.Count; i++)
            {
                var evt = shiftEvents[i];
                if (!evt.isExecuted && currentShiftTimer >= evt.timeOffsetSeconds)
                {
                    ExecuteShiftEvent(evt);
                }
            }

            // Check shift completion
            if (currentShiftTimer >= totalShiftDurationSeconds)
            {
                EndEmergencyShift();
            }
        }

        private void ExecuteShiftEvent(ScriptedShiftEvent evt)
        {
            evt.isExecuted = true;

            BuildingNode building = DispatchManager.Instance != null 
                ? DispatchManager.Instance.GetBuildingById(evt.targetBuildingId) 
                : null;

            if (building != null)
            {
                // Dynamic consequence check from Phase 1 bridge
                IncidentSeverity finalSeverity = evt.severity;
                string finalTitle = evt.eventTitle;
                string finalDesc = evt.eventDescription;
                int finalCivilians = evt.civiliansAtRisk;
                List<UnitType> finalUnits = new List<UnitType>(evt.requiredUnits);

                if (building.Phase1Result == InspectionResult.WronglyApproved)
                {
                    finalSeverity = IncidentSeverity.Critical;
                    finalTitle = $"[CRITICAL HAZARD] {evt.eventTitle}";
                    finalDesc = $"{evt.eventDescription}\n>> WARNING: Daytime inspection failed to detect critical hazard! Casualties elevated.";
                    finalCivilians = Mathf.RoundToInt(finalCivilians * 1.8f);

                    // If fire or collapse, require medical triage too
                    if (!finalUnits.Contains(UnitType.Ambulance))
                    {
                        finalUnits.Add(UnitType.Ambulance);
                    }
                }
                else if (building.Phase1Result == InspectionResult.FlaggedDefective)
                {
                    finalDesc = $"{evt.eventDescription}\n>> NOTICE: Early evacuation order in effect. Minimal casualties.";
                    finalCivilians = Mathf.Max(2, Mathf.RoundToInt(finalCivilians * 0.3f));
                }

                IncidentInstance incident = new IncidentInstance(
                    $"INC_{evt.targetBuildingId}_{Time.time:F0}",
                    evt.incidentType,
                    finalSeverity,
                    finalTitle,
                    finalDesc,
                    finalCivilians,
                    evt.workDuration,
                    finalUnits.ToArray()
                );

                building.TriggerIncident(incident);

                // Trigger building & camera tremors
                if (evt.tremorShakeIntensity > 0f)
                {
                    building.TriggerTremorShake(evt.tremorShakeIntensity, 1.5f);
                    TriggerCameraShake(evt.tremorShakeIntensity);
                }
            }

            OnEventTriggered?.Invoke(evt);
        }

        public void TriggerCameraShake(float intensity, float duration = 0.8f)
        {
            OnCameraShake?.Invoke(intensity);
            if (mainCamera == null) return;

            if (cameraShakeCoroutine != null) StopCoroutine(cameraShakeCoroutine);
            cameraShakeCoroutine = StartCoroutine(CameraShakeRoutine(intensity, duration));
        }

        private IEnumerator CameraShakeRoutine(float intensity, float duration)
        {
            Vector3 originalCamPos = mainCamera.transform.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float damp = 1f - (elapsed / duration);
                Vector3 shake = UnityEngine.Random.insideUnitSphere * intensity * damp;
                shake.z = 0; // Maintain 2.5D isometric view depth
                mainCamera.transform.position = originalCamPos + shake;
                yield return null;
            }

            mainCamera.transform.position = originalCamPos;
        }

        private void EndEmergencyShift()
        {
            isShiftActive = false;
            OnShiftEnded?.Invoke();
            Debug.Log("[ShiftTimelineController] Emergency Shift complete. Dawn has broken.");
        }

        public string GetSimulatedClockTime()
        {
            float progress = ShiftProgressNormalized;
            float currentHourDecimal = Mathf.Lerp(startHour, endHour, progress);
            int hour = Mathf.FloorToInt(currentHourDecimal);
            int minute = Mathf.FloorToInt((currentHourDecimal - hour) * 60f);
            return $"{hour:D2}:{minute:D2} AM";
        }

        private void PopulateDefaultScriptedTimelineIfEmpty()
        {
            if (shiftEvents.Count > 0) return;

            // 1. Initial Tremor in Quake District (Bottom-Left)
            shiftEvents.Add(new ScriptedShiftEvent
            {
                timeOffsetSeconds = 12f,
                targetBuildingId = "BLD_QUAKE_SECTOR",
                incidentType = IncidentType.StructuralCollapse,
                severity = IncidentSeverity.Medium,
                eventTitle = "SEISMIC COLLAPSE - SECTOR 4",
                eventDescription = "Ground shockwave collapsed residential facade. Multiple trapped citizens.",
                requiredUnits = new[] { UnitType.Ambulance },
                civiliansAtRisk = 14,
                workDuration = 8f,
                tremorShakeIntensity = 0.45f
            });

            // 2. Distant Fire in Industrial Grid (Bottom-Right) -> Helicopter Dilemma!
            shiftEvents.Add(new ScriptedShiftEvent
            {
                timeOffsetSeconds = 30f,
                targetBuildingId = "BLD_FIRE_SECTOR",
                incidentType = IncidentType.BuildingFire,
                severity = IncidentSeverity.Critical,
                eventTitle = "DISTANT BLAZE - EAST HARBOR",
                eventDescription = "Severe fire outbreak in distant sector! Ground engines are far; aerial helicopter water drop can reach rapidly, but will tie up air fleet.",
                requiredUnits = new[] { UnitType.FireEngine },
                civiliansAtRisk = 25,
                workDuration = 12f,
                tremorShakeIntensity = 0.15f
            });

            // 3. Suspected Looting Call near Quake Zone -> Potential False Alarm!
            shiftEvents.Add(new ScriptedShiftEvent
            {
                timeOffsetSeconds = 60f,
                targetBuildingId = "BLD_COMMERCIAL",
                incidentType = IncidentType.LootingAndUnrest,
                severity = IncidentSeverity.Low,
                eventTitle = "911 REPORT: SUSPECTED LOOTING",
                eventDescription = "Panicked 911 caller reports armed looters breaking into storefronts near quake zone. Dispatch Police to investigate.",
                requiredUnits = new[] { UnitType.PoliceCruiser },
                civiliansAtRisk = 6,
                workDuration = 4f,
                tremorShakeIntensity = 0f
            });

            // 4. Rooftop Isolation (Requires Helicopter!)
            shiftEvents.Add(new ScriptedShiftEvent
            {
                timeOffsetSeconds = 95f,
                targetBuildingId = "BLD_HOTEL",
                incidentType = IncidentType.RooftopStranded,
                severity = IncidentSeverity.Critical,
                eventTitle = "ROOFTOP EMERGENCY: CITIZENS STRANDED",
                eventDescription = "Stairwells collapsed after aftershock! Survivors trapped on roof. Ground vehicles unable to access; Helicopter mandatory!",
                requiredUnits = new[] { UnitType.RescueHelicopter },
                civiliansAtRisk = 18,
                workDuration = 14f,
                tremorShakeIntensity = 0.55f
            });

            // 5. Gas Explosion from Daytime Inspection Error
            shiftEvents.Add(new ScriptedShiftEvent
            {
                timeOffsetSeconds = 120f,
                targetBuildingId = "BLD_METRO_PLAZA",
                incidentType = IncidentType.GasExplosionFire,
                severity = IncidentSeverity.Critical,
                eventTitle = "GAS ACCELERANT EXPLOSION",
                eventDescription = "Severed gas line ruptured! Heavy fire and wounded casualties.",
                requiredUnits = new[] { UnitType.FireEngine, UnitType.Ambulance },
                civiliansAtRisk = 20,
                workDuration = 10f,
                tremorShakeIntensity = 0.35f
            });
        }
    }
}
