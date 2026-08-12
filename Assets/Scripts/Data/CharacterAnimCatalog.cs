using UnityEngine;

/// <summary>
/// Clips + controllers humanoid (Kevin Iglesias / CityPeople).
/// </summary>
[CreateAssetMenu(fileName = "CharacterAnimCatalog", menuName = "Kebab Empire/Character Anim Catalog")]
public class CharacterAnimCatalog : ScriptableObject
{
    public RuntimeAnimatorController maleLocomotion;
    public RuntimeAnimatorController femaleLocomotion;
    public Avatar maleAvatar;
    public Avatar femaleAvatar;

    public RuntimeAnimatorController GetControllerFor(string prefabName)
    {
        return IsFemaleName(prefabName) ? femaleLocomotion : maleLocomotion;
    }

    /// <summary>Avatar de secours selon le genre — jamais le sexe opposé.</summary>
    public Avatar GetAvatarFor(string prefabName)
    {
        if (IsFemaleName(prefabName))
            return femaleAvatar;
        return maleAvatar; // peut être null → caller refuse le setup humanoid
    }

    public static bool IsFemaleName(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        string n = name.ToLowerInvariant();
        if (n.Contains("female") || n.Contains("woman") || n.Contains("girl")) return true;
        if (n.Contains("male") || n.Contains("man") || n.Contains("boy")) return false;
        if (n.Contains("_f") || n.EndsWith("f")) return true;
        return false;
    }
}
