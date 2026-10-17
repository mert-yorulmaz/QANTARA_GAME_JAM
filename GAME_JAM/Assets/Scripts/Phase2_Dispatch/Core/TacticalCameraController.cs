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
        [Tooltip("Perspektif kamerada zoom ile inilebilecek en düşük / en yüksek kamera yüksekliği")]
        [SerializeField] private float minZoomHeight = 40f;
        [SerializeField] private float maxZoomHeight = 260f;

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
            float horizontal = 0f;
            float vertical = 0f;

            // 1. New Input System (Keyboard)
            try
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null)
                {
                    if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) horizontal += 1f;
                    if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) horizontal -= 1f;
                    if (kb.wKey.isPressed || kb.upArrowKey.isPressed) vertical += 1f;
                    if (kb.sKey.isPressed || kb.downArrowKey.isPressed) vertical -= 1f;
                }
            }
            catch { }

            // 2. Legacy Input Fallback
            if (horizontal == 0f && vertical == 0f)
            {
                try
                {
                    horizontal = Input.GetAxisRaw("Horizontal");
                    vertical = Input.GetAxisRaw("Vertical");
                }
                catch { }
            }

            if (enableEdgeScroll)
            {
                Vector2 mousePos = Vector2.zero;
                try
                {
                    var mouse = UnityEngine.InputSystem.Mouse.current;
                    if (mouse != null) mousePos = mouse.position.ReadValue();
                }
                catch
                {
                    try { mousePos = Input.mousePosition; } catch { }
                }

                if (mousePos.x >= Screen.width - edgeBorder) horizontal = 1f;
                if (mousePos.x <= edgeBorder) horizontal = -1f;
                if (mousePos.y >= Screen.height - edgeBorder) vertical = 1f;
                if (mousePos.y <= edgeBorder) vertical = -1f;
            }

            // Kameranın baktığı yöne göre zeminde kaydır (45° izometrikte eski formülle birebir aynı sonuç)
            Vector3 flatForward = transform.forward;
            flatForward.y = 0f;
            if (flatForward.sqrMagnitude < 0.0001f) flatForward = transform.up;
            flatForward.y = 0f;
            flatForward.Normalize();
            Vector3 flatRight = new Vector3(flatForward.z, 0f, -flatForward.x);

            Vector3 move = (flatRight * horizontal + flatForward * vertical).normalized * (panSpeed * Time.deltaTime);

            Vector3 newPos = transform.position + move;
            newPos.x = Mathf.Clamp(newPos.x, minPanLimits.x, maxPanLimits.x);
            newPos.z = Mathf.Clamp(newPos.z, minPanLimits.y, maxPanLimits.y);

            transform.position = newPos;
        }

        private void HandleZoom()
        {
            float scroll = 0f;
            try
            {
                var mouse = UnityEngine.InputSystem.Mouse.current;
                if (mouse != null) scroll = mouse.scroll.ReadValue().y * 0.001f;
            }
            catch { }

            if (Mathf.Abs(scroll) < 0.0001f)
            {
                try
                {
                    scroll = Input.GetAxis("Mouse ScrollWheel");
                }
                catch { }
            }

            if (Mathf.Abs(scroll) > 0.001f && cam != null)
            {
                if (cam.orthographic)
                {
                    cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - scroll * zoomSpeed * 10f, minOrthoSize, maxOrthoSize);
                }
                else
                {
                    Vector3 zoomed = transform.position + transform.forward * (scroll * zoomSpeed * 10f);
                    if (zoomed.y >= minZoomHeight && zoomed.y <= maxZoomHeight)
                    {
                        transform.position = zoomed;
                    }
                }
            }
        }
    }
}
