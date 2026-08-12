using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton central de l'empire commercial du joueur.
/// Gère l'argent, les dettes, les restaurants, les impôts et la santé financière.
/// </summary>
public class EmpireManager : MonoBehaviour
{
    public static EmpireManager Instance { get; private set; }

    // --- Événements UI ---
    public event Action OnEmpireUpdated;
    public event Action<string> OnNotification;
    public event Action<string> OnGameOver;
    public event Action<InspectionResult> OnInspectionOccurred;
    public event Action<CompetitorData> OnHostileTakeoverOffer;
    public event Action OnTaxesDue;

    [Header("État financier")]
    [SerializeField] private float money = GameConstants.STARTING_MONEY;
    [SerializeField] private float debt;
    [SerializeField] private float monthlyRevenueAccumulated;
    [SerializeField] private float globalReputation = 50f;

    [Header("Temps")]
    [SerializeField] private int currentDay = 1;
    [SerializeField] private int currentMonth = 1;
    [SerializeField] private bool taxesPaidThisMonth = true;

    [Header("Restaurants")]
    [SerializeField] private List<RestaurantData> restaurants = new List<RestaurantData>();
    [SerializeField] private int nextRestaurantId = 1;

    [Header("Références scène")]
    [Tooltip("Managers MonoBehaviour liés aux restaurants (ordre = index)")]
    public List<RestaurantManager> restaurantManagers = new List<RestaurantManager>();

    [Header("Concurrents")]
    public CompetitorManager competitorManager;

    private bool gameOver;
    private string gameOverReason;
    private GamePhase gamePhase = GamePhase.Playing;
    private bool awaitingSetup;
    private bool booted;

    public GamePhase CurrentPhase => gamePhase;
    public bool AwaitingSetup => awaitingSetup;
    /// <summary>True après Start() (save chargée ou nouvelle partie initialisée).</summary>
    public bool IsBooted => booted;

    // --- Propriétés publiques ---
    public float Money => money;
    public float Debt => debt;
    public int CurrentDay => currentDay;
    public int CurrentMonth => currentMonth;
    public float GlobalReputation => globalReputation;
    public int RestaurantCount => restaurants.Count;
    public IReadOnlyList<RestaurantData> Restaurants => restaurants;
    public bool IsGameOver => gameOver;
    public bool TaxesPaidThisMonth => taxesPaidThisMonth;
    public float MonthlyRevenueAccumulated => monthlyRevenueAccumulated;

    public FinancialHealth GetFinancialHealth()
    {
        if (debt > money * GameConstants.FRAGILE_DEBT_MULTIPLIER || (money < 0f && debt > 0f))
            return FinancialHealth.Fragile;
        if (debt > 0f || money < 1000f)
            return FinancialHealth.Unstable;
        return FinancialHealth.Healthy;
    }

    public string GetFinancialHealthLabel()
    {
        switch (GetFinancialHealth())
        {
            case FinancialHealth.Healthy: return "Saine";
            case FinancialHealth.Unstable: return "Instable";
            case FinancialHealth.Fragile: return "Fragile";
            default: return "?";
        }
    }

