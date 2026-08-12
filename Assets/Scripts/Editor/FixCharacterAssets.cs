using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Extrait clips, crée controllers humanoid, branche CityPeople / CharCrafter.
/// Menu : Kebab Empire > Fix Character Animations
/// </summary>
public static class FixCharacterAssets
{
    const string AnimFolder = "Assets/Resources/CharacterAnims";
    const string CatalogPath = "Assets/Resources/CharacterAnimCatalog.asset";
    const string CharCrafterBase = "Assets/CharCrafter – Free Preset Characters Pack (Vol. 1)";
    const string KevinBase = "Assets/Kevin Iglesias/Human Animations/Animations";
    const string HumanoidIdleDest = AnimFolder + "/Humanoid_Idle.anim";

    [MenuItem("Kebab Empire/Fix Character Animations")]
    public static void FixAll()
    {
        int clips = FixAllInternal();
        AutoWireVisualAssets.AutoWire(showDialog: false);
        EditorUtility.DisplayDialog(
            "Personnages corrigés",
            $"Clips + controllers humanoid OK ({clips} idle CharCrafter).\n\nAuto-Wire relancé.\nRebuild l'APK.",
            "OK");
    }

    public static void FixAllBatch()
    {
        FixAllInternal();
        AutoWireVisualAssets.AutoWire(showDialog: false);
        Debug.Log("[Kebab] FixCharacterAssets OK");
    }

    public static int FixAllInternal()
    {
        EnsureFolders();

        int clips = ExtractCharCrafterIdles();
        DuplicateSharedIdleForCharCrafter();
        FixCharCrafterPrefabs();
        TryCopyHumanoidIdle();
        BuildHumanoidLocomotion();
        BuildCharacterAnimCatalog();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return clips;
    }

