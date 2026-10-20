
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
        SeekFood
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
        }
    }

    void UpdateAnimation()
    {
        if (currentState == WolfState.Feed)
            return;

        float movement = agent.velocity.magnitude;
        bool moving = movement > 0.1f;

        bool shouldRun =
            currentState == WolfState.Called &&
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
            agent.speed = runSpeed;
            EnterState(WolfState.Called);
            Debug.Log("Wolf called to player!");
        }
        else
        {
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
        }
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
