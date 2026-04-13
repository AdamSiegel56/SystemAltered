using UnityEngine;

public class DrugStateController : MonoBehaviour
{
    public DrugStateData startingState;
    public DrugStateData soberState;

    private DrugStateData currentState;
    private float timer;

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

        DrugEventBus.OnDrugStateChanged?.Invoke(newState);
    }
}