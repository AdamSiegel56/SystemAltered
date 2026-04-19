using UnityEngine;

public class DrugStateController : MonoBehaviour
{
    [Header("States")]
    [SerializeField] private DrugStateData startingState;
    [SerializeField] private DrugStateData soberState;

    [Header("THC Particle")]
    [SerializeField] private GameObject particleWeed;
    [SerializeField] private Transform particleLoc;
    [SerializeField] private float particleLifetime = 8f;

    private DrugStateData _currentState;
    private float _timer;
    private float _stateStartTime;

    // --- Public accessors ---

    public DrugStateData CurrentState => _currentState;
    public DrugStateData SoberState => soberState;
    public bool IsSober => _currentState == soberState;

    /// <summary>
    /// Normalized progress through the current drug (0 = just taken, 1 = about to expire).
    /// </summary>
    public float NormalizedProgress
    {
        get
        {
            if (_currentState == null || _currentState.duration <= 0f) return 0f;
            return Mathf.Clamp01((Time.time - _stateStartTime) / _currentState.duration);
        }
    }

    /// <summary>
    /// Current hallucination intensity (0-1). Only meaningful if current state
    /// has escalatingHallucinations enabled; otherwise returns 0 or 1.
    /// </summary>
    public float HallucinationIntensity
    {
        get
        {
            if (_currentState == null) return 0f;
            if (_currentState.escalatingHallucinations) return NormalizedProgress;
            if (_currentState.spawnFakeEnemies) return 1f;
            return 0f;
        }
    }

    // --- Lifecycle ---

    private void Start()
    {
        SetState(startingState != null ? startingState : soberState);
    }

    private void Update()
    {
        TickTimer();
    }

    private void TickTimer()
    {
        _timer -= Time.deltaTime;
        if (_timer > 0f) return;

        SetState(GetNextState());
    }

    private DrugStateData GetNextState()
    {
        if (_currentState != null && _currentState.hasCrash && _currentState.crashState != null)
            return _currentState.crashState;

        return soberState;
    }

    // --- State transitions ---

    public void SetState(DrugStateData newState)
    {
        if (newState == null) return;

        _currentState = newState;
        _timer = newState.duration;
        _stateStartTime = Time.time;

        HandleStateEffects(newState);

        DrugEventBus.OnDrugStateChanged?.Invoke(newState);
    }

    private void HandleStateEffects(DrugStateData state)
    {
        if (state.stateType == DrugState.THC)
            SpawnWeedParticle();
    }

    // --- Effects ---

    private void SpawnWeedParticle()
    {
        if (particleWeed == null || particleLoc == null) return;

        var newParticle = Instantiate(particleWeed, particleLoc);
        Destroy(newParticle, particleLifetime);
    }
}