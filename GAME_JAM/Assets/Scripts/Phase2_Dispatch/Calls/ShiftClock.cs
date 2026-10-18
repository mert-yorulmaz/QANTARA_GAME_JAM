using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ZeroHour.Phase2.Core;

namespace ZeroHour.Phase2.Calls
{
    /// <summary>
    /// Gece vardiyası saati: sol üstte kalan süre (örn. 2:41) ve oyun saati (02:00 → 05:00).
    /// Süre bitince çağrılar durur, oyun durur ve vardiya sonu karnesi açılır.
    /// </summary>
    public class ShiftClock : MonoBehaviour
    {
        public static ShiftClock Instance { get; private set; }

        [Header("Vardiya")]
        [Tooltip("Gerçek süre (sn). 180 = 3 dakika")]
        [SerializeField] private float shiftDurationSeconds = 180f;
        [SerializeField] private int shiftStartHour = 2;
        [SerializeField] private int shiftEndHour = 5;
        [Tooltip("Son bu kadar saniyede sayaç kırmızıya döner")]
        [SerializeField] private float warningSeconds = 30f;

        [Header("Sol Üst Sayaç")]
        [SerializeField] private TMP_Text timeLeftText;
        [SerializeField] private TMP_Text clockText;

        [Header("Vardiya Sonu Karnesi")]
        [SerializeField] private GameObject debriefRoot;
        [SerializeField] private TMP_Text debriefStatsText;
        [SerializeField] private TMP_Text debriefGradeText;
        [SerializeField] private Button playAgainButton;

        private static readonly Color NormalColor = new Color(0.47f, 0.88f, 0.69f);
        private static readonly Color WarningColor = new Color(1f, 0.36f, 0.30f);

        private float elapsed;

        public bool IsOver { get; private set; }
        public float TimeLeft => Mathf.Max(0f, shiftDurationSeconds - elapsed);

        private void Awake()
        {
            Instance = this;
            if (debriefRoot != null) debriefRoot.SetActive(false);
            if (playAgainButton != null) playAgainButton.onClick.AddListener(PlayAgain);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (IsOver) return;

            elapsed += Time.deltaTime;
            RefreshTexts();

            if (elapsed >= shiftDurationSeconds) EndShift();
        }

        /// <summary>Oyun saati: vardiya süresi 02:00–05:00 aralığına yayılır.</summary>
        public string GetClockTime()
        {
            float t = Mathf.Clamp01(elapsed / Mathf.Max(1f, shiftDurationSeconds));
            int totalMinutes = Mathf.FloorToInt(Mathf.Lerp(shiftStartHour * 60, shiftEndHour * 60, t));
            return $"{totalMinutes / 60:00}:{totalMinutes % 60:00}";
        }

        private void RefreshTexts()
        {
            int left = Mathf.CeilToInt(TimeLeft);
            if (timeLeftText != null)
            {
                timeLeftText.text = $"{left / 60}:{left % 60:00}";
                timeLeftText.color = TimeLeft <= warningSeconds ? WarningColor : NormalColor;
            }
            if (clockText != null) clockText.text = Loc.Turkish ? GetClockTime() : $"{GetClockTime()} AM";
        }

        public void EndShift()
        {
            if (IsOver) return;
            IsOver = true;
            elapsed = shiftDurationSeconds;
            RefreshTexts();

            // Yeni çağrı gelmesin, açık dosya kapansın
            IncomingCallController calls = FindFirstObjectByType<IncomingCallController>();
            if (calls != null) calls.StopCalls();
            if (IncidentFileUI.Instance != null) IncidentFileUI.Instance.Close();

            ShowDebrief();
            Time.timeScale = 0f;
            Debug.Log("<color=yellow>[Shift] 🌅 Saat 05:00 — vardiya bitti.</color>");
        }

        private void ShowDebrief()
        {
            CityStatsManager stats = CityStatsManager.Instance;
            int survivors = stats != null ? stats.CurrentSurvivors : 0;
            int total = stats != null ? stats.InitialPopulation : 0;
            int rescued = stats != null ? stats.TotalRescued : 0;
            int lost = stats != null ? stats.TotalCasualties : 0;
            float rate = stats != null ? stats.SurvivalRatePercent : 0f;

            if (debriefStatsText != null)
            {
                debriefStatsText.text =
                    $"{Loc.T("Survivors")}<pos=56%>{survivors} / {total}\n" +
                    $"{Loc.T("Rescued")}<pos=56%>{rescued}\n" +
                    $"{Loc.T("Lost")}<pos=56%>{lost}\n" +
                    $"{Loc.T("Survival")}<pos=56%>{rate:0}%";
            }

            if (debriefGradeText != null) debriefGradeText.text = Grade(rate);
            if (debriefRoot != null) debriefRoot.SetActive(true);
        }

        private static string Grade(float survivalPercent)
        {
            if (survivalPercent >= 95f) return "S";
            if (survivalPercent >= 85f) return "A";
            if (survivalPercent >= 70f) return "B";
            if (survivalPercent >= 50f) return "C";
            return "F";
        }

        private void PlayAgain()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
