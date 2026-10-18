
using UnityEngine;
using TMPro;

public class InteractionSystem : MonoBehaviour
{
    public GameObject interactionPanel;
    public TMP_Text interactionText;

    private bool insideChairTrigger = false;
    private bool promptSuppressed = false;

    void Start()
    {
        if (interactionPanel != null)
            interactionPanel.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("ChairTrigger"))
            return;

        insideChairTrigger = true;

        // Önceki etkileþimden sonra tekrar girildi.
        promptSuppressed = false;

        ShowPrompt();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("ChairTrigger"))
            return;

        insideChairTrigger = false;
        promptSuppressed = false;

        HidePrompt();
    }

    public void HidePrompt()
    {
        promptSuppressed = true;

        if (interactionPanel != null)
            interactionPanel.SetActive(false);
    }

    private void ShowPrompt()
    {
        if (!insideChairTrigger || promptSuppressed)
            return;

        if (interactionText != null)
            interactionText.text = "[E] Sit Down";

        if (interactionPanel != null)
            interactionPanel.SetActive(true);
    }
}
