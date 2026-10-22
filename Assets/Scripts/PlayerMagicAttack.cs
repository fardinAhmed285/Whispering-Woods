
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMagicAttack : MonoBehaviour
{
    [Header("Projectile")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float projectileDamage = 10f;
    public float projectileSpeed = 12f;
    public float projectileLifetime = 10f;

    [Header("Combat")]
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
        if (attackTimer > 0f)
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
                "PlayerMagicAttack: Assign Projectile Prefab and Fire Point.",
                this);
            return;
        }

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogWarning("PlayerMagicAttack: No Main Camera found.");
            return;
        }

        // Aim through the centre of the screen.
        Ray ray = mainCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f));

        Vector3 targetPoint = ray.origin + ray.direction * attackRange;

        // Ignore colliders belonging to the player.
        RaycastHit[] hits = Physics.RaycastAll(ray, attackRange);

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.transform == transform ||
                hit.transform.IsChildOf(transform))
            {
                continue;
            }

            targetPoint = hit.point;
            break;
        }

        // Create and aim the projectile without rotating the player.
        Vector3 direction = targetPoint - firePoint.position;

        if (direction.sqrMagnitude < 0.001f)
            return;

        GameObject projectile = Instantiate(
            projectilePrefab,
            firePoint.position,
            Quaternion.LookRotation(direction.normalized));

        GhostProjectile magic =
            projectile.GetComponent<GhostProjectile>();

        if (magic == null)
        {
            Debug.LogError(
                "Projectile prefab is missing GhostProjectile.cs.",
                projectile);

            Destroy(projectile);
            return;
        }

        magic.damage = projectileDamage;
        magic.speed = projectileSpeed;
        magic.lifetime = projectileLifetime;
        magic.LaunchAtEnemyPoint(targetPoint);

        attackTimer = attackCooldown;
    }
}
