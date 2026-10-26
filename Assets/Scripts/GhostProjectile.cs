
using UnityEngine;

public class GhostProjectile : MonoBehaviour
{
    public float speed = 8f;
    public float damage = 10f;
    public float lifetime = 10f;

    private Transform target;
    private Vector3 targetPoint;
    private bool launched;
    private bool firedByPlayer;
    private bool hasHit;

    public void Launch(Transform player)
    {
        target = player;
        targetPoint = player.position + Vector3.up;
        firedByPlayer = false;
        launched = true;

        Destroy(gameObject, lifetime);
    }

    public void LaunchAtEnemyPoint(Vector3 point)
    {
        target = null;
        targetPoint = point;
        firedByPlayer = true;
        launched = true;

        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (!launched || hasHit)
            return;

        // Ghost shots fade away once the player is resting at a campfire
        // (they home in on the player, so they would hit them otherwise).
        if (!firedByPlayer && Campfire.PlayerIsSafe)
        {
            Destroy(gameObject);
            return;
        }

        if (target != null)
            targetPoint = target.position + Vector3.up;

        Vector3 direction = targetPoint - transform.position;

        if (direction.sqrMagnitude <= 0.16f)
        {
            HitTarget();
            return;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPoint,
            speed * Time.deltaTime
        );

        if (direction.sqrMagnitude > 0.001f)
            transform.forward = direction.normalized;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!launched || hasHit)
            return;

        if (firedByPlayer)
        {
            EnemyHealth health =
                other.GetComponentInParent<EnemyHealth>();

            if (health != null)
            {
                health.TakeDamage(damage);
                Destroy(gameObject);
            }
        }
        else
        {
            PlayerHealth health =
                other.GetComponentInParent<PlayerHealth>();

            if (health != null)
            {
                health.TakeDamage(damage);
                Destroy(gameObject);
            }
        }
    }

    void HitTarget()
    {
        if (hasHit)
            return;

        hasHit = true;
        launched = false;

        if (target != null)
        {
            if (firedByPlayer)
            {
                EnemyHealth health =
                    target.GetComponentInParent<EnemyHealth>();

                if (health != null)
                    health.TakeDamage(damage);
            }
            else
            {
                PlayerHealth health =
                    target.GetComponentInParent<PlayerHealth>();

                if (health != null)
                    health.TakeDamage(damage);
            }
        }

        Destroy(gameObject);
    }
}
