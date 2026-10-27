using UnityEngine;

public class FairyCompanion : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player; // Arrastra a tu jugador aquí o se buscará por Tag

    [Header("Configuración de Movimiento")]
    public float followDistance = 4f;   // Distancia mínima antes de empezar a seguirte si te alejas
    public float moveSpeed = 6f;        // Velocidad a la que vuela cuando te persigue
    public float hoverSpeed = 2f;       // Velocidad del revoloteo suave
    public float hoverRadius = 1.5f;    // Radio de órbita alrededor del jugador

    private float timeOffset;

    void Start()
    {
        // Si no asignas el jugador manualmente, lo busca por la etiqueta "Player"
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        // Un pequeño desfase aleatorio para que cada hada se mueva de forma única
        timeOffset = Random.Range(0f, 10f);
    }

    void Update()
    {
        if (player == null) return;

        // Medimos la distancia actual entre el hada y el jugador
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer > followDistance)
        {
            // --- ESTADO 1: SEGUIR AL JUGADOR (Si te deja atrás) ---
            // Calculamos una posición justo arriba y detrás del jugador
            Vector3 targetPosition = player.position + Vector3.up * 2f;
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        }
        else
        {
            // --- ESTADO 2: REVOLOTEAR CERCA (Órbita suave) ---
            // Usamos senos y cosenos basados en el tiempo para crear un movimiento circular y flotante
            float time = Time.time * hoverSpeed + timeOffset;
            float xOffset = Mathf.Cos(time) * hoverRadius;
            float zOffset = Mathf.Sin(time) * hoverRadius;
            float yOffset = Mathf.Sin(time * 2f) * 0.3f; // Pequeño bamboleo arriba y abajo

            Vector3 hoverTarget = player.position + new Vector3(xOffset, 2f + yOffset, zOffset);
            
            // Usamos Lerp para que el movimiento de revoloteo sea fluido y orgánico
            transform.position = Vector3.Lerp(transform.position, hoverTarget, Time.deltaTime * 3f);
            
            // Opcional: Hacer que mire hacia donde se mueve
            transform.LookAt(hoverTarget);
        }
    }
}