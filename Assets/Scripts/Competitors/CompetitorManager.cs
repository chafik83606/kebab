using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gère les concurrents IA : évolution financière, faillites et rachats.
/// </summary>
public class CompetitorManager : MonoBehaviour
{
    [SerializeField] private List<CompetitorData> competitors = new List<CompetitorData>();

    private static readonly string[] CompetitorNames =
    {
        "Doner King", "Kebab Express", "Chez Brahim", "Le Grand Broche", "Shawarma Palace"
    };

    public IReadOnlyList<CompetitorData> Competitors => competitors;

    /// <summary>Initialise les concurrents pour une nouvelle partie.</summary>
    public void InitializeCompetitors()
    {
        competitors.Clear();
        int count = Mathf.Min(GameConstants.COMPETITOR_COUNT, CompetitorNames.Length);

        for (int i = 0; i < count; i++)
        {
            var c = new CompetitorData
            {
                competitorName = CompetitorNames[i],
                treasury = Random.Range(3000f, 8000f),
                debt = Random.Range(0f, 1000f),
                isBankrupt = false,
                restaurants = new List<RestaurantData>()
            };

            // Chaque concurrent a 1 restaurant fictif
            c.restaurants.Add(new RestaurantData
            {
                restaurantName = CompetitorNames[i] + " — Local",
                locationName = "Quartier " + (i + 1),
                currentMeat = (MeatType)Random.Range(0, 3),
                meatStockKg = 15f,
                grillLevel = Random.Range(1, 3),
                fridgeLevel = Random.Range(1, 3),
                vitrineLevel = 1,
                reputation = Random.Range(30f, 70f),
                employees = new List<Employee>
                {
                    new Employee("Employé IA", Random.value > 0.3f)
                },
                isOwnedByPlayer = false
            });

            competitors.Add(c);
        }
    }

    /// <summary>Simule une journée pour tous les concurrents.</summary>
    public void SimulateDay()
    {
        for (int i = 0; i < competitors.Count; i++)
        {
            CompetitorData c = competitors[i];
            if (c.isBankrupt) continue;

            // Revenu / perte aléatoire
            float delta = Random.Range(-400f, 600f);
            c.treasury += delta;

            // Dette croît parfois
            if (Random.value < 0.1f)
                c.debt += Random.Range(50f, 300f);

            // Remboursement partiel
            if (c.treasury > 500f && c.debt > 0f && Random.value < 0.3f)
            {
                float pay = Mathf.Min(c.debt, c.treasury * 0.2f);
                c.treasury -= pay;
                c.debt -= pay;
            }

            if (c.treasury < 0f)
            {
                c.isBankrupt = true;
                c.treasury = 0f;
                if (EmpireManager.Instance != null)
                    EmpireManager.Instance.Notify(
                        $"📉 {c.competitorName} est en faillite ! Rachat possible.");
            }
        }
    }

    /// <summary>Retourne le concurrent le plus riche (pour rachat hostile).</summary>
    public CompetitorData GetStrongestCompetitor()
    {
        CompetitorData best = null;
        float bestMoney = -1f;
        for (int i = 0; i < competitors.Count; i++)
        {
            if (competitors[i].isBankrupt) continue;
            if (competitors[i].treasury > bestMoney)
            {
                bestMoney = competitors[i].treasury;
                best = competitors[i];
            }
        }
        return best;
    }

    /// <summary>Liste des concurrents en faillite (rachetables).</summary>
    public List<CompetitorData> GetBankruptCompetitors()
    {
        var list = new List<CompetitorData>();
        for (int i = 0; i < competitors.Count; i++)
        {
            if (competitors[i].isBankrupt)
                list.Add(competitors[i]);
        }
        return list;
    }

    /// <summary>
    /// Le joueur rachète un concurrent en faillite.
    /// Transfère ses restaurants à l'empire.
    /// </summary>
    public bool BuyoutCompetitor(CompetitorData competitor)
    {
        if (competitor == null || !competitor.isBankrupt) return false;
        if (EmpireManager.Instance == null) return false;

        float price = competitor.GetBuyoutPrice();
        if (!EmpireManager.Instance.Spend(price, $"Rachat de {competitor.competitorName}"))
        {
            EmpireManager.Instance.Notify("Fonds insuffisants pour le rachat.");
            return false;
        }

        for (int i = 0; i < competitor.restaurants.Count; i++)
        {
            EmpireManager.Instance.AddRestaurantFromBuyout(competitor.restaurants[i]);
        }

        EmpireManager.Instance.Notify(
            $"🏆 Vous avez racheté {competitor.competitorName} pour {price:F0}€ !");

        competitors.Remove(competitor);
        EmpireManager.Instance.AutoSave();
        return true;
    }

    public List<CompetitorData> GetCompetitorsCopy()
    {
        return new List<CompetitorData>(competitors);
    }

    public void LoadCompetitors(List<CompetitorData> loaded)
    {
        competitors = loaded ?? new List<CompetitorData>();
    }
}
