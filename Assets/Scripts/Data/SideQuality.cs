using UnityEngine;

/// <summary>
/// Qualité des accompagnements (salade, tomate, oignon…).
/// </summary>
public enum SideQuality
{
    Frais,
    Moyen,
    Bas
}

public static class SideQualityExtensions
{
    public static float GetReputationMultiplier(this SideQuality quality)
    {
        switch (quality)
        {
            case SideQuality.Frais: return 1.25f;
            case SideQuality.Moyen: return 1.0f;
            case SideQuality.Bas: return 0.78f;
            default: return 1f;
        }
    }

    public static float GetDailyCost(this SideQuality quality)
    {
        switch (quality)
        {
            case SideQuality.Frais: return 35f;
            case SideQuality.Moyen: return 12f;
            case SideQuality.Bas: return 0f;
            default: return 0f;
        }
    }

    public static float GetHealthRisk(this SideQuality quality)
    {
        switch (quality)
        {
            case SideQuality.Frais: return 0.01f;
            case SideQuality.Moyen: return 0.06f;
            case SideQuality.Bas: return 0.18f;
            default: return 0.1f;
        }
    }

    public static string GetDisplayName(this SideQuality quality)
    {
        switch (quality)
        {
            case SideQuality.Frais: return "Frais";
            case SideQuality.Moyen: return "Moyen";
            case SideQuality.Bas: return "Bas de gamme";
            default: return quality.ToString();
        }
    }

    public static string GetDescription(this SideQuality quality)
    {
        switch (quality)
        {
            case SideQuality.Frais:
                return "Légumes du jour. Plus de clients, coût élevé.";
            case SideQuality.Moyen:
                return "Correct. Bon compromis coût / clientèle.";
            case SideQuality.Bas:
                return "Peu frais mais économique. Moins de clients, risque hygiène.";
            default: return "";
        }
    }
}
