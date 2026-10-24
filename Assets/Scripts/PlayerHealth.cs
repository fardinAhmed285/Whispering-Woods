
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth = 100f;

    [Header("Health Bar")]
    public Slider healthBar;

    [Header("Damage Feedback")]
    public Image damageFlash;
    public float flashDuration = 0.2f;
    public float flashAlpha = 0.35f;

    private Coroutine flashCoroutine;

    void Start()
    {
        currentHealth = maxHealth;

        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;
        }

        if (damageFlash != null)
        {
            Color color = damageFlash.color;
            color.a = 0f;
            damageFlash.color = color;
        }
    }

    public void TakeDamage(float damage)
    {
        currentHealth = Mathf.Max(0f, currentHealth - damage);

        Debug.Log("Player health: " + currentHealth);

        if (healthBar != null)
        {
            healthBar.value = currentHealth;
        }

        if (damageFlash != null)
        {
            if (flashCoroutine != null)
                StopCoroutine(flashCoroutine);

            flashCoroutine = StartCoroutine(FlashScreen());
        }

        if (currentHealth <= 0f)
        {
            Debug.Log("Player defeated!");
        }
    }

    IEnumerator FlashScreen()
    {
        Color color = damageFlash.color;
        color.a = flashAlpha;
        damageFlash.color = color;

        yield return new WaitForSeconds(flashDuration);

        color.a = 0f;
        damageFlash.color = color;

        flashCoroutine = null;
    }
}
