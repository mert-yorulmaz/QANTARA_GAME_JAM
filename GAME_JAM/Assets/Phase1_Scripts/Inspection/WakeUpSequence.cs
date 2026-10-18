using System.Collections;
using UnityEngine;

// Oyun başında oyuncu kanepede uzanır, ekran açılırken kalkıp ayağa dikilir; sonra kontroller açılır.
public class WakeUpSequence : MonoBehaviour
{
    public Transform playerCamera;
    [Tooltip("Uyanma noktası (kanepe).")]
    public Transform sofa;
    [Tooltip("Uyanırken kapatılır (yürüme, kamera).")]
    public Behaviour[] disableWhileWaking;
    [Tooltip("Uyanırken gizlenir: kamera kanepeden kalkarken oyuncunun kapsülünün içinden geçmesin.")]
    public Renderer[] hideWhileWaking;

    [Header("Pose")]
    [Tooltip("Kanepenin ortasına göre baş konumu (dünya birimi).")]
    public Vector3 headOffset = new Vector3(1.0f, 0.15f, 0f);
    public Vector3 lyingEuler = new Vector3(-72f, 0f, -8f);

    [Header("Timing")]
    public float lieDuration = 2.2f;
    public float riseDuration = 2.4f;

    public bool IsDone { get; private set; }

    IEnumerator Start()
    {
        if (playerCamera == null || sofa == null)
        {
            IsDone = true;
            yield break;
        }

        foreach (var behaviour in disableWhileWaking)
            if (behaviour != null)
                behaviour.enabled = false;
        SetBodyVisible(false);

        Vector3 standPosition = playerCamera.position;
        Quaternion standRotation = playerCamera.rotation;
        Vector3 localPosition = playerCamera.localPosition;
        Quaternion localRotation = playerCamera.localRotation;

        Vector3 sofaCenter = SofaCenter();
        Vector3 lyingPosition = sofaCenter + headOffset;
        Quaternion lyingRotation = Quaternion.Euler(lyingEuler) * Quaternion.Euler(0f, standRotation.eulerAngles.y, 0f);

        playerCamera.SetPositionAndRotation(lyingPosition, lyingRotation);
        yield return new WaitForSeconds(lieDuration * 0.4f);
        Sfx.Play(SfxId.WakeBreath);
        yield return new WaitForSeconds(lieDuration * 0.6f);

        // Önce doğrulup otur, sonra kalk.
        Sfx.Play(SfxId.StandUp);
        for (float t = 0f; t < riseDuration; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / riseDuration);
            float lift = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t / riseDuration - 0.35f) / 0.65f));
            Vector3 position = Vector3.Lerp(lyingPosition, standPosition, k);
            position.y = Mathf.Lerp(lyingPosition.y, standPosition.y, lift);
            playerCamera.SetPositionAndRotation(position, Quaternion.Slerp(lyingRotation, standRotation, k));
            yield return null;
        }

        playerCamera.localPosition = localPosition;
        playerCamera.localRotation = localRotation;

        foreach (var behaviour in disableWhileWaking)
            if (behaviour != null)
                behaviour.enabled = true;
        SetBodyVisible(true);

        IsDone = true;
    }

    void SetBodyVisible(bool visible)
    {
        foreach (var r in hideWhileWaking)
            if (r != null)
                r.enabled = visible;
    }

    Vector3 SofaCenter()
    {
        var renderer = sofa.GetComponentInChildren<Renderer>();
        if (renderer == null)
            return sofa.position;

        var bounds = renderer.bounds;
        return new Vector3(bounds.center.x, bounds.min.y + bounds.size.y * 0.6f, bounds.center.z);
    }
}
