using UnityEngine;
using TMPro;

/// <summary>
/// Corrupts the HUD based on meth hallucination intensity.
/// Displays wrong ammo count and false damage direction indicators.
/// </summary>
public class HUDController : MonoBehaviour
{
    [Header("Ammo Display")]
    public TextMeshProUGUI ammoText;
    private int realAmmo;

    [Header("Damage Indicator")]
    public RectTransform damageIndicatorParent;
    public GameObject damageIndicatorPrefab;

    private float corruptionLevel;
    private float nextFalseDamageTime;

    /// <summary>
    /// Set by HallucinationEscalation. 0 = clean HUD, 1 = fully corrupted.
    /// </summary>
    public void SetCorruptionLevel(float level)
    {
        corruptionLevel = Mathf.Clamp01(level);
    }

    /// <summary>
    /// Call from weapon system whenever real ammo changes.
    /// </summary>
    public void SetRealAmmo(int ammo)
    {
        realAmmo = ammo;
    }

    void Update()
    {
        UpdateAmmoDisplay();

        if (corruptionLevel > 0.2f)
        {
            HandleFalseDamageIndicators();
        }
    }

    void UpdateAmmoDisplay()
    {
        if (ammoText == null) return;

        if (corruptionLevel <= 0.01f)
        {
            ammoText.text = realAmmo.ToString();
            return;
        }

        // Randomly show wrong ammo count. Higher corruption = bigger deviation.
        if (Random.value < corruptionLevel * 0.3f)
        {
            int deviation = Mathf.RoundToInt(Random.Range(-10f, 10f) * corruptionLevel);
            int displayed = Mathf.Max(0, realAmmo + deviation);
            ammoText.text = displayed.ToString();
        }
        else
        {
            ammoText.text = realAmmo.ToString();
        }

        // At high corruption, briefly flicker the text
        if (corruptionLevel > 0.6f && Random.value < 0.05f)
        {
            ammoText.alpha = Random.Range(0.3f, 1f);
        }
        else
        {
            ammoText.alpha = 1f;
        }
    }

    void HandleFalseDamageIndicators()
    {
        if (damageIndicatorParent == null || damageIndicatorPrefab == null) return;

        if (Time.time < nextFalseDamageTime) return;

        // Schedule next false indicator
        nextFalseDamageTime = Time.time + Random.Range(2f, 6f) / corruptionLevel;

        // Spawn a false damage indicator from a random direction
        GameObject indicator = Instantiate(damageIndicatorPrefab, damageIndicatorParent);
        float randomAngle = Random.Range(0f, 360f);
        indicator.transform.localRotation = Quaternion.Euler(0, 0, randomAngle);

        // Auto-destroy after brief flash
        Destroy(indicator, 0.5f);
    }
}