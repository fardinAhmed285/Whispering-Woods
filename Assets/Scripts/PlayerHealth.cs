using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth = 100f;

    public void TakeDamage(float damage)
    {
        currentHealth = Mathf.Max(0f, currentHealth - damage);

        Debug.Log("Player health: " + currentHealth);

        if (currentHealth <= 0f)
        {
            Debug.Log("Player defeated!");
        }
    }
}
