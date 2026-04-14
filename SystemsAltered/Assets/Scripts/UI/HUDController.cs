using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Corrupts the HUD based on meth hallucination intensity.
/// Displays wrong ammo count and triggers false damage flashes
/// through DrugRenderController (the single renderer authority).
/// </summary>
public class HUDController : MonoBehaviour
{
    [Header("Ammo Display")]
    public TextMeshProUGUI ammoText;
    private int realAmmo;

    [Header("False Damage")]
    public DrugRenderController drugRenderController;
    
    [Header("Drug UI")]
    public DrugStateController drugStateController;
    public Image drugProgressBar;

    private float corruptionLevel;
    private float nextFalseDamageTime;

    public void SetCorruptionLevel(float level)
    {
        corruptionLevel = Mathf.Clamp01(level);
    }

    public void SetRealAmmo(int ammo)
    {
        realAmmo = ammo;
    }

    void Update()
    {
        UpdateAmmoDisplay();
        UpdateDrugProgressBar();

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
        if (drugRenderController == null) return;

        if (Time.time < nextFalseDamageTime) return;

        nextFalseDamageTime = Time.time + Random.Range(2f, 6f) / corruptionLevel;

        drugRenderController.FlashFalseDamage();
    }
    
    void UpdateDrugProgressBar()
    {
        drugProgressBar.gameObject.SetActive(
            drugStateController.CurrentState != drugStateController.soberState
        );
        
        if (drugProgressBar == null || drugStateController == null)
            return;

        float progress = drugStateController.NormalizedProgress;

        // Fill decreases over time (feels more like a "timer")
        drugProgressBar.fillAmount = 1f - progress;

        // OPTIONAL: color shift (green → red as it ends)
        Color color = Color.Lerp(Color.green, Color.red, progress);
        drugProgressBar.color = color;

        // OPTIONAL: flicker when corruption is high
        if (corruptionLevel > 0.7f && Random.value < 0.1f)
        {
            drugProgressBar.fillAmount = Random.Range(0f, 1f);
        }
        
        
    }
}