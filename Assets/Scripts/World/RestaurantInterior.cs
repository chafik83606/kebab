using UnityEngine;

/// <summary>
/// Intérieur d'un kebab : comptoir, spawn employés / clients.
/// </summary>
public class RestaurantInterior : MonoBehaviour
{
    public Transform playerSpawn;
    public Transform counterPoint;
    public Transform[] employeeSlots;
    public Transform customerSpawn;
    public Transform customerExit;
    public Transform[] queueSlots;

    public NPCSpawner npcSpawner;
    public GameObject interiorRoot;

    public void SetActiveInterior(bool active)
    {
        if (interiorRoot != null)
            interiorRoot.SetActive(active);
        else
            gameObject.SetActive(active);

        if (active)
            InteriorEnvironmentSetup.Apply(this);
    }

    public void StartSimulation(RestaurantData data)
    {
        if (npcSpawner != null)
            npcSpawner.StartForRestaurant(data, this);
    }

    public void StopSimulation()
    {
        if (npcSpawner != null)
            npcSpawner.StopAll();
    }
}
