using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Overlay gestion 3D : viande, accompagnements, staff, impôts, achat resto.
/// Le joueur est le patron — pas le client au comptoir.
/// </summary>
public class ManageOverlayUI : MonoBehaviour
{
    public Text infoText;
    public Button cleanButton;
    public Button buyMeatButton;
    public Button hireDeclaredButton;
    public Button hireBlackButton;
    public Button fireButton;
    public Button meatBoeufButton;
    public Button meatPouletButton;
    public Button meatMysteryButton;
    public Button sideFreshButton;
    public Button sideMediumButton;
    public Button sideLowButton;
    public Button payTaxesButton;
    public Button modeToggleButton;
    public Button buyRestaurantButton;
    public Button closeButton;

    private void OnEnable()
    {
        Wire();
        Refresh();
    }

    private void Wire()
    {
        Bind(cleanButton, OnClean);
        Bind(buyMeatButton, OnBuyMeat);
        Bind(hireDeclaredButton, () => OnHire(true));
        Bind(hireBlackButton, () => OnHire(false));
        Bind(fireButton, OnFire);
        Bind(meatBoeufButton, () => OnChangeMeat(MeatType.Boeuf));
        Bind(meatPouletButton, () => OnChangeMeat(MeatType.Poulet));
        Bind(meatMysteryButton, () => OnChangeMeat(MeatType.PreferePasSavoir));
        Bind(sideFreshButton, () => OnSetAllIngredients(IngredientFreshness.Frais));
        Bind(sideMediumButton, OnToggleMode);
        Bind(sideLowButton, () => OnSetAllIngredients(IngredientFreshness.PeuFrais));
        Bind(payTaxesButton, OnPayTaxes);
        Bind(modeToggleButton, OnToggleMode);
        Bind(buyRestaurantButton, OnBuyRestaurant);
        Bind(closeButton, () =>
        {
            gameObject.SetActive(false);
            if (GameWorldManager.Instance != null && GameWorldManager.Instance.player != null)
                GameWorldManager.Instance.player.SetInputEnabled(true);
        });
    }

    private static void Bind(Button btn, UnityEngine.Events.UnityAction action)
    {
        if (btn == null) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(action);
    }

    private RestaurantManager GetManager()
    {
        var e = EmpireManager.Instance;
        if (e == null || e.RestaurantCount == 0) return null;
        e.EnsureRestaurantManagers();
        int idx = GetRestaurantIndex();
        if (e.restaurantManagers != null && idx < e.restaurantManagers.Count)
        {
            var mgr = e.restaurantManagers[idx];
            if (mgr != null) mgr.BindData(e.GetRestaurant(idx));
            return mgr;
        }
        return null;
    }

    private RestaurantData GetData()
    {
        var e = EmpireManager.Instance;
        if (e == null || e.RestaurantCount == 0) return null;
        return e.GetRestaurant(GetRestaurantIndex());
    }

    private int GetRestaurantIndex()
    {
        var e = EmpireManager.Instance;
        if (e == null || e.RestaurantCount == 0) return 0;

        if (GameWorldManager.Instance != null && GameWorldManager.Instance.CurrentBuilding != null)
        {
            int idx = GameWorldManager.Instance.CurrentBuilding.restaurantIndex;
            return Mathf.Clamp(idx, 0, e.RestaurantCount - 1);
        }

        // Préfère un vrai emplacement (région) plutôt qu'un fantôme
        for (int i = e.RestaurantCount - 1; i >= 0; i--)
        {
            var r = e.GetRestaurant(i);
            if (r != null && !EmpireManager.IsPlaceholderRestaurant(r))
                return i;
        }
        return Mathf.Clamp(e.RestaurantCount - 1, 0, e.RestaurantCount - 1);
    }

