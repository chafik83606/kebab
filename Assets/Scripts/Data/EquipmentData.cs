using System;
using UnityEngine;

/// <summary>
/// Types d'équipement de cuisine achetable.
/// </summary>
public enum EquipmentType
{
    Grill,
    Fridge,
    Vitrine
}

/// <summary>
/// Utilitaires pour les prix et effets du matériel.
/// </summary>
public static class EquipmentHelper
{
    public static float GetUpgradePrice(EquipmentType type, int targetLevel)
    {
        targetLevel = Mathf.Clamp(targetLevel, 1, 3);
        switch (type)
        {
            case EquipmentType.Grill: return GameConstants.GRILL_PRICES[targetLevel];
            case EquipmentType.Fridge: return GameConstants.FRIDGE_PRICES[targetLevel];
            case EquipmentType.Vitrine: return GameConstants.VITRINE_PRICES[targetLevel];
            default: return 0f;
        }
    }

    public static string GetDisplayName(EquipmentType type)
    {
        switch (type)
        {
            case EquipmentType.Grill: return "Grill";
            case EquipmentType.Fridge: return "Réfrigérateur";
            case EquipmentType.Vitrine: return "Vitrine chauffante";
            default: return type.ToString();
        }
    }

    public static string GetLevelDescription(EquipmentType type, int level)
    {
        level = Mathf.Clamp(level, 1, 3);
        switch (type)
        {
            case EquipmentType.Grill:
                if (level == 1) return "Cuisson lente, goût moyen";
                if (level == 2) return "Cuisson rapide, bon goût";
                return "Cuisson très rapide, goût excellent";
            case EquipmentType.Fridge:
                if (level == 1) return "Conservation 7 jours";
                if (level == 2) return "Conservation 14 jours";
                return "Conservation 30 jours";
            case EquipmentType.Vitrine:
                if (level == 1) return "Tombe souvent en panne";
                if (level == 2) return "Panne occasionnelle";
                return "Panne rare";
            default:
                return "";
        }
    }

    /// <summary>Bonus de revenu moyen selon les 3 équipements (1.0 à 1.5).</summary>
    public static float GetCombinedRevenueBonus(int grill, int fridge, int vitrine)
    {
        float g = GameConstants.EQUIPMENT_REVENUE_BONUS[Mathf.Clamp(grill, 1, 3)];
        float f = GameConstants.EQUIPMENT_REVENUE_BONUS[Mathf.Clamp(fridge, 1, 3)];
        float v = GameConstants.EQUIPMENT_REVENUE_BONUS[Mathf.Clamp(vitrine, 1, 3)];
        return (g + f + v) / 3f;
    }

    /// <summary>Probabilité de panne journalière de la vitrine selon le niveau.</summary>
    public static float GetVitrineBreakdownChance(int level)
    {
        switch (Mathf.Clamp(level, 1, 3))
        {
            case 1: return 0.20f;
            case 2: return 0.08f;
            case 3: return 0.02f;
            default: return 0.1f;
        }
    }
}
