using System;
using UnityEngine;

/// <summary>
/// MonoBehaviour attaché à chaque prefab Restaurant.
/// Gère le gameplay local : revenus, saleté, employés, viande, matériel.
/// </summary>
public class RestaurantManager : MonoBehaviour
{
    [Header("Données")]
    [SerializeField] private RestaurantData data;

    [Header("Composants")]
    public HygieneVisualController hygieneVisuals;
    public EmployeeManager employeeManager;
    public StockManager stockManager;

    public event Action OnRestaurantUpdated;

    public RestaurantData Data => data;
    public bool IsBound => data != null;

    // ======================== BINDING ========================

    /// <summary>Lie ce manager à une instance de données (depuis EmpireManager).</summary>
    public void BindData(RestaurantData restaurantData)
    {
        data = restaurantData;

        if (employeeManager != null)
            employeeManager.Bind(data);

        if (stockManager != null)
            stockManager.Bind(data);

        RefreshVisuals();
        OnRestaurantUpdated?.Invoke();
    }

    // ======================== CYCLE JOURNALIER ========================

    /// <summary>
    /// Traite une journée pour ce restaurant :
    /// revenus, saleté, consommation viande, salaires.
    /// Appelé par EmpireManager.StartNewDay().
    /// </summary>
    public void ProcessDay()
    {
        if (data == null || data.IsClosed)
        {
            if (data != null) data.dailyRevenue = 0f;
            return;
        }

        // Salaires (déduits de l'empire ; dette si fonds insuffisants)
        float wages = data.TotalDailyWageCost;
        if (EmpireManager.Instance != null && wages > 0f)
        {
            if (EmpireManager.Instance.Money >= wages)
            {
                EmpireManager.Instance.Spend(wages);
            }
            else
            {
                float remaining = wages - EmpireManager.Instance.Money;
                EmpireManager.Instance.Spend(EmpireManager.Instance.Money);
                EmpireManager.Instance.AddDebt(remaining);
            }
        }

        // Coût quotidien des accompagnements
        float sideCost = data.GetIngredientsDailyCost();
        if (EmpireManager.Instance != null && sideCost > 0f)
        {
            if (EmpireManager.Instance.Money >= sideCost)
                EmpireManager.Instance.Spend(sideCost, "Ingrédients");
            else
            {
                float remaining = sideCost - EmpireManager.Instance.Money;
                EmpireManager.Instance.Spend(EmpireManager.Instance.Money);
                EmpireManager.Instance.AddDebt(remaining);
            }
        }

        float hygieneCost = data.hygieneStaffCount * GameConstants.HYGIENE_STAFF_DAILY_COST;
        if (EmpireManager.Instance != null && hygieneCost > 0f)
        {
            if (EmpireManager.Instance.Money >= hygieneCost)
                EmpireManager.Instance.Spend(hygieneCost, "Propreté");
            else
            {
                float remaining = hygieneCost - EmpireManager.Instance.Money;
                EmpireManager.Instance.Spend(EmpireManager.Instance.Money);
                EmpireManager.Instance.AddDebt(remaining);
            }
        }

        for (int i = 0; i < data.employees.Count; i++)
            data.employees[i].daysEmployed++;

        // Consommation viande
        if (stockManager != null)
            stockManager.ConsumeDaily();
        else
            ConsumeMeatInternal();

        // Saleté
        float dirtAmount = GameConstants.BASE_DAILY_DIRT;
        dirtAmount *= data.currentMeat.GetDirtMultiplier();
        dirtAmount *= (1f + (data.sizeLevel - 1) * 0.3f);
        if (data.ownerIsWorking)
            dirtAmount += GameConstants.OWNER_SERVICE_EXTRA_DIRT;
        if (data.hygieneStaffCount > 0)
            dirtAmount *= (1f - GameConstants.HYGIENE_STAFF_DIRT_REDUCTION);
        AddDirt(dirtAmount);

        // Revenu
        data.dailyRevenue = CalculateDailyRevenue();

        // Crédite l'empire (EmpireManager additionne déjà via dailyRevenue,
        // donc on ne double-crédite pas ici — EmpireManager lit data.dailyRevenue)
        UpdateReputationFromDirt();
        OnRestaurantUpdated?.Invoke();
    }

