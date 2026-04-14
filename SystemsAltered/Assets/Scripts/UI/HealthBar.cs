using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// World-space health bar that floats above an entity.
/// Works for real enemies, fake enemies, and the player.
/// For enemies: attach to the enemy prefab root.
/// The bar always faces the camera (billboard).
/// </summary>
public class HealthBar : MonoBehaviour
{
    [Header("Setup")]
    public Transform target;
    public Vector3 offset = new Vector3(0, 2.2f, 0);

    [Header("Bar References")]
    public Image fillImage;
    public Image backgroundImage;
    public Canvas canvas;

    [Header("Colors")]
    public Color fullHealthColor = Color.green;
    public Color lowHealthColor = Color.red;
    public float lowHealthThreshold = 0.3f;

    [Header("Behavior")]
    public bool hideWhenFull = true;
    public float showDuration = 3f;

    private Camera mainCam;
    private float currentFill = 1f;
    private float showTimer;

    void Start()
    {
        mainCam = Camera.main;

        if (canvas != null && hideWhenFull)
            canvas.gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        if (target == null || mainCam == null) return;

        // Billboard: face camera
        transform.position = target.position + offset;
        transform.forward = mainCam.transform.forward;

        // Hide timer
        if (hideWhenFull)
        {
            showTimer -= Time.deltaTime;
            if (showTimer <= 0 && currentFill >= 1f)
            {
                if (canvas != null) canvas.gameObject.SetActive(false);
            }
        }
    }

    public void SetFill(float normalized)
    {
        currentFill = Mathf.Clamp01(normalized);

        if (fillImage != null)
        {
            fillImage.fillAmount = currentFill;
            fillImage.color = currentFill <= lowHealthThreshold ? lowHealthColor : fullHealthColor;
        }

        // Show the bar when health changes
        if (canvas != null && currentFill < 1f)
        {
            canvas.gameObject.SetActive(true);
            showTimer = showDuration;
        }
    }
}