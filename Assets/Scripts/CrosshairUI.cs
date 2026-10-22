
using UnityEngine;

public class CrosshairUI : MonoBehaviour
{
    public float size = 5f;
    public float thickness = 1.5f;
    public float gap = 3f;

    void OnGUI()
    {
        float x = Screen.width * 0.5f;
        float y = Screen.height * 0.5f;

        GUI.color = Color.cyan;

        GUI.DrawTexture(
            new Rect(x - thickness / 2, y - gap - size,
                thickness, size), Texture2D.whiteTexture);

        GUI.DrawTexture(
            new Rect(x - thickness / 2, y + gap,
                thickness, size), Texture2D.whiteTexture);

        GUI.DrawTexture(
            new Rect(x - gap - size, y - thickness / 2,
                size, thickness), Texture2D.whiteTexture);

        GUI.DrawTexture(
            new Rect(x + gap, y - thickness / 2,
                size, thickness), Texture2D.whiteTexture);
    }
}
