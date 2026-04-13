using UnityEngine;

/// <summary>
/// Cocaine paranoia system: spawns phantom directional audio and peripheral shadow figures.
/// Attach to the player. Requires a PlayerCamera reference for camera pull.
/// </summary>
public class ParanoiaSystem : MonoBehaviour
{
    [Header("References")]
    public PlayerCamera playerCamera;
    public AudioSource phantomAudioSource;    // 3D audio source on a child object
    public GameObject shadowFigurePrefab;     // Billboard sprite that faces the player

    [Header("Tuning")]
    public float shadowSpawnDistance = 12f;    // How far shadow figures appear
    public float shadowLifetime = 0.4f;       // How long before they vanish
    public float peripheralAngleMin = 50f;    // Min angle from forward to spawn shadows
    public float peripheralAngleMax = 80f;    // Max angle from forward

    [Header("Audio Clips")]
    public AudioClip[] phantomFootsteps;      // Randomized footstep sounds

    private DrugStateData currentState;
    private float nextPhantomTime;
    private bool paranoiaActive;

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
        paranoiaActive = state.enableParanoia;

        if (paranoiaActive)
        {
            ScheduleNextPhantom();
        }
    }

    void Update()
    {
        if (!paranoiaActive || currentState == null) return;

        if (Time.time >= nextPhantomTime)
        {
            TriggerPhantomEvent();
            ScheduleNextPhantom();
        }
    }

    void ScheduleNextPhantom()
    {
        if (currentState == null) return;
        float min = currentState.phantomAudioInterval.x;
        float max = currentState.phantomAudioInterval.y;
        nextPhantomTime = Time.time + Random.Range(min, max);
    }

    void TriggerPhantomEvent()
    {
        // Pick a random direction for the phantom sound
        float angle = Random.Range(0f, 360f);
        Vector3 phantomDir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
        Vector3 phantomPos = transform.position + phantomDir * 8f;

        // Play 3D audio at phantom position
        if (phantomAudioSource != null && phantomFootsteps.Length > 0)
        {
            phantomAudioSource.transform.position = phantomPos;
            phantomAudioSource.clip = phantomFootsteps[Random.Range(0, phantomFootsteps.Length)];
            phantomAudioSource.Play();
        }

        // Pull camera toward phantom sound direction
        float relativeAngle = angle - playerCamera.transform.eulerAngles.y;
        if (relativeAngle > 180f) relativeAngle -= 360f;
        if (relativeAngle < -180f) relativeAngle += 360f;
        playerCamera.SetParanoiaPullTarget(relativeAngle * 0.3f);

        // Chance to spawn a peripheral shadow figure
        if (currentState.spawnShadowFigures && Random.value < 0.4f)
        {
            SpawnShadowFigure();
        }
    }

    void SpawnShadowFigure()
    {
        if (shadowFigurePrefab == null) return;

        // Spawn at edge of peripheral vision
        float side = Random.value > 0.5f ? 1f : -1f;
        float angle = Random.Range(peripheralAngleMin, peripheralAngleMax) * side;
        Vector3 dir = Quaternion.Euler(0, angle, 0) * playerCamera.transform.forward;
        Vector3 spawnPos = transform.position + dir * shadowSpawnDistance;
        spawnPos.y = transform.position.y;

        GameObject shadow = Instantiate(shadowFigurePrefab, spawnPos, Quaternion.identity);

        // The shadow figure script handles facing the player and self-destructing
        // when the player looks directly at it
        var behavior = shadow.GetComponent<ShadowFigureBehavior>();
        if (behavior != null)
        {
            behavior.Init(playerCamera.transform, shadowLifetime);
        }
        else
        {
            Destroy(shadow, shadowLifetime);
        }
    }
}