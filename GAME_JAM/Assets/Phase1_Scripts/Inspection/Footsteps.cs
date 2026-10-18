using UnityEngine;

// Oyuncu yürüdükçe adım sesi çalar (yürüme kapalıyken susar).
public class Footsteps : MonoBehaviour
{
    public Behaviour movement;
    [Tooltip("İki adım arası yatay mesafe (metre).")]
    public float stepDistance = 1.7f;

    Vector3 lastPosition;
    float travelled;

    void Start()
    {
        lastPosition = transform.position;
    }

    void Update()
    {
        Vector3 delta = transform.position - lastPosition;
        lastPosition = transform.position;
        delta.y = 0f;

        if (movement != null && !movement.enabled)
        {
            travelled = 0f;
            return;
        }

        travelled += delta.magnitude;
        if (travelled < stepDistance)
            return;

        travelled = 0f;
        Sfx.Footstep();
    }
}