    // ======================== CYCLE DE VIE ========================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Ne pas DontDestroyOnLoad : sinon l'ancien Empire (UI 2D) bloque le monde 3D au re-Play.
    }

    private void Start()
    {
        SaveData loaded = SaveSystem.Load();
        if (loaded != null)
        {
            ApplySaveData(loaded);
            Notify("Partie chargée — Jour " + currentDay);
        }
        else
        {
            InitializeNewGame();
        }

        SyncManagersWithData();
        MigrateRestaurantData();
        if (GameFlowController.Instance != null)
            GameFlowController.Instance.InitFromSave((int)gamePhase, awaitingSetup);
        booted = true;
        NotifyEmpireUpdated();
    }

    private void MigrateRestaurantData()
    {
        // Supprime les kebabs fantômes (Centre-ville sans ville) créés par d'anciennes versions / race boot
        for (int i = restaurants.Count - 1; i >= 0; i--)
        {
            if (IsPlaceholderRestaurant(restaurants[i]))
            {
                Debug.Log($"[Empire] Suppression kebab fantôme : {restaurants[i].restaurantName}");
                restaurants.RemoveAt(i);
            }
        }

        if (restaurants.Count == 0)
        {
            gamePhase = GamePhase.MapSelection;
            awaitingSetup = true;
        }

        for (int i = 0; i < restaurants.Count; i++)
        {
            if (restaurants[i].locationMultiplier <= 0f)
                restaurants[i].locationMultiplier = LocationHelper.GetMultiplier(restaurants[i].locationName);
        }
    }

    /// <summary>Ancien starter / fantôme sans emplacement carte.</summary>
    public static bool IsPlaceholderRestaurant(RestaurantData r)
    {
        if (r == null) return true;
        if (!string.IsNullOrEmpty(r.regionId)) return false;
        if (r.mapWorldX != 0f || r.mapWorldZ != 0f) return false;
        string loc = r.locationName ?? "";
        string name = r.restaurantName ?? "";
        return loc == "Centre-ville"
               || loc == "Nouveau quartier"
               || name == "Mon Premier Kebab"
               || name == "Petit Kebab"
               || name.StartsWith("Kebab #");
    }

    /// <summary>Démarre une nouvelle partie — le joueur choisit l'emplacement sur la carte.</summary>
    public void InitializeNewGame()
    {
        money = GameConstants.STARTING_MONEY;
        debt = 0f;
        monthlyRevenueAccumulated = 0f;
        currentDay = 1;
        currentMonth = 1;
        taxesPaidThisMonth = true;
        globalReputation = 50f;
        nextRestaurantId = 1;
        gameOver = false;
        gameOverReason = null;
        restaurants.Clear();
        gamePhase = GamePhase.MapSelection;
        awaitingSetup = true;

        if (competitorManager != null)
            competitorManager.InitializeCompetitors();

        AutoSave();
        Notify("Bienvenue ! Choisissez sur la carte où installer votre premier kebab.");
    }

    /// <summary>Crée un restaurant après l'assistant de configuration.</summary>
    public void FoundRestaurant(FranceMapData.MapCity city, RestaurantSetupConfig config)
    {
        // Évite le double kebab (fantôme + ville choisie)
        for (int i = restaurants.Count - 1; i >= 0; i--)
        {
            if (IsPlaceholderRestaurant(restaurants[i]))
                restaurants.RemoveAt(i);
        }

        var data = new RestaurantData
        {
            restaurantId = nextRestaurantId++,
            restaurantName = "Kebab " + city.displayName,
            locationName = city.displayName,
            regionId = city.id,
            locationMultiplier = city.locationMultiplier,
            mapWorldX = city.worldX,
            mapWorldZ = city.worldZ,
            currentMeat = config.meat,
            saladFreshness = config.salad,
            tomatoFreshness = config.tomato,
            onionFreshness = config.onion,
            meatStockKg = 25f,
            currentDirt = 0f,
            hygieneStaffCount = config.hygieneStaff,
            managementMode = config.managementMode,
            ownerIsWorking = config.managementMode == ManagementMode.Manual,
            setupComplete = true,
            employees = new List<Employee>(),
            grillLevel = 1,
            fridgeLevel = 1,
            vitrineLevel = 1,
            reputation = 50f,
            sizeLevel = 1,
            isOwnedByPlayer = true
        };

        for (int i = 0; i < config.declaredStaff; i++)
            data.employees.Add(new Employee(GenerateEmployeeName(), true));
        for (int i = 0; i < config.undeclaredStaff; i++)
            data.employees.Add(new Employee(GenerateEmployeeName(), false));

        restaurants.Add(data);
        gamePhase = GamePhase.Playing;
        awaitingSetup = false;
        EnsureRestaurantManagers();
        SyncManager(restaurants.Count - 1);
        AutoSave();
        NotifyEmpireUpdated();
    }

    private static string GenerateEmployeeName()
    {
        string[] names = { "Karim", "Mehmet", "Hassan", "Youssef", "Ali", "Samir", "Bilal" };
        return names[UnityEngine.Random.Range(0, names.Length)];
    }

    private RestaurantData CreateDefaultRestaurant(string name, string location)
    {
        float locMult = LocationHelper.GetMultiplier(location);
        var data = new RestaurantData
        {
            restaurantId = nextRestaurantId++,
            restaurantName = name,
            locationName = location,
            locationMultiplier = locMult,
            currentMeat = MeatType.Poulet,
            sideQuality = SideQuality.Moyen,
            meatStockKg = 25f,
            currentDirt = 0f,
            employees = new List<Employee>(),
            grillLevel = 1,
            fridgeLevel = 1,
            vitrineLevel = 1,
            reputation = 50f,
            sizeLevel = 1,
            isOwnedByPlayer = true
        };
        // Premier employé déclaré offert
        data.employees.Add(new Employee("Karim", true));
        return data;
    }

    // ======================== JOUR SUIVANT ========================

    /// <summary>
    /// Calcule les revenus de tous les restaurants, augmente la saleté,
    /// vérifie les contrôles aléatoires, gère impôts et concurrents.
    /// </summary>
    public void StartNewDay()
    {
        if (gameOver) return;

        currentDay++;

        // Nouveau mois ?
        if ((currentDay - 1) % GameConstants.DAYS_PER_MONTH == 0 && currentDay > 1)
        {
            currentMonth++;
            OnMonthEnd();
        }

        float dayRevenue = 0f;

        for (int i = 0; i < restaurants.Count; i++)
        {
            RestaurantData resto = restaurants[i];

            // Fermeture administrative
            if (resto.closureDaysRemaining > 0)
            {
                resto.closureDaysRemaining--;
                if (resto.closureDaysRemaining == 0)
                    Notify($"{resto.restaurantName} rouvre après fermeture administrative.");
                SyncManager(i);
                continue;
            }

            // Calcul revenu via le manager si présent, sinon calcul direct
            float revenue = 0f;
            if (i < restaurantManagers.Count && restaurantManagers[i] != null)
            {
                restaurantManagers[i].ProcessDay();
                revenue = resto.dailyRevenue;
            }
            else
            {
                revenue = CalculateRevenueFor(resto);
                ApplyDailyDirt(resto);
                ConsumeMeat(resto);
                ApplyEmployeeWages(resto);
                ApplySideCost(resto);
            }

            dayRevenue += revenue;
            monthlyRevenueAccumulated += revenue;
        }

        money += dayRevenue;

        // Contrôles aléatoires
        TryRandomInspection();

        // Mise à jour concurrents
        if (competitorManager != null)
        {
            competitorManager.SimulateDay();
            CheckHostileTakeover();
        }

        CheckFinancialHealth();
        AutoSave();
        NotifyEmpireUpdated();

        Notify($"Jour {currentDay} — Revenus : +{dayRevenue:F0}€");
    }

    private void OnMonthEnd()
    {
        // Majoration si impôts non payés le mois précédent
        if (!taxesPaidThisMonth && debt > 0f)
        {
            float penalty = debt * GameConstants.TAX_PENALTY_RATE;
            debt += penalty;
            Notify($"Majoration fiscale : +{penalty:F0}€ de dette (10%).");
        }

        taxesPaidThisMonth = false;
        OnTaxesDue?.Invoke();
        Notify($"Mois {currentMonth} — Impôts à payer : {GetTaxAmountDue():F0}€");
    }

    // ======================== FINANCES ========================

    public float GetTaxAmountDue()
    {
        return monthlyRevenueAccumulated * GameConstants.TAX_RATE;
    }

    /// <summary>Paie les impôts du mois en cours.</summary>
    public bool PayTaxes()
    {
        if (taxesPaidThisMonth)
        {
            Notify("Impôts déjà payés ce mois-ci.");
            return false;
        }

        float amount = GetTaxAmountDue();
        if (money >= amount)
        {
            money -= amount;
            taxesPaidThisMonth = true;
            monthlyRevenueAccumulated = 0f;
            Notify($"Impôts payés : -{amount:F0}€");
            AutoSave();
            NotifyEmpireUpdated();
            return true;
        }

        // Pas assez d'argent → dette
        debt += amount - money;
        money = 0f;
        taxesPaidThisMonth = true;
        monthlyRevenueAccumulated = 0f;
        Notify($"Impôts partiels — Dette ajoutée. Dette totale : {debt:F0}€");
        AutoSave();
        NotifyEmpireUpdated();
        return false;
    }

    /// <summary>Rembourse une partie de la dette.</summary>
    public bool PayDebt(float amount)
    {
        amount = Mathf.Min(amount, debt);
        if (amount <= 0f || money < amount) return false;

        money -= amount;
        debt -= amount;
        Notify($"Dette remboursée : -{amount:F0}€");
        AutoSave();
        NotifyEmpireUpdated();
        return true;
    }

    public bool Spend(float amount, string reason = null)
    {
        if (money < amount) return false;
        money -= amount;
        if (!string.IsNullOrEmpty(reason))
            Notify($"{reason} : -{amount:F0}€");
        NotifyEmpireUpdated();
        return true;
    }

    public void AddMoney(float amount)
    {
        money += amount;
        NotifyEmpireUpdated();
    }

    public void AddDebt(float amount)
    {
        debt += amount;
        NotifyEmpireUpdated();
    }

    /// <summary>Vérifie si la santé financière est fragile.</summary>
    public void CheckFinancialHealth()
    {
        var health = GetFinancialHealth();
        if (health == FinancialHealth.Fragile)
            Debug.LogWarning("[Empire] Santé financière FRAGILE — risque de rachat hostile !");
    }

    // ======================== RESTAURANTS ========================

    /// <summary>Achète un nouveau restaurant.</summary>
    public bool BuyNewRestaurant(float price, string name = null, string location = null)
    {
        if (!Spend(price, "Achat local"))
        {
            Notify("Fonds insuffisants pour acheter un local.");
            return false;
        }

        var data = CreateDefaultRestaurant(
            name ?? $"Kebab #{nextRestaurantId}",
            location ?? "Nouveau quartier"
        );
        // Pas d'employé offert sur les locaux achetés
        data.employees.Clear();
        restaurants.Add(data);

        EnsureRestaurantManagers();
        SyncManager(restaurants.Count - 1);

        Notify($"Nouveau restaurant ouvert : {data.restaurantName} !");
        AutoSave();
        NotifyEmpireUpdated();
        return true;
    }

    /// <summary>Achète / améliore un équipement d'un restaurant.</summary>
    public bool BuyEquipment(int restaurantIndex, EquipmentType equipmentType, int newLevel)
    {
        if (!IsValidIndex(restaurantIndex)) return false;
        if (newLevel < 1 || newLevel > 3) return false;

        RestaurantData resto = restaurants[restaurantIndex];
        int currentLevel = GetEquipmentLevel(resto, equipmentType);

        if (newLevel <= currentLevel)
        {
            Notify("Niveau déjà atteint ou inférieur.");
            return false;
        }

        float price = EquipmentHelper.GetUpgradePrice(equipmentType, newLevel);
        if (!Spend(price, $"Upgrade {EquipmentHelper.GetDisplayName(equipmentType)} Nv.{newLevel}"))
        {
            Notify("Fonds insuffisants.");
            return false;
        }

        SetEquipmentLevel(resto, equipmentType, newLevel);
        SyncManager(restaurantIndex);
        AutoSave();
        NotifyEmpireUpdated();
        return true;
    }

    public RestaurantData GetRestaurant(int index)
    {
        return IsValidIndex(index) ? restaurants[index] : null;
    }

    public void AddRestaurantFromBuyout(RestaurantData resto)
    {
        resto.restaurantId = nextRestaurantId++;
        resto.isOwnedByPlayer = true;
        restaurants.Add(resto);
        AutoSave();
        NotifyEmpireUpdated();
    }

    // ======================== CONTRÔLES ========================

    private void TryRandomInspection()
    {
        if (restaurants.Count == 0) return;

        int targetIndex = UnityEngine.Random.Range(0, restaurants.Count);
        RestaurantData target = restaurants[targetIndex];
        if (target.IsClosed) return;

        float chance = GameConstants.BASE_INSPECTION_CHANCE;

        // Augmente si sale
        if (target.currentDirt > GameConstants.DIRT_THRESHOLD_DIRTY)
            chance += 0.15f;
        if (target.currentDirt > GameConstants.DIRT_THRESHOLD_INFESTATION)
            chance += 0.20f;

        // Augmente si non-déclarés
        chance += target.UndeclaredEmployeeCount * 0.08f;

        // Augmente si impôts non payés
        if (!taxesPaidThisMonth)
            chance += GameConstants.UNPAID_TAX_INSPECTION_BONUS;

        if (UnityEngine.Random.value > chance) return;

        InspectionResult result;
        float typeRoll = UnityEngine.Random.value;
        if (!taxesPaidThisMonth && typeRoll < 0.28f)
            result = PerformFiscalInspection(target, targetIndex);
        else if (typeRoll < 0.64f)
            result = PerformUrssafInspection(target, targetIndex);
        else
            result = PerformHealthInspection(target, targetIndex);

        OnInspectionOccurred?.Invoke(result);
        Notify(result.message);
        SyncManager(targetIndex);
        AutoSave();
        NotifyEmpireUpdated();
    }

    private InspectionResult PerformUrssafInspection(RestaurantData resto, int index)
    {
        int undeclared = resto.UndeclaredEmployeeCount;
        var result = new InspectionResult
        {
            type = InspectionType.URSSAF,
            restaurantIndex = index,
            restaurantName = resto.restaurantName
        };

        if (undeclared > 0)
        {
            float fine = undeclared * GameConstants.URSSAF_FINE_PER_UNDECLARED;
            ApplyFine(fine);
            resto.closureDaysRemaining = GameConstants.URSSAF_CLOSURE_DAYS;
            result.fine = fine;
            result.closureDays = GameConstants.URSSAF_CLOSURE_DAYS;
            result.passed = false;
            result.message = $"🚨 Contrôle URSSAF chez {resto.restaurantName} ! " +
                             $"{undeclared} non-déclaré(s) → Amende {fine:F0}€ + fermeture {GameConstants.URSSAF_CLOSURE_DAYS} jours.";
        }
        else
        {
            result.passed = true;
            result.message = $"✅ Contrôle URSSAF chez {resto.restaurantName} : tout est en règle.";
        }

        return result;
    }

    private InspectionResult PerformFiscalInspection(RestaurantData resto, int index)
    {
        var result = new InspectionResult
        {
            type = InspectionType.Fiscal,
            restaurantIndex = index,
            restaurantName = resto.restaurantName
        };

        if (!taxesPaidThisMonth && GetTaxAmountDue() > 0f)
        {
            float fine = GetTaxAmountDue() * GameConstants.FISCAL_FINE_RATE;
            ApplyFine(fine);
            resto.closureDaysRemaining = GameConstants.FISCAL_CLOSURE_DAYS;
            result.fine = fine;
            result.closureDays = GameConstants.FISCAL_CLOSURE_DAYS;
            result.passed = false;
            result.message = $"🚨 Contrôle fiscal ! Impôts impayés → Amende {fine:F0}€ + fermeture {GameConstants.FISCAL_CLOSURE_DAYS} jours.";
        }
        else
        {
            result.passed = true;
            result.message = $"✅ Contrôle fiscal chez {resto.restaurantName} : comptabilité OK.";
        }

        return result;
    }

    private InspectionResult PerformHealthInspection(RestaurantData resto, int index)
    {
        var result = new InspectionResult
        {
            type = InspectionType.Health,
            restaurantIndex = index,
            restaurantName = resto.restaurantName
        };

        bool dirty = resto.currentDirt > GameConstants.DIRT_THRESHOLD_DIRTY;
        bool badMeat = resto.currentMeat == MeatType.PreferePasSavoir;
        bool badSides = resto.GetIngredientsHealthRisk() > 0.1f;

        if (dirty || badMeat || badSides)
        {
            float fine = GameConstants.HEALTH_FINE;
            ApplyFine(fine);
            resto.closureDaysRemaining = GameConstants.HEALTH_CLOSURE_DAYS;
            result.fine = fine;
            result.closureDays = GameConstants.HEALTH_CLOSURE_DAYS;
            result.passed = false;

            string reason = dirty && badMeat ? "saleté + viande douteuse"
                : dirty ? "hygiène insuffisante"
                : "viande suspecte";

            result.message = $"🦠 Contrôle sanitaire chez {resto.restaurantName} ! " +
                             $"Problème : {reason} → Amende {fine:F0}€ + fermeture {GameConstants.HEALTH_CLOSURE_DAYS} jours.";
        }
        else
        {
            result.passed = true;
            result.message = $"✅ Contrôle sanitaire chez {resto.restaurantName} : hygiène OK.";
        }

        return result;
    }

    private void ApplyFine(float fine)
    {
        if (money >= fine)
            money -= fine;
        else
        {
            debt += fine - money;
            money = 0f;
        }
    }

    // ======================== RACHAT HOSTILE / GAME OVER ========================

    private void CheckHostileTakeover()
    {
        if (GetFinancialHealth() != FinancialHealth.Fragile) return;
        if (competitorManager == null) return;

        CompetitorData aggressor = competitorManager.GetStrongestCompetitor();
        if (aggressor == null || aggressor.isBankrupt) return;

        if (UnityEngine.Random.value < GameConstants.HOSTILE_TAKEOVER_BASE_CHANCE)
        {
            OnHostileTakeoverOffer?.Invoke(aggressor);
            Notify($"⚠️ {aggressor.competitorName} propose un rachat hostile de votre empire !");
        }
    }

    /// <summary>Le joueur accepte le rachat hostile → Game Over.</summary>
    public void AcceptHostileTakeover(CompetitorData competitor)
    {
        TriggerGameOver($"Racheté par {competitor.competitorName}. Votre empire est perdu.");
    }

    /// <summary>Le joueur refuse → faillite immédiate → Game Over.</summary>
    public void RefuseHostileTakeover()
    {
        TriggerGameOver("Faillite déclarée. Votre empire s'effondre.");
    }

    public void TriggerGameOver(string reason)
    {
        gameOver = true;
        gameOverReason = reason;
        OnGameOver?.Invoke(reason);
        Notify("GAME OVER — " + reason);
        // On conserve la save pour afficher le score, puis on peut la supprimer
    }

    public int CalculateFinalScore()
    {
        int score = (int)(money + restaurants.Count * 2000f + globalReputation * 10f - debt);
        return Mathf.Max(0, score);
    }

    // ======================== CALCULS INTERNES ========================

    public float CalculateRevenueFor(RestaurantData resto)
    {
        if (resto.IsClosed) return 0f;

        if (resto.locationMultiplier <= 0f)
            resto.locationMultiplier = LocationHelper.GetMultiplier(resto.locationName);

        float revenue = GameConstants.BASE_DAILY_REVENUE;
        revenue += resto.employees.Count * GameConstants.REVENUE_PER_EMPLOYEE;
        if (resto.ownerIsWorking)
            revenue += GameConstants.OWNER_SERVICE_REVENUE_BONUS;
        revenue *= resto.locationMultiplier;
        revenue *= resto.currentMeat.GetReputationMultiplier();
        revenue *= resto.GetIngredientsReputationMultiplier();
        revenue *= EquipmentHelper.GetCombinedRevenueBonus(resto.grillLevel, resto.fridgeLevel, resto.vitrineLevel);
        revenue *= (resto.reputation / 50f);

        // Pénalités saleté
        DirtLevel dirt = resto.GetDirtLevel();
        if (dirt == DirtLevel.Dirty)
            revenue *= 0.7f;
        else if (dirt == DirtLevel.Infestation)
            revenue *= GameConstants.INFESTATION_REVENUE_PENALTY;

        // Stock de viande épuisé
        if (resto.meatStockKg <= 0f)
            revenue *= 0.1f;

        // Panne vitrine éventuelle
        if (UnityEngine.Random.value < EquipmentHelper.GetVitrineBreakdownChance(resto.vitrineLevel))
            revenue *= 0.5f;

        resto.dailyRevenue = revenue;
        return revenue;
    }

    private void ApplyDailyDirt(RestaurantData resto)
    {
        float dirt = GameConstants.BASE_DAILY_DIRT;
        dirt *= resto.currentMeat.GetDirtMultiplier();
        dirt *= (1f + (resto.sizeLevel - 1) * 0.3f); // Plus grand = plus sale
        if (resto.ownerIsWorking)
            dirt += GameConstants.OWNER_SERVICE_EXTRA_DIRT;
        if (resto.hygieneStaffCount > 0)
            dirt *= (1f - GameConstants.HYGIENE_STAFF_DIRT_REDUCTION);
        resto.currentDirt = Mathf.Clamp(resto.currentDirt + dirt, 0f, 100f);

        // Impact réputation
        if (resto.GetDirtLevel() == DirtLevel.Dirty)
            resto.reputation = Mathf.Max(0f, resto.reputation - 2f);
        else if (resto.GetDirtLevel() == DirtLevel.Infestation)
            resto.reputation = Mathf.Max(0f, resto.reputation - 5f);
        else if (resto.GetDirtLevel() == DirtLevel.Clean)
            resto.reputation = Mathf.Min(100f, resto.reputation + 0.5f);
    }

    private void ConsumeMeat(RestaurantData resto)
    {
        float consumption = GameConstants.MEAT_CONSUMPTION_PER_DAY_KG;
        consumption *= (1f + resto.employees.Count * 0.15f);
        resto.meatStockKg = Mathf.Max(0f, resto.meatStockKg - consumption);
    }

    private void ApplyEmployeeWages(RestaurantData resto)
    {
        float wages = resto.TotalDailyWageCost;
        if (money >= wages)
            money -= wages;
        else
        {
            debt += wages - money;
            money = 0f;
        }

        for (int i = 0; i < resto.employees.Count; i++)
            resto.employees[i].daysEmployed++;
    }

    private void ApplySideCost(RestaurantData resto)
    {
        float sideCost = resto.GetIngredientsDailyCost();
        if (sideCost <= 0f) return;
        if (money >= sideCost)
            money -= sideCost;
        else
        {
            debt += sideCost - money;
            money = 0f;
        }

        float hygieneCost = resto.hygieneStaffCount * GameConstants.HYGIENE_STAFF_DAILY_COST;
        if (hygieneCost <= 0f) return;
        if (money >= hygieneCost)
            money -= hygieneCost;
        else
        {
            debt += hygieneCost - money;
            money = 0f;
        }
    }

    // ======================== HELPERS ========================

    private static int GetEquipmentLevel(RestaurantData r, EquipmentType t)
    {
        switch (t)
        {
            case EquipmentType.Grill: return r.grillLevel;
            case EquipmentType.Fridge: return r.fridgeLevel;
            case EquipmentType.Vitrine: return r.vitrineLevel;
            default: return 1;
        }
    }

    private static void SetEquipmentLevel(RestaurantData r, EquipmentType t, int level)
    {
        switch (t)
        {
            case EquipmentType.Grill: r.grillLevel = level; break;
            case EquipmentType.Fridge: r.fridgeLevel = level; break;
            case EquipmentType.Vitrine: r.vitrineLevel = level; break;
        }
    }

    private bool IsValidIndex(int index) => index >= 0 && index < restaurants.Count;

    private void SyncManagersWithData()
    {
        EnsureRestaurantManagers();
        for (int i = 0; i < restaurantManagers.Count && i < restaurants.Count; i++)
        {
            if (restaurantManagers[i] != null)
                restaurantManagers[i].BindData(restaurants[i]);
        }
    }

    /// <summary>Crée les RestaurantManager manquants (bootstrap 3D / nouvel achat).</summary>
    public void EnsureRestaurantManagers()
    {
        while (restaurantManagers.Count < restaurants.Count)
        {
            var go = new GameObject("RestaurantManager_" + restaurantManagers.Count);
            go.transform.SetParent(transform, false);
            var mgr = go.AddComponent<RestaurantManager>();
            restaurantManagers.Add(mgr);
        }
    }

    private void SyncManager(int index)
    {
        if (index < restaurantManagers.Count && restaurantManagers[index] != null)
            restaurantManagers[index].BindData(restaurants[index]);
    }

    public void Notify(string message)
    {
        Debug.Log($"[Empire] {message}");
        OnNotification?.Invoke(message);
    }

    private void NotifyEmpireUpdated() => OnEmpireUpdated?.Invoke();

    /// <summary>Pour UI externe (overlay gestion).</summary>
    public void NotifyEmpireUpdatedPublic() => NotifyEmpireUpdated();

    // ======================== SAUVEGARDE ========================

    public void AutoSave()
    {
        SaveSystem.Save(BuildSaveData());
    }

    public SaveData BuildSaveData()
    {
        var data = new SaveData
        {
            money = money,
            debt = debt,
            monthlyRevenueAccumulated = monthlyRevenueAccumulated,
            currentDay = currentDay,
            currentMonth = currentMonth,
            globalReputation = globalReputation,
            taxesPaidThisMonth = taxesPaidThisMonth,
            restaurants = new List<RestaurantData>(restaurants),
            nextRestaurantId = nextRestaurantId,
            gameOver = gameOver,
            gameOverReason = gameOverReason,
            gamePhase = (int)gamePhase,
            awaitingSetup = awaitingSetup
        };

        if (competitorManager != null)
            data.competitors = competitorManager.GetCompetitorsCopy();

        return data;
    }

    public void ApplySaveData(SaveData data)
    {
        money = data.money;
        debt = data.debt;
        monthlyRevenueAccumulated = data.monthlyRevenueAccumulated;
        currentDay = data.currentDay;
        currentMonth = data.currentMonth;
        globalReputation = data.globalReputation;
        taxesPaidThisMonth = data.taxesPaidThisMonth;
        restaurants = data.restaurants ?? new List<RestaurantData>();
        nextRestaurantId = data.nextRestaurantId;
        gameOver = data.gameOver;
        gameOverReason = data.gameOverReason;
        gamePhase = (GamePhase)Mathf.Clamp(data.gamePhase, 0, 2);
        awaitingSetup = data.awaitingSetup;
        if (restaurants.Count == 0 && !awaitingSetup)
            awaitingSetup = true;

        if (competitorManager != null && data.competitors != null)
            competitorManager.LoadCompetitors(data.competitors);
    }

    public void NewGame()
    {
        SaveSystem.DeleteSave();
        InitializeNewGame();
        SyncManagersWithData();
        NotifyEmpireUpdated();
    }
}

public enum FinancialHealth
{
    Healthy,
    Unstable,
    Fragile
}

public enum InspectionType
{
    URSSAF,
    Health,
    Fiscal
}

[Serializable]
public class InspectionResult
{
    public InspectionType type;
    public int restaurantIndex;
    public string restaurantName;
    public bool passed;
    public float fine;
    public int closureDays;
    public string message;
}