    public void Refresh()
    {
        var e = EmpireManager.Instance;
        var data = GetData();
        if (infoText == null) return;

        if (e == null || data == null)
        {
            infoText.text = "Aucune donnée restaurant.";
            return;
        }

        if (data.locationMultiplier <= 0f)
            data.locationMultiplier = LocationHelper.GetMultiplier(data.locationName);

        infoText.text =
            $"{data.locationName} ×{data.locationMultiplier:F2} · {e.Money:F0} € · J{e.CurrentDay}\n" +
            $"{data.currentMeat.GetDisplayName()} {data.meatStockKg:F0}kg · Rép {data.reputation:F0} · Hier +{data.dailyRevenue:F0}€\n" +
            $"{data.employees.Count} staff · Salété {data.currentDirt:F0}% · {(data.managementMode == ManagementMode.Automatic ? "AUTO" : "MANUEL")}";

        if (payTaxesButton != null)
        {
            payTaxesButton.interactable = !e.TaxesPaidThisMonth && e.GetTaxAmountDue() > 0f;
            var label = payTaxesButton.GetComponentInChildren<Text>();
            if (label != null)
                label.text = e.TaxesPaidThisMonth ? "Impôts OK" : $"Payer impôts ({e.GetTaxAmountDue():F0} €)";
        }
    }

    private void OnClean()
    {
        GetManager()?.CleanRestaurant();
        if (GameWorldManager.Instance != null && GameWorldManager.Instance.CurrentBuilding != null)
        {
            GameWorldManager.Instance.CurrentBuilding.interior?
                .GetComponentInChildren<HygieneVisualController>()?
                .UpdateVisuals(0f);
        }
        EmpireManager.Instance?.Notify("Ménage fait !");
        Refresh();
    }

    private void OnBuyMeat()
    {
        var mgr = GetManager();
        if (mgr == null) return;
        if (mgr.BuyMeatStock(10f))
            EmpireManager.Instance?.Notify("Stock viande +10 kg");
        Refresh();
    }

    private void OnHire(bool declared)
    {
        GetManager()?.HireEmployee(declared);
        EmpireManager.Instance?.Notify(declared ? "Embauche déclarée." : "Embauche au black (risque URSSAF).");
        Refresh();
    }

    private void OnFire()
    {
        var data = GetData();
        var mgr = GetManager();
        if (data == null || mgr == null || data.employees.Count == 0)
        {
            EmpireManager.Instance?.Notify("Aucun employé à licencier.");
            return;
        }
        mgr.FireEmployee(data.employees.Count - 1);
        Refresh();
    }

    private void OnChangeMeat(MeatType meat)
    {
        GetManager()?.ChangeMeat(meat);
        Refresh();
    }

    private void OnSetAllIngredients(IngredientFreshness f)
    {
        var data = GetData();
        if (data == null) return;
        data.saladFreshness = data.tomatoFreshness = data.onionFreshness = f;
        EmpireManager.Instance?.Notify($"Ingrédients : tout {f.GetDisplayName().ToLower()}");
        EmpireManager.Instance?.AutoSave();
        Refresh();
    }

    private void OnPayTaxes()
    {
        EmpireManager.Instance?.PayTaxes();
        Refresh();
    }

    private void OnBuyRestaurant()
    {
        gameObject.SetActive(false);
        if (GameWorldManager.Instance != null && GameWorldManager.Instance.player != null)
            GameWorldManager.Instance.player.SetInputEnabled(true);
        GameFlowController.Instance?.BeginMapSelection(false);
    }

    private void OnToggleMode()
    {
        var data = GetData();
        if (data == null) return;
        data.managementMode = data.managementMode == ManagementMode.Automatic
            ? ManagementMode.Manual
            : ManagementMode.Automatic;
        data.ownerIsWorking = data.managementMode == ManagementMode.Manual;
        EmpireManager.Instance?.Notify(data.managementMode == ManagementMode.Automatic
            ? "Mode AUTO — le kebab se gère seul."
            : "Mode MANUEL — vous pouvez encaisser à la caisse.");
        EmpireManager.Instance?.AutoSave();
        GameWorldManager.Instance?.RefreshCounterVisibility();
        Refresh();
    }
}
