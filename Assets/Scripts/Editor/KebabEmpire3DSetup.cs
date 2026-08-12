using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Setup complet du jeu 3D : carte, joysticks, caisse 3D, slots Asset Store.
/// Menu : Kebab Empire > Setup 3D Game World
/// </summary>
public static class KebabEmpire3DSetup
{
#if UNITY_EDITOR
    [MenuItem("Kebab Empire/FIX — Scène jouable (1 clic)")]
    public static void FixPlayableSceneOneClick()
    {
        AutoWireVisualAssets.AutoWire(showDialog: false);
        Setup3DWorld();
    }

    [MenuItem("Kebab Empire/Setup 3D Game World")]
    public static void Setup3DWorld()
    {
        DestroyIfExists("=== MANAGERS ===");
        DestroyIfExists("=== WORLD ===");
        DestroyIfExists("=== PLAYER ===");
        DestroyIfExists("=== RESTAURANTS ===");
        DestroyIfExists("GameplayHUD");
        DestroyIfExists("EventSystem");
        DestroyIfExists("CounterServicePanel");
        DestroyIfExists("Canvas");

        EnsureVisualCatalogAsset();
        SaveSystem.DeleteSave();

        KebabWorldBootstrap.BuildPlayableWorld(runtime: false);

        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");

        const string path = "Assets/Scenes/MainScene.unity";
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), path, true);
        EditorSceneManager.OpenScene(path);

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(path, true)
        };

        EditorUtility.DisplayDialog(
            "Kebab Empire 3D",
            "Monde 3D prêt et enregistré !\n\n" +
            "Scène : Assets/Scenes/MainScene.unity\n" +
            "(aussi ajoutée au Build Settings)\n\n" +
            "Appuie sur Play.\n" +
            "Tu dois voir la CARTE de la ville (vue dessus),\n" +
            "pas l'ancien menu « Gérer mon kebab ».",
            "OK");
    }

    private static void EnsureVisualCatalogAsset()
    {
        const string folder = "Assets/Resources";
        const string path = folder + "/VisualCatalog.asset";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets", "Resources");

        var existing = AssetDatabase.LoadAssetAtPath<VisualCatalog>(path);
        if (existing == null)
        {
            var cat = ScriptableObject.CreateInstance<VisualCatalog>();
            AssetDatabase.CreateAsset(cat, path);
            AssetDatabase.SaveAssets();
        }
    }

    private static void DestroyIfExists(string name)
    {
        var go = GameObject.Find(name);
        if (go != null) Object.DestroyImmediate(go);
    }
#endif
}
