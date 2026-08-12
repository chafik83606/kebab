using UnityEngine;

/// <summary>Qualité fraîcheur d'un ingrédient (salade, tomate, oignon).</summary>
public enum IngredientFreshness
{
    Frais,
    PeuFrais
}

public static class IngredientFreshnessExtensions
{
    public static float GetReputationMultiplier(this IngredientFreshness f)
    {
        return f == IngredientFreshness.Frais ? 1.12f : 0.88f;
    }

    public static float GetDailyCost(this IngredientFreshness f)
    {
        return f == IngredientFreshness.Frais ? 8f : 2f;
    }

    public static float GetHealthRisk(this IngredientFreshness f)
    {
        return f == IngredientFreshness.Frais ? 0.02f : 0.14f;
    }

    public static string GetDisplayName(this IngredientFreshness f)
    {
        return f == IngredientFreshness.Frais ? "Frais" : "Peu frais";
    }
}

public enum ManagementMode
{
    Automatic,
    Manual
}

public enum GamePhase
{
    MapSelection,
    SetupWizard,
    Playing
}
