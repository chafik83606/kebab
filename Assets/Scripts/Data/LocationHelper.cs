using UnityEngine;

/// <summary>
/// Emplacements et multiplicateurs de revenus selon le quartier.
/// </summary>
public static class LocationHelper
{
    public static float GetMultiplier(string locationName)
    {
        if (string.IsNullOrEmpty(locationName)) return 1f;
        string lower = locationName.ToLowerInvariant();
        if (lower.Contains("gare")) return 1.55f;
        if (lower.Contains("centre")) return 1.35f;
        if (lower.Contains("universit")) return 1.25f;
        if (lower.Contains("banlieue") || lower.Contains("quartier")) return 1.0f;
        if (lower.Contains("industri") || lower.Contains("zone")) return 0.82f;
        if (lower.Contains("campagn") || lower.Contains("périph")) return 0.75f;
        return 1f;
    }

    public static string PickRandomLocation(int restaurantCount)
    {
        string[] spots =
        {
            "Gare (passage)",
            "Centre-ville",
            "Quartier universitaire",
            "Banlieue résidentielle",
            "Zone industrielle",
            "Périphérie"
        };
        return spots[restaurantCount % spots.Length];
    }
}
