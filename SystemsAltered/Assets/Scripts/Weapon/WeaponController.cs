using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponController : MonoBehaviour
{
    public Camera cam;
    public GameObject bulletPrefab;

    public float bulletSpeed = 50f;

    [Header("HUD")]
    public HUDController hudController;

    private DrugStateData currentState;
    private float nextFireTime;
    private int ammoCount = 30;

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

        GameObject bulletObj = Instantiate(bulletPrefab, cam.transform.position, Quaternion.identity);

        Bullet bullet = bulletObj.GetComponent<Bullet>();
        bullet.speed = bulletSpeed;

        if (currentState != null)
            bullet.damage = damage;

        bullet.Init(direction);

        ammoCount = Mathf.Max(0, ammoCount - 1);

        // Report real ammo to HUD (HUDController handles corruption display)
        if (hudController != null)
            hudController.SetRealAmmo(ammoCount);

        ApplyRecoil();
    }

    void ApplyRecoil()
    {
        if (currentState == null) return;

        cam.transform.localRotation *= Quaternion.Euler(-currentState.recoil, 0f, 0f);
    }
}