using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZeroHour.Phase2.Data;
using ZeroHour.Phase2.Units;
using ZeroHour.Phase2.Core;

namespace ZeroHour.Phase2.Incidents
{
    [RequireComponent(typeof(Collider))]
    public class BuildingNode : MonoBehaviour
    {
        [Header("Building Metadata")]
        [SerializeField] private string buildingId = "BLD_01";
        [SerializeField] private string buildingName = "Sector Block";
        [SerializeField] private int totalCivilians = 50;

        [Header("Phase 1 Inspection Reference")]
        [SerializeField] private InspectionResult phase1Result = InspectionResult.PassedSafe;
        [SerializeField] private string phase1Notes = "";

        [Header("Holographic Visual Elements")]
        [SerializeField] private GameObject incidentPulseMarker;
        [SerializeField] private MeshRenderer buildingMeshRenderer;
        [SerializeField] private Color normalWireframeColor = new Color(0f, 0.9f, 0.7f, 1f);
        [SerializeField] private Color incidentWarningColor = new Color(1f, 0.25f, 0.1f, 1f);
        [SerializeField] private Color resolvedSuccessColor = new Color(0.2f, 1f, 0.4f, 1f);

        [Header("Audio / FX Anchors")]
        [SerializeField] private Transform vehicleParkingAnchor;
        [SerializeField] private Transform rooftopLandingAnchor;

        // Runtime state
        private IncidentInstance currentIncident = null;
        private Vector3 initialPosition;
        private Coroutine shakeCoroutine;

        public string BuildingId => buildingId;
        public string BuildingName => buildingName;
        public int TotalCivilians => totalCivilians;
        public InspectionResult Phase1Result => phase1Result;
        public string Phase1Notes => phase1Notes;
        public IncidentInstance CurrentIncident => currentIncident;
        public bool HasActiveIncident => currentIncident != null && !currentIncident.isResolved;

        public Vector3 ParkingPosition => vehicleParkingAnchor != null ? vehicleParkingAnchor.position : transform.position + new Vector3(0, 0, -1.2f);
        public Vector3 RooftopPosition => rooftopLandingAnchor != null ? rooftopLandingAnchor.position : transform.position + new Vector3(0, 2.5f, 0);

        public event Action<BuildingNode> OnBuildingClicked;
        public event Action<BuildingNode, IncidentInstance> OnIncidentStarted;
        public event Action<BuildingNode, IncidentInstance> OnIncidentResolved;

        private void Awake()
        {
            initialPosition = transform.position;
            SetMarkerActive(false);
        }

        public void ApplyPhase1Record(BuildingInspectionRecord record)
        {
            if (record == null) return;
            phase1Result = record.result;
            phase1Notes = record.inspectionNote;
        }

        public void TriggerIncident(IncidentInstance incident)
        {
            currentIncident = incident;
            SetMarkerActive(true);
            SetBuildingHighlight(incidentWarningColor);

            // Notify City Stats of endangered civilians
            if (CityStatsManager.Instance != null)
            {
                CityStatsManager.Instance.AddCiviliansAtRisk(incident.civiliansAtRisk);
            }

            OnIncidentStarted?.Invoke(this, incident);
        }

        public void TriggerTremorShake(float intensity = 0.35f, float duration = 1.2f)
        {
            if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
            shakeCoroutine = StartCoroutine(ShakeRoutine(intensity, duration));
        }

        private IEnumerator ShakeRoutine(float intensity, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float damp = 1f - (elapsed / duration);
                Vector3 offset = UnityEngine.Random.insideUnitSphere * intensity * damp;
                offset.y = 0; // Keep on 2.5D base plane
                transform.position = initialPosition + offset;
                yield return null;
            }
            transform.position = initialPosition;
        }

        public void NotifyUnitEngaged(EmergencyUnit unit)
        {
            if (currentIncident == null || currentIncident.isResolved) return;

            if (!currentIncident.assignedUnits.Contains(unit))
            {
                currentIncident.assignedUnits.Add(unit);
            }

            if (currentIncident.isFalseAlarm)
            {
                Debug.Log($"[BuildingNode] {unit.Callsign} arrived at {buildingName}: {currentIncident.falseAlarmReport}");
                // False alarm surveyed quickly
                ResolveIncident();
                return;
            }

            if (currentIncident.AreRequirementsMet())
            {
                currentIncident.isBeingTreated = true;
            }
        }

        public void NotifyUnitFinished(EmergencyUnit unit)
        {
            if (currentIncident == null || currentIncident.isResolved) return;

            // When units finish, evaluate resolution
            if (currentIncident.AreRequirementsMet())
            {
                ResolveIncident();
            }
        }

        public void DetachUnit(EmergencyUnit unit)
        {
            if (currentIncident != null)
            {
                currentIncident.assignedUnits.Remove(unit);
                currentIncident.isBeingTreated = currentIncident.AreRequirementsMet();
            }
        }

        public void ResolveIncident()
        {
            if (currentIncident == null || currentIncident.isResolved) return;

            currentIncident.isResolved = true;
            SetMarkerActive(false);
            SetBuildingHighlight(resolvedSuccessColor);

            if (CityStatsManager.Instance != null)
            {
                CityStatsManager.Instance.ResolveCiviliansAtRisk(currentIncident.civiliansAtRisk);
            }

            OnIncidentResolved?.Invoke(this, currentIncident);
        }

        private void Update()
        {
            // Tick urgency if unattended
            if (HasActiveIncident && !currentIncident.isBeingTreated)
            {
                currentIncident.elapsedUrgency += Time.deltaTime;
                if (currentIncident.elapsedUrgency >= currentIncident.urgencyTimer)
                {
                    if (Time.time - currentIncident.lastCasualtyTick >= currentIncident.casualtyTickInterval)
                    {
                        currentIncident.lastCasualtyTick = Time.time;
                        if (currentIncident.civiliansAtRisk > 0)
                        {
                            currentIncident.civiliansAtRisk--;
                            if (CityStatsManager.Instance != null)
                            {
                                CityStatsManager.Instance.RegisterCasualty(1);
                            }
                        }
                    }
                }
            }
        }

        private void OnMouseDown()
        {
            // Raycast click response on building
            OnBuildingClicked?.Invoke(this);
            if (DispatchManager.Instance != null)
            {
                DispatchManager.Instance.SelectBuilding(this);
            }
        }

        private void SetMarkerActive(bool active)
        {
            if (incidentPulseMarker != null)
            {
                incidentPulseMarker.SetActive(active);
            }
        }

        private void SetBuildingHighlight(Color col)
        {
            if (buildingMeshRenderer != null && buildingMeshRenderer.material != null)
            {
                buildingMeshRenderer.material.color = col;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = HasActiveIncident ? Color.red : Color.green;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 1f, new Vector3(2f, 2f, 2f));
        }
    }
}
