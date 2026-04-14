using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;


public class WeaponController : MonoBehaviour
{
    public Camera cam;
    public GameObject bulletPrefab;

    public float bulletSpeed = 50f;

    [Header("Magazine")]
    public int magazineSize = 30;
    public float reloadTime = 1f;

    [Header("HUD")]
    public HUDController hudController;

    private DrugStateData currentState;
    private float nextFireTime;
    private int currentAmmo;
    private bool isReloading;

    public static event System.Action<int, int> OnAmmoChanged;  // current, max
    public static event System.Action OnReloadStarted;
    public static event System.Action OnReloadFinished;

    void Start()
    {
        currentAmmo = magazineSize;
        UpdateHUD();
    }

    void OnEnable()
    {
        DrugEventBus.OnDrugStateChanged += ApplyState;
    }

    void OnDisable()
    {
        DrugEventBus.OnDrugStateChanged -= ApplyState;
    }

    void ApplyState(DrugStateData state)
    {
        currentState = state;
    }

    public void OnFire(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        TryFire();
    }

    void TryFire()
    {
        if (isReloading) return;

        if (currentAmmo <= 0)
        {
            StartCoroutine(Reload());
            return;
        }

        float fireRate = currentState != null ? currentState.fireRate : 5f;

        if (Time.time < nextFireTime) return;

        nextFireTime = Time.time + (1f / fireRate);

        Fire();
    }

    void Fire()
    {
        float spread = currentState != null ? currentState.spread : 0.01f;
        float damage = 10f;

        Vector3 direction = cam.transform.forward;
        direction += cam.transform.right * Random.Range(-spread, spread);
        direction += cam.transform.up * Random.Range(-spread, spread);
        direction.Normalize();

        // Spawn ahead of camera so bullet clears the player's collider
        Vector3 spawnPos = cam.transform.position + direction * 1.5f;

        GameObject bulletObj = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);

        Bullet bullet = bulletObj.GetComponent<Bullet>();
        bullet.speed = bulletSpeed;

        bullet.damage = damage;

        bullet.Init(direction);

        currentAmmo--;
        UpdateHUD();
        ApplyRecoil();

        if (currentAmmo <= 0)
        {
            StartCoroutine(Reload());
        }
    }

    IEnumerator Reload()
    {
        if (isReloading) yield break;

        isReloading = true;
        OnReloadStarted?.Invoke();

        yield return new WaitForSeconds(reloadTime);

        currentAmmo = magazineSize;
        isReloading = false;

        OnReloadFinished?.Invoke();
        UpdateHUD();
    }

    void UpdateHUD()
    {
        if (hudController != null)
            hudController.SetRealAmmo(currentAmmo);

        OnAmmoChanged?.Invoke(currentAmmo, magazineSize);
    }

    void ApplyRecoil()
    {
        if (currentState == null) return;

        cam.transform.localRotation *= Quaternion.Euler(-currentState.recoil, 0f, 0f);
    }
}
