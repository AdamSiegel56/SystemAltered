using UnityEngine;
using UnityEngine.Rendering.Universal;
using System;
using System.Collections;

/// <summary>
/// Player health system. Takes damage from enemies, flashes a damage
/// renderer via URP renderer index swap, and integrates with RagePullSystem.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("References")]
    public RagePullSystem ragePullSystem;
    public Camera cam;

    [Header("Damage Renderer")]
    [Tooltip("URP Renderer List index for the damage indicator shader")]
    public int damageRendererIndex = 3;
    public float flashDuration = 0.2f;

    private UniversalAdditionalCameraData cameraData;
    private int activeDrugRendererIndex;
    private Coroutine flashRoutine;

    public static event Action<float> OnHealthChanged;
    public static event Action OnPlayerDied;

    void Start()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth / maxHealth);

        if (cam != null)
            cameraData = cam.GetComponent<UniversalAdditionalCameraData>();
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
        activeDrugRendererIndex = state.rendererIndex;
    }

    public void TakeDamage(float damage, Vector3 sourcePosition)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        OnHealthChanged?.Invoke(currentHealth / maxHealth);

        if (ragePullSystem != null)
            ragePullSystem.OnDamageTaken(sourcePosition);

        FlashDamageRenderer();

        if (currentHealth <= 0)
            Die();
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        OnHealthChanged?.Invoke(currentHealth / maxHealth);
    }

    void FlashDamageRenderer()
    {
        if (cameraData == null) return;

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(DamageFlashRoutine());
    }

    IEnumerator DamageFlashRoutine()
    {
        cameraData.SetRenderer(damageRendererIndex);
        yield return new WaitForSeconds(flashDuration);
        cameraData.SetRenderer(activeDrugRendererIndex);
        flashRoutine = null;
    }

    void Die()
    {
        OnPlayerDied?.Invoke();
    }
}