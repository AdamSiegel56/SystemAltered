using UnityEngine;
using System.Collections;

public struct GunInput
{
    public bool Shoot;
    public bool Reload;
}

public class Gun : MonoBehaviour
{
    [Header("Weapon Stats")]
    [SerializeField] private float range = 50f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float fireRate = 10f;
    [SerializeField] private float inaccuracyDistance = 5f;

    [Header("Pellet Weapon")]
    [SerializeField] private bool isPelletWeapon;
    [SerializeField] private int bulletsPerShot = 6;

    [Header("Ammo")]
    [SerializeField] private int magSize;
    [SerializeField] private float reloadTime = 2f;

    [Header("Visuals")]
    [SerializeField] private Transform muzzle;
    [SerializeField] private GameObject laser;
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("References")]
    [SerializeField] private HUDController hudController;
    [SerializeField] private DrugStateController drugState;

    private Transform _cam;
    private bool _requestedShoot;
    private bool _requestedReload;
    private float _nextFireTime;
    private int _currentAmmo;
    private bool _isReloading;

    // --- Multiplier accessors ---

    private float FireRateMult =>
        drugState?.CurrentState?.fireRateMultiplier ?? 1f;

    private float SpreadMult =>
        drugState?.CurrentState?.spreadMultiplier ?? 1f;

    // --- Lifecycle ---

    public void Initialize()
    {
        _cam = Camera.main.transform;
        _currentAmmo = magSize;
    }

    public void UpdateInput(GunInput input)
    {
        _requestedShoot = input.Shoot;
        _requestedReload = input.Reload;
    }

    public void Tick(float deltaTime)
    {
        HandleShooting();
    }

    private void Update()
    {
        HandleReload();
        HandleShooting();
        UpdateUI();
    }

    // --- Shooting ---

    private void HandleShooting()
    {
        if (!_requestedShoot || Time.time < _nextFireTime) return;

        Shoot();
        _nextFireTime = Time.time + 1f / (fireRate * FireRateMult);
    }

    private void Shoot()
    {
        if (_isReloading) return;

        _currentAmmo--;

        if (!CanShoot())
        {
            if (CanReload()) StartCoroutine(Reload());
            return;
        }

        var shotCount = isPelletWeapon ? bulletsPerShot : 1;
        for (int i = 0; i < shotCount; i++)
            FireSingleRay();
    }

    private void FireSingleRay()
    {
        var direction = GetShootingDirection();

        if (Physics.Raycast(_cam.position, direction, out RaycastHit hit, range))
        {
            var enemy = hit.collider.GetComponent<EnemyAI>();
            if (enemy != null)
                enemy.TakeDamage(damage, hit.point, hit.normal);

            CreateLaser(hit.point);
        }
        else
        {
            CreateLaser(_cam.position + direction * range);
        }
    }

    private Vector3 GetShootingDirection()
    {
        var inaccuracy = inaccuracyDistance * SpreadMult;

        var targetPos = _cam.position + _cam.forward * range;
        targetPos += new Vector3(
            Random.Range(-inaccuracy, inaccuracy),
            Random.Range(-inaccuracy, inaccuracy),
            Random.Range(-inaccuracy, inaccuracy)
        );

        return (targetPos - _cam.position).normalized;
    }

    // --- Reloading ---

    private void HandleReload()
    {
        if (_requestedReload && !_isReloading && CanReload())
            StartCoroutine(Reload());

        _requestedReload = false;
    }

    private IEnumerator Reload()
    {
        if (!CanReload()) yield break;

        _isReloading = true;
        yield return new WaitForSeconds(reloadTime);

        _currentAmmo = magSize;
        _isReloading = false;
        _requestedReload = false;
    }

    // --- Queries ---

    private bool CanShoot() => _currentAmmo > 0;
    private bool CanReload() => _currentAmmo < magSize && !_isReloading;

    // --- UI ---

    private void UpdateUI()
    {
        hudController.SetRealAmmo(_currentAmmo);
    }

    // --- Visuals ---

    private void CreateLaser(Vector3 end)
    {
        var lr = Instantiate(laser).GetComponent<LineRenderer>();
        lr.SetPositions(new[] { muzzle.position, end });
        StartCoroutine(FadeLaser(lr));
    }

    private IEnumerator FadeLaser(LineRenderer lr)
    {
        float alpha = 1f;
        while (alpha > 0f)
        {
            alpha -= Time.deltaTime / fadeDuration;

            var start = lr.startColor;
            var end = lr.endColor;
            lr.startColor = new Color(start.r, start.g, start.b, alpha);
            lr.endColor = new Color(end.r, end.g, end.b, alpha);

            yield return null;
        }
    }
}