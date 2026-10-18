using UnityEngine;

namespace ZeroHour.Phase2.Calls
{
    /// <summary>
    /// İstasyon binasının çatısındaki kurum logosu (polis / ambulans / itfaiye / helikopter).
    /// Logo kameraya bakar, ekranda sabit boyda kalır ve diskin üstünde hafifçe salınır.
    /// Tıklanmaz ve collider'ı yoktur (olay rozeti tıklamalarını ve NavMesh'i etkilemez).
    /// </summary>
    public class StationBeacon : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer logo;
        [SerializeField] private Transform disk;

        [Header("Billboard Boyutu")]
        [SerializeField] private float sizePerDistance = 0.032f;
        [SerializeField] private float minWorldSize = 3f;
        [SerializeField] private float maxWorldSize = 14f;

        [Header("Salınım")]
        [SerializeField] private float bobAmplitude = 0.08f;
        [SerializeField] private float bobSpeed = 1.6f;

        private float logoSpriteSize = 1f;

        private void Awake()
        {
            if (logo != null && logo.sprite != null)
            {
                logoSpriteSize = Mathf.Max(logo.sprite.bounds.size.x, logo.sprite.bounds.size.y);
                if (logoSpriteSize <= 0f) logoSpriteSize = 1f;
            }
        }

        private void LateUpdate()
        {
            if (logo == null) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 basePos = transform.position;
            float distance = Vector3.Distance(cam.transform.position, basePos);
            float worldSize = Mathf.Clamp(distance * sizePerDistance, minWorldSize, maxWorldSize);

            Transform logoTransform = logo.transform;
            logoTransform.rotation = cam.transform.rotation;
            logoTransform.localScale = Vector3.one * (worldSize / logoSpriteSize);

            float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude * worldSize;
            logoTransform.position = basePos + Vector3.up * (worldSize * 0.75f + bob);
        }
    }
}
