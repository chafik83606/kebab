using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dashboard global : argent, jour, restos, impôts, achat local, concurrents.
/// Les actions détaillées (ménage, viande, employés…) sont dans RestaurantUI.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("Textes — Dashboard")]
    public Text moneyText;
    public Text dayText;
    public Text reputationText;
    public Text restaurantCountText;
    public Text debtText;
    public Text healthText;
    public Text notificationText;
    public Text competitorsText;

    [Header("Boutons dashboard")]
    public Button nextDayButton;
    public Button payTaxesButton;
    public Button buyRestaurantButton;
    public Button manageFirstRestaurantButton;
    public Button buyoutCompetitorButton;
    public Button newGameButton;

    [Header("Liste restaurants")]
    public Transform restaurantListParent;
    public Button restaurantButtonPrefab;

    [Header("Panels")]
    public GameObject dashboardPanel;
    public GameObject restaurantDetailPanel;
    public GameObject gameOverPanel;
    public GameObject hostileTakeoverPanel;
    public Text gameOverText;
    public Text hostileTakeoverText;

    [Header("Rachat hostile")]
    public Button acceptTakeoverButton;
    public Button refuseTakeoverButton;

    [Header("Références")]
    public RestaurantUI restaurantUI;

    private readonly List<Button> restaurantButtons = new List<Button>();
    private CompetitorData pendingHostileCompetitor;
    private float notificationTimer;

    public void Initialize()
    {
        WireButtons();
        SubscribeEvents();
        RefreshAll();
        ShowDashboard();
        StartCoroutine(RefreshNextFrame());
    }

    private System.Collections.IEnumerator RefreshNextFrame()
    {
        yield return null;
        RefreshAll();
    }

    private void OnDestroy() => UnsubscribeEvents();

    private void Update()
    {
        if (notificationTimer > 0f)
        {
            notificationTimer -= Time.deltaTime;
            if (notificationTimer <= 0f && notificationText != null)
                notificationText.text = "";
        }
    }

    private void WireButtons()
    {
        Bind(nextDayButton, OnNextDay);
        Bind(payTaxesButton, OnPayTaxes);
        Bind(buyRestaurantButton, OnBuyRestaurant);
        Bind(manageFirstRestaurantButton, () => OpenRestaurant(0));
        Bind(buyoutCompetitorButton, OnBuyoutCompetitor);
        Bind(newGameButton, OnNewGame);
        Bind(acceptTakeoverButton, OnAcceptHostileTakeover);
        Bind(refuseTakeoverButton, OnRefuseHostileTakeover);
    }

    private static void Bind(Button btn, UnityEngine.Events.UnityAction action)
    {
        if (btn == null) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(action);
    }

    private void SubscribeEvents()
    {
        if (EmpireManager.Instance == null) return;
        EmpireManager.Instance.OnEmpireUpdated += RefreshAll;
        EmpireManager.Instance.OnNotification += ShowNotification;
        EmpireManager.Instance.OnGameOver += ShowGameOver;
        EmpireManager.Instance.OnHostileTakeoverOffer += ShowHostileTakeover;
        EmpireManager.Instance.OnTaxesDue += OnTaxesDue;
        EmpireManager.Instance.OnInspectionOccurred += OnInspection;
    }

    private void UnsubscribeEvents()
    {
        if (EmpireManager.Instance == null) return;
        EmpireManager.Instance.OnEmpireUpdated -= RefreshAll;
        EmpireManager.Instance.OnNotification -= ShowNotification;
        EmpireManager.Instance.OnGameOver -= ShowGameOver;
        EmpireManager.Instance.OnHostileTakeoverOffer -= ShowHostileTakeover;
        EmpireManager.Instance.OnTaxesDue -= OnTaxesDue;
        EmpireManager.Instance.OnInspectionOccurred -= OnInspection;
    }

    private void OnTaxesDue() => ShowNotification("Impôts du mois à régler !");
    private void OnInspection(InspectionResult r) => ShowNotification(r.message);

    private void OnNextDay() => EmpireManager.Instance?.StartNewDay();
    private void OnPayTaxes() => EmpireManager.Instance?.PayTaxes();

    private void OnBuyRestaurant() =>
        EmpireManager.Instance?.BuyNewRestaurant(GameConstants.BASE_RESTAURANT_PRICE);

    private void OnBuyoutCompetitor()
    {
        var cm = EmpireManager.Instance?.competitorManager;
        if (cm == null) return;
        var list = cm.GetBankruptCompetitors();
        if (list.Count == 0)
        {
            EmpireManager.Instance.Notify("Aucun concurrent en faillite pour le moment.");
            return;
        }
        cm.BuyoutCompetitor(list[0]);
    }

    private void OnNewGame()
    {
        EmpireManager.Instance?.NewGame();
        ShowDashboard();
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    public void OnAcceptHostileTakeover()
    {
        if (pendingHostileCompetitor != null)
            EmpireManager.Instance?.AcceptHostileTakeover(pendingHostileCompetitor);
        if (hostileTakeoverPanel != null) hostileTakeoverPanel.SetActive(false);
    }

    public void OnRefuseHostileTakeover()
    {
        EmpireManager.Instance?.RefuseHostileTakeover();
        if (hostileTakeoverPanel != null) hostileTakeoverPanel.SetActive(false);
    }

    public void OpenRestaurant(int index)
    {
        if (EmpireManager.Instance == null || EmpireManager.Instance.RestaurantCount == 0)
        {
            ShowNotification("Aucun restaurant à gérer.");
            return;
        }
        if (index < 0 || index >= EmpireManager.Instance.RestaurantCount)
            index = 0;

        if (restaurantUI != null)
            restaurantUI.ShowRestaurant(index);

        if (dashboardPanel != null) dashboardPanel.SetActive(false);
        if (restaurantDetailPanel != null) restaurantDetailPanel.SetActive(true);
    }

    public void ShowDashboard()
    {
        if (dashboardPanel != null) dashboardPanel.SetActive(true);
        if (restaurantDetailPanel != null) restaurantDetailPanel.SetActive(false);
        RefreshAll();
    }

    public void RefreshAll()
    {
        var empire = EmpireManager.Instance;
        if (empire == null) return;

        Set(moneyText, $"{empire.Money:F0} €");
        Set(dayText, $"Jour {empire.CurrentDay} — Mois {empire.CurrentMonth}");
        Set(reputationText, $"Réputation : {empire.GlobalReputation:F0}");
        Set(restaurantCountText, $"Restaurants : {empire.RestaurantCount}");
        Set(debtText, $"Dettes : {empire.Debt:F0} €");
        Set(healthText, $"Santé : {empire.GetFinancialHealthLabel()}");

        // Impôts
        if (payTaxesButton != null)
        {
            bool due = !empire.TaxesPaidThisMonth;
            float tax = empire.GetTaxAmountDue();
            payTaxesButton.interactable = due && tax > 0f;
            var label = payTaxesButton.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = due
                    ? $"Payer impôts ({tax:F0} €)"
                    : "Impôts : rien à payer";
            }
        }

        if (buyRestaurantButton != null)
        {
            var label = buyRestaurantButton.GetComponentInChildren<Text>();
            if (label != null)
                label.text = $"Acheter un kebab ({GameConstants.BASE_RESTAURANT_PRICE:F0} €)";
            buyRestaurantButton.interactable =
                empire.Money >= GameConstants.BASE_RESTAURANT_PRICE && !empire.IsGameOver;
        }

        if (manageFirstRestaurantButton != null)
            manageFirstRestaurantButton.interactable = empire.RestaurantCount > 0;

        // Concurrents
        RefreshCompetitorsInfo();

        if (nextDayButton != null)
            nextDayButton.interactable = !empire.IsGameOver;

        RebuildRestaurantList();
    }

    private void RefreshCompetitorsInfo()
    {
        var cm = EmpireManager.Instance?.competitorManager;
        if (cm == null)
        {
            Set(competitorsText, "Concurrents : —");
            if (buyoutCompetitorButton != null)
                buyoutCompetitorButton.interactable = false;
            return;
        }

        int alive = 0;
        int bankrupt = 0;
        string bankruptName = null;
        float buyoutPrice = 0f;

        foreach (var c in cm.Competitors)
        {
            if (c.isBankrupt)
            {
                bankrupt++;
                if (bankruptName == null)
                {
                    bankruptName = c.competitorName;
                    buyoutPrice = c.GetBuyoutPrice();
                }
            }
            else alive++;
        }

        if (bankrupt > 0)
        {
            Set(competitorsText,
                $"Concurrents : {alive} actifs, {bankrupt} en faillite\n" +
                $"Rachat possible : {bankruptName} ({buyoutPrice:F0} €)");
            if (buyoutCompetitorButton != null)
            {
                buyoutCompetitorButton.interactable =
                    EmpireManager.Instance.Money >= buyoutPrice;
                var label = buyoutCompetitorButton.GetComponentInChildren<Text>();
                if (label != null)
                    label.text = $"Racheter {bankruptName} ({buyoutPrice:F0} €)";
            }
        }
        else
        {
            Set(competitorsText, $"Concurrents : {alive} actifs (aucun en faillite)");
            if (buyoutCompetitorButton != null)
            {
                buyoutCompetitorButton.interactable = false;
                var label = buyoutCompetitorButton.GetComponentInChildren<Text>();
                if (label != null)
                    label.text = "Racheter concurrent (aucun en faillite)";
            }
        }
    }

    private void RebuildRestaurantList()
    {
        if (restaurantListParent == null) return;

        for (int i = restaurantButtons.Count - 1; i >= 0; i--)
        {
            if (restaurantButtons[i] != null)
                Destroy(restaurantButtons[i].gameObject);
        }
        restaurantButtons.Clear();

        var empire = EmpireManager.Instance;
        if (empire == null) return;

        for (int i = 0; i < empire.Restaurants.Count; i++)
        {
            int index = i;
            restaurantButtons.Add(CreateRestaurantButton(empire.Restaurants[i], index));
        }
    }

    private Button CreateRestaurantButton(RestaurantData r, int index)
    {
        Button btn;
        if (restaurantButtonPrefab != null)
        {
            btn = Instantiate(restaurantButtonPrefab, restaurantListParent);
            btn.gameObject.SetActive(true);
        }
        else
        {
            var go = new GameObject($"RestoBtn_{index}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(restaurantListParent, false);
            var le = go.GetComponent<LayoutElement>();
            le.minHeight = 90f;
            le.preferredHeight = 90f;
            le.flexibleWidth = 1f;

            go.GetComponent<Image>().color = new Color(0.55f, 0.32f, 0.12f, 1f);
            btn = go.GetComponent<Button>();

            var textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var txt = textGo.AddComponent<Text>();
            txt.font = GetUIFont();
            txt.alignment = TextAnchor.MiddleLeft;
            txt.color = Color.white;
            txt.fontSize = 28;
            txt.fontStyle = FontStyle.Bold;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            var trt = textGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(16f, 4f);
            trt.offsetMax = new Vector2(-16f, -4f);
        }

        string owner = r.ownerIsWorking ? " | Patron au service" : "";
        string label = $"▶ {r.restaurantName}\n{r.GetDirtLevelLabel()} · {r.currentMeat.GetDisplayName()}{owner}";
        var text = btn.GetComponentInChildren<Text>();
        if (text != null) text.text = label;

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => OpenRestaurant(index));
        return btn;
    }

    private void ShowNotification(string message)
    {
        Set(notificationText, message);
        notificationTimer = 5f;
        Debug.Log("[UI] " + message);
    }

    private void ShowGameOver(string reason)
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        int score = EmpireManager.Instance != null ? EmpireManager.Instance.CalculateFinalScore() : 0;
        Set(gameOverText, $"GAME OVER\n{reason}\n\nScore final : {score}");
    }

    private void ShowHostileTakeover(CompetitorData competitor)
    {
        pendingHostileCompetitor = competitor;
        if (hostileTakeoverPanel != null) hostileTakeoverPanel.SetActive(true);
        Set(hostileTakeoverText,
            $"{competitor.competitorName} veut racheter votre empire !\nAccepter = Game Over  |  Refuser = Faillite");
    }

    private static void Set(Text t, string value)
    {
        if (t != null) t.text = value;
    }

    private static Font GetUIFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return font;
    }
}
