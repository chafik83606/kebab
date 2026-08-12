using System;
using UnityEngine;

/// <summary>
/// Mini-jeu de prise de commande derrière la caisse.
/// Le joueur demande : salade ? tomate ? oignon ? quelle sauce ?
/// Puis encaisse le client.
/// </summary>
public class CounterServiceController : MonoBehaviour
{
    public static CounterServiceController Instance { get; private set; }

    public event Action OnServiceStateChanged;
    public event Action<string> OnDialogue;
    public event Action OnServiceFinished;

    [Header("Économie du service")]
    [SerializeField] private float baseKebabPrice = 8f;
    [SerializeField] private float tipAmount = 2f;
    [SerializeField] private float meatPerKebabKg = 0.25f;
    [SerializeField] private float wrongSaucePenalty = 3f;

    public bool IsServing { get; private set; }
    public bool HasActiveCustomer { get; private set; }
    public KebabOrder CustomerWish { get; private set; }
    public KebabOrder Ticket { get; private set; }

    public bool AskedSalad { get; private set; }
    public bool AskedTomato { get; private set; }
    public bool AskedOnion { get; private set; }
    public bool AskedSauce { get; private set; }
    public bool SauceSelectedByPlayer { get; private set; }
    public SauceType? PlayerChosenSauce { get; private set; }

    public string CustomerName { get; private set; }
    public int CustomersServedToday { get; private set; }

    public bool TicketComplete =>
        AskedSalad && AskedTomato && AskedOnion && AskedSauce && SauceSelectedByPlayer;

    private static readonly string[] CustomerNames =
    {
        "Karim", "Léa", "Mehmet", "Sophie", "Yanis", "Julie", "Omar", "Nina", "Bilal", "Emma"
    };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    /// <summary>Démarre le service d'un nouveau client (nécessite patron au comptoir).</summary>
    public bool StartNextCustomer(RestaurantData resto)
    {
        if (resto == null || !resto.ownerIsWorking)
        {
            EmpireManager.Instance?.Notify("Active d'abord « Je fais le service moi-même ».");
            return false;
        }

        if (resto.IsClosed)
        {
            EmpireManager.Instance?.Notify("Le restaurant est fermé.");
            return false;
        }

        if (resto.meatStockKg < meatPerKebabKg)
        {
            EmpireManager.Instance?.Notify("Plus assez de viande en stock !");
            return false;
        }

        IsServing = true;
        HasActiveCustomer = true;
        CustomerWish = KebabOrder.CreateRandom();
        Ticket = new KebabOrder();
        AskedSalad = AskedTomato = AskedOnion = AskedSauce = false;
        SauceSelectedByPlayer = false;
        PlayerChosenSauce = null;
        CustomerName = CustomerNames[UnityEngine.Random.Range(0, CustomerNames.Length)];

        Speak($"{CustomerName} : « Bonjour ! Un kebab s'il vous plaît. »");
        Speak($"Toi : « Bien sûr ! Je vous prends la commande… »");
        OnServiceStateChanged?.Invoke();
        return true;
    }

    public void AskSalad()
    {
        if (!HasActiveCustomer || AskedSalad) return;
        AskedSalad = true;
        Ticket.wantsSalad = CustomerWish.wantsSalad;
        Speak(CustomerWish.wantsSalad
            ? $"{CustomerName} : « Oui, avec de la salade. »"
            : $"{CustomerName} : « Non, sans salade. »");
        OnServiceStateChanged?.Invoke();
    }

    public void AskTomato()
    {
        if (!HasActiveCustomer || AskedTomato) return;
        AskedTomato = true;
        Ticket.wantsTomato = CustomerWish.wantsTomato;
        Speak(CustomerWish.wantsTomato
            ? $"{CustomerName} : « Oui, tomate. »"
            : $"{CustomerName} : « Sans tomate, merci. »");
        OnServiceStateChanged?.Invoke();
    }

