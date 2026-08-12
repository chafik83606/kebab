using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Écran de gestion d'un restaurant : ménage, viande, stock, employés,
/// matériel, service du patron, retour dashboard.
/// </summary>
public class RestaurantUI : MonoBehaviour
{
    [Header("Info")]
    public Text nameText;
    public Text dirtText;
    public Text meatText;
    public Text stockText;
    public Text revenueText;
    public Text employeesText;
    public Text equipmentText;
    public Text statusText;
    public Text ownerServiceText;

    [Header("Boutons actions")]
    public Button cleanButton;
    public Button meatBoeufButton;
    public Button meatPouletButton;
    public Button meatMysteryButton;
    public Button hireDeclaredButton;
    public Button hireUndeclaredButton;
    public Button fireButton;
    public Button buyMeatButton;
    public Button upgradeGrillButton;
    public Button upgradeFridgeButton;
    public Button upgradeVitrineButton;
    public Button ownerServiceButton;
    public Button openCounterButton;
    public Button backButton;
    public Button buyoutCompetitorButton;

    [Header("Références")]
    public UIManager uiManager;
    public CustomerServiceUI customerServiceUI;

    private int currentIndex = -1;
    private RestaurantData currentData;
    private RestaurantManager currentManager;

    private void Awake() => WireButtons();

    private void OnEnable()
    {
        WireButtons();
        if (currentData != null) Refresh();
    }

    private void WireButtons()
    {
        Bind(cleanButton, OnClean);
        Bind(meatBoeufButton, () => OnChangeMeat(MeatType.Boeuf));
        Bind(meatPouletButton, () => OnChangeMeat(MeatType.Poulet));
        Bind(meatMysteryButton, () => OnChangeMeat(MeatType.PreferePasSavoir));
        Bind(hireDeclaredButton, () => OnHire(true));
        Bind(hireUndeclaredButton, () => OnHire(false));
        Bind(fireButton, OnFireLast);
        Bind(buyMeatButton, OnBuyMeat);
        Bind(upgradeGrillButton, () => OnUpgrade(EquipmentType.Grill));
        Bind(upgradeFridgeButton, () => OnUpgrade(EquipmentType.Fridge));
        Bind(upgradeVitrineButton, () => OnUpgrade(EquipmentType.Vitrine));
        Bind(ownerServiceButton, OnToggleOwnerService);
        Bind(openCounterButton, OnOpenCounter);
        Bind(backButton, OnBack);
        Bind(buyoutCompetitorButton, OnBuyoutBankrupt);
    }

    private static void Bind(Button btn, UnityEngine.Events.UnityAction action)
    {
        if (btn == null) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(action);
    }

    public void ShowRestaurant(int index)
    {
        currentIndex = index;
        if (EmpireManager.Instance == null) return;

        currentData = EmpireManager.Instance.GetRestaurant(index);
        currentManager = null;
        if (index < EmpireManager.Instance.restaurantManagers.Count)
            currentManager = EmpireManager.Instance.restaurantManagers[index];

        Refresh();
        gameObject.SetActive(true);
    }

    public void Refresh()
    {
        if (currentData == null) return;

        Set(nameText, $"{currentData.restaurantName} — {currentData.locationName}");
        Set(dirtText, $"Saleté : {currentData.currentDirt:F0}% ({currentData.GetDirtLevelLabel()})");
        Set(meatText, $"Viande : {currentData.currentMeat.GetDisplayName()}");
        Set(stockText, $"Stock viande : {currentData.meatStockKg:F1} kg");
        Set(revenueText, $"Revenu hier : {currentData.dailyRevenue:F0} €");
        Set(employeesText,
            $"Employés : {currentData.employees.Count} " +
            $"(Déclarés {currentData.DeclaredEmployeeCount} / Black {currentData.UndeclaredEmployeeCount})");
        Set(equipmentText,
            $"Grill Nv.{currentData.grillLevel} | Frigo Nv.{currentData.fridgeLevel} | Vitrine Nv.{currentData.vitrineLevel}");
        Set(statusText, currentData.IsClosed
            ? $"FERMÉ — {currentData.closureDaysRemaining} j restant(s)"
            : "Ouvert");

        Set(ownerServiceText, currentData.ownerIsWorking
            ? $"Service patron : OUI (+{GameConstants.OWNER_SERVICE_REVENUE_BONUS:F0}€/j)"
            : "Service patron : NON (vous ne travaillez pas au comptoir)");

        if (ownerServiceButton != null)
        {
            var label = ownerServiceButton.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = currentData.ownerIsWorking
                    ? "Arrêter de faire le service"
                    : "Je fais le service moi-même";
            }
        }

