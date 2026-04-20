using UnityEngine;
using System.Collections;
using DG.Tweening;

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
    [SerializeField] private GameObject gunModel;
    [SerializeField] private GameObject laser;
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("References")]
    [SerializeField] private HUDController hudController;
    [SerializeField] private DrugStateController drugState;

    private Transform cam;
    private bool requestedShoot;
    private bool requestedReload;
    private float nextFireTime;
    private int currentAmmo;
    private bool isReloading;

    // --- Multiplier accessors ---

    private float FireRateMult =>
        drugState?.CurrentState?.fireRateMultiplier ?? 1f;

    private float SpreadMult =>
        drugState?.CurrentState?.spreadMultiplier ?? 1f;

    // --- Lifecycle ---

    public void Initialize()
    {
        cam = Camera.main.transform;
        currentAmmo = magSize;
    }

    public void UpdateInput(GunInput input)
    {
        requestedShoot = input.Shoot;
        requestedReload = input.Reload;
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
        if (!requestedShoot || Time.time < nextFireTime) return;

        Shoot();
        nextFireTime = Time.time + 1f / (fireRate * FireRateMult);
    }

    private void Shoot()
    {
        if (isReloading) return;

        currentAmmo--;

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

        if (Physics.Raycast(cam.position, direction, out RaycastHit hit, range))
        {
            var enemy = hit.collider.GetComponent<EnemyAI>();
            if (enemy != null)
                enemy.TakeDamage(damage, hit.point, hit.normal);

            CreateLaser(hit.point);
        }
        else
        {
            CreateLaser(cam.position + direction * range);
        }
    }

    private Vector3 GetShootingDirection()
    {
        var inaccuracy = inaccuracyDistance * SpreadMult;

        var targetPos = cam.position + cam.forward * range;
        targetPos += new Vector3(
            Random.Range(-inaccuracy, inaccuracy),
            Random.Range(-inaccuracy, inaccuracy),
            Random.Range(-inaccuracy, inaccuracy)
        );

        return (targetPos - cam.position).normalized;
    }

    // --- Reloading ---

    private void HandleReload()
    {
        if (requestedReload && !isReloading && CanReload())
            StartCoroutine(Reload());

        requestedReload = false;
    }

    private IEnumerator Reload()
    {
        if (!CanReload()) yield break;

        isReloading = true;
        gunModel.transform.DOLocalRotate(new Vector3(360, 0, 0), reloadTime, RotateMode.FastBeyond360).SetEase(Ease.Linear);
        yield return new WaitForSeconds(reloadTime);

        currentAmmo = magSize;
        isReloading = false;
        requestedReload = false;
    }

    // --- Queries ---

    private bool CanShoot() => currentAmmo > 0;
    private bool CanReload() => currentAmmo < magSize && !isReloading;

    // --- UI ---

    private void UpdateUI()
    {
        hudController.SetRealAmmo(currentAmmo);
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