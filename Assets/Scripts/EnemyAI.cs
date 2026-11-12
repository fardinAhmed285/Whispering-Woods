using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    public enum EnemyState
    {
        Patrol,
        Chase,
        Attack,
        Flee
    }

    [Header("Target")]
    public Transform player;
    public string playerTag = "Player";

    [Header("Campfire Orbit")]
    public Transform patrolCenter;
    public float orbitRadius = 3f;
    public int orbitPoints = 8;

    [Header("Campfire Fear")]
    [Tooltip("Ghosts based at a campfire disappear when it is lit.")]
    public bool vanishWhenCampfireLit = true;
    [Tooltip("Seconds before the ghost disappears.")]
    public float fleeDuration = 3f;
    public float fleeDistance = 15f;

    [Header("Detection")]
    public float detectionRange = 8f;
    public float loseTargetRange = 12f;
    [Tooltip("Seconds the ghost keeps chasing after the player gets out of range.")]
    public float chaseMemory = 4f;

    [Header("Movement")]
    public float patrolWaitTime = 1f;
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
    private int currentOrbitPoint;

    private float fleeTimer;
    private float lostTargetTimer;
    private Vector3 originalScale;

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

        patrolOrigin = patrolCenter != null
            ? patrolCenter.position
            : transform.position;

        originalScale = transform.localScale;

        ChoosePatrolDestination();
    }

    void Update()
    {
        // Its base is now inside a lit campfire: flee
        if (vanishWhenCampfireLit &&
            currentState != EnemyState.Flee &&
            CampfireInteraction.IsInsideLitCampfire(patrolOrigin))
        {
            StartFleeing();
        }

        if (currentState == EnemyState.Flee)
        {
            UpdateFlee();
            UpdateMovementAnimation();
            return;
        }

        if (player == null)
        {
            FindPlayer();
            UpdateMovementAnimation();
            return;
        }

        float distance = Vector3.Distance(
            transform.position, player.position);

        attackTimer -= Time.deltaTime;

        // Player safe at a lit campfire: stop chasing
        bool playerIsSafe =
            CampfireInteraction.IsInsideLitCampfire(player.position);

        if (playerIsSafe && currentState != EnemyState.Patrol)
        {
            currentState = EnemyState.Patrol;
            agent.isStopped = false;
            ChoosePatrolDestination();
        }

        switch (currentState)
        {
            case EnemyState.Patrol:
                if (distance <= detectionRange && !playerIsSafe)
                {
                    lostTargetTimer = 0f;
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
                    lostTargetTimer += Time.deltaTime;
                else
                    lostTargetTimer = 0f;

                if (lostTargetTimer >= chaseMemory)
                {
                    lostTargetTimer = 0f;
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

    void StartFleeing()
    {
        currentState = EnemyState.Flee;
        fleeTimer = 0f;

        Vector3 away = transform.position - patrolOrigin;
        away.y = 0f;

        if (away.sqrMagnitude < 0.01f)
            away = Random.insideUnitSphere;

        away.y = 0f;

        Vector3 fleeTarget =
            transform.position + away.normalized * fleeDistance;

        agent.isStopped = false;

        if (NavMesh.SamplePosition(
            fleeTarget, out NavMeshHit hit, 5f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }

        Debug.Log(name + " flees the campfire light!");
    }

    void UpdateFlee()
    {
        fleeTimer += Time.deltaTime;

        // Shrink, then disappear
        float fadeStart = Mathf.Max(0f, fleeDuration - 1f);

        if (fleeTimer > fadeStart)
        {
            float t = Mathf.Clamp01(
                (fleeTimer - fadeStart) / Mathf.Max(0.01f, fleeDuration - fadeStart));

            transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
        }

        if (fleeTimer >= fleeDuration)
            Destroy(gameObject);
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

        if (orbitPoints < 3)
            orbitPoints = 8;

        float angle = currentOrbitPoint *
            (360f / orbitPoints) * Mathf.Deg2Rad;

        Vector3 center = patrolCenter != null
            ? patrolCenter.position
            : patrolOrigin;

        Vector3 target = center + new Vector3(
            Mathf.Sin(angle) * orbitRadius,
            0f,
            Mathf.Cos(angle) * orbitRadius
        );

        currentOrbitPoint =
            (currentOrbitPoint + 1) % orbitPoints;

        if (NavMesh.SamplePosition(
            target, out NavMeshHit hit, 2f, NavMesh.AllAreas))
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

        FlashRed();

        if (animator != null &&
            !string.IsNullOrEmpty(attackTrigger))
        {
            animator.SetTrigger(attackTrigger);
        }

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
        Gizmos.color = Color.cyan;

        Vector3 center = patrolCenter != null
            ? patrolCenter.position
            : transform.position;

        Gizmos.DrawWireSphere(center, orbitRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, loseTargetRange);
    }
}