    public void AskOnion()
    {
        if (!HasActiveCustomer || AskedOnion) return;
        AskedOnion = true;
        Ticket.wantsOnion = CustomerWish.wantsOnion;
        Speak(CustomerWish.wantsOnion
            ? $"{CustomerName} : « Oignon, oui. »"
            : $"{CustomerName} : « Pas d'oignon. »");
        OnServiceStateChanged?.Invoke();
    }

    public void AskSauce()
    {
        if (!HasActiveCustomer || AskedSauce) return;
        AskedSauce = true;
        Speak($"{CustomerName} : « Sauce {CustomerWish.sauce.GetDisplayName()} ! »");
        Speak("Toi : (Choisis la bonne sauce sur le présentoir)");
        OnServiceStateChanged?.Invoke();
    }

    /// <summary>Le joueur sélectionne une sauce après avoir demandé.</summary>
    public void SelectSauce(SauceType sauce)
    {
        if (!HasActiveCustomer || !AskedSauce) return;
        PlayerChosenSauce = sauce;
        SauceSelectedByPlayer = true;
        Ticket.sauce = sauce;
        Speak($"Toi : « Sauce {sauce.GetDisplayName()}, c'est noté. »");
        OnServiceStateChanged?.Invoke();
    }

    /// <summary>Encaissement final après prise de commande complète.</summary>
    public bool Checkout(RestaurantData resto)
    {
        if (!HasActiveCustomer || !TicketComplete || resto == null) return false;

        bool sauceOk = PlayerChosenSauce.HasValue && PlayerChosenSauce.Value == CustomerWish.sauce;
        float price = baseKebabPrice;

        // Bonus viande premium
        if (resto.currentMeat == MeatType.Boeuf) price += 2f;
        else if (resto.currentMeat == MeatType.PreferePasSavoir) price -= 1.5f;

        if (sauceOk)
        {
            price += tipAmount;
            Speak($"{CustomerName} : « Merci, bonne journée ! » (+{price:F0} €)");
            resto.reputation = Mathf.Min(100f, resto.reputation + 0.3f);
        }
        else
        {
            price = Mathf.Max(3f, price - wrongSaucePenalty);
            Speak($"{CustomerName} : « Euh… j'avais demandé {CustomerWish.sauce.GetDisplayName()}… » (+{price:F0} €)");
            resto.reputation = Mathf.Max(0f, resto.reputation - 1.5f);
        }

        resto.meatStockKg = Mathf.Max(0f, resto.meatStockKg - meatPerKebabKg);
        EmpireManager.Instance?.AddMoney(price);
        EmpireManager.Instance?.Notify($"Kebab servi — +{price:F0} €");
        EmpireManager.Instance?.AutoSave();

        CustomersServedToday++;
        HasActiveCustomer = false;
        IsServing = false;
        OnServiceStateChanged?.Invoke();
        OnServiceFinished?.Invoke();
        return true;
    }

    public void CancelCustomer()
    {
        if (!HasActiveCustomer) return;
        Speak($"{CustomerName} : « Bon bah j'me casse… »");
        HasActiveCustomer = false;
        IsServing = false;
        OnServiceStateChanged?.Invoke();
        OnServiceFinished?.Invoke();
    }

    public string GetTicketText()
    {
        if (!HasActiveCustomer) return "Aucun client.";

        string salad = AskedSalad ? (Ticket.wantsSalad ? "Oui" : "Non") : "…";
        string tomato = AskedTomato ? (Ticket.wantsTomato ? "Oui" : "Non") : "…";
        string onion = AskedOnion ? (Ticket.wantsOnion ? "Oui" : "Non") : "…";
        string sauce = SauceSelectedByPlayer
            ? Ticket.sauce.GetDisplayName()
            : (AskedSauce ? "à choisir →" : "…");

        return $"TICKET — {CustomerName}\n" +
               $"Salade : {salad}\n" +
               $"Tomate : {tomato}\n" +
               $"Oignon : {onion}\n" +
               $"Sauce : {sauce}";
    }

    private void Speak(string line)
    {
        Debug.Log("[Caisse] " + line);
        OnDialogue?.Invoke(line);
    }
}