        if (openCounterButton != null)
        {
            openCounterButton.gameObject.SetActive(true);
            openCounterButton.interactable = currentData.ownerIsWorking && !currentData.IsClosed;
            var label = openCounterButton.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = currentData.ownerIsWorking
                    ? "Passer derrière la caisse\n(prendre les commandes)"
                    : "Caisse : active d'abord le service patron";
            }
        }
    }

    private void OnOpenCounter()
    {
        if (currentData == null || !currentData.ownerIsWorking)
        {
            EmpireManager.Instance?.Notify("Active d'abord « Je fais le service moi-même ».");
            return;
        }

        if (customerServiceUI == null)
            customerServiceUI = FindObjectOfType<CustomerServiceUI>();

        if (customerServiceUI != null)
            customerServiceUI.OpenForRestaurant(currentData);
        else
            EmpireManager.Instance?.Notify("UI caisse introuvable — relance Setup Main Scene.");
    }

    private void OnClean()
    {
        if (currentManager != null)
            currentManager.CleanRestaurant();
        else if (currentData != null)
        {
            currentData.currentDirt = 0f;
            EmpireManager.Instance?.Notify($"{currentData.restaurantName} nettoyé !");
            EmpireManager.Instance?.AutoSave();
        }
        Refresh();
    }

    private void OnChangeMeat(MeatType meat)
    {
        if (currentManager != null)
            currentManager.ChangeMeat(meat);
        else if (currentData != null)
        {
            currentData.currentMeat = meat;
            EmpireManager.Instance?.AutoSave();
        }
        Refresh();
    }

    private void OnHire(bool isDeclared)
    {
        if (currentManager != null)
            currentManager.HireEmployee(isDeclared);
        else if (currentData != null)
        {
            currentData.employees.Add(new Employee($"Employé {currentData.employees.Count + 1}", isDeclared));
            EmpireManager.Instance?.AutoSave();
        }
        Refresh();
    }

    private void OnFireLast()
    {
        if (currentData == null || currentData.employees.Count == 0) return;
        int index = currentData.employees.Count - 1;
        if (currentManager != null)
            currentManager.FireEmployee(index);
        else
        {
            EmpireManager.Instance?.Spend(GameConstants.FIRE_SEVERANCE_PAY, "Indemnité");
            currentData.employees.RemoveAt(index);
            EmpireManager.Instance?.AutoSave();
        }
        Refresh();
    }

    private void OnBuyMeat()
    {
        const float kg = 10f;
        if (currentManager != null)
            currentManager.BuyMeatStock(kg);
        else if (currentData != null && EmpireManager.Instance != null)
        {
            float cost = kg * currentData.currentMeat.GetPurchaseCostPerKg();
            if (EmpireManager.Instance.Spend(cost, $"Achat {kg} kg viande"))
            {
                currentData.meatStockKg += kg;
                EmpireManager.Instance.AutoSave();
            }
        }
        Refresh();
    }

    private void OnUpgrade(EquipmentType type)
    {
        if (currentManager != null)
            currentManager.UpgradeEquipment(type);
        else if (currentData != null && EmpireManager.Instance != null)
        {
            int current = type == EquipmentType.Grill ? currentData.grillLevel
                : type == EquipmentType.Fridge ? currentData.fridgeLevel
                : currentData.vitrineLevel;
            if (current < 3)
                EmpireManager.Instance.BuyEquipment(currentIndex, type, current + 1);
        }

        if (EmpireManager.Instance != null)
            currentData = EmpireManager.Instance.GetRestaurant(currentIndex);
        Refresh();
    }

    private void OnToggleOwnerService()
    {
        if (currentManager != null)
            currentManager.ToggleOwnerService();
        else if (currentData != null)
        {
            currentData.ownerIsWorking = !currentData.ownerIsWorking;
            EmpireManager.Instance?.AutoSave();
        }
        Refresh();
    }

    private void OnBuyoutBankrupt()
    {
        var cm = EmpireManager.Instance?.competitorManager;
        if (cm == null) return;
        var bankrupts = cm.GetBankruptCompetitors();
        if (bankrupts.Count == 0)
        {
            EmpireManager.Instance.Notify("Aucun concurrent en faillite.");
            return;
        }
        cm.BuyoutCompetitor(bankrupts[0]);
        Refresh();
    }

    private void OnBack()
    {
        if (uiManager != null)
            uiManager.ShowDashboard();
        else
            gameObject.SetActive(false);
    }

    private static void Set(Text t, string value)
    {
        if (t != null) t.text = value;
    }
}
