using UnityEngine;
using System;

/// <summary>
/// Player health system. Takes damage from enemies and integrates with
/// RagePullSystem (steroids) and HUD damage indicators.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("References")]
    public RagePullSystem ragePullSystem;
    public HUDController hudController;

    [Header("Damage Feedback")]
    public GameObject realDamageIndicatorPrefab;
    public RectTransform damageIndicatorParent;

    public static event Action<float> OnHealthChanged;
    public static event Action OnPlayerDied;

    void Start()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth / maxHealth);
    }

    public void TakeDamage(float damage, Vector3 sourcePosition)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        OnHealthChanged?.Invoke(currentHealth / maxHealth);

        // Trigger steroid rage pull
        if (ragePullSystem != null)
        {
            ragePullSystem.OnDamageTaken(sourcePosition);
        }

        // Show real damage indicator pointing toward source
        ShowDamageIndicator(sourcePosition);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        OnHealthChanged?.Invoke(currentHealth / maxHealth);
    }

    void ShowDamageIndicator(Vector3 sourcePosition)
    {
        if (realDamageIndicatorPrefab == null || damageIndicatorParent == null) return;

        GameObject indicator = Instantiate(realDamageIndicatorPrefab, damageIndicatorParent);

        // Calculate angle from player forward to damage source
        Vector3 dir = (sourcePosition - transform.position).normalized;
        float angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        float playerAngle = transform.eulerAngles.y;
        float relativeAngle = angle - playerAngle;

        indicator.transform.localRotation = Quaternion.Euler(0, 0, -relativeAngle);

        Destroy(indicator, 0.8f);
    }

    void Die()
    {
        OnPlayerDied?.Invoke();
        // Respawn, game over screen, etc. — implement as needed
    }
}