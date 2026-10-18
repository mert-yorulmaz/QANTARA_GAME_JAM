using TMPro;
using UnityEngine;

namespace ZeroHour.Phase2.Calls
{
    /// <summary>
    /// Olay binasının üstünde havada yüzen, kameraya bakan (billboard) ve tıklanabilen olay rozeti.
    /// Tıklama algılaması IncidentBoard'da New Input System ile yapılır.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(SphereCollider))]
    public class IncidentMarker : MonoBehaviour
    {
        [Header("Billboard Boyutu")]
        [Tooltip("Kamera mesafesine göre ölçek: rozet ekranda hep aynı boyda görünür")]
        [SerializeField] private float sizePerDistance = 0.055f;
        [SerializeField] private float minWorldSize = 4f;

        [Header("Sine Salınımı")]
        [SerializeField] private float bobAmplitude = 0.18f;
        [SerializeField] private float bobSpeed = 2.4f;

        [Header("Aciliyet Nabzı")]
        [Tooltip("Kalan süre bunun altına inince rozet nabız gibi atar")]
        [SerializeField] private float urgentPulseBelowSeconds = 15f;

        [Header("Geri Sayım (rozetin üstünde)")]
        [SerializeField] private TMP_Text countdownText;

        private static readonly Color CountdownNormal = Color.white;
        private static readonly Color CountdownUrgent = new Color(1f, 0.32f, 0.25f);
        private static readonly Color CountdownEnRoute = new Color(0.47f, 0.88f, 0.69f);

        private SpriteRenderer spriteRenderer;
        private float spriteWorldSize = 1f;
        private float bobOffset;

        public IncidentReport Report { get; private set; }

        public void Initialize(IncidentReport report, Sprite sprite)
        {
            Report = report;
            spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.sortingOrder = 500;

            // Farklı piksel boyutlu rozetler (96px / 88px) aynı büyüklükte görünsün
            spriteWorldSize = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
            if (spriteWorldSize <= 0f) spriteWorldSize = 1f;

            SphereCollider col = GetComponent<SphereCollider>();
            col.radius = spriteWorldSize * 0.55f;

            bobOffset = Random.value * 10f;
            transform.position = report.markerAnchor;
        }

        public void SetResponding()
        {
            if (spriteRenderer != null) spriteRenderer.color = new Color(1f, 1f, 1f, 0.55f);
        }

        private void LateUpdate()
        {
            if (Report == null) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            // Billboard: kameraya bak
            transform.rotation = cam.transform.rotation;

            // Ekranda sabit boy
            float distance = Vector3.Distance(cam.transform.position, Report.markerAnchor);
            float worldSize = Mathf.Max(minWorldSize, distance * sizePerDistance);

            float pulse = 1f;
            if (Report.status == CallIncidentStatus.Waiting && Report.timeRemaining < urgentPulseBelowSeconds)
            {
                pulse = 1f + 0.18f * Mathf.Abs(Mathf.Sin(Time.time * 6f));
            }

            transform.localScale = Vector3.one * (worldSize / spriteWorldSize) * pulse;

            // Sine salınımı (yukarı-aşağı)
            float speed = Report.status == CallIncidentStatus.Waiting ? bobSpeed : bobSpeed * 0.4f;
            float bob = Mathf.Sin((Time.time + bobOffset) * speed) * bobAmplitude * worldSize;
            transform.position = Report.markerAnchor + Vector3.up * (worldSize * 0.6f + bob);

            UpdateCountdown();
        }

        private void UpdateCountdown()
        {
            if (countdownText == null) return;

            if (Report.status == CallIncidentStatus.Responding)
            {
                countdownText.text = "<mark=#0B0F14CC> EN ROUTE </mark>";
                countdownText.color = CountdownEnRoute;
                return;
            }

            if (Report.status == CallIncidentStatus.Working)
            {
                // Olay yerinde iş: kalan süre ekip sayısıyla kısalır
                int left = Mathf.CeilToInt(Report.WorkSecondsLeft);
                countdownText.text = $"<mark=#0B0F14CC> WORK {left / 60}:{left % 60:00} </mark>";
                countdownText.color = CountdownEnRoute;
                if (spriteRenderer != null) spriteRenderer.color = Color.white;
                return;
            }

            int seconds = Mathf.CeilToInt(Mathf.Max(0f, Report.timeRemaining));
            countdownText.text = $"<mark=#0B0F14CC> {seconds / 60}:{seconds % 60:00} </mark>";
            countdownText.color = Report.timeRemaining < urgentPulseBelowSeconds ? CountdownUrgent : CountdownNormal;
        }
    }
}
