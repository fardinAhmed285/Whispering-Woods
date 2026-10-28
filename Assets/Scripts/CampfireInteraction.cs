using UnityEngine;
using UnityEngine.InputSystem;

public class CampfireInteraction : MonoBehaviour
{
    public GameObject fireEffect;
    public Light fireLight;
    public float interactionDistance = 3f;

    private bool isLit = false;
    private bool playerNearby = false;

    private void Start()
    {
        if (fireEffect != null)
            fireEffect.SetActive(false);

        if (fireLight != null)
            fireLight.enabled = false;
    }

    private void Update()
    {
        if (!playerNearby || isLit || Keyboard.current == null)
            return;

        if (Keyboard.current.fKey.wasPressedThisFrame)
            LightCampfire();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            playerNearby = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            playerNearby = false;
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
