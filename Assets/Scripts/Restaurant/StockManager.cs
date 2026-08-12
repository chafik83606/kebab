using UnityEngine;

/// <summary>
/// Gère le stock de viande d'un restaurant (kg + péremption liée au frigo).
/// </summary>
public class StockManager : MonoBehaviour
{
    private RestaurantData data;

    [Tooltip("Jours écoulés depuis le dernier réapprovisionnement (péremption)")]
    [SerializeField] private int daysSinceRestock;

    public void Bind(RestaurantData restaurantData)
    {
        data = restaurantData;
    }

    public void AddStock(float kg)
    {
        if (data == null) return;
        data.meatStockKg += kg;
        daysSinceRestock = 0;
    }

    /// <summary>Consommation journalière + vérification péremption.</summary>
    public void ConsumeDaily()
    {
        if (data == null) return;

        float consumption = GameConstants.MEAT_CONSUMPTION_PER_DAY_KG;
        consumption *= (1f + data.employees.Count * 0.15f);
        data.meatStockKg = Mathf.Max(0f, data.meatStockKg - consumption);

        daysSinceRestock++;

        // Péremption selon niveau du frigo
        int shelfLife = data.FridgeShelfLifeDays;
        if (daysSinceRestock >= shelfLife && data.meatStockKg > 0f)
        {
            // La viande périme : on jette le stock
            Debug.Log($"[Stock] {data.restaurantName} : viande périmée ! Stock perdu.");
            data.meatStockKg = 0f;
            daysSinceRestock = 0;

            if (EmpireManager.Instance != null)
                EmpireManager.Instance.Notify($"⚠️ Viande périmée chez {data.restaurantName} !");
        }
    }

    public float CurrentStock => data?.meatStockKg ?? 0f;
}
