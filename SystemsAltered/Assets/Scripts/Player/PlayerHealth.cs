using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Player health system. Takes damage from enemies and integrates with
/// RagePullSystem, DrugRenderController, and a screen-space health bar.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("References")]
    public RagePullSystem ragePullSystem;
    public DrugRenderController drugRenderController;

    [Header("Player Health Bar (Screen Space)")]
    public Image healthBarFill;
    public Color fullHealthColor = Color.green;
    public Color lowHealthColor = Color.red;

    public static event System.Action<float> OnHealthChanged;
    public static event System.Action OnPlayerDied;

    void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthBar();
        OnHealthChanged?.Invoke(currentHealth / maxHealth);
    }

    public void TakeDamage(float damage, Vector3 sourcePosition)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        UpdateHealthBar();
        OnHealthChanged?.Invoke(currentHealth / maxHealth);

        if (ragePullSystem != null)
            ragePullSystem.OnDamageTaken(sourcePosition);

        if (drugRenderController != null)
            drugRenderController.FlashDamage();

        if (currentHealth <= 0)
            Die();
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        UpdateHealthBar();
        OnHealthChanged?.Invoke(currentHealth / maxHealth);
    }

    void UpdateHealthBar()
    {
        if (healthBarFill == null) return;

        float normalized = currentHealth / maxHealth;
        healthBarFill.fillAmount = normalized;
        healthBarFill.color = normalized <= 0.3f ? lowHealthColor : fullHealthColor;
    }

    void Die()
    {
        OnPlayerDied?.Invoke();
    }
}