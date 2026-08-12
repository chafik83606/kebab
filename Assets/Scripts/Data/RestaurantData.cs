using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// État runtime complet d'un restaurant.
/// Sérialisable en JSON pour la sauvegarde.
/// Les templates initiaux utilisent RestaurantTemplate (ScriptableObject).
/// </summary>
[Serializable]
public class RestaurantData
{
    [Header("Identité")]
    public string restaurantName = "Petit Kebab";
    public string locationName = "Centre-ville";
    public string regionId = "";
    public float locationMultiplier = 1f;
    public float mapWorldX;
    public float mapWorldZ;
    public int restaurantId;

    [Header("Viande")]
    public MeatType currentMeat = MeatType.Poulet;
    public float meatStockKg = 20f;

    [Header("Accompagnements (par ingrédient)")]
    public IngredientFreshness saladFreshness = IngredientFreshness.Frais;
    public IngredientFreshness tomatoFreshness = IngredientFreshness.Frais;
    public IngredientFreshness onionFreshness = IngredientFreshness.Frais;
    [Tooltip("Legacy — moyenne calculée depuis les ingrédients si besoin")]
    public SideQuality sideQuality = SideQuality.Moyen;

    [Header("Hygiène")]
    [Range(0f, 100f)]
    public float currentDirt = 0f;
    public int hygieneStaffCount;

    [Header("Mode de gestion")]
    public ManagementMode managementMode = ManagementMode.Automatic;
    public bool setupComplete = true;

    [Header("Employés")]
    public List<Employee> employees = new List<Employee>();

    [Header("Matériel (niveaux 1-3)")]
    [Range(1, 3)] public int grillLevel = 1;
    [Range(1, 3)] public int fridgeLevel = 1;
    [Range(1, 3)] public int vitrineLevel = 1;

    [Header("Finances & État")]
    public float dailyRevenue;
    public float reputation = 50f;          // 0-100
    public int closureDaysRemaining;        // > 0 = fermé administrativement
    public bool isOwnedByPlayer = true;
    public bool ownerIsWorking;             // Le joueur fait le service lui-même

    [Header("Taille")]
    [Tooltip("Influence la vitesse d'accumulation de saleté")]
    [Range(1, 5)] public int sizeLevel = 1;

    // --- Propriétés calculées ---

    public int UndeclaredEmployeeCount
    {
        get
        {
            int count = 0;
            if (employees == null) return 0;
            for (int i = 0; i < employees.Count; i++)
            {
                if (!employees[i].isDeclared) count++;
            }
            return count;
        }
    }

    public int DeclaredEmployeeCount => employees != null ? employees.Count - UndeclaredEmployeeCount : 0;

    public float TotalDailyWageCost
    {
        get
        {
            float total = 0f;
            if (employees == null) return 0f;
            for (int i = 0; i < employees.Count; i++)
                total += employees[i].dailyWage;
            return total;
        }
    }

    public bool IsClosed => closureDaysRemaining > 0;

    public bool IsManualCashier => managementMode == ManagementMode.Manual;

    public bool OwnerIsWorking => ownerIsWorking;

    public float GetIngredientsReputationMultiplier()
    {
        return (saladFreshness.GetReputationMultiplier()
                + tomatoFreshness.GetReputationMultiplier()
                + onionFreshness.GetReputationMultiplier()) / 3f;
    }

    public float GetIngredientsDailyCost()
    {
        return saladFreshness.GetDailyCost()
               + tomatoFreshness.GetDailyCost()
               + onionFreshness.GetDailyCost();
    }

    public float GetIngredientsHealthRisk()
    {
        return (saladFreshness.GetHealthRisk()
                + tomatoFreshness.GetHealthRisk()
                + onionFreshness.GetHealthRisk()) / 3f;
    }

    public string GetIngredientsSummary()
    {
        return $"Salade {saladFreshness.GetDisplayName()} · Tomate {tomatoFreshness.GetDisplayName()} · Oignon {onionFreshness.GetDisplayName()}";
    }

    public DirtLevel GetDirtLevel()
    {
        if (currentDirt <= GameConstants.DIRT_THRESHOLD_NEGLECTED) return DirtLevel.Clean;
        if (currentDirt <= GameConstants.DIRT_THRESHOLD_DIRTY) return DirtLevel.Neglected;
        if (currentDirt <= GameConstants.DIRT_THRESHOLD_INFESTATION) return DirtLevel.Dirty;
        return DirtLevel.Infestation;
    }

    public string GetDirtLevelLabel()
    {
        switch (GetDirtLevel())
        {
            case DirtLevel.Clean: return "Propre";
            case DirtLevel.Neglected: return "Négligé";
            case DirtLevel.Dirty: return "Crado";
            case DirtLevel.Infestation: return "Infestation";
            default: return "?";
        }
    }

    /// <summary>Durée de conservation du frigo en jours.</summary>
    public int FridgeShelfLifeDays => GameConstants.FRIDGE_SHELF_LIFE_DAYS[Mathf.Clamp(fridgeLevel, 1, 3)];
}

/// <summary>Paliers visuels et gameplay de saleté.</summary>
public enum DirtLevel
{
    Clean,       // 0-20%
    Neglected,   // 21-50%
    Dirty,       // 51-75%
    Infestation  // 76-100%
}

/// <summary>
/// Template ScriptableObject pour créer de nouveaux restaurants achetable.
/// Créer via : Assets > Create > Kebab Empire > Restaurant Template
/// </summary>
[CreateAssetMenu(fileName = "NewRestaurant", menuName = "Kebab Empire/Restaurant Template")]
public class RestaurantTemplate : ScriptableObject
{
    public string restaurantName = "Nouveau Kebab";
    public string locationName = "Quartier";
    public float purchasePrice = GameConstants.BASE_RESTAURANT_PRICE;
    public float locationMultiplier = 1f;   // Multiplie le prix et les revenus potentiels
    [Range(1, 5)] public int sizeLevel = 1;
    public MeatType startingMeat = MeatType.Poulet;
    public float startingMeatStock = 20f;
    public int startingGrillLevel = 1;
    public int startingFridgeLevel = 1;
    public int startingVitrineLevel = 1;

    /// <summary>Crée une instance runtime à partir de ce template.</summary>
    public RestaurantData CreateInstance(int id)
    {
        return new RestaurantData
        {
            restaurantId = id,
            restaurantName = restaurantName,
            locationName = locationName,
            locationMultiplier = locationMultiplier,
            currentMeat = startingMeat,
            sideQuality = SideQuality.Moyen,
            meatStockKg = startingMeatStock,
            currentDirt = 0f,
            employees = new List<Employee>(),
            grillLevel = startingGrillLevel,
            fridgeLevel = startingFridgeLevel,
            vitrineLevel = startingVitrineLevel,
            reputation = 50f,
            sizeLevel = sizeLevel,
            isOwnedByPlayer = true,
            closureDaysRemaining = 0
        };
    }
}
