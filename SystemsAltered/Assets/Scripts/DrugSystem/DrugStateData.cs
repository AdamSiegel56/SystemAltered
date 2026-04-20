using UnityEngine;

public enum DrugState
{
    Sober,
    Cocaine,
    Steroids,
    THC,
    Meth,
    Crash
}

[CreateAssetMenu(fileName = "DrugState_", menuName = "DrugRush/Drug State")]
public class DrugStateData : ScriptableObject
{
    [Header("Info")]
    public DrugState stateType;
    public float duration = 10f;
    public bool hasCrash = true;
    public DrugStateData crashState;

    [Header("Movement Multipliers")]
    public float moveSpeedMultiplier = 1f;
    public float jumpForceMultiplier = 1f;
    public float gravityScaleMultiplier = 1f;

    [Header("Look Multipliers")]
    public float lookSensitivityMultiplier = 1f;
    public float fov = 60f;

    [Header("Combat Multipliers")]
    public float fireRateMultiplier = 1f;
    public float recoilMultiplier = 1f;
    public float spreadMultiplier = 1f;

    [Header("Rendering (URP Renderer Swap)")]
    [Tooltip("Index in the URP Renderer List")]
    public int rendererIndex = 0;

    [Header("Hallucination — Fake Enemies")]
    public bool spawnFakeEnemies;
    public float fakeEnemyChance;

    [Header("Hallucination — Escalation (Meth)")]
    [Tooltip("If true, hallucination intensity ramps 0-1 over duration")]
    public bool escalatingHallucinations;
    [Tooltip("Intensity at which the HUD starts lying")]
    public float hudCorruptionThreshold = 0.4f;
    [Tooltip("Intensity at which geometry starts fracturing")]
    public float geometryFractureThreshold = 0.7f;

    [Header("Environmental Deception (THC)")]
    public bool enableBreathingGeometry;
    [Tooltip("Vertex displacement amplitude for wall breathing")]
    public float breathingAmplitude = 0.05f;
    [Tooltip("Invert door open/closed visual state")]
    public bool invertDoorVisuals;
    [Tooltip("Objects drift when not looked at")]
    public bool enableObjectDrift;
    public float objectDriftSpeed = 0.2f;
    [Tooltip("Jump height variance ±percentage (0.3 = ±30%)")]
    public float jumpVariance = 0f;

    [Header("Rage (Steroids)")]
    public bool enableRagePull;
    [Tooltip("Duration of involuntary lunge toward damage source")]
    public float ragePullDuration = 0.3f;
    [Tooltip("Force of the rage lunge")]
    public float ragePullForce = 8f;
    [Tooltip("Lower camera Y offset to feel heavier")]
    public float cameraHeightOffset = 0f;
    [Tooltip("Peripheral tunnel vignette intensity 0-1")]
    public float tunnelVignetteIntensity = 0f;
    
    [Header("Enemy Appearance Override")]
    [Tooltip("If true, enemies will swap to the mesh/material below")]
    public bool overrideEnemyAppearance;
    public Mesh enemyMesh;
    public Material enemyMaterial;
    public Vector3 enemyScale = Vector3.one;

    [Header("Enemy Stat Multipliers")]
    public float enemySpeedMultiplier = 1f;
    public float enemyDamageMultiplier = 1f;
    public float enemyHealthMultiplier = 1f;
    public float enemyAttackRateMultiplier = 1f;
}