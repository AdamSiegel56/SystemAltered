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
    private Transform cam;

    [SerializeField] private float range = 50f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float fireRate = 10f;
    [SerializeField] private int bulletsPerShot = 6;
    [SerializeField] private bool isPelletWeapon;
    [SerializeField] private Transform muzzle;
    [SerializeField] private float inaccuracyDistance = 5f;

    [SerializeField] private GameObject gunModel;
    [SerializeField] private float reloadTime;
    
    [SerializeField] private GameObject laser;
    [SerializeField] private float fadeDuration = 0.5f;

    private bool requestedShoot;
    private bool requestedReload;
    private float nextFireTime;
    
    private int currentAmmo;
    [SerializeField] private int magSize;
    [SerializeField] private HUDController hudController;
    
    private bool isReloading;
    

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

    private void HandleShooting()
    {
        if (requestedShoot && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + 1f / fireRate;
        }
    }

    private void Shoot()
    {
        if (isReloading) return;
        
        currentAmmo--;
        
        if (CanShoot() && !isReloading)
        {
            if (isPelletWeapon)
            {
                for (int i = 0; i < bulletsPerShot; i++)
                {
                    RaycastHit hit;
                    if (Physics.Raycast(cam.position, GetShootingDirection(), out hit, range))
                    {
                        EnemyAI enemy = hit.collider.GetComponent<EnemyAI>();
                        if (enemy != null)
                        {
                            enemy.TakeDamage(damage, hit.point, hit.normal);
                        }
                        CreateLaser(hit.point);
                    }
                    else
                    {
                        CreateLaser(cam.position + GetShootingDirection() * range);
                    }
                }
            }
            else
            {
                RaycastHit hit;
                if (Physics.Raycast(cam.position, GetShootingDirection(), out hit, range))
                {
                    EnemyAI enemy = hit.collider.GetComponent<EnemyAI>();
                    if (enemy != null)
                    {
                        enemy.TakeDamage(damage, hit.point, hit.normal);
                    }
                    CreateLaser(hit.point);
                }
                else
                {
                    CreateLaser(cam.position + GetShootingDirection() * range);
                }
            }
        }
        else if (!isReloading && CanReload())
        {
            StartCoroutine(Reload());
        }

    }

    IEnumerator Reload()
    {
        if (!CanReload()) yield break;

        isReloading = true;
        
        gunModel.transform.DOLocalRotate(new Vector3(360, 0, 0), reloadTime, RotateMode.FastBeyond360).SetEase(Ease.Linear);
        yield return new WaitForSeconds(reloadTime);

        currentAmmo = magSize;
        
        isReloading = false;
        requestedReload = false;
    }
    
    private void HandleReload()
    {
        if (requestedReload && !isReloading && CanReload())
        {
            StartCoroutine(Reload());
        }

        requestedReload = false;
    }

    private void Update()
    {
        HandleReload();
        HandleShooting();
        UpdateUI();
    }

    bool CanShoot()
    {
        if (currentAmmo > 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    bool CanReload()
    {
        return currentAmmo < magSize && !isReloading;
    }
    
    void UpdateUI()
    {
        hudController.SetRealAmmo(currentAmmo);
    }

    Vector3 GetShootingDirection()
    {
        Vector3 targetPos = cam.position + cam.forward * range;
        
        targetPos = new Vector3
        (
            targetPos.x + Random.Range(-inaccuracyDistance, inaccuracyDistance),
            targetPos.y + Random.Range(-inaccuracyDistance, inaccuracyDistance),
            targetPos.z + Random.Range(-inaccuracyDistance, inaccuracyDistance)
        );
        
        Vector3 direction = targetPos - cam.position;
        return direction.normalized;
    }

    void CreateLaser(Vector3 end)
    {
        LineRenderer lr = Instantiate(laser).GetComponent<LineRenderer>();
        lr.SetPositions(new Vector3[2]  { muzzle.position, end } );
        StartCoroutine(FadeLaser(lr));
    }
    
    IEnumerator FadeLaser(LineRenderer lr)
    {
        float alpha = 1;
        while (alpha > 0)
        {
            alpha -= Time.deltaTime / fadeDuration;
            lr.startColor = new Color(lr.startColor.r,lr.startColor.g,lr.startColor.b,alpha);
            lr.endColor = new Color(lr.endColor.r,lr.endColor.g,lr.endColor.b,alpha);
            yield return null;
        }
    }
    
}