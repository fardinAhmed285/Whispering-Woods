
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMagicAttack : MonoBehaviour
{
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float projectileDamage = 10f;
    public float attackCooldown = 0.7f;
    public float attackRange = 30f;

    private float attackTimer;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        attackTimer -= Time.deltaTime;

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame &&
            attackTimer <= 0f)
        {
            Shoot();
        }
    }

    void Shoot()
    {
        if (projectilePrefab == null || firePoint == null)
        {
            Debug.LogWarning(
                "Assign the projectile prefab and fire point.");
            return;
        }

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            return;

        Ray ray = mainCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f));

        Vector3 targetPoint =
            ray.origin + ray.direction * attackRange;

        if (Physics.Raycast(ray, out RaycastHit hit, attackRange))
            targetPoint = hit.point;

        Vector3 direction = targetPoint - firePoint.position;

        GameObject projectile = Instantiate(
            projectilePrefab,
            firePoint.position,
            Quaternion.LookRotation(direction));

        GhostProjectile magic =
            projectile.GetComponent<GhostProjectile>();

        if (magic != null)
        {
            magic.damage = projectileDamage;
            magic.LaunchAtEnemyPoint(targetPoint);
        }

        attackTimer = attackCooldown;
    }
}
