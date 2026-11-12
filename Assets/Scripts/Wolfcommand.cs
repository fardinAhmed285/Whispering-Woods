using UnityEngine;
using UnityEngine.InputSystem;

// Put on PlayerArmature.
// Right click (or Q) while aiming at a ghost: your companion wolves go attack it.
public class WolfCommand : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Keyboard alternative to right click (handy on a trackpad).")]
    public Key commandKey = Key.Q;

    [Header("Aiming")]
    public float maxRange = 40f;
    [Tooltip("How far from the crosshair a ghost can be and still count (metres).")]
    public float aimRadius = 2.5f;
    [Tooltip("Extra tolerance per metre of distance, so far ghosts are easier to pick.")]
    public float aimRadiusPerMetre = 0.05f;

    private Camera mainCamera;
    private string feedback;
    private float feedbackTimer;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (feedbackTimer > 0f)
            feedbackTimer -= Time.deltaTime;

        bool rightClick = Mouse.current != null &&
                          Mouse.current.rightButton.wasPressedThisFrame;

        bool keyPressed = Keyboard.current != null &&
                          Keyboard.current[commandKey].wasPressedThisFrame;

        if (!rightClick && !keyPressed)
            return;

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            return;

        EnemyHealth target = FindAimedEnemy();

        if (target == null)
        {
            ShowFeedback("No ghost there");
            return;
        }

        int wolvesSent = 0;

        foreach (WolfWander wolf in
                 FindObjectsByType<WolfWander>(FindObjectsSortMode.None))
        {
            if (wolf.CommandAttack(target))
                wolvesSent++;
        }

        ShowFeedback(wolvesSent > 0 ? "Attack!" : "No wolf with you");
    }

    EnemyHealth FindAimedEnemy()
    {
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        EnemyHealth best = null;
        float bestOffset = float.MaxValue;

        foreach (EnemyHealth enemy in
                 FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
        {
            if (enemy.currentHealth <= 0f)
                continue;

            Vector3 toEnemy = enemy.transform.position + Vector3.up - ray.origin;
            float along = Vector3.Dot(toEnemy, ray.direction);

            if (along < 0f || along > maxRange)
                continue;

            // Distance between the ghost and the crosshair line
            float offset = (toEnemy - ray.direction * along).magnitude;
            float tolerance = aimRadius + along * aimRadiusPerMetre;

            if (offset <= tolerance && offset < bestOffset)
            {
                bestOffset = offset;
                best = enemy;
            }
        }

        return best;
    }

    void ShowFeedback(string text)
    {
        feedback = text;
        feedbackTimer = 1f;
    }

    void OnGUI()
    {
        if (feedbackTimer <= 0f)
            return;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 20;
        style.alignment = TextAnchor.MiddleCenter;

        GUI.Label(
            new Rect(Screen.width / 2f - 150f, Screen.height / 2f + 30f, 300f, 30f),
            feedback,
            style);
    }
}