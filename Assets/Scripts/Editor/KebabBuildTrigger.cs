using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Désactivé par défaut : les builds auto "script only" trompaient sur la taille APK.
/// Pour forcer : crée Builds/REQUEST_FULL_BUILD
/// </summary>
[InitializeOnLoad]
public static class KebabBuildTrigger
{
    const string FlagPath = "Builds/REQUEST_FULL_BUILD";
    static double nextCheck;

    static KebabBuildTrigger()
    {
        EditorApplication.update += Tick;
    }

    static void Tick()
    {
        if (EditorApplication.timeSinceStartup < nextCheck) return;
        nextCheck = EditorApplication.timeSinceStartup + 2.0;
        if (EditorApplication.isCompiling || EditorApplication.isPlaying) return;
        if (!File.Exists(FlagPath)) return;

        try { File.Delete(FlagPath); } catch { return; }
        Debug.Log("[Kebab] REQUEST_FULL_BUILD → full APK…");
        EditorApplication.delayCall += KebabAndroidBuild.BuildAndRun;
    }
}
