
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class ComputerInteraction : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public Collider chairCollider;
    public GameObject computerCanvas;
    public InteractionSystem interactionSystem;
    public FPSCamera fpsCamera;
    public Transform computerViewPoint;
    [Tooltip("Bakinca da oturulabilen masa parcalari (masa, monitor, kasa, klavye).")]
    public Collider[] deskColliders;

    [Header("Settings")]
    public float interactionDistance = 5f;
    public float cameraMoveDuration = 0.8f;

    private bool usingComputer = false;
    private bool movingCamera = false;

    private Vector3 oldPosition;
    private Quaternion oldRotation;

    private Coroutine cameraMoveCoroutine;

    void Start()
    {
        if (computerCanvas != null)
            computerCanvas.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Ayarlar menusu acikken (ESC) sandalye / bilgisayar girdisi beklesin
        if (ZeroHour.Shared.GamePause.IsMenuOpen)
            return;

        if (Keyboard.current == null ||
            playerCamera == null ||
            chairCollider == null)
            return;

        if (movingCamera)
            return;

        if (usingComputer)
        {
            // Masadan sadece E ile kalkilir; ESC ayarlari acar ve oyunu durdurur
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                ExitComputer();
            }

            return;
        }

        Ray ray = playerCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        if (!Keyboard.current.eKey.wasPressedThisFrame)
            return;

        // Oturma: sandalye alanindayken ya da sandalyeye / masaya / bilgisayara bakarken E
        bool inZone = interactionSystem != null && interactionSystem.InChairZone;
        bool lookingAtDesk = false;
        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            lookingAtDesk = hit.collider == chairCollider;
            if (deskColliders != null)
                foreach (var c in deskColliders)
                    if (c != null && hit.collider == c)
                        lookingAtDesk = true;
        }

        if (inZone || lookingAtDesk)
            EnterComputer();
    }

    void EnterComputer()
    {
        if (computerViewPoint == null)
        {
            Debug.LogWarning(
                "Computer View Point atanmamýþ!"
            );
            return;
        }

        usingComputer = true;

        oldPosition = playerCamera.transform.position;
        oldRotation = playerCamera.transform.rotation;

        if (fpsCamera != null)
            fpsCamera.enabled = false;

        // Bilgisayara girince etkileþim yazýsýný kapat.
        if (interactionSystem != null)
            interactionSystem.HidePrompt();

        if (computerCanvas != null)
            computerCanvas.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        MoveCamera(
            computerViewPoint.position,
            Quaternion.Euler(0f, -90f, 0f)
        );
    }

    void ExitComputer()
    {
        usingComputer = false;

        if (computerCanvas != null)
            computerCanvas.SetActive(false);

        // Çýkýþta da yazý kapalý kalmalý.
        if (interactionSystem != null)
            interactionSystem.HidePrompt();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        MoveCamera(
            oldPosition,
            oldRotation,
            true
        );
    }

    void MoveCamera(
        Vector3 targetPosition,
        Quaternion targetRotation,
        bool restoreCameraControl = false)
    {
        if (cameraMoveCoroutine != null)
            StopCoroutine(cameraMoveCoroutine);

        cameraMoveCoroutine = StartCoroutine(
            MoveCameraRoutine(
                targetPosition,
                targetRotation,
                restoreCameraControl
            )
        );
    }

    IEnumerator MoveCameraRoutine(
        Vector3 targetPosition,
        Quaternion targetRotation,
        bool restoreCameraControl)
    {
        movingCamera = true;

        Transform cam = playerCamera.transform;

        Vector3 startPosition = cam.position;
        Quaternion startRotation = cam.rotation;

        float elapsed = 0f;
        float duration = Mathf.Max(
            0.01f,
            cameraMoveDuration
        );

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t);

            cam.SetPositionAndRotation(
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    t
                ),
                Quaternion.Slerp(
                    startRotation,
                    targetRotation,
                    t
                )
            );

            yield return null;
        }

        cam.SetPositionAndRotation(
            targetPosition,
            targetRotation
        );

        movingCamera = false;
        cameraMoveCoroutine = null;

        if (restoreCameraControl && fpsCamera != null)
            fpsCamera.enabled = true;
    }
}
