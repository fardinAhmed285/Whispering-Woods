using UnityEngine;
using UnityEngine.SceneManagement;

public class RestartGame : MonoBehaviour
{
    public void Restart()
    {
        Time.timeScale = 1f;

        // A campfire was lit: respawn there instead of restarting
        CampfireInteraction checkpoint = CampfireInteraction.LastCheckpoint;
        PlayerHealth player = FindFirstObjectByType<PlayerHealth>();

        if (checkpoint != null && player != null)
        {
            player.Respawn(checkpoint.GetRespawnPoint());
            return;
        }

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }
}