using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI de prise de commande derrière la caisse.
/// Demande salade / tomate / oignon / sauce, puis encaisse.
/// </summary>
public class CustomerServiceUI : MonoBehaviour
{
    [Header("Panel")]
    public GameObject servicePanel;

    [Header("Textes")]
    public Text dialogueText;
    public Text ticketText;
    public Text hintText;

    [Header("Questions")]
    public Button askSaladButton;
    public Button askTomatoButton;
    public Button askOnionButton;
    public Button askSauceButton;

    [Header("Sauces (après avoir demandé)")]
    public Button sauceBlancheButton;
    public Button sauceAlgerienneButton;
    public Button sauceSamouraiButton;
    public Button sauceHarissaButton;
    public Button sauceKetchupMayoButton;
    public Button sauceSansButton;
    public GameObject sauceButtonsRoot;

    [Header("Actions")]
    public Button startCustomerButton;
    public Button checkoutButton;
    public Button cancelButton;
    public Button closeServiceButton;

    [Header("Références")]
    public CounterServiceController serviceController;
    public RestaurantUI restaurantUI;

    private readonly StringBuilder dialogueLog = new StringBuilder();
    private RestaurantData boundRestaurant;

    private void Awake()
    {
        if (serviceController == null)
            serviceController = FindObjectOfType<CounterServiceController>();
        WireButtons();
    }

    private void OnEnable()
    {
        WireButtons();
        if (serviceController != null)
        {
            serviceController.OnDialogue += AppendDialogue;
            serviceController.OnServiceStateChanged += Refresh;
        }
        Refresh();
    }

    private void OnDisable()
    {
        if (serviceController != null)
        {
            serviceController.OnDialogue -= AppendDialogue;
            serviceController.OnServiceStateChanged -= Refresh;
        }
    }

    private void WireButtons()
    {
        Bind(askSaladButton, () => serviceController?.AskSalad());
        Bind(askTomatoButton, () => serviceController?.AskTomato());
        Bind(askOnionButton, () => serviceController?.AskOnion());
        Bind(askSauceButton, () => serviceController?.AskSauce());

        Bind(sauceBlancheButton, () => serviceController?.SelectSauce(SauceType.Blanche));
        Bind(sauceAlgerienneButton, () => serviceController?.SelectSauce(SauceType.Algerienne));
        Bind(sauceSamouraiButton, () => serviceController?.SelectSauce(SauceType.Samourai));
        Bind(sauceHarissaButton, () => serviceController?.SelectSauce(SauceType.Harissa));
        Bind(sauceKetchupMayoButton, () => serviceController?.SelectSauce(SauceType.KetchupMayo));
        Bind(sauceSansButton, () => serviceController?.SelectSauce(SauceType.SansSauce));

        Bind(startCustomerButton, OnStartCustomer);
        Bind(checkoutButton, OnCheckout);
        Bind(cancelButton, () => serviceController?.CancelCustomer());
        Bind(closeServiceButton, ClosePanel);
    }

    private static void Bind(Button btn, UnityEngine.Events.UnityAction action)
    {
        if (btn == null) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(action);
    }

    public void OpenForRestaurant(RestaurantData data)
    {
        boundRestaurant = data;
        if (servicePanel != null) servicePanel.SetActive(true);
        dialogueLog.Clear();
        AppendDialogue("Tu es derrière la caisse. Un client peut arriver…");
        Refresh();
    }

    public void ClosePanel()
    {
        if (CounterService3D.Instance != null)
        {
            CounterService3D.Instance.EndSession();
            return;
        }
        ClosePanelImmediate();
    }

    /// <summary>Ferme l'UI sans relancer EndSession (appel interne).</summary>
    public void ClosePanelImmediate()
    {
        if (serviceController != null && serviceController.HasActiveCustomer)
            serviceController.CancelCustomer();
        if (servicePanel != null) servicePanel.SetActive(false);
        restaurantUI?.Refresh();
    }

