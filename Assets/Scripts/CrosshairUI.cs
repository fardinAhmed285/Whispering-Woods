
using UnityEngine;

public class CrosshairUI : MonoBehaviour
{
    [Header("Crosshair Settings")]
    public float size = 5f;
    public float thickness = 1.5f;
    public float gap = 3f;

    [Header("Player Health")]
    public PlayerHealth playerHealth;

    void Start()
    {
        if (playerHealth == null)
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
        }
    }

    void OnGUI()
    {
        // Hide the crosshair when the player is dead.
        if (playerHealth != null && playerHealth.IsDead)
            return;

        float x = Screen.width * 0.5f;
        float y = Screen.height * 0.5f;

        GUI.color = Color.cyan;

        // Top line
        GUI.DrawTexture(
            new Rect(x - thickness / 2, y - gap - size, thickness, size),
            Texture2D.whiteTexture
        );

        // Bottom line
        GUI.DrawTexture(
            new Rect(x - thickness / 2, y + gap, thickness, size),
            Texture2D.whiteTexture
        );

        // Left line
        GUI.DrawTexture(
            new Rect(x - gap - size, y - thickness / 2, size, thickness),
            Texture2D.whiteTexture
        );

        // Right line
        GUI.DrawTexture(
            new Rect(x + gap, y - thickness / 2, size, thickness),
            Texture2D.whiteTexture
        );
    }
}
