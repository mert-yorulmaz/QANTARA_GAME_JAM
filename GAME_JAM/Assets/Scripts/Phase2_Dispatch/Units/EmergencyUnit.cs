using System;
using UnityEngine;
using ZeroHour.Phase2.Data;
using ZeroHour.Phase2.Incidents;

namespace ZeroHour.Phase2.Units
{
    public class EmergencyUnit : MonoBehaviour
    {
        [Header("Unit Identity")]
        [SerializeField] private string unitCallsign = "UNIT-01";
        [SerializeField] private UnitType unitType;
        [SerializeField] private float travelSpeed = 8f;
        [SerializeField] private float workEfficiencyMultiplier = 1f;

        [Header("Helicopter Specific")]
        [SerializeField] private float flightAltitude = 5f;

        [Header("Visual & Siren Flasher")]
        [SerializeField] private GameObject sirenLights;
        [SerializeField] private TrailRenderer motionTrail;

        // Runtime states
        private UnitState currentState = UnitState.IdleAtBase;
        private UnitStation homeStation;
        private BuildingNode targetBuilding;
        private Vector3 targetPosition;
        private float currentWorkTimer = 0f;
        private float requiredWorkDuration = 0f;

        public string Callsign => unitCallsign;
        public UnitType Type => unitType;
        public UnitState CurrentState => currentState;
        public BuildingNode TargetBuilding => targetBuilding;
        public float WorkProgressNormalized => requiredWorkDuration > 0f ? Mathf.Clamp01(currentWorkTimer / requiredWorkDuration) : 0f;

        public event Action<EmergencyUnit, UnitState> OnStateChanged;
        public event Action<EmergencyUnit> OnArrivedAtTarget;
        public event Action<EmergencyUnit> OnCompletedTask;

        public void Initialize(UnitStation station, string callsign, UnitType type)
        {
            homeStation = station;
            unitCallsign = callsign;
            unitType = type;
            currentState = UnitState.IdleAtBase;
            transform.position = station != null ? station.DispatchPoint : transform.position;

            if (station != null)
            {
                station.RegisterUnit(this);
            }

            SetSirenActive(false);
        }

        private UnityEngine.AI.NavMeshAgent navAgent;

        private void Awake()
        {
            navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (navAgent != null)
            {
                navAgent.obstacleAvoidanceType = UnityEngine.AI.ObstacleAvoidanceType.HighQualityObstacleAvoidance;
                // Avoidance priority: FireEngine = 10, Ambulance = 20, Police = 30, Chopper = 40
                navAgent.avoidancePriority = Mathf.Clamp(((int)unitType + 1) * 10, 10, 90);
                navAgent.autoBraking = true;
                navAgent.acceleration = 16f;
                navAgent.angularSpeed = 240f;
            }
        }

        public bool Dispatch(BuildingNode building, float workDuration)
        {
            if (currentState != UnitState.IdleAtBase)
            {
                Debug.LogWarning($"[EmergencyUnit] {unitCallsign} is not idle! Current: {currentState}");
                return false;
            }

            targetBuilding = building;
            requiredWorkDuration = Mathf.Max(2f, workDuration / workEfficiencyMultiplier);
            currentWorkTimer = 0f;
            targetPosition = building.ParkingPosition;

            bool isHeli = (unitType == UnitType.RescueHelicopter);
            if (isHeli)
            {
                targetPosition = building.RooftopPosition;
                if (navAgent != null) navAgent.enabled = false;
            }
            else if (navAgent != null && navAgent.isOnNavMesh)
            {
                navAgent.enabled = true;
                navAgent.speed = travelSpeed;
                navAgent.ResetPath();
                navAgent.SetDestination(targetPosition);
            }

            SetState(UnitState.EnRouteToIncident);
            SetSirenActive(true);
            return true;
        }

        public void Recall()
        {
            if (currentState == UnitState.IdleAtBase) return;

            if (targetBuilding != null)
            {
                targetBuilding.DetachUnit(this);
                targetBuilding = null;
            }

            StartReturningToBase();
        }

        private void StartReturningToBase()
        {
            SetState(UnitState.ReturningToBase);
            Vector3 basePos = homeStation != null ? homeStation.DispatchPoint : Vector3.zero;
            bool isHeli = (unitType == UnitType.RescueHelicopter);

            if (!isHeli && navAgent != null && navAgent.isOnNavMesh)
            {
                navAgent.enabled = true;
                navAgent.speed = travelSpeed;
                navAgent.ResetPath();
                navAgent.SetDestination(basePos);
            }
        }

        private void Update()
        {
            switch (currentState)
            {
                case UnitState.EnRouteToIncident:
                    MoveTowards(targetPosition, OnArrivalAtIncident);
                    break;

                case UnitState.WorkingAtSite:
                    currentWorkTimer += Time.deltaTime;
                    if (currentWorkTimer >= requiredWorkDuration)
                    {
                        FinishTaskAtSite();
                    }
                    break;

                case UnitState.ReturningToBase:
                    Vector3 basePos = homeStation != null ? homeStation.DispatchPoint : Vector3.zero;
                    MoveTowards(basePos, OnArrivalAtHomeStation);
                    break;
            }
        }

        private void MoveTowards(Vector3 destination, Action onReached)
        {
            bool isHeli = (unitType == UnitType.RescueHelicopter);

            if (!isHeli && navAgent != null && navAgent.enabled && navAgent.isOnNavMesh)
            {
                // A* Pathfinding via Unity NavMeshAgent along city roads
                if (!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance + 0.5f)
                {
                    if (!navAgent.hasPath || navAgent.velocity.sqrMagnitude < 0.1f)
                    {
                        onReached?.Invoke();
                    }
                }
                return;
            }

            // Aerial flight for helicopter or fallback if no navmesh baked
            Vector3 finalDest = destination;
            if (isHeli)
            {
                finalDest.y = flightAltitude;
            }

            transform.position = Vector3.MoveTowards(transform.position, finalDest, travelSpeed * Time.deltaTime);

            Vector3 moveDir = (finalDest - transform.position);
            moveDir.y = 0;
            if (moveDir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 12f * Time.deltaTime);
            }

            if (Vector3.Distance(transform.position, finalDest) < 0.6f)
            {
                onReached?.Invoke();
            }
        }

        private void OnArrivalAtIncident()
        {
            SetState(UnitState.WorkingAtSite);
            OnArrivedAtTarget?.Invoke(this);

            if (targetBuilding != null)
            {
                targetBuilding.NotifyUnitEngaged(this);
            }
        }

        private void FinishTaskAtSite()
        {
            OnCompletedTask?.Invoke(this);

            if (targetBuilding != null)
            {
                targetBuilding.NotifyUnitFinished(this);
                targetBuilding = null;
            }

            StartReturningToBase();
        }

        private void OnArrivalAtHomeStation()
        {
            SetState(UnitState.IdleAtBase);
            SetSirenActive(false);

            if (unitType == UnitType.RescueHelicopter && homeStation != null)
            {
                // Touch down on helipad
                transform.position = homeStation.DispatchPoint;
            }
            else if (homeStation != null)
            {
                transform.position = homeStation.DispatchPoint;
                transform.rotation = homeStation.transform.rotation;
            }
        }

        private void SetState(UnitState newState)
        {
            currentState = newState;
            OnStateChanged?.Invoke(this, newState);
        }

        private void SetSirenActive(bool active)
        {
            if (sirenLights != null) sirenLights.SetActive(active);
            if (motionTrail != null) motionTrail.emitting = active;
        }
    }
}
