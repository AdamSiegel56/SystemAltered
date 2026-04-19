using UnityEngine;

/// <summary>
/// Steroids rage pull: when the player takes damage, involuntarily lunge
/// toward the damage source. Camera snaps hard toward the attacker and
/// the body is pulled by force.
/// </summary>
public class RagePullSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerCam playerCam;
    [SerializeField] private PlayerCharacter playerCharacter;
    [SerializeField] private DrugStateController drugState;

    [Header("Tuning")]
    [Tooltip("Initial impulse force multiplier — layered on top of the drug's ragePullForce")]
    [SerializeField] private float initialImpulseMultiplier = 2f;

    private bool _ragePullActive;
    private float _ragePullTimer;
    private Vector3 _ragePullDirection;
    private Vector3 _damageSource;

    private DrugStateData State => drugState != null ? drugState.CurrentState : null;
    private bool RagePullEnabled => State != null && State.enableRagePull;

    // --- Public API ---

    public void OnDamageTaken(Vector3 damageSourcePosition)
    {
        if (!RagePullEnabled) return;

        _damageSource = damageSourcePosition;
        _ragePullDirection = (damageSourcePosition - transform.position).normalized;
        _ragePullDirection.y = 0f;
        _ragePullTimer = State.ragePullDuration;
        _ragePullActive = true;

        ApplyCameraSnap();
        ApplyInitialImpulse();
    }

    // --- Lifecycle ---

    private void FixedUpdate()
    {
        if (!_ragePullActive || State == null) return;

        _ragePullTimer -= Time.fixedDeltaTime;
        if (_ragePullTimer <= 0f)
        {
            _ragePullActive = false;
            return;
        }

        ApplySustainedPull();
        ApplyCameraSnap();
    }

    // --- Effects ---

    private void ApplyInitialImpulse()
    {
        if (playerCharacter == null) return;

        playerCharacter.AddExternalImpulse(
            _ragePullDirection * State.ragePullForce * initialImpulseMultiplier
        );
    }

    private void ApplySustainedPull()
    {
        if (playerCharacter == null) return;

        // Acceleration-style: scaled by fixedDeltaTime so feels continuous
        playerCharacter.AddExternalImpulse(
            _ragePullDirection * State.ragePullForce * Time.fixedDeltaTime
        );
    }

    private void ApplyCameraSnap()
    {
        if (playerCam == null) return;
        playerCam.ApplyRagePull(_damageSource);
    }
}