using System.Collections;
using UnityEngine;

public class FairyBehavior : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player; // Se asigna en Inspector o se busca por Tag "Player"

    [Header("Capas de Detección (LayerMasks)")]
    public LayerMask structureLayer; // Asigna la capa "Estructura"
    public LayerMask enemyLayer;     // Asigna la capa "Enemy"

    [Header("Configuración de Movimiento")]
    public float followDistance = 4f;   // Rango máximo permitido del jugador
    public float moveSpeed = 6f;        // Velocidad de vuelo hacia estructuras/jugador
    public float hoverSpeed = 2f;       // Velocidad del revoloteo suave
    public float hoverRadius = 1.5f;    // Radio de órbita cuando no hay objetivo

    [Header("Configuración de Evasión")]
    public float enemyDetectionRadius = 5f; // Radio para detectar enemigos
    public float evasionStrength = 2.5f;    // Fuerza con la que esquiva al enemigo

    [Header("Configuración de Radar")]
    public float maxStructureDetectionDistance = 25f; // Distancia máxima a la que el hada "huele" estructuras

    private float timeOffset;
    private bool isEvasionActive = false;
    private Coroutine evasionCoroutine;

    void Start()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        timeOffset = Random.Range(0f, 10f);
    }

    void Update()
    {
        if (player == null) return;

        Vector3 currentPos = transform.position;
        Vector3 playerPos = player.position;
        float distanceToPlayer = Vector3.Distance(currentPos, playerPos);

        // --- REGLA 1: SI ESTÁ FUERA DEL RANGO DEL JUGADOR, VOLVER DIRECTAMENTE ---
        if (distanceToPlayer > followDistance)
        {
            Vector3 targetPosition = playerPos + Vector3.up * 2f;
            transform.position = Vector3.MoveTowards(currentPos, targetPosition, moveSpeed * Time.deltaTime);
            return;
        }

        // --- REGLA 2: DENTRO DEL RANGO del JUGADOR ---
        Transform nearestStructure = GetNearestStructure();

        Vector3 moveDirection = Vector3.zero;

        if (nearestStructure != null)
        {
            // Ir hacia la estructura más cercana
            moveDirection = (nearestStructure.position - currentPos).normalized;
        }

        // --- REGLA 3: SI EL ÍTEM ESTÁ ACTIVO, SUMAR FUERZA DE RECHAZO DE ENEMIGOS ---
        if (isEvasionActive)
        {
            Vector3 enemyRepulsion = CalculateEnemyRepulsion();
            // Sumamos el vector a la estructura + la repulsión para rodearlos suavemente
            moveDirection = (moveDirection + (enemyRepulsion * evasionStrength)).normalized;
        }

        // --- SI NO HAY ESTRUCTURA NI ENEMIGOS: REVOLOTEO HABITUAL ---
        if (moveDirection == Vector3.zero)
        {
            float time = Time.time * hoverSpeed + timeOffset;
            float xOffset = Mathf.Cos(time) * hoverRadius;
            float zOffset = Mathf.Sin(time) * hoverRadius;
            float yOffset = Mathf.Sin(time * 2f) * 0.3f;

            Vector3 hoverTarget = playerPos + new Vector3(xOffset, 2f + yOffset, zOffset);
            transform.position = Vector3.Lerp(currentPos, hoverTarget, Time.deltaTime * 3f);
            return;
        }

        // Calcular la posición deseada en este frame
        Vector3 futurePosition = currentPos + (moveDirection * moveSpeed * Time.deltaTime);

        // --- REGLA 4: PRIORIDAD ABSOLUTA DEL RANGO DEL JUGADOR ---
        // Si el movimiento (incluso empujado por un enemigo) la saca del rango, se limita al borde exacto
        if (Vector3.Distance(playerPos, futurePosition) > followDistance)
        {
            Vector3 directionFromPlayer = (futurePosition - playerPos).normalized;
            futurePosition = playerPos + (directionFromPlayer * followDistance);
        }

        // Aplicar posición final
        transform.position = futurePosition;

        // Orientar al hada visualmente hacia donde avanza
        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }
    }

    // Busca la estructura más cercana alrededor del Hada en un radio amplio
    private Transform GetNearestStructure()
    {
        // Ahora el centro de búsqueda es el HADAS (transform.position) con un radio más grande
        Collider[] structures = Physics.OverlapSphere(transform.position, maxStructureDetectionDistance, structureLayer);
        Transform closest = null;
        float minDistance = Mathf.Infinity;

        foreach (Collider col in structures)
        {
            float dist = Vector3.Distance(transform.position, col.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closest = col.transform;
            }
        }
        return closest;
    }

    // Calcula el campo de fuerza que la aleja de los enemigos cercanos
    private Vector3 CalculateEnemyRepulsion()
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, enemyDetectionRadius, enemyLayer);
        Vector3 repulsionVector = Vector3.zero;

        foreach (Collider enemy in enemies)
        {
            Vector3 awayFromEnemy = transform.position - enemy.transform.position;
            float distance = awayFromEnemy.magnitude;

            if (distance > 0)
            {
                // A menor distancia del enemigo, más fuerte es el empuje de alejamiento
                repulsionVector += awayFromEnemy.normalized / distance;
            }
        }
        return repulsionVector.normalized;
    }

    // Método que activa el DetectorCube al ser recogido
    public void ActivarEvasionTemporal(float duration)
    {
        if (evasionCoroutine != null)
        {
            StopCoroutine(evasionCoroutine);
        }
        evasionCoroutine = StartCoroutine(EvasionRoutine(duration));
    }

    private IEnumerator EvasionRoutine(float duration)
    {
        isEvasionActive = true;
        yield return new WaitForSeconds(duration);
        isEvasionActive = false;
    }
}