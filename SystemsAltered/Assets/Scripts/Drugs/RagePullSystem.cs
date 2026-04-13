using UnityEngine;

/// <summary>
/// Steroids rage pull: when the player takes damage, involuntarily lunge
/// toward the damage source for a brief, non-cancellable duration.
/// Attach to the player.
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

        // Reset any active rage pull on state change
        ragePullActive = false;
        ragePullTimer = 0f;
    }

    /// <summary>
    /// Call this when the player takes damage from a known source position.
    /// If steroids rage pull is enabled, triggers the involuntary lunge.
    /// </summary>
    public void OnDamageTaken(Vector3 damageSourcePosition)
    {
        if (currentState == null || !currentState.enableRagePull) return;

        // Start rage pull
        ragePullDirection = (damageSourcePosition - transform.position).normalized;
        ragePullDirection.y = 0; // Keep horizontal
        ragePullTimer = currentState.ragePullDuration;
        ragePullActive = true;

        // Snap camera toward damage source
        if (playerCamera != null)
        {
            playerCamera.ApplyRagePull(damageSourcePosition);
        }
    }

    void FixedUpdate()
    {
        if (!ragePullActive || playerRigidbody == null) return;

        ragePullTimer -= Time.fixedDeltaTime;

        if (ragePullTimer <= 0f)
        {
            ragePullActive = false;
            return;
        }

        // Apply involuntary force toward damage source
        playerRigidbody.AddForce(
            ragePullDirection * currentState.ragePullForce,
            ForceMode.Acceleration
        );
    }
}