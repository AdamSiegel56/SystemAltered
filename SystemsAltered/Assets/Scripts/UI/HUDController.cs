using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Corrupts the HUD based on meth hallucination intensity.
/// Displays wrong ammo count and triggers false damage flashes
/// through DrugRenderController (the single renderer authority).
/// </summary>
public class HUDController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DrugStateController drugStateController;
    [SerializeField] private DrugRenderController drugRenderController;

    [Header("Ammo Display")]
    [SerializeField] private TextMeshProUGUI ammoText;

    [Header("Drug Progress Bar")]
    [SerializeField] private Image drugProgressBar;

    [Header("False Damage Tuning")]
    [Tooltip("Corruption level above which false damage flashes can trigger")]
    [SerializeField] private float falseDamageThreshold = 0.2f;
    [Tooltip("Base interval between false damage flashes (divided by corruption)")]
    [SerializeField] private Vector2 falseDamageIntervalRange = new(2f, 6f);

    [Header("Ammo Corruption Tuning")]
    [Tooltip("Chance per frame of displaying a wrong ammo number (scaled by corruption)")]
    [SerializeField] private float ammoLieChance = 0.3f;
    [Tooltip("Max deviation from the real ammo count (scaled by corruption)")]
    [SerializeField] private float ammoLieRange = 10f;
    [Tooltip("Corruption level above which ammo text flickers")]
    [SerializeField] private float ammoFlickerThreshold = 0.6f;

    private int _realAmmo;
    private float _corruptionLevel;
    private float _nextFalseDamageTime;

    // --- Public API ---

    public void SetRealAmmo(int ammo) => _realAmmo = ammo;

    public void SetCorruptionLevel(float level)
    {
        _corruptionLevel = Mathf.Clamp01(level);
    }

    // --- Lifecycle ---

    private void Update()
    {
        UpdateAmmoDisplay();
        UpdateDrugProgressBar();

        if (_corruptionLevel > falseDamageThreshold)
            HandleFalseDamageIndicators();
    }

    // --- Ammo display ---

    private void UpdateAmmoDisplay()
    {
        if (ammoText == null) return;

        ammoText.text = GetDisplayedAmmo().ToString();
        ammoText.alpha = GetAmmoAlpha();
    }

    private int GetDisplayedAmmo()
    {
        if (_corruptionLevel <= 0.01f) return _realAmmo;

        if (Random.value >= _corruptionLevel * ammoLieChance) return _realAmmo;

        var deviation = Mathf.RoundToInt(Random.Range(-ammoLieRange, ammoLieRange) * _corruptionLevel);
        return Mathf.Max(0, _realAmmo + deviation);
    }

    private float GetAmmoAlpha()
    {
        if (_corruptionLevel > ammoFlickerThreshold && Random.value < 0.05f)
            return Random.Range(0.3f, 1f);

        return 1f;
    }

    // --- False damage ---

    private void HandleFalseDamageIndicators()
    {
        if (drugRenderController == null) return;
        if (Time.time < _nextFalseDamageTime) return;

        var interval = Random.Range(falseDamageIntervalRange.x, falseDamageIntervalRange.y) / _corruptionLevel;
        _nextFalseDamageTime = Time.time + interval;

        drugRenderController.FlashFalseDamage();
    }

    // --- Drug progress bar ---

    private void UpdateDrugProgressBar()
    {
        if (drugProgressBar == null || drugStateController == null) return;

        drugProgressBar.gameObject.SetActive(!drugStateController.IsSober);
        if (drugStateController.IsSober) return;

        var progress = drugStateController.NormalizedProgress;

        drugProgressBar.fillAmount = GetProgressFill(progress);
        drugProgressBar.color = Color.Lerp(Color.green, Color.red, progress);
    }

    private float GetProgressFill(float progress)
    {
        // Flicker the bar when corruption is high
        if (_corruptionLevel > 0.7f && Random.value < 0.1f)
            return Random.Range(0f, 1f);

        // Fill decreases over time (feels like a countdown timer)
        return 1f - progress;
    }
}