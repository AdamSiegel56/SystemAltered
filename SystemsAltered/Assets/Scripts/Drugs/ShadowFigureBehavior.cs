using UnityEngine;

/// <summary>
/// Behavior for cocaine paranoia shadow figures.
/// Billboard-faces the player and self-destructs when the player
/// looks directly at it (within a threshold angle).
/// </summary>
public class ShadowFigureBehavior : MonoBehaviour
{
    [Header("Tuning")]
    public float lookDestroyAngle = 15f;  // Destroy when player looks within this angle
    public float fadeSpeed = 4f;

    private Transform playerCam;
    private float lifetime;
    private float spawnTime;
    private Renderer rend;

    public void Init(Transform cameraTransform, float maxLifetime)
    {
        playerCam = cameraTransform;
        lifetime = maxLifetime;
        spawnTime = Time.time;
        rend = GetComponent<Renderer>();
    }

    void Update()
    {
        if (playerCam == null) return;

        // Billboard: always face the player camera
        Vector3 lookDir = playerCam.position - transform.position;
        lookDir.y = 0;
        if (lookDir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(lookDir);

        // Check if player is looking directly at us
        Vector3 toShadow = (transform.position - playerCam.position).normalized;
        float angle = Vector3.Angle(playerCam.forward, toShadow);

        if (angle < lookDestroyAngle)
        {
            // Player caught us — vanish instantly
            Destroy(gameObject);
            return;
        }

        // Lifetime expiry
        if (Time.time - spawnTime > lifetime)
        {
            Destroy(gameObject);
        }
    }
}