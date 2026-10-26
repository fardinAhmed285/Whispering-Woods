
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

    [Header("Game Over")]
    public GameObject gameOverPanel;

    private Coroutine flashCoroutine;
    private bool isDead;

    public bool IsDead => isDead;

    void Start()
    {
        currentHealth = maxHealth;
        isDead = false;

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

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    public void TakeDamage(float damage)
    {
        if (isDead || damage <= 0f)
            return;

        currentHealth = Mathf.Max(0f, currentHealth - damage);

        Debug.Log("Player health: " + currentHealth);

        if (healthBar != null)
            healthBar.value = currentHealth;

        if (currentHealth > 0f && damageFlash != null)
        {
            if (flashCoroutine != null)
                StopCoroutine(flashCoroutine);

            flashCoroutine = StartCoroutine(FlashScreen());
        }

        if (currentHealth <= 0f)
            Die();
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

    void Die()
    {
        if (isDead)
            return;

        isDead = true;
        Debug.Log("WASTED");

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }

        if (damageFlash != null)
        {
            Color color = damageFlash.color;
            color.a = 0f;
            damageFlash.color = color;
        }

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        // Stop scripts responsible for player movement and attacks.
        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();

        foreach (MonoBehaviour script in scripts)
        {
            if (script != this &&
                (script.GetType().Name.Contains("Movement") ||
                 script.GetType().Name.Contains("Controller") ||
                 script.GetType().Name == "PlayerMagicAttack"))
            {
                script.enabled = false;
            }
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
