/// <summary>
/// Constantes globales du jeu Kebab Empire.
/// Centralise tous les paramètres d'équilibrage pour un réglage facile.
/// </summary>
public static class GameConstants
{
    // --- Employés ---
    public const float DECLARED_EMPLOYEE_DAILY_COST = 150f;
    public const float UNDECLARED_EMPLOYEE_DAILY_COST = 70f;
    public const float FIRE_SEVERANCE_PAY = 200f;
    public const float URSSAF_FINE_PER_UNDECLARED = 5000f;
    public const int URSSAF_CLOSURE_DAYS = 3;

    // --- Hygiène / Contrôles ---
    public const float BASE_INSPECTION_CHANCE = 0.15f;
    public const float HYGIENE_STAFF_DAILY_COST = 95f;
    public const float HYGIENE_STAFF_DIRT_REDUCTION = 0.55f;
    public const float UNPAID_TAX_INSPECTION_BONUS = 0.12f;
    public const float FISCAL_FINE_RATE = 0.35f;
    public const int FISCAL_CLOSURE_DAYS = 2;
    public const float HEALTH_FINE = 2000f;
    public const int HEALTH_CLOSURE_DAYS = 2;
    public const float DIRT_THRESHOLD_NEGLECTED = 20f;
    public const float DIRT_THRESHOLD_DIRTY = 50f;
    public const float DIRT_THRESHOLD_INFESTATION = 75f;
    public const float BASE_DAILY_DIRT = 8f;
    public const float INFESTATION_REVENUE_PENALTY = 0.2f;

    // --- Finances ---
    public const int DAYS_PER_MONTH = 30;
    public const float TAX_RATE = 0.20f;
    public const float TAX_PENALTY_RATE = 0.10f;
    public const float FRAGILE_DEBT_MULTIPLIER = 3f;
    public const float STARTING_MONEY = 12000f;

    // --- Matériel ---
    public static readonly float[] GRILL_PRICES = { 0f, 500f, 1500f, 4000f };
    public static readonly float[] FRIDGE_PRICES = { 0f, 400f, 1200f, 3000f };
    public static readonly float[] VITRINE_PRICES = { 0f, 300f, 900f, 2500f };
    public static readonly int[] FRIDGE_SHELF_LIFE_DAYS = { 0, 7, 14, 30 };
    public static readonly float[] EQUIPMENT_REVENUE_BONUS = { 0f, 1.0f, 1.25f, 1.5f };

    // --- Restaurants ---
    public const float BASE_RESTAURANT_PRICE = 4500f;
    public const float BASE_DAILY_REVENUE = 400f;
    public const float REVENUE_PER_EMPLOYEE = 80f;
    public const float MEAT_CONSUMPTION_PER_DAY_KG = 5f;

    // --- Service du patron (le joueur derrière le comptoir) ---
    public const float OWNER_SERVICE_REVENUE_BONUS = 120f;
    public const float OWNER_SERVICE_EXTRA_DIRT = 3f;

    // --- Concurrents ---
    public const int COMPETITOR_COUNT = 3;
    public const float HOSTILE_TAKEOVER_BASE_CHANCE = 0.25f;
}
