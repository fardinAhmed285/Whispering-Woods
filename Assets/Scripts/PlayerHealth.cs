
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth = 100f;

    [Header("Health Bar")]
    public Slider healthBar;

    void Start()
    {
        currentHealth = maxHealth;

        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;
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

        if (currentHealth <= 0f)
        {
            Debug.Log("Player defeated!");
        }
    }
}
