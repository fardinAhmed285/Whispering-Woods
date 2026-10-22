using UnityEngine;

public class CrosshairUI : MonoBehaviour
{
    public float size = 8f;
    public float thickness = 2f;
    public float gap = 4f;

    void OnGUI()
    {
        float x = Screen.width / 2f;
        float y = Screen.height / 2f;

        GUI.color = Color.white;

        GUI.DrawTexture(
            new Rect(x - thickness / 2f, y - gap - size,
                     thickness, size), Texture2D.whiteTexture);

        GUI.DrawTexture(
            new Rect(x - thickness / 2f, y + gap,
                     thickness, size), Texture2D.whiteTexture);

        GUI.DrawTexture(
            new Rect(x - gap - size, y - thickness / 2f,
                     size, thickness), Texture2D.whiteTexture);

        GUI.DrawTexture(
            new Rect(x + gap, y - thickness / 2f,
                     size, thickness), Texture2D.whiteTexture);
    }
}
