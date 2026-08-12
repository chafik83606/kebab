using System;
using UnityEngine;

/// <summary>
/// Orchestre le flux : carte → confirmation → assistant config → jeu.
/// </summary>
public class GameFlowController : MonoBehaviour
{
    public static GameFlowController Instance { get; private set; }

    public GamePhase Phase { get; private set; } = GamePhase.Playing;
    public FranceMapData.MapCity? PendingCity { get; private set; }
    public float PendingPrice { get; private set; }
    public bool ShowWorldMap { get; set; }

    public FranceMapUI mapUI;
    public RestaurantSetupWizardUI wizardUI;

    public event Action OnPhaseChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void InitFromSave(int phaseInt, bool awaitingSetup)
    {
        if (EmpireManager.Instance != null && EmpireManager.Instance.RestaurantCount == 0 && awaitingSetup)
            Phase = GamePhase.MapSelection;
        else
            Phase = (GamePhase)Mathf.Clamp(phaseInt, 0, 2);
    }

    public void BeginMapSelection(bool worldTab = false)
    {
        Phase = GamePhase.MapSelection;
        ShowWorldMap = worldTab;
        PendingCity = null;
        OnPhaseChanged?.Invoke();
        ShowMap();
    }

    public void SelectCity(FranceMapData.MapCity city)
    {
        PendingCity = city;
        PendingPrice = FranceMapData.GetPlacementPrice(city, EmpireManager.Instance?.RestaurantCount ?? 0);
        mapUI?.ShowConfirm(city, PendingPrice);
    }

    public bool ConfirmPlacement()
    {
        if (!PendingCity.HasValue || EmpireManager.Instance == null) return false;
        var city = PendingCity.Value;
        if (!EmpireManager.Instance.Spend(PendingPrice, $"Emplacement {city.displayName}"))
        {
            EmpireManager.Instance.Notify($"Pas assez d'argent ({PendingPrice:F0} €).");
            return false;
        }
        Phase = GamePhase.SetupWizard;
        mapUI?.Hide();
        wizardUI?.Begin(city);
        OnPhaseChanged?.Invoke();
        return true;
    }

    public void CancelPlacement()
    {
        PendingCity = null;
        mapUI?.HideConfirm();
    }

    public void CompleteSetup(RestaurantSetupConfig config)
    {
        if (!PendingCity.HasValue || EmpireManager.Instance == null) return;
        var city = PendingCity.Value;
        EmpireManager.Instance.FoundRestaurant(city, config);
        PendingCity = null;
        Phase = GamePhase.Playing;
        wizardUI?.Hide();
        OnPhaseChanged?.Invoke();
        GameWorldManager.Instance?.OnFlowPhaseChanged();
        GameWorldManager.Instance?.EnterNewlyFoundedRestaurant();
        EmpireManager.Instance.Notify($"Kebab ouvert à {city.displayName} !");
    }

    public void ShowMap()
    {
        mapUI?.Show(ShowWorldMap);
    }

    public bool IsBlockingGameplay =>
        Phase == GamePhase.MapSelection || Phase == GamePhase.SetupWizard;
}

[Serializable]
public class RestaurantSetupConfig
{
    public MeatType meat = MeatType.Poulet;
    public IngredientFreshness salad = IngredientFreshness.Frais;
    public IngredientFreshness tomato = IngredientFreshness.Frais;
    public IngredientFreshness onion = IngredientFreshness.Frais;
    public int kitchenStaff;
    public int declaredStaff;
    public int undeclaredStaff;
    public int hygieneStaff;
    public ManagementMode managementMode = ManagementMode.Automatic;
}
