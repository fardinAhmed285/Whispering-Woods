
using UnityEngine;
using UnityEngine.InputSystem;

public class CampfireInteraction : MonoBehaviour
{
    public GameObject fireEffect;
    public Light fireLight;
    public float healPerSecond = 5f;

    private bool isLit = false;
    private bool playerNearby = false;
    private PlayerHealth playerHealth;

    private void Start()
    {
        if (fireEffect != null)
            fireEffect.SetActive(false);

        if (fireLight != null)
            fireLight.enabled = false;
    }

    private void Update()
    {
        if (isLit && playerNearby && playerHealth != null)
        {
            playerHealth.Heal(healPerSecond * Time.deltaTime);
        }

        if (playerNearby && !isLit && Keyboard.current != null
            && Keyboard.current.fKey.wasPressedThisFrame)
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

        if (fireEffect != null)
            fireEffect.SetActive(true);

        if (fireLight != null)
            fireLight.enabled = true;

        Debug.Log("Campfire relit!");
    }
}
