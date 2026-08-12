using UnityEngine;

/// <summary>
/// Catalogue de visuels (Asset Store).
/// Créer via : Assets > Create > Kebab Empire > Visual Catalog
/// Puis glisser tes prefabs importés dans les slots.
/// </summary>
[CreateAssetMenu(fileName = "VisualCatalog", menuName = "Kebab Empire/Visual Catalog")]
public class VisualCatalog : ScriptableObject
{
    [Header("Personnages (prefabs Asset Store)")]
    [Tooltip("Prefab joueur (avec Animator de préférence)")]
    public GameObject playerPrefab;

    [Tooltip("Prefabs employés / PNJ (aléatoire)")]
    public GameObject[] employeePrefabs;

    [Tooltip("Prefabs clients (aléatoire)")]
    public GameObject[] customerPrefabs;

    [Header("Magasin")]
    [Tooltip("Prefab extérieur kebab (bâtiment)")]
    public GameObject kebabExteriorPrefab;

    [Tooltip("Prefab intérieur kebab (salle + comptoir)")]
    public GameObject kebabInteriorPrefab;

    [Tooltip("Prefab bâtiment complet (extérieur + intérieur + RestaurantBuilding)")]
    public RestaurantBuilding restaurantBuildingPrefab;

    [Header("Échelle / ajustements")]
    public float characterScale = 1f;
    public Vector3 characterPositionOffset = Vector3.zero;

    public GameObject GetRandomEmployee()
    {
        if (employeePrefabs == null || employeePrefabs.Length == 0) return null;
        return employeePrefabs[Random.Range(0, employeePrefabs.Length)];
    }

    public GameObject GetRandomCustomer()
    {
        if (customerPrefabs == null || customerPrefabs.Length == 0) return null;
        return customerPrefabs[Random.Range(0, customerPrefabs.Length)];
    }

    public bool HasEmployeeVisuals => employeePrefabs != null && employeePrefabs.Length > 0;
    public bool HasCustomerVisuals => customerPrefabs != null && customerPrefabs.Length > 0;
}
