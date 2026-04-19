using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// World-space health bar for an enemy. Billboards to face the camera,
/// color-shifts based on health remaining, and hides when full or dead.
/// Attach to a child GameObject of the enemy with a Canvas (World Space)
/// containing an Image set as the fill bar.
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image fillImage;
    [SerializeField] private Canvas canvas;

    [Header("Appearance")]
    [SerializeField] private Color fullColor = Color.green;
    [SerializeField] private Color midColor = Color.yellow;
    [SerializeField] private Color lowColor = Color.red;
    [Range(0f, 1f)] [SerializeField] private float midThreshold = 0.6f;
    [Range(0f, 1f)] [SerializeField] private float lowThreshold = 0.3f;

    [Header("Behaviour")]
    [Tooltip("Hide the bar when the enemy is at full health")]
    [SerializeField] private bool hideWhenFull = true;
    [Tooltip("Local offset above the enemy root")]
    [SerializeField] private Vector3 worldOffset = new(0f, 2.2f, 0f);

    private EnemyAI _owner;
    private Camera _playerCam;

    // --- Public API ---

    public void Initialize(EnemyAI owner)
    {
        _owner = owner;
        Refresh();
    }

    public void Refresh()
    {
        if (_owner == null || fillImage == null) return;

        var normalized = _owner.NormalizedHealth;
        fillImage.fillAmount = normalized;
        fillImage.color = GetColor(normalized);

        if (canvas != null)
            canvas.enabled = ShouldShow(normalized);
    }

    public void Hide()
    {
        if (canvas != null) canvas.enabled = false;
    }

    // --- Lifecycle ---

    private void LateUpdate()
    {
        if (_owner == null) return;

        PositionAboveOwner();
        FaceCamera();
    }

    // --- Positioning ---

    private void PositionAboveOwner()
    {
        transform.position = _owner.transform.position + worldOffset;
    }

    private void FaceCamera()
    {
        if (!EnsureCamera()) return;

        // Face away from the camera so the text reads correctly
        var forward = transform.position - _playerCam.transform.position;
        transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
    }

    private bool EnsureCamera()
    {
        if (_playerCam != null) return true;
        _playerCam = Camera.main;
        return _playerCam != null;
    }

    // --- Visuals ---

    private Color GetColor(float normalized)
    {
        if (normalized > midThreshold) return fullColor;
        if (normalized > lowThreshold) return midColor;
        return lowColor;
    }

    private bool ShouldShow(float normalized)
    {
        if (_owner == null) return false;
        if (hideWhenFull && normalized >= 0.999f) return false;
        return true;
    }
}