    private void OnStartCustomer()
    {
        if (boundRestaurant == null && EmpireManager.Instance != null)
            boundRestaurant = EmpireManager.Instance.GetRestaurant(0);

        dialogueLog.Clear();
        serviceController?.StartNextCustomer(boundRestaurant);
        restaurantUI?.Refresh();
    }

    private void OnCheckout()
    {
        if (boundRestaurant == null) return;
        float moneyBefore = EmpireManager.Instance != null ? EmpireManager.Instance.Money : 0f;
        float meatBefore = boundRestaurant.meatStockKg;
        bool ok = serviceController != null && serviceController.Checkout(boundRestaurant);
        if (ok && EmpireManager.Instance != null)
        {
            float gained = EmpireManager.Instance.Money - moneyBefore;
            float meatLeft = boundRestaurant.meatStockKg;
            if (hintText != null)
            {
                hintText.text = gained > 0
                    ? $"+{gained:F0} € · Viande {meatLeft:F1} kg (−{meatBefore - meatLeft:F1}) · Client suivant ?"
                    : $"Encaissé · Viande {meatLeft:F1} kg";
            }
            EmpireManager.Instance.NotifyEmpireUpdatedPublic();
        }
        restaurantUI?.Refresh();
    }

    private void AppendDialogue(string line)
    {
        if (dialogueLog.Length > 0) dialogueLog.AppendLine();
        dialogueLog.Append(line);
        // Garde les ~8 dernières lignes
        string full = dialogueLog.ToString();
        string[] lines = full.Split('\n');
        if (lines.Length > 8)
        {
            dialogueLog.Clear();
            for (int i = lines.Length - 8; i < lines.Length; i++)
            {
                if (i > lines.Length - 8) dialogueLog.AppendLine();
                dialogueLog.Append(lines[i]);
            }
        }
        if (dialogueText != null)
            dialogueText.text = dialogueLog.ToString();
    }

    public void Refresh()
    {
        var s = serviceController;
        bool has = s != null && s.HasActiveCustomer;

        if (ticketText != null)
            ticketText.text = s != null ? s.GetTicketText() : "";

        if (hintText != null)
        {
            if (boundRestaurant != null && !boundRestaurant.ownerIsWorking)
                hintText.text = "Active d'abord « Je fais le service moi-même ».";
            else if (!has)
                hintText.text = "Appuie sur « Client suivant » puis pose tes questions.";
            else if (!s.AskedSauce)
                hintText.text = "Demande : salade, tomate, oignon, puis la sauce.";
            else if (!s.SauceSelectedByPlayer)
                hintText.text = "Le client a dit sa sauce — choisis-la sur le présentoir !";
            else
                hintText.text = "Commande prête — encaisse le client.";
        }

        SetInteractable(askSaladButton, has && s != null && !s.AskedSalad);
        SetInteractable(askTomatoButton, has && s != null && !s.AskedTomato);
        SetInteractable(askOnionButton, has && s != null && !s.AskedOnion);
        SetInteractable(askSauceButton, has && s != null && !s.AskedSauce);

        bool showSauces = has && s != null && s.AskedSauce && !s.SauceSelectedByPlayer;
        if (sauceButtonsRoot != null)
            sauceButtonsRoot.SetActive(showSauces);
        else
        {
            SetInteractable(sauceBlancheButton, showSauces);
            SetInteractable(sauceAlgerienneButton, showSauces);
            SetInteractable(sauceSamouraiButton, showSauces);
            SetInteractable(sauceHarissaButton, showSauces);
            SetInteractable(sauceKetchupMayoButton, showSauces);
            SetInteractable(sauceSansButton, showSauces);
        }

        bool canStart = boundRestaurant != null && boundRestaurant.ownerIsWorking && !has;
        SetInteractable(startCustomerButton, canStart);
        SetInteractable(checkoutButton, has && s != null && s.TicketComplete);
        SetInteractable(cancelButton, has);
    }

    private static void SetInteractable(Button btn, bool value)
    {
        if (btn != null) btn.interactable = value;
    }
}
