using UnityEngine;

/// <summary>
/// Bâtiment kebab sur la carte de la ville.
/// Le joueur s'approche et appuie sur E / bouton pour entrer.
/// </summary>
public class RestaurantBuilding : MonoBehaviour
{
    public int restaurantIndex;
    public string displayName = "Kebab";
    public Transform entrancePoint;
    public Transform interiorSpawnPoint;
    public RestaurantInterior interior;

    [Header("Visuel")]
    public Renderer signRenderer;
    public Color openColor = new Color(0.9f, 0.55f, 0.1f);
    public Color closedColor = new Color(0.3f, 0.3f, 0.3f);

    public Transform EntrancePoint => entrancePoint != null ? entrancePoint : transform;

    public void Bind(int index, string name)
    {
        restaurantIndex = index;
        displayName = name;
        gameObject.name = "Kebab_" + name.Replace(" ", "_");
        UpdateSign();
    }

    public void UpdateSign()
    {
        var data = EmpireManager.Instance != null
            ? EmpireManager.Instance.GetRestaurant(restaurantIndex)
            : null;

        if (signRenderer != null)
        {
            bool closed = data != null && data.IsClosed;
            signRenderer.material.color = closed ? closedColor : openColor;
        }
    }

    public void Enter()
    {
        if (GameWorldManager.Instance != null)
            GameWorldManager.Instance.EnterRestaurant(this);
        else
            Debug.LogWarning("GameWorldManager manquant.");
    }

    private void OnMouseDown()
    {
        // Clic souris sur le bâtiment (raycast collider)
        if (PlayerController.Instance != null)
        {
            float d = Vector3.Distance(PlayerController.Instance.transform.position, EntrancePoint.position);
            if (d <= PlayerController.Instance.interactRange * 1.5f)
                Enter();
        }
        else
        {
            Enter();
        }
    }
}
