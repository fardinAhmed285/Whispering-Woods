
using UnityEngine;

public class GhostProjectile : MonoBehaviour
{
    public float speed = 4f;
    public float damage = 10f;
    public float lifetime = 10f;

    private Transform target;
    private Vector3 targetPoint;
    private bool launched;
    private bool firedByPlayer;

    public void Launch(Transform player)
    {
        target = player;
        targetPoint = player.position + Vector3.up;
        launched = true;
        firedByPlayer = false;

        PlayParticles();
        Destroy(gameObject, lifetime);
    }

    public void LaunchAtEnemy(Transform enemy)
    {
        target = enemy;
        targetPoint = enemy.position + Vector3.up;
        launched = true;
        firedByPlayer = true;

        PlayParticles();
        Destroy(gameObject, lifetime);
    }

    void PlayParticles()
    {
        ParticleSystem[] particles =
            GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem ps in particles)
        {
            ps.Play(true);
        }
    }

    void Update()
    {
        if (!launched)
            return;

        if (target != null)
            targetPoint = target.position + Vector3.up;

        Vector3 direction = targetPoint - transform.position;

        if (direction.sqrMagnitude < 0.25f)
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
        if (!launched)
            return;

        if (firedByPlayer)
        {
            EnemyHealth health =
                other.GetComponentInParent<EnemyHealth>();

            if (health != null)
            {
                health.TakeDamage(damage);
                DestroyProjectile();
            }
        }
        else
        {
            PlayerHealth health =
                other.GetComponentInParent<PlayerHealth>();

            if (health != null)
            {
                health.TakeDamage(damage);
                DestroyProjectile();
            }
        }
    }

    void HitTarget()
    {
        if (!launched)
            return;

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

        DestroyProjectile();
    }

    void DestroyProjectile()
    {
        launched = false;
        Destroy(gameObject);
    }
}
