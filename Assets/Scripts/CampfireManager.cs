using UnityEngine;

public class CampfireManager : MonoBehaviour
{
    public int totalCampfires = 6;

    private int litCampfires = 0;

    public void RegisterCampfireLit()
    {
        litCampfires++;

        Debug.Log("Campfires lit: " + litCampfires + "/" + totalCampfires);

        if (litCampfires >= totalCampfires)
        {
            Debug.Log("All campfires are lit! Dawn has broken. You win!");
        }
    }
}
