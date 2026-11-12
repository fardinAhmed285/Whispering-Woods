using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CampfireInteraction : MonoBehaviour
{
    private static readonly List<CampfireInteraction> litCampfires =
        new List<CampfireInteraction>();

    [Header("Safe Zone")]
    [Tooltip("Ghosts ignore the player inside this radius once lit.")]
    public float safeRadius = 10f;

    public bool IsLit => isLit;

    // Last lit campfire the player visited: respawn point after death.
    public static CampfireInteraction LastCheckpoint { get; private set; }

    public Vector3 GetRespawnPoint()
    {
        Vector3 point = transform.position + transform.forward * 2.5f;

        if (UnityEngine.AI.NavMesh.SamplePosition(
            point, out UnityEngine.AI.NavMeshHit hit, 4f,
            UnityEngine.AI.NavMesh.AllAreas))
        {
            return hit.position;
        }

        return point + Vector3.up;
    }

    public static bool IsInsideLitCampfire(Vector3 position)
    {
        foreach (CampfireInteraction campfire in litCampfires)
        {
            if (campfire == null)
                continue;

            Vector3 offset = position - campfire.transform.position;
            offset.y = 0f;

            if (offset.sqrMagnitude <= campfire.safeRadius * campfire.safeRadius)
                return true;
        }

        return false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetLitCampfires()
    {
        litCampfires.Clear();
        LastCheckpoint = null;
    }

    private void OnDestroy()
    {
        litCampfires.Remove(this);
    }

    private void OnDrawGizmosSelected()
    {
        // Safe zone
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, safeRadius);
    }

    [Header("Campfire Effects")]
    public GameObject fireEffect;
    public Light fireLight;

    [Header("Healing")]
    public float healPerSecond = 5f;

    [Header("Manager")]
    public CampfireManager manager;

    private bool isLit = false;
    private bool playerNearby = false;
    private PlayerHealth playerHealth;

    private void Start()
    {
        if (fireEffect != null)
            fireEffect.SetActive(false);

        if (fireLight != null)
            fireLight.enabled = false;

        Debug.Log("Campfire initialized. Manager assigned: " + (manager != null));
    }

    private void Update()
    {
        if (isLit && playerNearby && playerHealth != null)
        {
            playerHealth.Heal(healPerSecond * Time.deltaTime);
        }

        if (playerNearby && !isLit &&
            Keyboard.current != null &&
            Keyboard.current.fKey.wasPressedThisFrame)
        {
            LightCampfire();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();

        if (health != null)
        {
            playerNearby = true;
            playerHealth = health;

            if (isLit)
                LastCheckpoint = this;
            Debug.Log("Player entered campfire zone.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();

        if (health != null && health == playerHealth)
        {
            playerNearby = false;
            playerHealth = null;
        }
    }

    private void LightCampfire()
    {
        isLit = true;
        LastCheckpoint = this;

        if (!litCampfires.Contains(this))
            litCampfires.Add(this);

        Debug.Log("Campfire relit!");

        Debug.Log("Manager reference: " + (manager != null));

        if (manager != null)
        {
            manager.RegisterCampfireLit();
        }
        else
        {
            Debug.LogError("CampfireManager is not assigned!");
        }

        if (fireEffect != null)
            fireEffect.SetActive(true);

        if (fireLight != null)
            fireLight.enabled = true;
    }
}