    // ======================== REVENUS ========================

    /// <summary>
    /// Calcule le revenu journalier selon viande, propreté, matériel, employés.
    /// </summary>
    public float CalculateDailyRevenue()
    {
        if (data == null || data.IsClosed) return 0f;

        if (data.locationMultiplier <= 0f)
            data.locationMultiplier = LocationHelper.GetMultiplier(data.locationName);

        float revenue = GameConstants.BASE_DAILY_REVENUE;
        revenue += data.employees.Count * GameConstants.REVENUE_PER_EMPLOYEE;
        if (data.ownerIsWorking)
            revenue += GameConstants.OWNER_SERVICE_REVENUE_BONUS;
        revenue *= data.locationMultiplier;
        revenue *= data.currentMeat.GetReputationMultiplier();
        revenue *= data.GetIngredientsReputationMultiplier();
        if (data.managementMode == ManagementMode.Manual && !data.ownerIsWorking)
            revenue *= 0.85f;
        revenue *= EquipmentHelper.GetCombinedRevenueBonus(
            data.grillLevel, data.fridgeLevel, data.vitrineLevel);
        revenue *= (data.reputation / 50f);

        DirtLevel dirt = data.GetDirtLevel();
        if (dirt == DirtLevel.Dirty)
            revenue *= 0.7f;
        else if (dirt == DirtLevel.Infestation)
            revenue *= GameConstants.INFESTATION_REVENUE_PENALTY;

        if (data.meatStockKg <= 0f)
            revenue *= 0.1f;

        // Risque de panne vitrine
        if (UnityEngine.Random.value < EquipmentHelper.GetVitrineBreakdownChance(data.vitrineLevel))
            revenue *= 0.5f;

        return revenue;
    }

    // ======================== HYGIÈNE ========================

    /// <summary>Augmente la saleté et met à jour le visuel.</summary>
    public void AddDirt(float amount)
    {
        if (data == null) return;
        data.currentDirt = Mathf.Clamp(data.currentDirt + amount, 0f, 100f);
        RefreshVisuals();
        OnRestaurantUpdated?.Invoke();
    }

    /// <summary>Remet la saleté à 0 (bouton Faire le ménage).</summary>
    public void CleanRestaurant()
    {
        if (data == null) return;
        data.currentDirt = 0f;
        RefreshVisuals();

        if (hygieneVisuals != null)
            hygieneVisuals.PlayCleanAnimation();

        if (EmpireManager.Instance != null)
        {
            EmpireManager.Instance.Notify($"{data.restaurantName} est nickel !");
            EmpireManager.Instance.AutoSave();
        }

        OnRestaurantUpdated?.Invoke();
    }

    private void RefreshVisuals()
    {
        if (hygieneVisuals != null && data != null)
            hygieneVisuals.UpdateVisuals(data.currentDirt);
    }

    private void UpdateReputationFromDirt()
    {
        if (data == null) return;
        switch (data.GetDirtLevel())
        {
            case DirtLevel.Clean:
                data.reputation = Mathf.Min(100f, data.reputation + 0.5f);
                break;
            case DirtLevel.Dirty:
                data.reputation = Mathf.Max(0f, data.reputation - 2f);
                break;
            case DirtLevel.Infestation:
                data.reputation = Mathf.Max(0f, data.reputation - 5f);
                break;
        }
    }

    // ======================== EMPLOYÉS ========================

    public void HireEmployee(bool isDeclared, string employeeName = null)
    {
        if (data == null) return;

        if (employeeManager != null)
        {
            employeeManager.Hire(isDeclared, employeeName);
        }
        else
        {
            string name = employeeName ?? GenerateRandomName();
            data.employees.Add(new Employee(name, isDeclared));
        }

        if (EmpireManager.Instance != null)
        {
            EmpireManager.Instance.Notify(
                $"Embauche : {(isDeclared ? "déclaré" : "au black")} chez {data.restaurantName}");
            EmpireManager.Instance.AutoSave();
        }

        OnRestaurantUpdated?.Invoke();
    }

