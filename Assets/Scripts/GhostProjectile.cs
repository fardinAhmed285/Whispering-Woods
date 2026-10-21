using UnityEngine;

public class GhostProjectile : MonoBehaviour
{
    public float speed = 6f;
    public float damage = 10f;
    public float lifetime = 5f;

    private Transform target;
    private Vector3 targetPoint;
    private bool launched;


    public void Launch(Transform player)
    {
        target = player;
        targetPoint = player.position + Vector3.up;
        launched = true;

        // Start the particle effect when the projectile is fired.
        ParticleSystem[] particles =
            GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem ps in particles)
        {
            ps.Play(true);
        }

        Destroy(gameObject, lifetime);
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
            HitPlayer();
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

        PlayerHealth health =
            other.GetComponentInParent<PlayerHealth>();

        if (health != null)
        {
            HitPlayer();
        }
    }

    void HitPlayer()
    {
        if (!launched)
            return;

        launched = false;

        if (target != null)
        {
            PlayerHealth health =
                target.GetComponentInParent<PlayerHealth>();

            if (health != null)
                health.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}
