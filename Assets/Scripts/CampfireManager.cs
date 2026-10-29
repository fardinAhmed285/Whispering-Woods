using UnityEngine;
using UnityEngine.Rendering;

public class CampfireManager : MonoBehaviour
{
    [Header("Campfire Progress")]
    public int totalCampfires = 6;
    private int litCampfires = 0;

    [Header("Environment Brightness")]
    public Light directionalLight;
    public float startingIntensity = 0.7f;
    public float finalIntensity = 1.5f;

    private void Start()
    {
        if (directionalLight != null)
            directionalLight.intensity = startingIntensity;
    }

    public void RegisterCampfireLit()
    {
        litCampfires++;

        Debug.Log("Campfires lit: " + litCampfires + "/" + totalCampfires);

        UpdateBrightness();

        if (litCampfires >= totalCampfires)
        {
            Debug.Log("All campfires are lit! Dawn has broken. You win!");
        }
    }

    private void UpdateBrightness()
    {
        if (directionalLight == null)
            return;

        float progress = (float)litCampfires / totalCampfires;

        directionalLight.intensity = Mathf.Lerp(
            startingIntensity,
            finalIntensity,
            progress
        );

        Debug.Log("Forest brightness: " + directionalLight.intensity);
    }
}