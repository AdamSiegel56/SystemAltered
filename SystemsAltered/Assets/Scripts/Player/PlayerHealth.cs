using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Player health system. Takes damage from enemies and integrates with
/// RagePullSystem, DrugRenderController, and a screen-space health bar.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;

    [Header("References")]
    [SerializeField] private RagePullSystem ragePullSystem;
    [SerializeField] private DrugRenderController drugRenderController;

    [Header("Health Bar (Screen Space)")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private Color fullHealthColor = Color.green;
    [SerializeField] private Color midHealthColor = Color.yellow;
    [SerializeField] private Color lowHealthColor = Color.red;
    [Range(0f, 1f)] [SerializeField] private float midHealthThreshold = 0.6f;
    [Range(0f, 1f)] [SerializeField] private float lowHealthThreshold = 0.3f;

    private float _currentHealth;

    public static event System.Action<float> OnHealthChanged;
    public static event System.Action OnPlayerDied;

    public float CurrentHealth => _currentHealth;
    public float MaxHealth => maxHealth;
    public float NormalizedHealth => _currentHealth / maxHealth;

    // --- Lifecycle ---

    private void Start()
    {
        _currentHealth = maxHealth;
        RefreshUI();
        BroadcastHealthChange();
    }

    // --- Public API ---

    public void TakeDamage(float damage, Vector3 sourcePosition)
    {
        if (_currentHealth <= 0f) return;

        _currentHealth = Mathf.Max(0f, _currentHealth - damage);

        RefreshUI();
        BroadcastHealthChange();

        TriggerDamageReactions(sourcePosition);

        if (_currentHealth <= 0f)
            Die();
    }

    public void Heal(float amount)
    {
        _currentHealth = Mathf.Min(_currentHealth + amount, maxHealth);
        RefreshUI();
        BroadcastHealthChange();
    }

    // --- Damage reactions ---

    private void TriggerDamageReactions(Vector3 sourcePosition)
    {
        if (ragePullSystem != null)
            ragePullSystem.OnDamageTaken(sourcePosition);

        if (drugRenderController != null)
            drugRenderController.FlashDamage();
    }

    // --- UI ---

    private void RefreshUI()
    {
        if (healthBarFill == null) return;

        var normalized = NormalizedHealth;
        healthBarFill.fillAmount = normalized;
        healthBarFill.color = GetHealthColor(normalized);
    }

    private Color GetHealthColor(float normalized)
    {
        if (normalized > midHealthThreshold) return fullHealthColor;
        if (normalized > lowHealthThreshold) return midHealthColor;
        return lowHealthColor;
    }

    private void BroadcastHealthChange()
    {
        OnHealthChanged?.Invoke(NormalizedHealth);
    }

    // --- Death ---

    private void Die()
    {
        OnPlayerDied?.Invoke();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}