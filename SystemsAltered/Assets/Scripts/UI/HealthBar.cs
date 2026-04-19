using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// World-space health bar that floats above an entity.
/// Uses Image fill for the bar visual. Billboard-faces the camera.
/// Hides when full.
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
    public Color midHealthColor = Color.yellow;
    public Color lowHealthColor = Color.red;

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

        transform.position = target.position + offset;
        transform.forward = mainCam.transform.forward;

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

            if (currentFill > 0.6f)
                fillImage.color = fullHealthColor;
            else if (currentFill > 0.3f)
                fillImage.color = midHealthColor;
            else
                fillImage.color = lowHealthColor;
        }

        if (canvas != null && currentFill < 1f)
        {
            canvas.gameObject.SetActive(true);
            showTimer = showDuration;
        }
    }
}
