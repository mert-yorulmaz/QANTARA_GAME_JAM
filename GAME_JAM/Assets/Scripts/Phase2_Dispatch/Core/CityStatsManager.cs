using System;
using UnityEngine;

namespace ZeroHour.Phase2.Core
{
    public class CityStatsManager : MonoBehaviour
    {
        public static CityStatsManager Instance { get; private set; }

        [Header("City Population & Triage")]
        [SerializeField] private int initialTotalPopulation = 150;
        
        // Runtime stats
        private int currentCiviliansAtRisk = 0;
        private int totalCasualties = 0;
        private int totalRescued = 0;

        public int InitialPopulation => initialTotalPopulation;
        public int CiviliansAtRisk => currentCiviliansAtRisk;
        public int TotalCasualties => totalCasualties;
        public int TotalRescued => totalRescued;
        public int CurrentSurvivors => Mathf.Max(0, initialTotalPopulation - totalCasualties);
        public float SurvivalRatePercent => initialTotalPopulation > 0 
            ? ((float)(initialTotalPopulation - totalCasualties) / initialTotalPopulation) * 100f 
            : 100f;

        public event Action OnStatsUpdated;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void AddCiviliansAtRisk(int count)
        {
            currentCiviliansAtRisk += count;
            OnStatsUpdated?.Invoke();
        }

        public void RegisterCasualty(int count = 1)
        {
            totalCasualties += count;
            currentCiviliansAtRisk = Mathf.Max(0, currentCiviliansAtRisk - count);
            OnStatsUpdated?.Invoke();
        }

        public void ResolveCiviliansAtRisk(int count)
        {
            int saved = Mathf.Min(count, currentCiviliansAtRisk);
            currentCiviliansAtRisk -= saved;
            totalRescued += saved;
            OnStatsUpdated?.Invoke();
        }

        public void ResetStats(int startingPopulation = 15000)
        {
            initialTotalPopulation = startingPopulation;
            currentCiviliansAtRisk = 0;
            totalCasualties = 0;
            totalRescued = 0;
            OnStatsUpdated?.Invoke();
        }
    }
}
