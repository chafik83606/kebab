using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Export Xcode pour Codemagic (iPhone). Entry point : KebabIosBuild.BuildIosBatch
/// </summary>
public static class KebabIosBuild
{
    const string ScenePath = "Assets/Scenes/MainScene.unity";
    const string IosDir = "Builds/ios";

    public static void BuildIosBatch()
    {
        FixCharacterAssets.FixAllInternal();
        AutoWireVisualAssets.AutoWire(showDialog: false);
        EnsureScenes();
        EnsureBundleId();

        if (Directory.Exists(IosDir))
            Directory.Delete(IosDir, true);
        Directory.CreateDirectory(IosDir);

        var opts = new BuildPlayerOptions
        {
            scenes = GetEnabledScenes(),
            locationPathName = IosDir,
            target = BuildTarget.iOS,
            options = BuildOptions.None
        };

        Debug.Log($"[Kebab] Export iOS v{PlayerSettings.bundleVersion} → {IosDir}");
        BuildReport report = BuildPipeline.BuildPlayer(opts);

        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError("[Kebab] iOS export FAIL : " + report.summary.result);
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log("[Kebab] iOS export OK");
        EditorApplication.Exit(0);
    }

    static void EnsureBundleId()
    {
        const string id = "com.DefaultCompany.kebabmanager";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, id);
        PlayerSettings.iOS.buildNumber = PlayerSettings.Android.bundleVersionCode.ToString();
    }

    static void EnsureScenes()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        {
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            return;
        }

        var active = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        if (!string.IsNullOrEmpty(active.path))
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(active.path, true) };
    }

    static string[] GetEnabledScenes()
    {
        var list = new System.Collections.Generic.List<string>();
        foreach (var s in EditorBuildSettings.scenes)
            if (s.enabled) list.Add(s.path);
        if (list.Count == 0 && File.Exists(Path.Combine(Application.dataPath, "Scenes", "MainScene.unity")))
            list.Add(ScenePath);
        return list.ToArray();
    }
}
