using UnityEngine;
using UnityEngine.InputSystem;
using StarterAssets;

// Campfire safe zone.
// Walk up to the fire and press F to sit down: the player slowly heals,
// ghosts stop detecting them (they go back to patrolling) and incoming
// ghost shots fade away. Press F again to stand up.
//
// Setup on the campfire object:
//  - this script (a trigger Sphere Collider is added and set up automatically)
//  - a NavMesh Obstacle, Shape = Capsule, Carve = on, radius a bit bigger
//    than the Sphere Collider, so ghosts can't walk into the safe zone
[RequireComponent(typeof(SphereCollider))]
public class Campfire : MonoBehaviour
{
    // Read by EnemyAI and GhostProjectile.
    public static bool PlayerIsSafe { get; private set; }

    [Header("Controls")]
    [Tooltip("E is already used to feed wolves and talk to the Guardian.")]
    public Key sitKey = Key.F;

    [Header("Healing")]
    public float healPerSecond = 10f;

    [Header("Safety")]
    [Tooltip("On: the player is only safe while sitting. Off: safe as soon as they are inside the zone.")]
    public bool safeOnlyWhenSitting = true;

    private bool playerInRange;
    private bool isSitting;

    private ThirdPersonController controller;
    private PlayerMagicAttack magicAttack;
    private PlayerHealth health;
    private Animator animator;

    // Makes sure the flag starts false even if domain reload is disabled.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        PlayerIsSafe = false;
    }

    // Called when the script is first added: sets up the trigger zone.
    void Reset()
    {
        SphereCollider zone = GetComponent<SphereCollider>();
        zone.isTrigger = true;
        zone.radius = 4f;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInRange = true;
        controller = other.GetComponentInParent<ThirdPersonController>();
        magicAttack = other.GetComponentInParent<PlayerMagicAttack>();
        health = other.GetComponentInParent<PlayerHealth>();
        animator = other.GetComponentInParent<Animator>();
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (isSitting)
            StandUp();

        playerInRange = false;
        PlayerIsSafe = false;
    }

    void Update()
    {
        if (!playerInRange)
            return;

        if (health != null && health.IsDead)
        {
            isSitting = false;
            PlayerIsSafe = false;
            return;
        }

        if (Keyboard.current != null &&
            Keyboard.current[sitKey].wasPressedThisFrame)
        {
            if (isSitting)
                StandUp();
            else
                SitDown();
        }

        PlayerIsSafe = isSitting || !safeOnlyWhenSitting;

        if (isSitting && health != null)
            health.Heal(healPerSecond * Time.deltaTime);
    }

    void SitDown()
    {
        isSitting = true;

        // No moving or shooting while resting (no sniping from safety).
        if (controller != null)
            controller.enabled = false;

        if (magicAttack != null)
            magicAttack.enabled = false;

        // Otherwise the character stays frozen mid-run.
        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
            animator.SetFloat("MotionSpeed", 0f);
        }
    }

    void StandUp()
    {
        isSitting = false;

        // Don't give control back to a dead player.
        if (health != null && health.IsDead)
            return;

        if (controller != null)
            controller.enabled = true;

        if (magicAttack != null)
            magicAttack.enabled = true;
    }

    void OnDisable()
    {
        if (isSitting)
            StandUp();

        if (playerInRange)
            PlayerIsSafe = false;
    }

    void OnGUI()
    {
        if (!playerInRange || (health != null && health.IsDead))
            return;

        string text = isSitting
            ? "Resting by the fire...   Press " + sitKey + " to stand up"
            : "Press " + sitKey + " to sit down";

        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = 22;
        style.alignment = TextAnchor.MiddleCenter;

        GUI.Box(
            new Rect(Screen.width / 2f - 220f, Screen.height - 110f, 440f, 44f),
            text,
            style);
    }

    void OnDrawGizmosSelected()
    {
        SphereCollider zone = GetComponent<SphereCollider>();

        if (zone == null)
            return;

        float scale = Mathf.Max(
            transform.lossyScale.x,
            transform.lossyScale.y,
            transform.lossyScale.z);

        // Orange circle: the safe zone.
        Gizmos.color = new Color(1f, 0.6f, 0.1f);
        Gizmos.DrawWireSphere(
            transform.TransformPoint(zone.center), zone.radius * scale);
    }
}
