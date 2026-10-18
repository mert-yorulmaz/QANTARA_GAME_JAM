using UnityEngine;
using UnityEngine.UI;

namespace ZeroHour.Phase2.Calls
{
    /// <summary>
    /// Olay binasının üstünde havada yüzen, kameraya bakan (billboard) ve tıklanabilen olay rozeti.
    /// Çevresindeki çember:
    ///  • Süre yayı (beyaz → kırmızı) saat yönünde azalır; bitince kalan yaralılar kaybedilir.
    ///  • Ekip varınca aynı noktadan sarı iş yayı dolmaya başlar; süre bitmeden tam tur atarsa olay çözülür.
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

        [Header("Çember")]
        [SerializeField] private Image timeRing;
        [SerializeField] private Image workRing;
        [Tooltip("Kalan süre bu oranın altına inince rozet nabız gibi atar ve yay kırmızılaşır")]
        [SerializeField] private float urgentFraction = 0.3f;

        private static readonly Color TimeColor = new Color(0.95f, 0.95f, 0.95f, 0.95f);
        private static readonly Color UrgentColor = new Color(1f, 0.25f, 0.2f, 1f);
        private static readonly Color WorkColor = new Color(1f, 0.82f, 0.15f, 1f);

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
            col.radius = spriteWorldSize * 0.6f;

            bobOffset = Random.value * 10f;
            transform.position = report.markerAnchor;
            UpdateRings();
        }

        public void SetResponding()
        {
            if (spriteRenderer != null) spriteRenderer.color = new Color(1f, 1f, 1f, 0.8f);
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

            float fraction = Report.timeLimit > 0f ? Report.timeRemaining / Report.timeLimit : 0f;
            float pulse = 1f;
            if (fraction < urgentFraction) pulse = 1f + 0.15f * Mathf.Abs(Mathf.Sin(Time.time * 6f));

            transform.localScale = Vector3.one * (worldSize / spriteWorldSize) * pulse;

            // Sine salınımı (yukarı-aşağı)
            float speed = Report.status == CallIncidentStatus.Waiting ? bobSpeed : bobSpeed * 0.4f;
            float bob = Mathf.Sin((Time.time + bobOffset) * speed) * bobAmplitude * worldSize;
            transform.position = Report.markerAnchor + Vector3.up * (worldSize * 0.6f + bob);

            if (Report.status == CallIncidentStatus.Working && spriteRenderer != null) spriteRenderer.color = Color.white;
            UpdateRings();
        }

        private void UpdateRings()
        {
            if (Report == null) return;

            float fraction = Report.timeLimit > 0f ? Mathf.Clamp01(Report.timeRemaining / Report.timeLimit) : 0f;
            if (timeRing != null)
            {
                timeRing.fillAmount = fraction;
                timeRing.color = fraction < urgentFraction
                    ? Color.Lerp(UrgentColor, TimeColor, fraction / urgentFraction * 0.3f)
                    : TimeColor;
            }

            if (workRing != null)
            {
                float work = Report.workRequired > 0f ? Mathf.Clamp01(Report.workDone / Report.workRequired) : 0f;
                workRing.fillAmount = work;
                workRing.color = WorkColor;
                workRing.enabled = work > 0.001f;
            }
        }
    }
}
