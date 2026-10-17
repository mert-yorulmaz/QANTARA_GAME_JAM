using UnityEngine;
using TMPro;
using ZeroHour.Phase2.Core;
using ZeroHour.Phase2.Data;
using ZeroHour.Phase2.Units;

namespace ZeroHour.Phase2.UI
{
    public enum UnitCountDisplayMode
    {
        NumberOnly,          // Sadece sayı: "3"
        AvailableSlashTotal, // Boşta / Toplam: "3/3"
        AvailableSlashActive // Boşta / Görevde
    }

    public class DispatchHUD : MonoBehaviour
    {
        [Header("--- 1. SABİT ÜST BAR: BİRİM SAYAÇLARI (UNITS) ---")]
        [Tooltip("Polis ikonunun altındaki sayı Text'i")]
        [SerializeField] private TMP_Text policeCountText;

        [Tooltip("Ambulans ikonunun altındaki sayı Text'i")]
        [SerializeField] private TMP_Text ambulanceCountText;

        [Tooltip("İtfaiye ikonunun altındaki sayı Text'i")]
        [SerializeField] private TMP_Text fireTruckCountText;

        [Tooltip("Helikopter ikonunun altındaki sayı Text'i")]
        [SerializeField] private TMP_Text helicopterCountText;

        [Tooltip("Birimlerin altında sayının nasıl görüneceği")]
        [SerializeField] private UnitCountDisplayMode countDisplayMode = UnitCountDisplayMode.NumberOnly;

        [Header("--- 2. SABİT ÜST BAR: CANLI NÜFUS SAYACI (150/150) ---")]
        [Tooltip("Sağ üstteki nüfus Text'i (Örn: 150 / 150)")]
        [SerializeField] private TMP_Text populationText;

        [Tooltip("Nüfus formatı. {0} = Kalan Canlı, {1} = Toplam Nüfus")]
        [SerializeField] private string populationFormat = "{0} / {1}";

        [Header("--- 3. İSTEĞE BAĞLI ALANLAR (Opsiyonel) ---")]
        [SerializeField] private TMP_Text clockText;
        [SerializeField] private TMP_Text atRiskText;
        [SerializeField] private TMP_Text casualtiesText;
        [SerializeField] private TMP_Text rescuedText;

        private void OnEnable()
        {
            SubscribeEvents();
            RefreshCityStats();
            RefreshFleetDock();
        }

        private void Start()
        {
            RefreshCityStats();
            RefreshFleetDock();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void SubscribeEvents()
        {
            if (CityStatsManager.Instance != null)
            {
                CityStatsManager.Instance.OnStatsUpdated -= RefreshCityStats;
                CityStatsManager.Instance.OnStatsUpdated += RefreshCityStats;
            }

            if (DispatchManager.Instance != null)
            {
                DispatchManager.Instance.OnUnitStateChanged -= HandleUnitChanged;
                DispatchManager.Instance.OnUnitStateChanged += HandleUnitChanged;
                DispatchManager.Instance.OnUnitDispatched -= HandleUnitDispatched;
                DispatchManager.Instance.OnUnitDispatched += HandleUnitDispatched;
            }
        }

        private void UnsubscribeEvents()
        {
            if (CityStatsManager.Instance != null)
            {
                CityStatsManager.Instance.OnStatsUpdated -= RefreshCityStats;
            }

            if (DispatchManager.Instance != null)
            {
                DispatchManager.Instance.OnUnitStateChanged -= HandleUnitChanged;
                DispatchManager.Instance.OnUnitDispatched -= HandleUnitDispatched;
            }
        }

        private void Update()
        {
            // Dijital Saat (Opsiyonel)
            if (ShiftTimelineController.Instance != null && clockText != null)
            {
                clockText.text = ShiftTimelineController.Instance.GetSimulatedClockTime();
            }
        }

        private void HandleUnitChanged(EmergencyUnit unit, UnitState state)
        {
            RefreshFleetDock();
        }

        private void HandleUnitDispatched(EmergencyUnit unit, Incidents.BuildingNode building)
        {
            RefreshFleetDock();
        }

        /// <summary>
        /// Nüfus sayacını (150/150) günceller.
        /// </summary>
        public void RefreshCityStats()
        {
            if (CityStatsManager.Instance == null) return;

            int survivors = CityStatsManager.Instance.CurrentSurvivors;
            int total = CityStatsManager.Instance.InitialPopulation;

            if (populationText != null)
            {
                populationText.text = string.Format(populationFormat, survivors, total);
            }

            if (atRiskText != null)
                atRiskText.text = $"AT RISK: {CityStatsManager.Instance.CiviliansAtRisk}";

            if (casualtiesText != null)
                casualtiesText.text = $"CASUALTIES: {CityStatsManager.Instance.TotalCasualties}";

            if (rescuedText != null)
                rescuedText.text = $"RESCUED: {CityStatsManager.Instance.TotalRescued}";
        }

        /// <summary>
        /// 4 Birimin altındaki sayıları günceller (Police, Ambulance, Fire, Helicopter).
        /// </summary>
        public void RefreshFleetDock()
        {
            if (DispatchManager.Instance == null) return;

            UpdateUnitText(policeCountText, UnitType.PoliceCruiser);
            UpdateUnitText(ambulanceCountText, UnitType.Ambulance);
            UpdateUnitText(fireTruckCountText, UnitType.FireEngine);
            UpdateUnitText(helicopterCountText, UnitType.RescueHelicopter);
        }

        private void UpdateUnitText(TMP_Text targetText, UnitType type)
        {
            if (targetText == null) return;

            int idle = DispatchManager.Instance.GetIdleCount(type);
            int total = DispatchManager.Instance.GetTotalCount(type);
            int active = DispatchManager.Instance.GetActiveCount(type);

            switch (countDisplayMode)
            {
                case UnitCountDisplayMode.NumberOnly:
                    // Sadece hazır/boşta olan araç sayısı: "3"
                    targetText.text = idle.ToString();
                    break;

                case UnitCountDisplayMode.AvailableSlashTotal:
                    // "3/3" veya "1/2"
                    targetText.text = $"{idle}/{total}";
                    break;

                case UnitCountDisplayMode.AvailableSlashActive:
                    // Boşta / Görevde
                    targetText.text = $"{idle} ({active})";
                    break;
            }
        }

        [ContextMenu("⚡ Force Update HUD")]
        public void ForceUpdateHUD()
        {
            RefreshCityStats();
            RefreshFleetDock();
        }
    }
}
