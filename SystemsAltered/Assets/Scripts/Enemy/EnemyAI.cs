using UnityEngine;
using UnityEngine.AI;

public enum EnemyState
{
    Patrol,
    Chase,
    Attack
}

/// <summary>
/// Core enemy AI. Patrols between waypoints, detects the player via
/// range + line of sight, chases using NavMeshAgent, and attacks
/// within melee/shooting range. Integrates with the drug state system
/// to disguise as mundane objects when the player is sober.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyHealth))]
public class EnemyAI : MonoBehaviour
{
    [Header("Detection")]
    public float detectRange = 15f;
    public float attackRange = 3f;
    public float fieldOfView = 120f;
    public LayerMask obstacleMask;

    [Header("Combat")]
    public float attackDamage = 10f;
    public float attackCooldown = 1.5f;

    [Header("Patrol")]
    public Transform[] patrolPoints;
    public float patrolWaitTime = 2f;

    [Header("Disguise (Sober State)")]
    public GameObject realModel;
    public GameObject disguiseModel;

    [Header("Aggression Scaling")]
    public float baseSpeed = 3f;
    public float chaseSpeedMultiplier = 1.5f;

    private NavMeshAgent agent;
    private EnemyState currentState = EnemyState.Patrol;
    private Transform player;
    private RagePullSystem playerRagePull;

    private int currentPatrolIndex;
    private float patrolWaitTimer;
    private float attackTimer;
    private bool isDisguised;
    private bool playerOnDrugs;

    // Drug state can modify aggression
    private float aggressionMultiplier = 1f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = baseSpeed;

        // Find player
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerRagePull = playerObj.GetComponent<RagePullSystem>();
        }

        // Start disguised if we have a disguise model
        if (disguiseModel != null)
        {
            SetDisguised(true);
        }

        if (patrolPoints.Length > 0)
        {
            agent.SetDestination(patrolPoints[0].position);
        }
    }

    void OnEnable()
    {
        DrugEventBus.OnDrugStateChanged += OnDrugStateChanged;
    }

    void OnDisable()
    {
        DrugEventBus.OnDrugStateChanged -= OnDrugStateChanged;
    }

    void OnDrugStateChanged(DrugStateData state)
    {
        playerOnDrugs = state.stateType != DrugState.Sober && state.stateType != DrugState.Crash;

        // Reveal true form when player is on drugs, disguise when sober
        if (disguiseModel != null)
        {
            SetDisguised(!playerOnDrugs);
        }

        // Scale aggression based on drug — enemies are more aggressive on stimulants
        switch (state.stateType)
        {
            case DrugState.Cocaine:
                aggressionMultiplier = 1.3f;
                break;
            case DrugState.Meth:
                aggressionMultiplier = 1.5f;
                break;
            case DrugState.Steroids:
                aggressionMultiplier = 1.2f;
                break;
            case DrugState.THC:
                aggressionMultiplier = 0.8f;
                break;
            case DrugState.Crash:
                aggressionMultiplier = 1.6f;  // Enemies are ruthless during crash
                break;
            default:
                aggressionMultiplier = 1f;
                break;
        }
    }

    void Update()
    {
        if (player == null) return;

        attackTimer -= Time.deltaTime;

        switch (currentState)
        {
            case EnemyState.Patrol:
                UpdatePatrol();
                break;
            case EnemyState.Chase:
                UpdateChase();
                break;
            case EnemyState.Attack:
                UpdateAttack();
                break;
        }
    }

    // --- PATROL ---

    void UpdatePatrol()
    {
        agent.speed = baseSpeed;

        // Check for player detection
        if (CanSeePlayer())
        {
            TransitionTo(EnemyState.Chase);
            return;
        }

        if (patrolPoints.Length == 0) return;

        // Wait at patrol point
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            patrolWaitTimer -= Time.deltaTime;

            if (patrolWaitTimer <= 0f)
            {
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                agent.SetDestination(patrolPoints[currentPatrolIndex].position);
                patrolWaitTimer = patrolWaitTime;
            }
        }
    }

    // --- CHASE ---

    void UpdateChase()
    {
        agent.speed = baseSpeed * chaseSpeedMultiplier * aggressionMultiplier;
        agent.SetDestination(player.position);

        float dist = Vector3.Distance(transform.position, player.position);

        // Close enough to attack
        if (dist <= attackRange)
        {
            TransitionTo(EnemyState.Attack);
            return;
        }

        // Lost the player
        if (dist > detectRange * 1.5f || !CanSeePlayer())
        {
            TransitionTo(EnemyState.Patrol);
        }
    }

    // --- ATTACK ---

    void UpdateAttack()
    {
        agent.SetDestination(transform.position);  // Stop moving

        // Face the player
        Vector3 lookDir = (player.position - transform.position).normalized;
        lookDir.y = 0;
        if (lookDir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(lookDir),
                Time.deltaTime * 8f
            );

        float dist = Vector3.Distance(transform.position, player.position);

        // Player moved out of range
        if (dist > attackRange * 1.3f)
        {
            TransitionTo(EnemyState.Chase);
            return;
        }

        // Attack on cooldown
        if (attackTimer <= 0f)
        {
            DealDamage();
            attackTimer = attackCooldown / aggressionMultiplier;
        }
    }

    // --- DETECTION ---

    bool CanSeePlayer()
    {
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > detectRange) return false;

        // FOV check
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, dirToPlayer);
        if (angle > fieldOfView * 0.5f) return false;

        // Line of sight check
        if (Physics.Raycast(transform.position + Vector3.up, dirToPlayer, dist, obstacleMask))
            return false;

        return true;
    }

    // --- COMBAT ---

    void DealDamage()
    {
        // Deal damage to player (you'll need a PlayerHealth component)
        var playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(attackDamage, transform.position);
        }

        // Notify rage pull system so steroids can react
        if (playerRagePull != null)
        {
            playerRagePull.OnDamageTaken(transform.position);
        }
    }

    // --- DISGUISE ---

    void SetDisguised(bool disguised)
    {
        isDisguised = disguised;

        if (realModel != null)
            realModel.SetActive(!disguised);

        if (disguiseModel != null)
            disguiseModel.SetActive(disguised);
    }

    // --- TRANSITIONS ---

    void TransitionTo(EnemyState newState)
    {
        currentState = newState;

        if (newState == EnemyState.Patrol && patrolPoints.Length > 0)
        {
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);
            patrolWaitTimer = patrolWaitTime;
        }
    }

    void OnDrawGizmosSelected()
    {
        // Visualize detection range and FOV in editor
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // FOV lines
        Vector3 leftBound = Quaternion.Euler(0, -fieldOfView * 0.5f, 0) * transform.forward;
        Vector3 rightBound = Quaternion.Euler(0, fieldOfView * 0.5f, 0) * transform.forward;
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, leftBound * detectRange);
        Gizmos.DrawRay(transform.position, rightBound * detectRange);
    }
}