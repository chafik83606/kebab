using UnityEngine;

/// <summary>Sauces proposées au comptoir.</summary>
public enum SauceType
{
    Blanche,
    Algerienne,
    Samourai,
    Harissa,
    KetchupMayo,
    SansSauce
}

public static class SauceTypeExtensions
{
    public static string GetDisplayName(this SauceType sauce)
    {
        switch (sauce)
        {
            case SauceType.Blanche: return "Blanche";
            case SauceType.Algerienne: return "Algérienne";
            case SauceType.Samourai: return "Samouraï";
            case SauceType.Harissa: return "Harissa";
            case SauceType.KetchupMayo: return "Ketchup-Mayo";
            case SauceType.SansSauce: return "Sans sauce";
            default: return sauce.ToString();
        }
    }

    public static SauceType RandomSauce()
    {
        // SansSauce un peu moins fréquent
        int roll = Random.Range(0, 10);
        if (roll == 0) return SauceType.SansSauce;
        return (SauceType)Random.Range(0, 5); // Blanche → Harissa
    }
}

/// <summary>Commande souhaitée par un client (réponses aux questions).</summary>
[System.Serializable]
public class KebabOrder
{
    public bool wantsSalad;
    public bool wantsTomato;
    public bool wantsOnion;
    public SauceType sauce;

    public static KebabOrder CreateRandom()
    {
        return new KebabOrder
        {
            wantsSalad = Random.value > 0.35f,
            wantsTomato = Random.value > 0.4f,
            wantsOnion = Random.value > 0.45f,
            sauce = SauceTypeExtensions.RandomSauce()
        };
    }

    public string IngredientsSummary()
    {
        var parts = new System.Collections.Generic.List<string>();
        if (wantsSalad) parts.Add("salade");
        if (wantsTomato) parts.Add("tomate");
        if (wantsOnion) parts.Add("oignon");
        if (parts.Count == 0) parts.Add("rien dedans");
        return string.Join(", ", parts) + " + " + sauce.GetDisplayName();
    }
}
