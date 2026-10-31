using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("References")]
    public Material skyboxMaterial;
    public Light sun;

    [Header("Progress")]
    public int totalCampfires = 6;
    public float transitionSpeed = 1f;

    [Header("Night Settings")]
    public Color nightSkyTint = new Color(0.09f, 0.14f, 0.25f);
    public Color nightGroundColor = new Color(0.08f, 0.09f, 0.12f);
    public float nightExposure = 0.2f;
    public float nightSunIntensity = 0.15f;

    [Header("Day Settings")]
    public Color daySkyTint = new Color(0.5f, 0.65f, 0.85f);
    public Color dayGroundColor = new Color(0.4f, 0.4f, 0.4f);
    public float dayExposure = 1.3f;
    public float daySunIntensity = 1.2f;

    private float targetProgress;
    private float currentProgress;

    void Start()
    {
        RenderSettings.skybox = skyboxMaterial;
        UpdateLighting();
    }

    void Update()
    {
        currentProgress = Mathf.MoveTowards(
            currentProgress,
            targetProgress,
            transitionSpeed * Time.deltaTime
        );

        UpdateLighting();
    }

    public void UpdateCampfireProgress(int litCampfires)
    {
        targetProgress = Mathf.Clamp01(
            (float)litCampfires / totalCampfires
        );
    }

    void UpdateLighting()
    {
        if (skyboxMaterial == null || sun == null)
            return;

        skyboxMaterial.SetColor(
            "_SkyTint",
            Color.Lerp(nightSkyTint, daySkyTint, currentProgress)
        );

        skyboxMaterial.SetColor(
            "_GroundColor",
            Color.Lerp(nightGroundColor, dayGroundColor, currentProgress)
        );

        skyboxMaterial.SetFloat(
            "_Exposure",
            Mathf.Lerp(nightExposure, dayExposure, currentProgress)
        );

        sun.intensity = Mathf.Lerp(
            nightSunIntensity,
            daySunIntensity,
            currentProgress
        );

        sun.transform.rotation = Quaternion.Euler(
            Mathf.Lerp(-10f, 45f, currentProgress),
            -30f,
            0f
        );

        RenderSettings.ambientIntensity =
            Mathf.Lerp(0.25f, 1f, currentProgress);

        DynamicGI.UpdateEnvironment();
    }
}