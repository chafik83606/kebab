using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fait vivre le magasin : spawn employés selon la data, clients qui affluent.
/// </summary>
public class NPCSpawner : MonoBehaviour
{
    public float customerInterval = 3.2f;
    public int maxCustomersInShop = 6;
    public VisualCatalog visualCatalog;

    private RestaurantData data;
    private RestaurantInterior interior;
    private readonly List<GameObject> spawned = new List<GameObject>();
    private Coroutine customerRoutine;
    private int activeCustomers;

    public void StartForRestaurant(RestaurantData restaurantData, RestaurantInterior restoInterior)
    {
        StopAll();
        data = restaurantData;
        interior = restoInterior;
        SpawnEmployees();
        customerRoutine = StartCoroutine(CustomerLoop());
    }

    public void StopAll()
    {
        if (customerRoutine != null)
        {
            StopCoroutine(customerRoutine);
            customerRoutine = null;
        }

        for (int i = 0; i < spawned.Count; i++)
        {
            if (spawned[i] != null)
                Destroy(spawned[i]);
        }
        spawned.Clear();
        activeCustomers = 0;
    }

    private void SpawnEmployees()
    {
        if (data == null || interior == null || interior.employeeSlots == null) return;

        int count = Mathf.Min(data.employees.Count, interior.employeeSlots.Length);
        // Si le patron travaille, on le voit aussi au comptoir s'il reste un slot
        bool ownerSlot = data.ownerIsWorking && count < interior.employeeSlots.Length;

        for (int i = 0; i < count; i++)
        {
            var emp = data.employees[i];
            var go = CharacterVisualFactory.CreateCharacter("Employee_" + emp.employeeName, 1.75f, visualCatalog, false);
            go.transform.SetParent(transform);
            var npc = go.AddComponent<EmployeeNPC>();
            npc.Init(interior.employeeSlots[i], emp.isDeclared, emp.employeeName);
            spawned.Add(go);
        }

        if (ownerSlot)
        {
            var go = CharacterVisualFactory.CreateCharacter("Patron", 1.8f, visualCatalog, false);
            go.transform.SetParent(transform);
            var npc = go.AddComponent<EmployeeNPC>();
            npc.Init(interior.employeeSlots[count], true, "Patron");
            var r = go.GetComponentInChildren<Renderer>();
            if (r != null) r.material.color = new Color(0.2f, 0.35f, 0.7f);
            spawned.Add(go);
        }

        // Personne derrière le comptoir ? Au moins un "aide" fantôme si vide
        if (count == 0 && !ownerSlot && interior.employeeSlots.Length > 0)
        {
            // Magasin ouvert mais sans staff
        }
    }

    private IEnumerator CustomerLoop()
    {
        yield return new WaitForSeconds(1f);
        while (true)
        {
            if (data != null && !data.IsClosed && activeCustomers < maxCustomersInShop)
            {
                float chance = 1f;
                if (data.currentDirt > 50f) chance *= 0.5f;
                if (data.meatStockKg <= 0f) chance *= 0.2f;
                if (data.employees.Count == 0 && !data.ownerIsWorking) chance *= 0.35f;

                if (Random.value < chance)
                    SpawnCustomer();
            }
            float wait = customerInterval * Random.Range(0.7f, 1.4f);
            yield return new WaitForSeconds(wait);
        }
    }

    private void SpawnCustomer()
    {
        if (interior == null || interior.queueSlots == null || interior.queueSlots.Length == 0) return;

        var go = CharacterVisualFactory.CreateCharacter("Customer", Random.Range(1.6f, 1.85f), visualCatalog, true);
        go.transform.SetParent(transform);
        var npc = go.AddComponent<CustomerNPC>();

        var path = new List<Transform>();
        if (interior.customerSpawn != null) path.Add(interior.customerSpawn);
        for (int i = interior.queueSlots.Length - 1; i >= 0; i--)
            path.Add(interior.queueSlots[i]);

        Color clothes = Color.HSVToRGB(Random.value, 0.5f, Random.Range(0.4f, 0.9f));
        activeCustomers++;
        npc.Init(path.ToArray(), interior.customerExit, clothes, () =>
        {
            activeCustomers = Mathf.Max(0, activeCustomers - 1);
        });

        spawned.Add(go);
    }
}
