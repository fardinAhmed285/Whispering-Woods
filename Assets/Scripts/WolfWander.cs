using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class WolfWander : MonoBehaviour
{
    public enum WolfState
    {
        Wander,
        Rest,
        Feed,
        Called,
        Follow,
        SeekFood,
        Attack
    }

    [Header("State")]
    public WolfState currentState = WolfState.Wander;

    [Header("Navigation")]
    public float wanderRadius = 12f;
    public float wanderWaitTime = 3f;
    public float followDistance = 2.5f;
    public float destinationUpdateRate = 0.35f;

    [Header("Player")]
    public Transform player;
    public float feedingDistance = 3f;
    public float callDistance = 2.5f;

    [Header("Trust")]
    public int trustLevel = 0;
    public int trustRequiredToFollow = 3;

    [Header("World Food")]
    public float foodSearchRadius = 12f;
    public float foodEatDistance = 1.5f;
    public float foodSearchInterval = 2f;
    public float worldFoodBiteDuration = 2f;
    public float hunger = 60f;
    public float hungerDecreaseRate = 1f;
    public float hungerThreshold = 35f;
    public float hungerRestoredByFood = 50f;

    [Header("Animation")]
    public string speedParameter = "Speed";
    public string runningParameter = "IsRunning";
    public string biteParameter = "Bite";
    public float biteDuration = 2f;

    [Header("Movement Speeds")]
    public float walkSpeed = 2f;
    public float runSpeed = 5f;

    [Header("Call")]
    [Tooltip("Teleports when called from farther than this. 0 = never.")]
    public float teleportDistance = 25f;

    [Header("Combat (companion only)")]
    [Tooltip("Off: the wolf only attacks ghosts you point at (right click). On: it also attacks nearby ghosts on its own.")]
    public bool autoAttack = false;
    [Tooltip("Max distance from the player when chasing a ghost you pointed at.")]
    public float commandMaxDistance = 40f;
    [Tooltip("Attacks ghosts this close to the player.")]
    public float enemyDetectionRange = 12f;
    [Tooltip("Stops fighting this far from the player.")]
    public float maxDistanceFromPlayer = 15f;
    public float biteRange = 2f;
    public float biteDamage = 8f;
    public float biteCooldown = 2f;
    public float enemySearchInterval = 0.5f;

    private NavMeshAgent agent;
    private Animator animator;
    private WolfFoodInventory foodInventory;

    private float stateTimer;
    private float destinationTimer;
    private float foodSearchTimer;

    private bool initialized;
    private bool companionActive;
    private bool eatingWorldFood;

    private GameObject foodTarget;

    private EnemyHealth enemyTarget;
    private bool commandedAttack;
    private WolfHealth health;

    public bool IsCompanion => companionActive;
    private float enemySearchTimer;
    private float biteTimer;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();

        if (agent == null || animator == null)
        {
            Debug.LogError(
                "Wolf needs a NavMeshAgent and Animator.", this);
            enabled = false;
            return;
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogError(
                "Wolf is not on the baked NavMesh.", this);
            enabled = false;
            return;
        }

        health = GetComponent<WolfHealth>();

        if (health == null)
            health = gameObject.AddComponent<WolfHealth>();

        if (player != null)
        {
            foodInventory =
                player.GetComponentInParent<WolfFoodInventory>();
        }

        initialized = true;
        agent.speed = walkSpeed;

        animator.SetFloat(speedParameter, 0f);
        animator.SetBool(runningParameter, false);

        EnterState(WolfState.Wander);
    }

    void Update()
    {
        if (!initialized || !agent.isOnNavMesh)
            return;

        hunger = Mathf.Clamp(
            hunger - hungerDecreaseRate * Time.deltaTime,
            0f, 100f);

        if (Keyboard.current != null &&
            Keyboard.current.hKey.wasPressedThisFrame &&
            player != null &&
            trustLevel >= trustRequiredToFollow &&
            currentState != WolfState.SeekFood &&
            !(currentState == WolfState.Feed && eatingWorldFood))
        {
            ToggleCompanion();
        }

        if (currentState != WolfState.Feed &&
            currentState != WolfState.SeekFood &&
            currentState != WolfState.Attack &&
            player != null &&
            Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame &&
            Vector3.Distance(transform.position, player.position)
                <= feedingDistance)
        {
            StartFeeding();
            return;
        }

        // Search for world food while wandering or resting.
        if (!companionActive &&
            hunger <= hungerThreshold &&
            (currentState == WolfState.Wander ||
             currentState == WolfState.Rest))
        {
            foodSearchTimer += Time.deltaTime;

            if (foodSearchTimer >= foodSearchInterval)
            {
                foodSearchTimer = 0f;

                if (FindNearbyFood())
                    return;
            }
        }

        // Companion: look for ghosts near the player
        biteTimer -= Time.deltaTime;

        if (autoAttack &&
            companionActive &&
            (health == null || !health.IsWounded) &&
            (currentState == WolfState.Follow ||
             currentState == WolfState.Called))
        {
            enemySearchTimer += Time.deltaTime;

            if (enemySearchTimer >= enemySearchInterval)
            {
                enemySearchTimer = 0f;

                EnemyHealth enemy = FindNearestEnemy();

                if (enemy != null)
                {
                    enemyTarget = enemy;
                    commandedAttack = false;
                    EnterState(WolfState.Attack);
                    Debug.Log("Wolf is attacking " + enemy.name + "!");
                }
            }
        }

        UpdateAnimation();

        switch (currentState)
        {
            case WolfState.Wander:
                UpdateWander();
                break;

            case WolfState.Rest:
                UpdateRest();
                break;

            case WolfState.Feed:
                UpdateFeed();
                break;

            case WolfState.Called:
                UpdateCalled();
                break;

            case WolfState.Follow:
                UpdateFollow();
                break;

            case WolfState.SeekFood:
                UpdateSeekFood();
                break;

            case WolfState.Attack:
                UpdateAttack();
                break;
        }
    }

    void UpdateAnimation()
    {
        if (currentState == WolfState.Feed)
            return;

        float movement = agent.velocity.magnitude;
        bool moving = movement > 0.1f;

        bool shouldRun =
            (currentState == WolfState.Called ||
             currentState == WolfState.Attack) &&
            companionActive &&
            moving;

        animator.SetFloat(
            speedParameter, moving ? 1f : 0f);

        animator.SetBool(runningParameter, shouldRun);
    }

    void ToggleCompanion()
    {
        companionActive = !companionActive;

        if (companionActive)
        {
            foodTarget = null;
            TeleportNearPlayerIfFar();
            agent.speed = runSpeed;
            EnterState(WolfState.Called);
            Debug.Log("Wolf called to player!");
        }
        else
        {
            enemyTarget = null;
            agent.speed = walkSpeed;
            EnterState(WolfState.Wander);
            Debug.Log("Wolf released to wander.");
        }
    }

    void EnterState(WolfState newState)
    {
        currentState = newState;
        stateTimer = 0f;
        destinationTimer = destinationUpdateRate;

        switch (newState)
        {
            case WolfState.Wander:
                agent.isStopped = false;
                agent.speed = walkSpeed;
                animator.SetBool(runningParameter, false);
                ChooseWanderDestination();
                break;

            case WolfState.Rest:
                agent.isStopped = true;
                agent.ResetPath();
                animator.SetFloat(speedParameter, 0f);
                animator.SetBool(runningParameter, false);
                break;

            case WolfState.Feed:
                agent.isStopped = true;
                agent.ResetPath();
                animator.SetFloat(speedParameter, 0f);
                animator.SetBool(runningParameter, false);
                animator.ResetTrigger(biteParameter);
                animator.SetTrigger(biteParameter);
                break;

            case WolfState.Called:
                agent.isStopped = false;
                agent.speed = runSpeed;
                animator.SetBool(runningParameter, true);
                break;

            case WolfState.Follow:
                agent.isStopped = false;
                agent.speed = walkSpeed;
                animator.SetBool(runningParameter, false);
                break;

            case WolfState.SeekFood:
                agent.isStopped = false;
                agent.speed = walkSpeed;
                animator.SetBool(runningParameter, false);
                break;

            case WolfState.Attack:
                agent.isStopped = false;
                agent.speed = runSpeed;
                animator.SetBool(runningParameter, true);
                break;
        }
    }

    void TeleportNearPlayerIfFar()
    {
        if (player == null || teleportDistance <= 0f)
            return;

        if (FlatDistance(transform.position, player.position)
                <= teleportDistance)
            return;

        Vector3 behindPlayer = player.position - player.forward * 3f;

        if (NavMesh.SamplePosition(
            behindPlayer, out NavMeshHit hit, 5f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
            transform.rotation = Quaternion.LookRotation(
                Vector3.ProjectOnPlane(player.forward, Vector3.up));

            Debug.Log("Wolf was far away and teleported to you.");
        }
    }

    EnemyHealth FindNearestEnemy()
    {
        if (player == null)
            return null;

        EnemyHealth[] enemies =
            FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);

        EnemyHealth closest = null;
        float closestDistance = enemyDetectionRange;

        foreach (EnemyHealth enemy in enemies)
        {
            if (enemy == null || enemy.currentHealth <= 0f)
                continue;

            // Ghosts close to the player or to the wolf itself
            float distance = Mathf.Min(
                FlatDistance(enemy.transform.position, player.position),
                FlatDistance(enemy.transform.position, transform.position));

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = enemy;
            }
        }

        return closest;
    }

    void UpdateAttack()
    {
        if (enemyTarget == null || !companionActive || player == null)
        {
            StopAttacking();
            return;
        }

        // Badly hurt: retreat to the player
        if (health != null && health.IsWounded)
        {
            Debug.Log(name + " is hurt and retreats to you.");
            StopAttacking();
            return;
        }

        // Too far from the player
        float leash = commandedAttack ? commandMaxDistance : maxDistanceFromPlayer;

        if (FlatDistance(transform.position, player.position) > leash)
        {
            Debug.Log("Wolf breaks off the fight to stay with you.");
            StopAttacking();
            return;
        }

        Vector3 enemyPosition = enemyTarget.transform.position;
        float distance = FlatDistance(transform.position, enemyPosition);

        if (distance > biteRange)
        {
            destinationTimer += Time.deltaTime;

            if (destinationTimer >= destinationUpdateRate)
            {
                destinationTimer = 0f;
                agent.isStopped = false;
                agent.speed = runSpeed;
                agent.SetDestination(enemyPosition);
            }

            return;
        }

        // In range: bite
        agent.isStopped = true;
        agent.ResetPath();

        Vector3 lookDirection = enemyPosition - transform.position;
        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(lookDirection);

        if (biteTimer > 0f)
            return;

        biteTimer = biteCooldown;

        animator.ResetTrigger(biteParameter);
        animator.SetTrigger(biteParameter);

        enemyTarget.TakeDamage(biteDamage);
    }

    // Called by WolfCommand when the player points at a ghost.
    public bool CommandAttack(EnemyHealth enemy)
    {
        if (!companionActive || enemy == null)
            return false;

        if (health != null && health.IsWounded)
        {
            Debug.Log(name + " is too hurt to fight.");
            return false;
        }

        enemyTarget = enemy;
        commandedAttack = true;
        EnterState(WolfState.Attack);

        Debug.Log(name + " attacks " + enemy.name + " on your command!");
        return true;
    }

    public void KnockOut()
    {
        Debug.Log(name + " is exhausted and runs away. Feed it again to tame it.");

        companionActive = false;
        trustLevel = 0;
        enemyTarget = null;
        commandedAttack = false;
        foodTarget = null;
        eatingWorldFood = false;
        EnterState(WolfState.Wander);
    }

    void StopAttacking()
    {
        enemyTarget = null;
        commandedAttack = false;

        if (companionActive)
            EnterState(WolfState.Called);
        else
            EnterState(WolfState.Wander);
    }

    float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    void OnDrawGizmosSelected()
    {
        if (player == null)
            return;

        // Attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(player.position, enemyDetectionRange);

        // Max distance from player
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(player.position, maxDistanceFromPlayer);
    }

    bool FindNearbyFood()
    {
        GameObject[] fruits;

        try
        {
            fruits = GameObject.FindGameObjectsWithTag("Fruit");
        }
        catch (UnityException)
        {
            Debug.LogError("Create the Fruit tag in Unity.");
            return false;
        }

        float closestDistance = foodSearchRadius;
        GameObject closestFruit = null;

        foreach (GameObject fruit in fruits)
        {
            if (fruit == null)
                continue;

            float distance = Vector3.Distance(
                transform.position, fruit.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestFruit = fruit;
            }
        }

        if (closestFruit == null)
            return false;

        foodTarget = closestFruit;
        EnterState(WolfState.SeekFood);

        agent.SetDestination(foodTarget.transform.position);

        Debug.Log("Wolf found fruit and is moving towards it.");
        return true;
    }

    void UpdateSeekFood()
    {
        if (companionActive)
        {
            foodTarget = null;
            EnterState(WolfState.Called);
            return;
        }

        if (foodTarget == null)
        {
            EnterState(WolfState.Wander);
            return;
        }

        destinationTimer += Time.deltaTime;

        if (destinationTimer >= destinationUpdateRate)
        {
            destinationTimer = 0f;

            agent.isStopped = false;
            agent.SetDestination(foodTarget.transform.position);
        }

        float distance = Vector3.Distance(
            transform.position, foodTarget.transform.position);

        if (distance <= foodEatDistance)
        {
            eatingWorldFood = true;
            EnterState(WolfState.Feed);
        }
        else if (!agent.pathPending &&
                 agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            foodTarget = null;
            EnterState(WolfState.Wander);
        }
    }

    void UpdateWander()
    {
        if (ReachedDestination())
            EnterState(WolfState.Rest);
    }

    void UpdateRest()
    {
        stateTimer += Time.deltaTime;

        if (stateTimer >= wanderWaitTime)
            EnterState(WolfState.Wander);
    }

    void StartFeeding()
    {
        eatingWorldFood = false;

        if (foodInventory != null)
        {
            if (!foodInventory.TryConsumeBerry())
            {
                Debug.Log("You need a berry to feed the wolf.");
                return;
            }
        }

        companionActive = false;
        EnterState(WolfState.Feed);
    }

    void UpdateFeed()
    {
        stateTimer += Time.deltaTime;

        float duration = eatingWorldFood
            ? worldFoodBiteDuration
            : biteDuration;

        if (stateTimer < duration)
            return;

        if (eatingWorldFood)
        {
            if (foodTarget != null)
                Destroy(foodTarget);

            foodTarget = null;
            hunger = Mathf.Clamp(
                hunger + hungerRestoredByFood, 0f, 100f);

            eatingWorldFood = false;

            Debug.Log("Wolf ate wild fruit. Hunger: " + hunger);

            EnterState(WolfState.Rest);
            return;
        }

        trustLevel++;

        Debug.Log("Wolf trust: " + trustLevel);

        if (trustLevel >= trustRequiredToFollow)
            Debug.Log("Wolf trusts you! Press H to call it.");

        EnterState(WolfState.Rest);
    }

    void UpdateCalled()
    {
        if (player == null)
            return;

        destinationTimer += Time.deltaTime;

        if (destinationTimer >= destinationUpdateRate)
        {
            destinationTimer = 0f;

            float distance = Vector3.Distance(
                transform.position, player.position);

            if (distance > callDistance)
            {
                agent.isStopped = false;
                agent.speed = runSpeed;
                agent.SetDestination(player.position);
            }
            else
            {
                EnterState(WolfState.Follow);
            }
        }
    }

    void UpdateFollow()
    {
        if (player == null)
            return;

        destinationTimer += Time.deltaTime;

        if (destinationTimer >= destinationUpdateRate)
        {
            destinationTimer = 0f;

            float distance = Vector3.Distance(
                transform.position, player.position);

            if (distance > followDistance)
            {
                agent.isStopped = false;
                agent.speed = walkSpeed;
                agent.SetDestination(player.position);
            }
            else
            {
                agent.isStopped = true;
                agent.ResetPath();
                animator.SetFloat(speedParameter, 0f);
                animator.SetBool(runningParameter, false);
            }
        }
    }

    bool ReachedDestination()
    {
        return !agent.pathPending &&
               agent.hasPath &&
               agent.remainingDistance <= agent.stoppingDistance;
    }

    void ChooseWanderDestination()
    {
        if (!agent.isOnNavMesh)
            return;

        Vector3 randomPoint =
            transform.position +
            Random.insideUnitSphere * wanderRadius;

        if (NavMesh.SamplePosition(
            randomPoint, out NavMeshHit hit,
            wanderRadius, NavMesh.AllAreas))
        {
            agent.isStopped = false;
            agent.speed = walkSpeed;
            agent.SetDestination(hit.position);
        }
        else
        {
            EnterState(WolfState.Rest);
        }
    }
}