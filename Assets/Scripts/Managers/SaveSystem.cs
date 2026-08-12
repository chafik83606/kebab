using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Système de sauvegarde / chargement JSON (JsonUtility).
/// Sauvegarde automatique après chaque action importante.
/// </summary>
public static class SaveSystem
{
    private const string SAVE_FILE_NAME = "kebab_empire_save.json";

    public static string SavePath => Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);

    public static bool SaveExists => File.Exists(SavePath);

    /// <summary>Sauvegarde l'état complet du jeu.</summary>
    public static void Save(SaveData data)
    {
        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);
            Debug.Log($"[SaveSystem] Sauvegarde OK → {SavePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Échec de la sauvegarde : {e.Message}");
        }
    }

    /// <summary>Charge la sauvegarde. Retourne null si absente ou corrompue.</summary>
    public static SaveData Load()
    {
        if (!SaveExists)
        {
            Debug.Log("[SaveSystem] Aucune sauvegarde trouvée.");
            return null;
        }

        try
        {
            string json = File.ReadAllText(SavePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            Debug.Log($"[SaveSystem] Chargement OK — Jour {data.currentDay}, Argent {data.money:F0}€");
            return data;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Échec du chargement : {e.Message}");
            return null;
        }
    }

    /// <summary>Supprime la sauvegarde (nouvelle partie).</summary>
    public static void DeleteSave()
    {
        if (SaveExists)
        {
            File.Delete(SavePath);
            Debug.Log("[SaveSystem] Sauvegarde supprimée.");
        }
    }
}
