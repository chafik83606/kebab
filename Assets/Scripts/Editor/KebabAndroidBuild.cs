using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Menu : Kebab Empire > Build And Run Android
/// Force un VRAI full APK (pas "script only") + incrémente la version.
/// </summary>
public static class KebabAndroidBuild
{
    const string ScenePath = "Assets/Scenes/MainScene.unity";
    const string ApkPath = "Builds/kebabmanager.apk";

    [MenuItem("Kebab Empire/Build And Run Android")]
    public static void BuildAndRun()
    {
        string msg;
        bool ok = BuildAndInstallInternal(out msg);
        EditorUtility.DisplayDialog(ok ? "Kebab — Build Android" : "Build échoué", msg, "OK");
    }

    /// <summary>Entry point batchmode : -executeMethod KebabAndroidBuild.BuildAndRunBatch</summary>
    public static void BuildAndRunBatch()
    {
        string msg;
        bool ok = BuildAndInstallInternal(out msg);
        Debug.Log(ok ? "[Kebab] BATCH OK\n" + msg : "[Kebab] BATCH FAIL\n" + msg);
        if (!ok) EditorApplication.Exit(1);
        else EditorApplication.Exit(0);
    }

    static bool BuildAndInstallInternal(out string msg)
    {
        FixCharacterAssets.FixAllInternal();
        AutoWireVisualAssets.AutoWire(showDialog: false);
        EnsureScenes();
        BumpVersionAndStamp();

        if (!Directory.Exists("Builds"))
            Directory.CreateDirectory("Builds");

        if (File.Exists(ApkPath))
        {
            try { File.Delete(ApkPath); }
            catch (Exception e) { Debug.LogWarning("[Kebab] Impossible de supprimer l'ancien APK : " + e.Message); }
        }

        var opts = new BuildPlayerOptions
        {
            scenes = GetEnabledScenes(),
            locationPathName = ApkPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        Debug.Log($"[Kebab] FULL build Android v{PlayerSettings.bundleVersion} (code {PlayerSettings.Android.bundleVersionCode})…");
        BuildReport report = BuildPipeline.BuildPlayer(opts);

        if (report.summary.result != BuildResult.Succeeded)
        {
            msg = report.summary.result.ToString();
            Debug.LogError("[Kebab] Build échoué : " + msg);
            return false;
        }

        long size = File.Exists(ApkPath) ? new FileInfo(ApkPath).Length : 0;
        Debug.Log($"[Kebab] Build OK → {ApkPath} ({size} octets)");

        bool installed = TryAdbInstall(ApkPath);
        msg =
            $"APK : {size / 1024f / 1024f:F2} Mo\n" +
            $"Version : {PlayerSettings.bundleVersion} (code {PlayerSettings.Android.bundleVersionCode})\n\n" +
            (installed
                ? "Installé sur le téléphone.\nAccepte la popup « Installer via USB » si Xiaomi le demande."
                : "APK créé mais install échouée.\nSur Xiaomi : Paramètres → Options développeur → « Installer via USB » = ON\nPuis relance le menu Build.");
        return installed || size > 0;
    }

    static void BumpVersionAndStamp()
    {
        int code = PlayerSettings.Android.bundleVersionCode;
        if (code < 1) code = 1;
        code++;
        PlayerSettings.Android.bundleVersionCode = code;
        PlayerSettings.bundleVersion = "1." + code;

        string stampDir = Path.Combine(Application.dataPath, "StreamingAssets");
        if (!Directory.Exists(stampDir))
            Directory.CreateDirectory(stampDir);

        string stamp =
            $"build={DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
            $"version={PlayerSettings.bundleVersion}\n" +
            $"code={code}\n" +
            $"guid={Guid.NewGuid()}\n";
        File.WriteAllText(Path.Combine(stampDir, "build_stamp.txt"), stamp);
        AssetDatabase.Refresh();
    }

    static bool TryAdbInstall(string apkPath)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "adb",
                Arguments = $"install -r \"{Path.GetFullPath(apkPath)}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using (var p = System.Diagnostics.Process.Start(psi))
            {
                string stdout = p.StandardOutput.ReadToEnd();
                string stderr = p.StandardError.ReadToEnd();
                p.WaitForExit(180000);
                Debug.Log("[Kebab] adb install:\n" + stdout + stderr);
                bool ok = p.ExitCode == 0 && (stdout + stderr).IndexOf("Success", StringComparison.OrdinalIgnoreCase) >= 0;
                if (ok)
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "adb",
                        Arguments = "shell am start -n com.DefaultCompany.kebabmanager/com.unity3d.player.UnityPlayerActivity",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    })?.WaitForExit(15000);
                }
                return ok;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Kebab] adb install erreur : " + e.Message);
            return false;
        }
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
