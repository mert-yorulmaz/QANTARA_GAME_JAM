using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Duvardaki kriz monitörü: mesai boyunca kapalıdır, zil çalınca açılır ve gece vardiyasının şehir görüntüsünü gösterir.
// Oyuncu monitöre bakıp E'ye basınca kamera ekrana doğru kayıp onu doldurur, ardından gece vardiyası (Faz 2) sahnesi yüklenir.
public class CrisisMonitorExit : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    [Tooltip("Bakılınca etkileşime giren collider'lar (monitör gövdesi ve ekran).")]
    public Collider[] monitorColliders;
    [Tooltip("Ekran yüzeyi (Quad).")]
    public Renderer screen;
    public Material screenOff;
    public Material screenOn;
    public ShiftClock clock;
    public InteractionSystem interactionSystem;
    public Image fadePanel;
    [Tooltip("Geçişte kapatılır (yürüme, kamera).")]
    public Behaviour[] disableOnEnter;
    [Tooltip("Geçişte gizlenir (görev kutusu gibi ekran arayüzü).")]
    public GameObject[] hideOnEnter;

    [Header("Settings")]
    public float interactionDistance = 5f;
    [Tooltip("Ekranı doldurduğunda kameranın ekrandan uzaklığı çarpanı (1 = tam sığar, küçükse ekran taşar).")]
    public float fillFactor = 0.92f;
    public float zoomDuration = 1.6f;
    public float powerOnDuration = 0.7f;
    public float fadeDuration = 0.35f;
    [Tooltip("Gece vardiyası sahnesi (Faz 2).")]
    public string nextSceneName = "SampleScene";

    bool showingPrompt;
    bool poweredOn;
    bool entering;

    public bool HasLeft => entering;
    public bool IsOn => poweredOn;

    void Start()
    {
        if (screen != null && screenOff != null)
            screen.sharedMaterial = screenOff;

        if (clock != null)
        {
            clock.ShiftEnded += PowerOn;
            if (clock.IsOver)
                PowerOn();
        }
    }

    void OnDestroy()
    {
        if (clock != null)
            clock.ShiftEnded -= PowerOn;
    }

    void PowerOn()
    {
        if (poweredOn)
            return;
        poweredOn = true;
        StartCoroutine(PowerOnRoutine());
    }

    // Eski bir ekran gibi birkaç kez titreyip açılır.
    IEnumerator PowerOnRoutine()
    {
        Sfx.Play(SfxId.Boot);
        float[] flicker = { 0.08f, 0.06f, 0.1f, 0.05f, 0.14f };
        bool on = false;
        float total = 0f;
        foreach (float wait in flicker)
        {
            on = !on;
            if (screen != null)
                screen.sharedMaterial = on ? screenOn : screenOff;
            yield return new WaitForSeconds(wait);
            total += wait;
        }
        if (screen != null)
            screen.sharedMaterial = screenOn;
        if (total < powerOnDuration)
            yield return new WaitForSeconds(powerOnDuration - total);
    }

    void Update()
    {
        // Ayarlar menüsü açıkken monitör girdisi beklesin
        if (ZeroHour.Shared.GamePause.IsMenuOpen)
            return;

        if (entering || Keyboard.current == null || playerCamera == null)
            return;

        if (!IsLookingAtMonitor())
        {
            HidePrompt();
            return;
        }

        if (poweredOn)
        {
            ShowPrompt("[E] Start the night shift");
            if (Keyboard.current.eKey.wasPressedThisFrame)
                StartCoroutine(Enter());
        }
        else
        {
            ShowPrompt($"Crisis desk opens at {(clock != null ? clock.endHour : 17):00}:00");
        }
    }

    bool IsLookingAtMonitor()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
            return false;
        foreach (Collider c in monitorColliders)
            if (c != null && hit.collider == c)
                return true;
        return false;
    }

    void ShowPrompt(string message)
    {
        if (interactionSystem == null || interactionSystem.interactionPanel == null)
            return;

        if (interactionSystem.interactionText != null)
            interactionSystem.interactionText.text = message;
        interactionSystem.interactionPanel.SetActive(true);
        showingPrompt = true;
    }

    // Sadece bu scriptin açtığı yazıyı kapatır; diğer etkileşim yazılarına dokunmaz.
    void HidePrompt()
    {
        if (!showingPrompt)
            return;

        showingPrompt = false;
        if (interactionSystem != null && interactionSystem.interactionPanel != null)
            interactionSystem.interactionPanel.SetActive(false);
    }

    IEnumerator Enter()
    {
        entering = true;
        HidePrompt();

        foreach (var behaviour in disableOnEnter)
            if (behaviour != null)
                behaviour.enabled = false;
        foreach (var go in hideOnEnter)
            if (go != null)
                go.SetActive(false);

        Sfx.Play(SfxId.NightDrone);

        // Kamera ekranın tam önüne, ekranı dolduracak uzaklığa kayar.
        Transform cam = playerCamera.transform;
        Vector3 startPos = cam.position;
        Quaternion startRot = cam.rotation;
        GetFillPose(out Vector3 endPos, out Quaternion endRot);

        for (float t = 0f; t < zoomDuration; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / zoomDuration);
            cam.SetPositionAndRotation(Vector3.Lerp(startPos, endPos, k), Quaternion.Slerp(startRot, endRot, k));
            yield return null;
        }
        cam.SetPositionAndRotation(endPos, endRot);

        // Kısa kararma: ekrandaki görüntüden gerçek gece vardiyasına geçiş.
        if (fadePanel != null)
        {
            fadePanel.gameObject.SetActive(true);
            Color color = fadePanel.color;
            for (float t = 0f; t < fadeDuration; t += Time.deltaTime)
            {
                color.a = Mathf.Clamp01(t / fadeDuration);
                fadePanel.color = color;
                yield return null;
            }
            color.a = 1f;
            fadePanel.color = color;
        }

        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
    }

    // Ekranın odaya bakan yüzünün önünde, ekranı görüş alanına sığdıran kamera konumu.
    void GetFillPose(out Vector3 position, out Quaternion rotation)
    {
        Transform s = screen != null ? screen.transform : transform;
        Bounds b = screen != null ? screen.bounds : new Bounds(s.position, Vector3.one);

        // Quad'ın görünen yüzü -forward yönüne bakar; oyuncu hangi taraftaysa o yüzü kullan.
        Vector3 normal = -s.forward;
        if (Vector3.Dot(normal, playerCamera.transform.position - b.center) < 0f)
            normal = -normal;

        float width = s.lossyScale.x;
        float height = s.lossyScale.y;
        float vHalf = playerCamera.fieldOfView * 0.5f * Mathf.Deg2Rad;
        float hHalf = Mathf.Atan(Mathf.Tan(vHalf) * playerCamera.aspect);
        float distance = Mathf.Min(height * 0.5f / Mathf.Tan(vHalf), width * 0.5f / Mathf.Tan(hHalf)) * fillFactor;

        position = b.center + normal * Mathf.Max(0.05f, distance);
        rotation = Quaternion.LookRotation(-normal, s.up);
    }
}
