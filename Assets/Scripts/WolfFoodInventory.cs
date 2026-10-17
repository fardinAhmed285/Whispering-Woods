using UnityEngine;

public class WolfFoodInventory : MonoBehaviour
{
    [Header("Food Inventory")]
    public int berries = 3;
    public int maxBerries = 20;

    public bool TryConsumeBerry()
    {
        if (berries <= 0)
        {
            Debug.Log("You have no berries!");
            return false;
        }

        berries--;
        Debug.Log("Berry used. Remaining: " + berries);
        return true;
    }

    public void AddBerries(int amount)
    {
        berries = Mathf.Clamp(berries + amount, 0, maxBerries);
        Debug.Log("Berries: " + berries);
    }

    void OnGUI()
    {
        GUI.Box(new Rect(15, 15, 220, 75), "");

        GUI.Label(
            new Rect(27, 25, 200, 25),
            "Berries: " + berries + " / " + maxBerries
        );

        GUI.Label(
            new Rect(27, 50, 200, 25),
            "Collect berries around the forest"
        );
    }
}
