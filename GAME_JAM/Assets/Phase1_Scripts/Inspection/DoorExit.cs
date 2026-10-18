using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Mesai bitince kapıya bakıp E'ye basınca kapı açılır, ekran kararır ve gece vardiyası (Faz 2) sahnesi yüklenir.
public class DoorExit : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public Collider doorCollider;
    public ShiftClock clock;
    public InteractionSystem interactionSystem;
    public Image fadePanel;
    public SojaExiles.opencloseDoor door;
    [Tooltip("Çıkışta kapatılır (yürüme, kamera).")]
    public Behaviour[] disableOnExit;

    [Header("Settings")]
    public float interactionDistance = 3f;
    public float fadeDuration = 1.5f;
    public float fadeDelay = 0.4f;
    [Tooltip("Ekran karardıktan sonra yüklenecek sahne (Faz 2: gece vardiyası). Boşsa sahne değişmez.")]
    public string nextSceneName = "SampleScene";
    [Tooltip("Kararmış ekranda bekleme (sn), sonra sahne yüklenir.")]
    public float holdBeforeLoad = 1f;

    bool showingPrompt;
    bool leaving;

    public bool HasLeft => leaving;

    void Update()
    {
        // Ayarlar menüsü açıkken kapı girdisi beklesin
        if (ZeroHour.Shared.GamePause.IsMenuOpen)
            return;

        if (leaving || Keyboard.current == null || playerCamera == null || doorCollider == null)
            return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        bool lookingAtDoor = Physics.Raycast(ray, out RaycastHit hit, interactionDistance) && hit.collider == doorCollider;

        if (!lookingAtDoor)
        {
            HidePrompt();
            return;
        }

        if (clock != null && clock.IsOver)
        {
            ShowPrompt("[E] Head to the night shift");
            if (Keyboard.current.eKey.wasPressedThisFrame)
                StartCoroutine(Leave());
        }
        else
        {
            ShowPrompt($"Your shift ends at {(clock != null ? clock.endHour : 17):00}:00");
        }
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

    // Sadece bu scriptin açtığı yazıyı kapatır; sandalye yazısına dokunmaz.
    void HidePrompt()
    {
        if (!showingPrompt)
            return;

        showingPrompt = false;
        if (interactionSystem != null && interactionSystem.interactionPanel != null)
            interactionSystem.interactionPanel.SetActive(false);
    }

    IEnumerator Leave()
    {
        leaving = true;
        HidePrompt();

        foreach (var behaviour in disableOnExit)
            if (behaviour != null)
                behaviour.enabled = false;

        if (door != null && !door.open && door.openandclose != null)
        {
            door.openandclose.Play("Opening");
            door.open = true;
        }
        Sfx.Play(SfxId.DoorCreak);

        yield return new WaitForSeconds(fadeDelay);

        // Gece vardiyasına geçiş.
        Sfx.Play(SfxId.NightDrone);
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

        if (string.IsNullOrEmpty(nextSceneName))
            yield break;

        yield return new WaitForSeconds(holdBeforeLoad);
        SceneManager.LoadScene(nextSceneName);
    }
}
