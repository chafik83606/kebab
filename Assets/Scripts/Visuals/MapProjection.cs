using UnityEngine;

/// <summary>Projection lat/lon → coordonnées UI (0-1) pour France et Monde.</summary>
public static class MapProjection
{
    // Cadre France métropole + Corse + marge mer
    private const float FrMinLat = 41.2f;
    private const float FrMaxLat = 51.2f;
    private const float FrMinLon = -5.5f;
    private const float FrMaxLon = 9.8f;

    public static Vector2 FranceToUI(float latitude, float longitude)
    {
        float x = Mathf.InverseLerp(FrMinLon, FrMaxLon, longitude);
        float y = Mathf.InverseLerp(FrMinLat, FrMaxLat, latitude);
        return new Vector2(Mathf.Clamp01(x), Mathf.Clamp01(y));
    }

    public static Vector2 WorldToUI(float latitude, float longitude)
    {
        // Projection équirectangulaire classique
        float x = (longitude + 180f) / 360f;
        float y = (latitude + 90f) / 180f;
        return new Vector2(Mathf.Clamp01(x), Mathf.Clamp01(y));
    }

    public static Vector2 ToUI(float latitude, float longitude, bool worldMap)
    {
        return worldMap ? WorldToUI(latitude, longitude) : FranceToUI(latitude, longitude);
    }
}
