using UnityEngine;

namespace ZeroHour.Phase2.Core
{
    public class TacticalCameraController : MonoBehaviour
    {
        [Header("Pan & Movement")]
        [SerializeField] private float panSpeed = 15f;
        [SerializeField] private Vector2 minPanLimits = new Vector2(-30f, -30f);
        [SerializeField] private Vector2 maxPanLimits = new Vector2(30f, 30f);

        [Header("Zoom Settings")]
        [SerializeField] private float zoomSpeed = 8f;
        [SerializeField] private float minOrthoSize = 6f;
        [SerializeField] private float maxOrthoSize = 22f;

        [Header("Edge Scrolling (Optional)")]
        [SerializeField] private bool enableEdgeScroll = false;
        [SerializeField] private float edgeBorder = 15f;

        private Camera cam;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam == null) cam = Camera.main;
        }

        private void Update()
        {
            HandlePan();
            HandleZoom();
        }

        private void HandlePan()
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            if (enableEdgeScroll)
            {
                if (Input.mousePosition.x >= Screen.width - edgeBorder) horizontal = 1f;
                if (Input.mousePosition.x <= edgeBorder) horizontal = -1f;
                if (Input.mousePosition.y >= Screen.height - edgeBorder) vertical = 1f;
                if (Input.mousePosition.y <= edgeBorder) vertical = -1f;
            }

            // In isometric 45-degree projection, move along world X and Z
            Vector3 move = new Vector3(horizontal + vertical, 0, vertical - horizontal).normalized * (panSpeed * Time.deltaTime);

            Vector3 newPos = transform.position + move;
            newPos.x = Mathf.Clamp(newPos.x, minPanLimits.x, maxPanLimits.x);
            newPos.z = Mathf.Clamp(newPos.z, minPanLimits.y, maxPanLimits.y);

            transform.position = newPos;
        }

        private void HandleZoom()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f && cam != null)
            {
                if (cam.orthographic)
                {
                    cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - scroll * zoomSpeed, minOrthoSize, maxOrthoSize);
                }
                else
                {
                    transform.position += transform.forward * (scroll * zoomSpeed);
                }
            }
        }
    }
}
