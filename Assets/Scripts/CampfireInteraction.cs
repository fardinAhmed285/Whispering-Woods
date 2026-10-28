
using UnityEngine;
using UnityEngine.InputSystem;

public class CampfireInteraction : MonoBehaviour
{
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
