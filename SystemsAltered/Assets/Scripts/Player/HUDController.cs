using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;
using TMPro;

/// <summary>
/// Corrupts the HUD based on meth hallucination intensity.
/// Displays wrong ammo count and triggers false damage flashes
/// by briefly swapping to the damage URP renderer index.
/// </summary>
public class HUDController : MonoBehaviour
{
    [Header("Ammo Display")]
    public TextMeshProUGUI ammoText;
    private int realAmmo;

    [Header("False Damage Renderer")]
    public Camera cam;
    [Tooltip("URP Renderer List index for the damage indicator shader")]
    public int damageRendererIndex = 3;
    public float falseFlashDuration = 0.15f;

    private UniversalAdditionalCameraData cameraData;
    private int activeDrugRendererIndex;

    private float corruptionLevel;
    private float nextFalseDamageTime;
    private Coroutine flashRoutine;

    void Start()
    {
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
        if (cameraData == null) return;

        if (Time.time < nextFalseDamageTime) return;

        nextFalseDamageTime = Time.time + Random.Range(2f, 6f) / corruptionLevel;

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FalseFlashRoutine());
    }

    IEnumerator FalseFlashRoutine()
    {
        cameraData.SetRenderer(damageRendererIndex);
        yield return new WaitForSeconds(falseFlashDuration);
        cameraData.SetRenderer(activeDrugRendererIndex);
        flashRoutine = null;
    }
}