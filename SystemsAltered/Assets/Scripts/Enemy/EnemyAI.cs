using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private DrugStateController drugState;
    [SerializeField] private EnemyHealthBar healthBar;
    private Transform _player;

    [Header("Combat")]
    [SerializeField] private GameObject projectile;
    [SerializeField] private GameObject hitEffect;
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float timeBetweenAttacks = 1.5f;
    [SerializeField] private float forwardImpulse = 32f;
    [SerializeField] private float upwardImpulse = 8f;

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

    [Header("Death")]
    [SerializeField] private float destroyDelay = 0.5f;

    // Runtime state
    private float _currentHealth;
    private Vector3 _walkPoint;
    private bool _walkPointSet;
    private bool _alreadyAttacked;
    private bool _playerInSightRange;
    private bool _playerInAttackRange;
    private bool _isDead;

    // Cached base appearance
    private float _baseSpeed;
    private Vector3 _baseScale;
    private Mesh _baseMesh;
    private Material _baseMaterial;
    private DrugStateData _lastAppliedState;

    // --- Multiplier accessors ---

    private float SpeedMult =>
        drugState?.CurrentState?.enemySpeedMultiplier ?? 1f;

    private float DamageMult =>
        drugState?.CurrentState?.enemyDamageMultiplier ?? 1f;

    private float AttackRateMult =>
        drugState?.CurrentState?.enemyAttackRateMultiplier ?? 1f;

    // --- Public accessors ---

    public float CurrentHealth => _currentHealth;
    public float MaxHealth => maxHealth;
    public float NormalizedHealth => _currentHealth / maxHealth;
    public bool IsFake => isFakeEnemy;

    // --- Lifecycle ---

    private void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (meshFilter == null) meshFilter = GetComponentInChildren<MeshFilter>();
        if (meshRenderer == null) meshRenderer = GetComponentInChildren<MeshRenderer>();
        if (healthBar == null) healthBar = GetComponentInChildren<EnemyHealthBar>();

        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) _player = playerObj.transform;

        CacheBaseAppearance();
        InitializeHealth();
    }

    private void Update()
    {
        if (_isDead) return;

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

    private void InitializeHealth()
    {
        _currentHealth = maxHealth;

        if (isFakeEnemy && healthBar != null)
        {
            healthBar.gameObject.SetActive(false);
            return;
        }

        if (healthBar != null)
            healthBar.Initialize(this);
    }

    // --- Drug response ---

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

    // --- Detection ---

    private void UpdateDetection()
    {
        if (_player == null)
        {
            _playerInSightRange = false;
            _playerInAttackRange = false;
            return;
        }

        var distance = Vector3.Distance(transform.position, _player.position);
        _playerInSightRange  = distance <= sightRange;
        _playerInAttackRange = distance <= attackRange;
    }

    private void UpdateBehaviour()
    {
        if (!_playerInSightRange && !_playerInAttackRange) Patrol();
        else if (_playerInSightRange && !_playerInAttackRange) Chase();
        else if (_playerInAttackRange && _playerInSightRange) Attack();
    }

    // --- Patrol ---

    private void Patrol()
    {
        if (!_walkPointSet) SearchWalkPoint();

        if (_walkPointSet)
            agent.SetDestination(_walkPoint);

        var distanceToWalkPoint = transform.position - _walkPoint;
        if (distanceToWalkPoint.magnitude < walkPointArrivalDistance)
            _walkPointSet = false;
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
            _walkPoint = hit.position;
            _walkPointSet = true;
        }
    }

    // --- Chase / Attack ---

    private void Chase()
    {
        if (_player != null)
            agent.SetDestination(_player.position);
    }

    private void Attack()
    {
        if (_player == null) return;

        agent.SetDestination(transform.position);
        transform.LookAt(_player);

        if (_alreadyAttacked) return;

        FireProjectile();

        _alreadyAttacked = true;
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
                rb.AddForce(transform.forward * forwardImpulse * DamageMult, ForceMode.Impulse);
                rb.AddForce(transform.up * upwardImpulse, ForceMode.Impulse);
            }
        }
    }

    private void ResetAttack()
    {
        _alreadyAttacked = false;
    }

    // --- Damage / Death ---

    public void TakeDamage(float damage, Vector3 hitPos, Vector3 hitNormal)
    {
        if (_isDead) return;

        if (hitEffect != null)
            Instantiate(hitEffect, hitPos, Quaternion.LookRotation(hitNormal));

        if (isFakeEnemy)
        {
            PopFakeEnemy();
            return;
        }

        _currentHealth = Mathf.Max(0f, _currentHealth - damage);

        if (healthBar != null)
            healthBar.Refresh();

        if (_currentHealth <= 0f)
            Die();
    }

    private void Die()
    {
        _isDead = true;

        if (agent != null) agent.isStopped = true;
        if (healthBar != null) healthBar.Hide();

        Invoke(nameof(DestroyEnemy), destroyDelay);
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

    private void DestroyEnemy()
    {
        Destroy(gameObject);
    }

    // --- Gizmos ---

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);
    }
}