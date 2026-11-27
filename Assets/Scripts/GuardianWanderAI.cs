
using UnityEngine;
using UnityEngine.AI;

public class GuardianWanderAI : MonoBehaviour
{
    private enum GuardianState
    {
        Wander,
        Idle,
        Talking
    }

    [Header("Wandering")]
    public float wanderRadius = 15f;
    public float waitTime = 3f;

    private NavMeshAgent agent;
    private Animator animator;
    private GuardianState currentState;

    private Vector3 startingPosition;
    private float idleTimer;

    private Transform player;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        startingPosition = transform.position;

        currentState = GuardianState.Wander;

        ChooseNewDestination();
    }

    private void Update()
    {
        if (animator != null && agent != null)
        {
            float speed = agent.velocity.magnitude;
            animator.SetFloat("Speed", speed);
        }

        switch (currentState)
        {
            case GuardianState.Wander:
                HandleWandering();
                break;

            case GuardianState.Idle:
                HandleIdle();
                break;

            case GuardianState.Talking:
                FacePlayer();
                break;
        }
    }

    private void HandleWandering()
    {
        if (agent.pathPending)
            return;

        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            currentState = GuardianState.Idle;
            idleTimer = waitTime;
            agent.isStopped = true;
        }
    }

    private void HandleIdle()
    {
        idleTimer -= Time.deltaTime;

        if (idleTimer <= 0f)
        {
            agent.isStopped = false;
            currentState = GuardianState.Wander;
            ChooseNewDestination();
        }
    }

    private void ChooseNewDestination()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 randomPoint = startingPosition +
                Random.insideUnitSphere * wanderRadius;

            if (NavMesh.SamplePosition(
                randomPoint,
                out NavMeshHit hit,
                wanderRadius,
                NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                return;
            }
        }
    }

    public void StartTalking(Transform playerTransform)
    {
        player = playerTransform;

        currentState = GuardianState.Talking;

        agent.isStopped = true;
        agent.ResetPath();
    }

    public void EndTalking()
    {
        player = null;

        agent.isStopped = false;

        currentState = GuardianState.Wander;

        ChooseNewDestination();
    }

    private void FacePlayer()
    {
        if (player == null)
            return;

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                5f * Time.deltaTime
            );
        }
    }
}
