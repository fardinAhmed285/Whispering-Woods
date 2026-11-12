using UnityEngine;

// Put on each wolf, next to WolfWander.
public class WolfHealth : MonoBehaviour
{
    public float maxHealth = 60f;
    public float currentHealth;

    [Header("Regeneration")]
    [Tooltip("Seconds without taking damage before the wolf starts healing.")]
    public float regenDelay = 4f;
    public float regenPerSecond = 3f;

    [Header("Wounded")]
    [Tooltip("Below this fraction of health, the wolf stops starting fights.")]
    [Range(0f, 1f)]
    public float woundedThreshold = 0.3f;

    private WolfWander wolf;
    private float timeSinceDamage;

    public bool IsWounded => currentHealth < maxHealth * woundedThreshold;

    // Ghosts only target wolves that are following the player.
    public bool CanBeTargeted => wolf != null && wolf.IsCompanion;

    void Awake()
    {
        wolf = GetComponent<WolfWander>();
        currentHealth = maxHealth;
    }

    void Update()
    {
        timeSinceDamage += Time.deltaTime;

        if (timeSinceDamage >= regenDelay && currentHealth < maxHealth)
            currentHealth = Mathf.Min(
                maxHealth, currentHealth + regenPerSecond * Time.deltaTime);
    }

    public void TakeDamage(float damage)
    {
        if (damage <= 0f)
            return;

        timeSinceDamage = 0f;
        currentHealth = Mathf.Max(0f, currentHealth - damage);

        Debug.Log(name + " health: " + currentHealth);

        if (currentHealth <= 0f)
        {
            // Knocked out: the wolf loses trust and runs off.
            currentHealth = maxHealth;

            if (wolf != null)
                wolf.KnockOut();
        }
    }
}