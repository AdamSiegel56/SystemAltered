using UnityEngine;

/// <summary>
/// Enemy health with hit feedback, death handling, and health bar support.
/// Set invulnerable = true when disguised so bullets pass through.
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    public float maxHealth = 50f;
    private float currentHealth;

    [Header("Feedback")]
    public Renderer bodyRenderer;
    private Material bodyMaterial;
    private Color originalColor;
    private float flashTimer;

    [Header("Health Bar")]
    public HealthBar healthBar;

    /// <summary>
    /// Set by EnemyAI when disguised. While true, TakeDamage does nothing.
    /// </summary>
    [HideInInspector]
    public bool invulnerable;

    public static event System.Action<GameObject> OnEnemyKilled;

    void Start()
    {
        currentHealth = maxHealth;

        if (bodyRenderer != null)
        {
            bodyMaterial = bodyRenderer.material;
            originalColor = bodyMaterial.color;
        }

        if (healthBar != null)
            healthBar.SetFill(1f);
    }

    void Update()
    {
        if (flashTimer > 0 && bodyMaterial != null)
        {
            flashTimer -= Time.deltaTime;
            float t = flashTimer / 0.15f;
            bodyMaterial.color = Color.Lerp(originalColor, Color.white, t);
        }
    }

    public void TakeDamage(float dmg)
    {
        if (invulnerable) return;

        currentHealth -= dmg;
        flashTimer = 0.15f;

        if (healthBar != null)
            healthBar.SetFill(currentHealth / maxHealth);

        if (currentHealth <= 0)
        {
            OnEnemyKilled?.Invoke(gameObject);
            Destroy(gameObject);
        }
    }
}