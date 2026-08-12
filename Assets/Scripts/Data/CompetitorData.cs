using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Données d'un concurrent IA (sérialisable).
/// </summary>
[Serializable]
public class CompetitorData
{
    public string competitorName;
    public float treasury;
    public float debt;
    public bool isBankrupt;
    public List<RestaurantData> restaurants = new List<RestaurantData>();

    public bool IsFinanciallyFragile =>
        treasury <= 0f || debt > treasury * GameConstants.FRAGILE_DEBT_MULTIPLIER;

    public float GetBuyoutPrice()
    {
        // Prix réduit si en faillite
        float baseValue = 0f;
        for (int i = 0; i < restaurants.Count; i++)
            baseValue += GameConstants.BASE_RESTAURANT_PRICE * 0.6f;

        if (isBankrupt)
            return baseValue * 0.4f; // Prix bradé

        return baseValue;
    }
}

/// <summary>
/// Conteneur de sauvegarde JSON de tout l'empire.
/// </summary>
[Serializable]
public class SaveData
{
    public float money;
    public float debt;
    public float monthlyRevenueAccumulated;
    public int currentDay;
    public int currentMonth;
    public float globalReputation;
    public bool taxesPaidThisMonth;
    public List<RestaurantData> restaurants = new List<RestaurantData>();
    public List<CompetitorData> competitors = new List<CompetitorData>();
    public int nextRestaurantId;
    public bool gameOver;
    public string gameOverReason;
    public int gamePhase = 2;
    public bool awaitingSetup;
}
