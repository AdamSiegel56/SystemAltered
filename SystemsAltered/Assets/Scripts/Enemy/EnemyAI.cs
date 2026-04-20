using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DrugStateController drugState;

    [Header("Combat")]
    [SerializeField] private GameObject projectile;
    [SerializeField] private GameObject hitEffect;
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float timeBetweenAttacks = 1.5f;
    [SerializeField] private float forwardImpulse = 32f;
    [SerializeField] private float upwardImpulse = 8f;

    public LayerMask whatIsPlayer;
    public LayerMask whatIsGround;

    [Header("Detection")]
    [SerializeField] private float sightRange = 15f;
    [SerializeField] private float attackRange = 8f;

    [Header("Patrol")]
    [SerializeField] private float walkPointRange = 10f;
    [SerializeField] private float navMeshSampleDistance = 2f;
    [SerializeField] private float walkPointArrivalDistance = 1f;

    [Header("Fake Enemy")]
    [SerializeField] private bool isFakeEnemy;
    [SerializeField] private GameObject fakeDeathPopPrefab;

    // Runtime state
    private float currentHealth;
    private Vector3 walkPoint;
    private bool walkPointSet;
    private bool alreadyAttacked;
    private bool playerInSightRange;
    private bool playerInAttackRange;
    private bool isDead;

    // Cached base appearance
    private float _baseSpeed;
    private Vector3 _baseScale;
    private Mesh _baseMesh;
    private Material _baseMaterial;
    private DrugStateData _lastAppliedState;

    // --- Drug state injection ---

    /// <summary>
    /// Set by EnemySpawner after instantiation. Overrides any prefab-assigned value.
    /// </summary>
    public DrugStateController DrugState
    {
        set => drugState = value;
    }

    // --- Multiplier accessors ---

    private float SpeedMult =>
        drugState?.CurrentState?.enemySpeedMultiplier ?? 1f;

    private float DamageMult =>
        drugState?.CurrentState?.enemyDamageMultiplier ?? 1f;

    private float AttackRateMult =>
        drugState?.CurrentState?.enemyAttackRateMultiplier ?? 1f;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float NormalizedHealth => currentHealth / maxHealth;
    public bool IsFake => isFakeEnemy;

    public static event System.Action EnemyKilled;

    void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (meshFilter == null) meshFilter = GetComponentInChildren<MeshFilter>();
        if (meshRenderer == null) meshRenderer = GetComponentInChildren<MeshRenderer>();

        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        currentHealth = maxHealth;
        CacheBaseAppearance();
    }

    private void Update()
    {
        ApplyDrugStateIfChanged();
        UpdateDetection();
        UpdateBehaviour();
    }

    private void CacheBaseAppearance()
    {
        _baseSpeed = agent != null ? agent.speed : 0f;
        _baseScale = transform.localScale;
        _baseMesh = meshFilter != null ? meshFilter.sharedMesh : null;
        _baseMaterial = meshRenderer != null ? meshRenderer.sharedMaterial : null;
    }

    private void ApplyDrugStateIfChanged()
    {
        var state = drugState != null ? drugState.CurrentState : null;
        if (state == _lastAppliedState) return;

        _lastAppliedState = state;
        ApplyAppearance(state);
        ApplyStats();
    }

    private void ApplyAppearance(DrugStateData state)
    {
        if (state != null && state.overrideEnemyAppearance)
        {
            SetMesh(state.enemyMesh != null ? state.enemyMesh : _baseMesh);
            SetMaterial(state.enemyMaterial != null ? state.enemyMaterial : _baseMaterial);
            transform.localScale = Vector3.Scale(_baseScale, state.enemyScale);
        }
        else
        {
            SetMesh(_baseMesh);
            SetMaterial(_baseMaterial);
            transform.localScale = _baseScale;
        }
    }

    private void SetMesh(Mesh mesh)
    {
        if (meshFilter != null) meshFilter.mesh = mesh;
    }

    private void SetMaterial(Material material)
    {
        if (meshRenderer != null) meshRenderer.material = material;
    }

    private void ApplyStats()
    {
        if (agent != null)
            agent.speed = _baseSpeed * SpeedMult;
    }

    private void UpdateDetection()
    {
        if (player == null)
        {
            playerInSightRange = false;
            playerInAttackRange = false;
            return;
        }

        var distance = Vector3.Distance(transform.position, player.position);
        playerInSightRange  = distance <= sightRange;
        playerInAttackRange = distance <= attackRange;
    }

    private void UpdateBehaviour()
    {
        if (!playerInSightRange && !playerInAttackRange) Patrol();
        else if (playerInSightRange && !playerInAttackRange) Chase();
        else if (playerInAttackRange && playerInSightRange) Attack();
    }

    // --- Patrol ---

    private void Patrol()
    {
        if (!walkPointSet) SearchWalkPoint();

        if (walkPointSet)
            agent.SetDestination(walkPoint);

        var distanceToWalkPoint = transform.position - walkPoint;
        if (distanceToWalkPoint.magnitude < walkPointArrivalDistance)
            walkPointSet = false;
    }

    private void SearchWalkPoint()
    {
        var randomX = Random.Range(-walkPointRange, walkPointRange);
        var randomZ = Random.Range(-walkPointRange, walkPointRange);

        var candidate = new Vector3(
            transform.position.x + randomX,
            transform.position.y,
            transform.position.z + randomZ
        );

        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, navMeshSampleDistance, NavMesh.AllAreas))
        {
            walkPoint = hit.position;
            walkPointSet = true;
        }
    }

    // --- Chase / Attack ---

    private void Chase()
    {
        if (player != null)
            agent.SetDestination(player.position);
    }

    private void Attack()
    {
        if (player == null) return;

        agent.SetDestination(transform.position);
        transform.LookAt(player);

        if (alreadyAttacked) return;

        FireProjectile();

        alreadyAttacked = true;
        var cooldown = timeBetweenAttacks / Mathf.Max(AttackRateMult, 0.01f);
        Invoke(nameof(ResetAttack), cooldown);
    }

    private void FireProjectile()
    {
        if (isFakeEnemy) return;
        if (projectile == null) return;

        var bulletObj = Instantiate(projectile, transform.position, Quaternion.identity);
        var bullet = bulletObj.GetComponent<Bullet>();

        var finalDamage = attackDamage * DamageMult;

        if (bullet != null)
        {
            var direction = (transform.forward + transform.up * 0.2f).normalized;
            bullet.Init(direction, finalDamage, gameObject);
        }
        else
        {
            var rb = bulletObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(transform.forward * (forwardImpulse * DamageMult), ForceMode.Impulse);
                rb.AddForce(transform.up * upwardImpulse, ForceMode.Impulse);
            }
        }
    }

    private void ResetAttack()
    {
        alreadyAttacked = false;
    }

    private bool hasntEvent;
    public void TakeDamage(float damage, Vector3 hitPos, Vector3 hitNormal)
    {
        if (hitEffect != null)
            Instantiate(hitEffect, hitPos, Quaternion.LookRotation(hitNormal));

        if (isFakeEnemy)
        {
            PopFakeEnemy();
            return;
        }

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            if (!hasntEvent)
            {
                EnemyKilled?.Invoke();
                hasntEvent = true;
            }

            Invoke(nameof(DestroyEnemy), 0.5f);
        }
    }

    private void DestroyEnemy()
    {
        Destroy(gameObject);
    }

    private void PopFakeEnemy()
    {
        if (fakeDeathPopPrefab != null)
        {
            var pop = Instantiate(fakeDeathPopPrefab, transform.position, Quaternion.identity);
            Destroy(pop, 0.5f);
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);
    }

    [Header("References")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private MeshRenderer meshRenderer;
    private Transform player;
}