
using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    public enum EnemyState
    {
        Patrol,
        Chase,
        Attack
    }

    [Header("Target")]
    public Transform player;
    public string playerTag = "Player";

    [Header("Detection")]
    public float detectionRange = 12f;
    public float loseTargetRange = 18f;

    [Header("Movement")]
    public float patrolRadius = 8f;
    public float patrolWaitTime = 3f;
    public float destinationUpdateRate = 0.25f;

    [Header("Attack")]
    public float attackRange = 1.8f;
    public float attackDamage = 10f;
    public float attackCooldown = 1.5f;

    [Header("Projectile")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float projectileDamage = 10f;

    [Header("Attack Effect")]
    public Color attackFlashColor = Color.red;
    public float flashDuration = 0.25f;

    [Header("Animation")]
    public Animator animator;
    public string speedParameter = "Speed";
    public string attackTrigger = "Attack";

    [Header("Debug")]
    public EnemyState currentState = EnemyState.Patrol;

    private NavMeshAgent agent;
    private Vector3 patrolOrigin;
    private float destinationTimer;
    private float patrolTimer;
    private float attackTimer;

    private Renderer[] ghostRenderers;
    private Color[] originalColors;
    private Coroutine flashCoroutine;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (agent == null)
        {
            Debug.LogError("Ghost needs a NavMeshAgent.", this);
            enabled = false;
            return;
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogError("Ghost is not on the NavMesh.", this);
            enabled = false;
            return;
        }

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        CacheRenderers();

        if (player == null)
        {
            GameObject playerObject =
                GameObject.FindGameObjectWithTag(playerTag);

            if (playerObject != null)
                player = playerObject.transform;
        }

        patrolOrigin = transform.position;
        ChoosePatrolDestination();
    }

    void Update()
    {
        if (player == null)
        {
            FindPlayer();
            UpdateMovementAnimation();
            return;
        }

        float distance = Vector3.Distance(
            transform.position, player.position);

        attackTimer -= Time.deltaTime;

        switch (currentState)
        {
            case EnemyState.Patrol:

                if (distance <= detectionRange)
                {
                    currentState = EnemyState.Chase;
                    destinationTimer = destinationUpdateRate;
                }
                else
                {
                    UpdatePatrol();
                }

                break;

            case EnemyState.Chase:

                if (distance > loseTargetRange)
                {
                    currentState = EnemyState.Patrol;
                    ChoosePatrolDestination();
                }
                else if (distance <= attackRange)
                {
                    currentState = EnemyState.Attack;
                    agent.isStopped = true;
                    agent.ResetPath();
                }
                else
                {
                    UpdateChase();
                }

                break;

            case EnemyState.Attack:

                if (distance > attackRange * 1.2f)
                {
                    currentState = EnemyState.Chase;
                    agent.isStopped = false;
                }
                else
                {
                    UpdateAttack();
                }

                break;
        }

        UpdateMovementAnimation();
    }

    void FindPlayer()
    {
        GameObject found =
            GameObject.FindGameObjectWithTag(playerTag);

        if (found != null)
            player = found.transform;
    }

    void UpdatePatrol()
    {
        if (agent.pathPending)
            return;

        if (!agent.hasPath ||
            agent.remainingDistance <= agent.stoppingDistance + 0.1f)
        {
            patrolTimer += Time.deltaTime;

            if (patrolTimer >= patrolWaitTime)
            {
                patrolTimer = 0f;
                ChoosePatrolDestination();
            }
        }
    }

    void ChoosePatrolDestination()
    {
        if (agent == null || !agent.isOnNavMesh)
            return;

        Vector3 randomPoint = patrolOrigin +
            Random.insideUnitSphere * patrolRadius;

        if (NavMesh.SamplePosition(
            randomPoint,
            out NavMeshHit hit,
            patrolRadius,
            NavMesh.AllAreas))
        {
            agent.isStopped = false;
            agent.SetDestination(hit.position);
        }
    }

    void UpdateChase()
    {
        destinationTimer += Time.deltaTime;

        if (destinationTimer >= destinationUpdateRate)
        {
            destinationTimer = 0f;
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
    }

    void UpdateAttack()
    {
        if (attackTimer > 0f)
            return;

        attackTimer = attackCooldown;

        // Keep the existing red attack flash.
        FlashRed();

        // Optional attack animation.
        if (animator != null &&
            !string.IsNullOrEmpty(attackTrigger))
        {
            animator.SetTrigger(attackTrigger);
        }

        // Shoot the projectile toward the player.
        if (projectilePrefab == null || player == null)
        {
            Debug.LogWarning(
                "Assign the projectile prefab in EnemyAI.", this);
            return;
        }

        Vector3 spawnPosition = firePoint != null
            ? firePoint.position
            : transform.position + Vector3.up;

        GameObject projectile = Instantiate(
            projectilePrefab,
            spawnPosition,
            Quaternion.identity
        );

        GhostProjectile projectileScript =
            projectile.GetComponent<GhostProjectile>();

        if (projectileScript != null)
        {
            projectileScript.damage = projectileDamage;
            projectileScript.Launch(player);
        }
        else
        {
            Debug.LogError(
                "The projectile prefab needs GhostProjectile.cs.", this);

            Destroy(projectile);
        }
    }


    void CacheRenderers()
    {
        ghostRenderers = GetComponentsInChildren<Renderer>();

        originalColors = new Color[ghostRenderers.Length];

        for (int i = 0; i < ghostRenderers.Length; i++)
        {
            Material mat = ghostRenderers[i].material;

            if (mat.HasProperty("_BaseColor"))
                originalColors[i] = mat.GetColor("_BaseColor");
            else if (mat.HasProperty("_Color"))
                originalColors[i] = mat.GetColor("_Color");
            else
                originalColors[i] = Color.white;
        }
    }

    void FlashRed()
    {
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(RedFlashEffect());
    }

    IEnumerator RedFlashEffect()
    {
        SetGhostColor(attackFlashColor);

        yield return new WaitForSeconds(flashDuration);

        RestoreGhostColor();

        flashCoroutine = null;
    }

    void SetGhostColor(Color color)
    {
        for (int i = 0; i < ghostRenderers.Length; i++)
        {
            Material[] materials = ghostRenderers[i].materials;

            foreach (Material mat in materials)
            {
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", color);
                else if (mat.HasProperty("_Color"))
                    mat.SetColor("_Color", color);
            }
        }
    }

    void RestoreGhostColor()
    {
        for (int i = 0; i < ghostRenderers.Length; i++)
        {
            Material[] materials = ghostRenderers[i].materials;

            foreach (Material mat in materials)
            {
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", originalColors[i]);
                else if (mat.HasProperty("_Color"))
                    mat.SetColor("_Color", originalColors[i]);
            }
        }
    }

    void UpdateMovementAnimation()
    {
        if (animator == null)
            return;

        // Only update Speed if the Animator has this parameter.
        foreach (AnimatorControllerParameter parameter
                 in animator.parameters)
        {
            if (parameter.name == speedParameter &&
                parameter.type == AnimatorControllerParameterType.Float)
            {
                animator.SetFloat(
                    speedParameter, agent.velocity.magnitude);
                break;
            }
        }
    }



    void OnDrawGizmosSelected()
    {
        // Red circle: range where the ghost detects the player.
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Yellow circle: range where the ghost attacks.
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Orange circle: range where the ghost can lose its target.
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, loseTargetRange);
    }

}