    public void FireEmployee(int index)
    {
        if (data == null || index < 0 || index >= data.employees.Count) return;

        // Indemnité
        if (EmpireManager.Instance != null)
        {
            EmpireManager.Instance.Spend(GameConstants.FIRE_SEVERANCE_PAY, "Indemnité licenciement");
        }

        string firedName = data.employees[index].employeeName;

        if (employeeManager != null)
            employeeManager.Fire(index);
        else
            data.employees.RemoveAt(index);

        if (EmpireManager.Instance != null)
        {
            EmpireManager.Instance.Notify($"{firedName} licencié.");
            EmpireManager.Instance.AutoSave();
        }

        OnRestaurantUpdated?.Invoke();
    }

    // ======================== VIANDE ========================

    public void ChangeMeat(MeatType newMeat)
    {
        if (data == null) return;
        data.currentMeat = newMeat;

        if (EmpireManager.Instance != null)
        {
            EmpireManager.Instance.Notify(
                $"{data.restaurantName} passe au {newMeat.GetDisplayName()}");
            EmpireManager.Instance.AutoSave();
        }

        OnRestaurantUpdated?.Invoke();
    }

    /// <summary>Achète du stock de viande (kg).</summary>
    public bool BuyMeatStock(float kg)
    {
        if (data == null || kg <= 0f) return false;

        float cost = kg * data.currentMeat.GetPurchaseCostPerKg();
        if (EmpireManager.Instance == null || !EmpireManager.Instance.Spend(cost, "Achat viande"))
            return false;

        if (stockManager != null)
            stockManager.AddStock(kg);
        else
            data.meatStockKg += kg;

        EmpireManager.Instance.AutoSave();
        OnRestaurantUpdated?.Invoke();
        return true;
    }

    /// <summary>Active / désactive le service fait par le patron.</summary>
    public void ToggleOwnerService()
    {
        if (data == null) return;
        data.ownerIsWorking = !data.ownerIsWorking;

        if (EmpireManager.Instance != null)
        {
            EmpireManager.Instance.Notify(data.ownerIsWorking
                ? $"{data.restaurantName} : vous faites le service (+{GameConstants.OWNER_SERVICE_REVENUE_BONUS:F0}€/j)."
                : $"{data.restaurantName} : vous ne faites plus le service.");
            EmpireManager.Instance.AutoSave();
        }

        OnRestaurantUpdated?.Invoke();
    }

    // ======================== MATÉRIEL ========================

    public bool UpgradeEquipment(EquipmentType type)
    {
        if (data == null || EmpireManager.Instance == null) return false;

        int current = type == EquipmentType.Grill ? data.grillLevel
            : type == EquipmentType.Fridge ? data.fridgeLevel
            : data.vitrineLevel;

        if (current >= 3) return false;

        int restoIndex = FindIndexInEmpire();
        if (restoIndex < 0) return false;

        bool ok = EmpireManager.Instance.BuyEquipment(restoIndex, type, current + 1);
        if (ok) OnRestaurantUpdated?.Invoke();
        return ok;
    }

    private int FindIndexInEmpire()
    {
        if (EmpireManager.Instance == null || data == null) return -1;
        for (int i = 0; i < EmpireManager.Instance.Restaurants.Count; i++)
        {
            if (EmpireManager.Instance.Restaurants[i].restaurantId == data.restaurantId)
                return i;
        }
        return -1;
    }

    // ======================== HELPERS ========================

    private void ConsumeMeatInternal()
    {
        float consumption = GameConstants.MEAT_CONSUMPTION_PER_DAY_KG;
        consumption *= (1f + data.employees.Count * 0.15f);
        data.meatStockKg = Mathf.Max(0f, data.meatStockKg - consumption);
    }

    private static readonly string[] RandomNames =
    {
        "Mehmet", "Hassan", "Youssef", "Ali", "Omar", "Samir", "Karim", "Bilal", "Amine", "Nabil"
    };

    private static string GenerateRandomName()
    {
        return RandomNames[UnityEngine.Random.Range(0, RandomNames.Length)];
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (hygieneVisuals == null)
            hygieneVisuals = GetComponentInChildren<HygieneVisualController>();
        if (employeeManager == null)
            employeeManager = GetComponentInChildren<EmployeeManager>();
        if (stockManager == null)
            stockManager = GetComponentInChildren<StockManager>();
    }
#endif
}