    static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(AnimFolder))
            AssetDatabase.CreateFolder("Assets/Resources", "CharacterAnims");
    }

    static int ExtractCharCrafterIdles()
    {
        int count = 0;
        string baseModel = CharCrafterBase + "/BaseModel";
        if (!AssetDatabase.IsValidFolder(baseModel)) return 0;

        foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { baseModel }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase)) continue;

            string modelName = Path.GetFileNameWithoutExtension(path).Replace(" ", "_");
            AnimationClip idle = LoadFirstClip(path);
            if (idle == null) continue;

            string outPath = AnimFolder + "/" + modelName + "_Idle.anim";
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(outPath) == null)
            {
                var copy = Object.Instantiate(idle);
                copy.name = "Idle";
                AssetDatabase.CreateAsset(copy, outPath);
            }
            count++;
        }
        return count;
    }

    static void DuplicateSharedIdleForCharCrafter()
    {
        string templatePath = AnimFolder + "/Male_Athlate_Idle.anim";
        var template = AssetDatabase.LoadAssetAtPath<AnimationClip>(templatePath);
        if (template == null) return;

        string[] names = { "Male_Casual_Urban", "Male_Doctor", "Male_Old_Man", "Male_Young_Guy" };
        foreach (string n in names)
        {
            string outPath = AnimFolder + "/" + n + "_Idle.anim";
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(outPath) != null) continue;
            var copy = Object.Instantiate(template);
            copy.name = "Idle";
            AssetDatabase.CreateAsset(copy, outPath);
        }
    }

    static int FixCharCrafterPrefabs()
    {
        int count = 0;
        string prefabFolder = CharCrafterBase + "/Prefabs";
        if (!AssetDatabase.IsValidFolder(prefabFolder)) return 0;

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { prefabFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            var animator = prefab.GetComponent<Animator>();
            if (animator == null) continue;

            string fbxPath = CharCrafterBase + "/BaseModel/" + Path.GetFileNameWithoutExtension(path) + ".fbx";
            Avatar avatar = LoadAvatarFromModel(fbxPath);
            if (avatar != null && animator.avatar != avatar)
            {
                animator.avatar = avatar;
                EditorUtility.SetDirty(prefab);
                count++;
            }
        }
        return count;
    }

    static void TryCopyHumanoidIdle()
    {
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(HumanoidIdleDest) != null) return;
        string src = CharCrafterBase + "/Animations/Idle.anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(src);
        if (clip == null) return;
        var copy = Object.Instantiate(clip);
        copy.name = "Humanoid_Idle";
        AssetDatabase.CreateAsset(copy, HumanoidIdleDest);
    }

    static void BuildHumanoidLocomotion()
    {
        var idleM = LoadFirstClip(KevinBase + "/Male/Idles/HumanM@Idle01.fbx");
        var walkM = LoadFirstClip(KevinBase + "/Male/Movement/Walk/HumanM@Walk01_Forward.fbx");
        var idleF = LoadFirstClip(KevinBase + "/Female/Idles/HumanF@Idle01.fbx");
        var walkF = LoadFirstClip(KevinBase + "/Female/Movement/Walk/HumanF@Walk01_Forward.fbx");

        if (idleM != null && walkM != null)
            CreateLocomotionController(idleM, walkM, AnimFolder + "/Humanoid_M.controller");
        if (idleF != null && walkF != null)
            CreateLocomotionController(idleF, walkF, AnimFolder + "/Humanoid_F.controller");
    }

    static void CreateLocomotionController(AnimationClip idle, AnimationClip walk, string path)
    {
        if (File.Exists(path)) AssetDatabase.DeleteAsset(path);

        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("IsWalking", AnimatorControllerParameterType.Bool);

        var sm = controller.layers[0].stateMachine;
        var idleState = sm.AddState("Idle", new Vector3(300, 0, 0));
        idleState.motion = idle;
        var walkState = sm.AddState("Walk", new Vector3(300, 100, 0));
        walkState.motion = walk;

        var toWalk = idleState.AddTransition(walkState);
        toWalk.AddCondition(AnimatorConditionMode.If, 0, "IsWalking");
        toWalk.duration = 0.15f;
        var toIdle = walkState.AddTransition(idleState);
        toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsWalking");
        toIdle.duration = 0.15f;

        sm.defaultState = idleState;
        EditorUtility.SetDirty(controller);
    }

    static void BuildCharacterAnimCatalog()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<CharacterAnimCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<CharacterAnimCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }

        catalog.maleLocomotion = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimFolder + "/Humanoid_M.controller");
        catalog.femaleLocomotion = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimFolder + "/Humanoid_F.controller");
        catalog.femaleAvatar = FindAvatarByGender(true);
        catalog.maleAvatar = FindAvatarByGender(false);

        // Ne jamais laisser maleAvatar = femaleAvatar
        if (catalog.maleAvatar != null && catalog.femaleAvatar != null &&
            catalog.maleAvatar == catalog.femaleAvatar)
            catalog.maleAvatar = null;

        EditorUtility.SetDirty(catalog);
        Debug.Log($"[Kebab] AnimCatalog — maleAvatar={(catalog.maleAvatar != null ? catalog.maleAvatar.name : "NULL")} femaleAvatar={(catalog.femaleAvatar != null ? catalog.femaleAvatar.name : "NULL")}");
    }

    static Avatar FindAvatarByGender(bool female)
    {
        string[] folders = { "Assets" };
        foreach (string guid in AssetDatabase.FindAssets("t:Model", folders))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase)) continue;
            if (!path.Contains("CityPeople") && !path.Contains("PolyPeople") && !path.Contains("Kevin Iglesias"))
                continue;

            string file = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
            bool isFemale =
                file.Contains("female") ||
                file.Contains("humanf") ||
                file.Contains("_f_") ||
                file.EndsWith("_f") ||
                file.EndsWith("dummy_f");
            bool isMale =
                (file.Contains("male") && !file.Contains("female")) ||
                file.Contains("humanm") ||
                file.Contains("_m_") ||
                file.EndsWith("_m") ||
                file.EndsWith("dummy_m");

            if (female && !isFemale) continue;
            if (!female && (!isMale || isFemale)) continue;

            Avatar av = LoadAvatarFromModel(path);
            if (av != null && av.isValid && av.isHuman)
                return av;
        }

        // Fallback Kevin dummies
        string kevinDummy = female
            ? "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Basic Motions/Prefabs/Human_BasicMotionsDummy_F.prefab"
            : "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Basic Motions/Prefabs/Human_BasicMotionsDummy_M.prefab";
        var go = AssetDatabase.LoadAssetAtPath<GameObject>(kevinDummy);
        if (go != null)
        {
            var a = go.GetComponentInChildren<Animator>();
            if (a != null && a.avatar != null && a.avatar.isHuman)
                return a.avatar;
        }

        if (female)
            return LoadAvatarFromModel("Assets/CityPeople_Free/Meshes/Female_Adult.fbx");
        return null;
    }

    static Avatar LoadAvatarFromModel(string fbxPath)
    {
        if (string.IsNullOrEmpty(fbxPath)) return null;
        return AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<Avatar>().FirstOrDefault();
    }

    static AnimationClip LoadFirstClip(string fbxPath)
    {
        if (string.IsNullOrEmpty(fbxPath)) return null;
        foreach (Object sub in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
        {
            if (sub is AnimationClip clip && !clip.name.StartsWith("__"))
                return clip;
        }
        return null;
    }
}
