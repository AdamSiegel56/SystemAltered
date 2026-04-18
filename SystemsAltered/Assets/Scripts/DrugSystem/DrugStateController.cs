using UnityEngine;

public class DrugStateController : MonoBehaviour
{
    public DrugStateData startingState;
    public DrugStateData soberState;


    public GameObject particleWeed;
    public Transform particleLoc;
    
    private DrugStateData currentState;
    private float timer;
    private float stateStartTime;
    
    /// <summary>
    /// Normalized progress through the current drug (0 = just taken, 1 = about to expire).
    /// Used by escalation systems (e.g. meth hallucination ramp).
    /// </summary>
    public float NormalizedProgress
    {
        get
        {
            if (currentState == null || currentState.duration <= 0f) return 0f;
            return Mathf.Clamp01((Time.time - stateStartTime) / currentState.duration);
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
            if (currentState == null) return 0f;
            if (currentState.escalatingHallucinations) return NormalizedProgress;
            if (currentState.spawnFakeEnemies) return 1f;
            return 0f;
        }
    }

    public DrugStateData CurrentState => currentState;

    void Start()
    {
        SetState(startingState != null ? startingState : soberState);
        
        
    }

    void Update()
    {
        timer -= Time.deltaTime;

        if (timer <= 0)
        {
            if (currentState.hasCrash && currentState.crashState != null)
                SetState(currentState.crashState);
            else
                SetState(soberState);
        }
    }

    public void SetState(DrugStateData newState)
    {
        currentState = newState;
        timer = newState.duration;
        stateStartTime = Time.time;

        
        
        if (newState.stateType ==  DrugState.THC)
        {
            WeedParticle();
        }

        DrugEventBus.OnDrugStateChanged?.Invoke(newState);
    }

    private void WeedParticle()
    {
        GameObject newParticle = Instantiate(particleWeed, particleLoc);
        Destroy(newParticle, 8f);
    }

}