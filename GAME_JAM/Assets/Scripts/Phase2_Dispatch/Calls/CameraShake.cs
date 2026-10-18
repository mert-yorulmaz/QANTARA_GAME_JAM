using UnityEngine;

namespace ZeroHour.Phase2.Calls
{
    /// <summary>
    /// Deprem / patlama kamera sarsıntısı. Sarsıntı ofsetini LateUpdate'te ekler ve bir sonraki karenin
    /// başında (diğer scriptlerden önce) geri alır; böylece TacticalCameraController'ın pan / zoom / sınırları bozulmaz.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }

        [SerializeField] private float maxStrength = 2.5f;

        private Vector3 appliedOffset;
        private float strength;
        private float duration;
        private float timeLeft;

        public static void Shake(float shakeStrength, float shakeDuration)
        {
            if (Instance == null) return;
            Instance.Begin(shakeStrength, shakeDuration);
        }

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnDisable()
        {
            transform.position -= appliedOffset;
            appliedOffset = Vector3.zero;
        }

        private void Begin(float shakeStrength, float shakeDuration)
        {
            // Güçlü olan baskın gelir; üst üste gelen sarsıntılar süreyi uzatır
            float currentStrength = timeLeft > 0f ? strength * (timeLeft / Mathf.Max(0.01f, duration)) : 0f;
            strength = Mathf.Min(maxStrength, Mathf.Max(currentStrength, shakeStrength));
            duration = Mathf.Max(shakeDuration, timeLeft);
            timeLeft = duration;
        }

        private void Update()
        {
            // Diğer scriptler kamerayı sarsıntısız konumdan okusun
            transform.position -= appliedOffset;
            appliedOffset = Vector3.zero;
        }

        private void LateUpdate()
        {
            if (timeLeft <= 0f) return;

            timeLeft -= Time.deltaTime;
            float fade = Mathf.Clamp01(timeLeft / Mathf.Max(0.01f, duration));
            Vector3 offset = Random.insideUnitSphere * (strength * fade);
            offset.y *= 0.5f;

            appliedOffset = offset;
            transform.position += appliedOffset;
        }
    }
}
