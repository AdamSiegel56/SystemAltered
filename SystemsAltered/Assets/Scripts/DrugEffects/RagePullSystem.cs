using UnityEngine;

/// <summary>
/// Steroids rage pull: when the player takes damage, involuntarily lunge
/// toward the damage source. Camera snaps hard toward the attacker and
/// the body is pulled by force. Feels aggressive and out of control.
/// </summary>
public class RagePullSystem : MonoBehaviour
{
    [Header("References")]
    public PlayerCamera playerCamera;
    public Rigidbody playerRigidbody;

    private DrugStateData currentState;
    private bool ragePullActive;
    private float ragePullTimer;
    private Vector3 ragePullDirection;
    private Vector3 damageSource;

    void OnEnable()
    {
        DrugEventBus.OnDrugStateChanged += ApplyState;
    }

    void OnDisable()
    {
        DrugEventBus.OnDrugStateChanged -= ApplyState;
    }

    void ApplyState(DrugStateData state)
    {
        currentState = state;
        ragePullActive = false;
        ragePullTimer = 0f;
    }

    public void OnDamageTaken(Vector3 damageSourcePosition)
    {
        if (currentState == null || !currentState.enableRagePull) return;

        damageSource = damageSourcePosition;
        ragePullDirection = (damageSourcePosition - transform.position).normalized;
        ragePullDirection.y = 0;
        ragePullTimer = currentState.ragePullDuration;
        ragePullActive = true;

        // Hard camera snap toward damage source — not a gentle lerp
        if (playerCamera != null)
        {
            playerCamera.ApplyRagePull(damageSourcePosition);
        }

        // Immediate impulse so the player feels it instantly
        if (playerRigidbody != null)
        {
            playerRigidbody.AddForce(
                ragePullDirection * currentState.ragePullForce * 2f,
                ForceMode.Impulse
            );
        }
    }

    void FixedUpdate()
    {
        if (!ragePullActive || playerRigidbody == null || currentState == null) return;

        ragePullTimer -= Time.fixedDeltaTime;

        if (ragePullTimer <= 0f)
        {
            ragePullActive = false;
            return;
        }

        // Sustained pull force toward damage source
        playerRigidbody.AddForce(
            ragePullDirection * currentState.ragePullForce,
            ForceMode.Acceleration
        );

        // Keep snapping camera toward source during the pull
        if (playerCamera != null)
        {
            playerCamera.ApplyRagePull(damageSource);
        }
    }
}
