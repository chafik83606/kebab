using UnityEngine;

/// <summary>
/// Types de viande disponibles pour les kebabs, classés par qualité.
/// Hiérarchie : Boeuf (Premium) > Poulet (Moyen) > PreferePasSavoir (Bas).
/// </summary>
public enum MeatType
{
    Boeuf,              // Qualité Premium
    Poulet,             // Qualité Moyenne
    PreferePasSavoir    // Qualité Basse ("Je préfère pas savoir")
}

/// <summary>
/// Propriétés associées à chaque type de viande.
/// </summary>
public static class MeatTypeExtensions
{
    /// <summary>Coût d'achat par kg (en euros).</summary>
    public static float GetPurchaseCostPerKg(this MeatType meat)
    {
        switch (meat)
        {
            case MeatType.Boeuf: return 18f;
            case MeatType.Poulet: return 10f;
            case MeatType.PreferePasSavoir: return 3f;
            default: return 10f;
        }
    }

    /// <summary>Bonus/malus de réputation appliqué aux revenus journaliers.</summary>
    public static float GetReputationMultiplier(this MeatType meat)
    {
        switch (meat)
        {
            case MeatType.Boeuf: return 1.4f;          // Fort bonus
            case MeatType.Poulet: return 1.1f;         // Bonus modéré
            case MeatType.PreferePasSavoir: return 0.6f; // Grosse pénalité
            default: return 1f;
        }
    }

    /// <summary>Multiplicateur d'accumulation de saleté par jour.</summary>
    public static float GetDirtMultiplier(this MeatType meat)
    {
        switch (meat)
        {
            case MeatType.Boeuf: return 0.5f;           // Augmente lentement
            case MeatType.Poulet: return 1.0f;          // Normal
            case MeatType.PreferePasSavoir: return 2.5f; // Très rapide
            default: return 1f;
        }
    }

    /// <summary>Risque sanitaire de base (0 à 1) lié à la viande.</summary>
    public static float GetHealthRisk(this MeatType meat)
    {
        switch (meat)
        {
            case MeatType.Boeuf: return 0.02f;          // Très faible
            case MeatType.Poulet: return 0.08f;         // Faible
            case MeatType.PreferePasSavoir: return 0.35f; // Élevé (intoxications)
            default: return 0.1f;
        }
    }

    /// <summary>Nom affiché dans l'UI.</summary>
    public static string GetDisplayName(this MeatType meat)
    {
        switch (meat)
        {
            case MeatType.Boeuf: return "Bœuf (Premium)";
            case MeatType.Poulet: return "Poulet (Moyen)";
            case MeatType.PreferePasSavoir: return "Je préfère pas savoir";
            default: return meat.ToString();
        }
    }

    /// <summary>Description courte pour l'UI.</summary>
    public static string GetDescription(this MeatType meat)
    {
        switch (meat)
        {
            case MeatType.Boeuf:
                return "Viande premium. Coût élevé, excellente réputation, saleté lente.";
            case MeatType.Poulet:
                return "Choix équilibré. Bon rapport qualité/prix.";
            case MeatType.PreferePasSavoir:
                return "Très bon marché... mais risque sanitaire élevé et saleté rapide.";
            default:
                return "";
        }
    }

    /// <summary>Couleur associée (pour l'UI).</summary>
    public static Color GetDisplayColor(this MeatType meat)
    {
        switch (meat)
        {
            case MeatType.Boeuf: return new Color(0.75f, 0.2f, 0.15f);
            case MeatType.Poulet: return new Color(0.9f, 0.7f, 0.2f);
            case MeatType.PreferePasSavoir: return new Color(0.4f, 0.55f, 0.2f);
            default: return Color.white;
        }
    }
}
