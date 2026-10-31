using UnityEngine;

public class CampfireManager : MonoBehaviour
{
    [Header("Campfire Progress")]
    public int totalCampfires = 6;
    private int litCampfires = 0;

    [Header("Day Night Cycle")]
    public DayNightCycle dayNightCycle;

    public void RegisterCampfireLit()
    {
        litCampfires++;

        Debug.Log("Campfires lit: " + litCampfires + "/" + totalCampfires);

        if (dayNightCycle != null)
        {
            dayNightCycle.UpdateCampfireProgress(litCampfires);
        }
        else
        {
            Debug.LogError("DayNightCycle is not assigned!");
        }

        if (litCampfires >= totalCampfires)
        {
            Debug.Log("All campfires are lit! Dawn has broken. You win!");
        }
    }
}