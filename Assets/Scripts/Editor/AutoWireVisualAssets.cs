using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Branche automatiquement les assets importés dans VisualCatalog.
/// Menu : Kebab Empire > Auto-Wire Imported Assets
/// </summary>
public static class AutoWireVisualAssets
{
    private const string CatalogPath = "Assets/Resources/VisualCatalog.asset";
    private const string InteriorPrefabPath = "Assets/Prefabs/World/KebabInterior_Assembled.prefab";
    private const string BuildingPrefabPath = "Assets/Prefabs/World/KebabBuilding_Assembled.prefab";

    [MenuItem("Kebab Empire/Auto-Wire Imported Assets")]
    public static void AutoWireMenu()
    {
        AutoWire(showDialog: true);
    }

    public static void AutoWire(bool showDialog = true)
    {
        try
        {
            EnsureFolders();

            var catalog = LoadOrCreateCatalog();
            var characters = FindCharacterPrefabs();
            Debug.Log($"[Auto-Wire] Personnages trouvés : {characters.Count}");

            var exterior = FindExteriorPrefab();
            Debug.Log($"[Auto-Wire] Extérieur : {(exterior != null ? exterior.name : "AUCUN")}");

            var interior = BuildInteriorSafe();
            Debug.Log($"[Auto-Wire] Intérieur : {(interior != null ? interior.name : "AUCUN")}");

            catalog.playerPrefab = characters.Count > 0 ? characters[0] : null;
            // Employés / clients : privilégier CityPeople humanoid, CharCrafter en secours
            var city = characters.Where(c =>
            {
                string p = AssetDatabase.GetAssetPath(c);
                return p.Contains("CityPeople") || p.Contains("PolyPeople");
            }).ToList();
            var others = characters.Where(c => !city.Contains(c)).ToList();
            var ordered = city.Count > 0 ? city.Concat(others).ToList() : characters;

            catalog.employeePrefabs = ordered.ToArray();
            catalog.customerPrefabs = (city.Count > 0 ? city : ordered).ToArray();
            catalog.kebabExteriorPrefab = exterior;
            catalog.kebabInteriorPrefab = interior;
            catalog.characterScale = 1f;

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();

            var building = BuildRestaurantBuildingPrefab(catalog);
            catalog.restaurantBuildingPrefab = building;
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string msg =
                "Assets branchés avec succès !\n\n" +
                $"Personnages : {characters.Count}\n" +
                $"Extérieur : {(exterior != null ? exterior.name : "fallback")}\n" +
                $"Intérieur : {(interior != null ? interior.name : "fallback")}\n" +
                $"Bâtiment : {(building != null ? building.name : "AUCUN")}\n\n" +
                "Ensuite :\n" +
                "Kebab Empire > Setup 3D Game World\n" +
                "Save → Play";

            if (showDialog)
                EditorUtility.DisplayDialog("Kebab Empire — Auto-Wire", msg, "OK");
            else
                Debug.Log("[Auto-Wire] " + msg.Replace("\n", " | "));
        }
        catch (Exception e)
        {
            Debug.LogError("[Auto-Wire] Échec : " + e);
            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Auto-Wire — Erreur",
                    "Échec du branchement.\n\n" + e.Message +
                    "\n\nRegarde la Console pour le détail.",
                    "OK");
            }
        }
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/World"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "World");
    }

    private static VisualCatalog LoadOrCreateCatalog()
    {
        var cat = AssetDatabase.LoadAssetAtPath<VisualCatalog>(CatalogPath);
        if (cat != null) return cat;
        cat = ScriptableObject.CreateInstance<VisualCatalog>();
        AssetDatabase.CreateAsset(cat, CatalogPath);
        return cat;
    }

    private static List<GameObject> FindCharacterPrefabs()
    {
        var list = new List<GameObject>();
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".prefab")) continue;
            if (path.Contains("/Demo_") || path.Contains("/Demo ") || path.Contains("DemoScene")) continue;

            bool isChar =
                (path.Contains("CharCrafter") && path.Contains("/Prefabs/")) ||
                (path.Contains("CityPeople") && path.Contains("/Prefabs/")) ||
                (path.Contains("PolyPeople") && path.Contains("/Prefabs/"));

            if (!isChar) continue;

            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go != null) list.Add(go);
        }

        // Dédupliquer par nom (évite doublons free + full pack)
        return list
            .GroupBy(p => p.name)
            .Select(g => g.First())
            .OrderBy(p => p.name)
            .ToList();
    }

    private static GameObject FindExteriorPrefab()
    {
        string[] paths =
        {
            "Assets/shawarma_shop/Prefabs/shop/SM_shop_ss.prefab",
            "Assets/Low Poly City Buildings Pack/PREFABS/BURGER SHOP.prefab",
            "Assets/Low Poly City Buildings Pack/PREFABS/PIZZA SHOP.prefab",
            "Assets/Low Poly City Buildings Pack/PREFABS/COFFEE SHOP.prefab"
        };

        foreach (var p in paths)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if (go != null) return go;
        }

        string[] guids = AssetDatabase.FindAssets("SM_shop_ss t:Prefab");
        if (guids.Length > 0)
            return AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[0]));

        return null;
    }

    private static GameObject BuildInteriorSafe()
    {
        // PRIORITÉ : scène Demo Asset Store (23 €) — layout + props comme la vitrine
        try
        {
            var fromDemo = BuildInteriorFromDemoScene();
            if (fromDemo != null)
            {
                Debug.Log("[Auto-Wire] Intérieur = scène Demo Asset Store (Kebab Interior).");
                return fromDemo;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Auto-Wire] Demo scene échouée : " + e.Message);
        }

        try
        {
            Debug.LogWarning("[Auto-Wire] Fallback : assemblage prefabs loose.");
            return BuildInteriorFromLoosePrefabs();
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Auto-Wire] Assemblage prefabs échoué : " + e.Message);
        }

        return null;
    }

    private static GameObject BuildInteriorFromLoosePrefabs()
    {
        var container = new GameObject("KebabInterior_Assembled");

        // Layout dense type Asset Store Demo
        Place(container.transform, "Assets/Kebab Interior/Prefabs/Floor.prefab", new Vector3(0f, 0f, 0f));
        Place(container.transform, "Assets/Kebab Interior/Prefabs/Ceiling.prefab", new Vector3(0f, 3.2f, 0f));
        Place(container.transform, "Assets/Kebab Interior/Prefabs/Wall 1.prefab", new Vector3(0f, 0f, 4.5f));
        Place(container.transform, "Assets/Kebab Interior/Prefabs/Wall 2.prefab", new Vector3(-5.5f, 0f, 0f));
        Place(container.transform, "Assets/Kebab Interior/Prefabs/Wall 3.prefab", new Vector3(5.5f, 0f, 0f));
        Place(container.transform, "Assets/Kebab Interior/Prefabs/Wall 4 windows.prefab", new Vector3(0f, 0f, -5f));
        Place(container.transform, "Assets/Kebab Interior/Prefabs/Bar.prefab", new Vector3(0f, 0f, 2.4f));
        Place(container.transform, "Assets/Kebab Interior/Prefabs/Bar food.prefab", new Vector3(0.2f, 1.05f, 2.35f));
        Place(container.transform, "Assets/Kebab Interior/Prefabs/Doner machine.prefab", new Vector3(-2.4f, 0f, 3.4f));
        Place(container.transform, "Assets/Kebab Interior/Prefabs/Doner machine.prefab", new Vector3(-1.3f, 0f, 3.4f));
        Place(container.transform, "Assets/Kebab Interior/Prefabs/Meat 1.prefab", new Vector3(-2.4f, 1.2f, 3.4f));
        Place(container.transform, "Assets/Kebab Interior/Prefabs/Meat 2.prefab", new Vector3(-1.3f, 1.2f, 3.4f));
        Place(container.transform, "Assets/Kebab Interior/Prefabs/Cash register.prefab", new Vector3(1.6f, 1.05f, 2.15f));
        Place(container.transform, "Assets/Kebab Interior/Prefabs/Showcase.prefab", new Vector3(2.8f, 0f, 2.6f));
        Place(container.transform, "Assets/Kebab Interior/Prefabs/Extractor.prefab", new Vector3(-1.8f, 2.55f, 3.4f));
        Place(container.transform, "Assets/Kebab Interior/Prefabs/Menu.prefab", new Vector3(0.2f, 2.25f, 3.8f));
        Place(container.transform, "Assets/Kebab Interior/Prefabs/Restaurant furniture.prefab", new Vector3(0f, 0f, -0.5f));
        Place(container.transform, "Assets/Kebab Interior/Prefabs/Ceiling lamp.prefab", new Vector3(0f, 3.0f, 0f));
        Place(container.transform, "Assets/Kebab Interior/Prefabs/Ceiling lamp.prefab", new Vector3(-2.5f, 3.0f, -2f));
        Place(container.transform, "Assets/Kebab Interior/Prefabs/Ceiling lamp.prefab", new Vector3(2.5f, 3.0f, -2f));

        // Salle : tables visibles dès l'entrée (côtés du comptoir)
        Place(container.transform, "Assets/Kebab Interior/Prefabs/Table.prefab", new Vector3(-3.4f, 0f, 0.2f));
        Place(container.transform, "Assets/Kebab Interior/Prefabs/Chair.prefab", new Vector3(-3.4f, 0f, -0.7f));
        Place(container.transform, "Assets/Kebab Interior/Prefabs/Chair.prefab", new Vector3(-3.4f, 0f, 0.9f));
        Place(container.transform, "Assets/Kebab Interior/Prefabs/Table.prefab", new Vector3(3.4f, 0f, 0.2f));
        Place(container.transform, "Assets/Kebab Interior/Prefabs/Chair.prefab", new Vector3(3.4f, 0f, -0.7f));
        Place(container.transform, "Assets/Kebab Interior/Prefabs/Chair.prefab", new Vector3(3.4f, 0f, 0.9f));
        Place(container.transform, "Assets/Kebab Interior/Prefabs/Metallic table.prefab", new Vector3(0f, 0f, -3.2f));
        Place(container.transform, "Assets/Kebab Interior/Prefabs/Plant.prefab", new Vector3(4.5f, 0f, -4f));

        // Comptoir : sauces / pain
        Place(container.transform, "Assets/Kebab Interior/Prefabs/Sauce 1.prefab", new Vector3(0.4f, 1.05f, 2.05f));
        Place(container.transform, "Assets/Kebab Interior/Prefabs/Sauce 2.prefab", new Vector3(0.7f, 1.05f, 2.05f));
        Place(container.transform, "Assets/Kebab Interior/Prefabs/Lettuce.prefab", new Vector3(-0.4f, 1.05f, 2.05f));
        Place(container.transform, "Assets/Kebab Interior/Prefabs/Tomato.prefab", new Vector3(-0.1f, 1.05f, 2.05f));
        Place(container.transform, "Assets/Kebab Interior/Prefabs/Onion.prefab", new Vector3(0.15f, 1.05f, 2.05f));
        Place(container.transform, "Assets/Kebab Interior/Prefabs/Pita bread.prefab", new Vector3(-0.9f, 1.05f, 1.95f));
        Place(container.transform, "Assets/Kebab Interior/Prefabs/Knife 1.prefab", new Vector3(-1.8f, 1.05f, 2.5f));

        Place(container.transform, "Assets/shawarma_shop/Prefabs/props/SM_sauces.prefab", new Vector3(1.15f, 1.05f, 2.15f));
        Place(container.transform, "Assets/shawarma_shop/Prefabs/props/SM_electric_grill.prefab", new Vector3(2.2f, 0f, 3.2f));

        if (container.transform.childCount == 0)
        {
            UnityEngine.Object.DestroyImmediate(container);
            throw new Exception("Aucun prefab Kebab Interior trouvé.");
        }

        AttachInteriorGameplay(container);
        EnsureDirectory("Assets/Prefabs/World");
        var saved = PrefabUtility.SaveAsPrefabAsset(container, InteriorPrefabPath);
        UnityEngine.Object.DestroyImmediate(container);
        return saved;
    }

    private static void Place(Transform parent, string path, Vector3 localPos)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) return;

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        if (go == null) return;

        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;
    }

    private static GameObject BuildInteriorFromDemoScene()
    {
        string demoPath = null;
        if (File.Exists(Path.Combine(Application.dataPath, "Kebab Interior", "Demo.unity")))
            demoPath = "Assets/Kebab Interior/Demo.unity";
        else if (File.Exists(Path.Combine(Application.dataPath, "Kebab Interior", "Scene", "Demo.unity")))
            demoPath = "Assets/Kebab Interior/Scene/Demo.unity";
        else
            throw new Exception("Scène Demo introuvable.");

        string previousPath = EditorSceneManager.GetActiveScene().path;
        Scene demo = EditorSceneManager.OpenScene(demoPath, OpenSceneMode.Additive);
        var roots = demo.GetRootGameObjects();

        var container = new GameObject("KebabInterior_Assembled");
        Bounds bounds = new Bounds(Vector3.zero, Vector3.one);
        bool hasBounds = false;
        int copied = 0;

        foreach (var root in roots)
        {
            if (root == null) continue;
            string n = root.name.ToLowerInvariant();
            if (root.GetComponent<Camera>() != null) continue;
            if (n.Contains("eventsystem")) continue;
            if (n.Contains("directional")) continue;
            if (n.Contains("skybox") || n.Contains("sky ")) continue;
            if (n.Contains("terrain") || n == "new terrain") continue;
            // Garder Point/Spot lights de la Demo (ambiance Asset Store)

            var copy = UnityEngine.Object.Instantiate(root);
            copy.name = root.name;
            copy.transform.SetParent(container.transform, true);
            copied++;

            foreach (var r in copy.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null) continue;
                if (!hasBounds) { bounds = r.bounds; hasBounds = true; }
                else bounds.Encapsulate(r.bounds);
            }
        }

        if (copied == 0)
        {
            UnityEngine.Object.DestroyImmediate(container);
            EditorSceneManager.CloseScene(demo, true);
            throw new Exception("Demo vide — aucun objet copié.");
        }

        if (hasBounds)
        {
            Vector3 offset = -bounds.center;
            offset.y = -bounds.min.y;
            foreach (Transform child in container.transform)
                child.position += offset;
        }

        // Pas de caméras / audio listeners dans le prefab gameplay
        var cams = container.GetComponentsInChildren<Camera>(true);
        for (int i = 0; i < cams.Length; i++)
        {
            if (cams[i] != null)
                UnityEngine.Object.DestroyImmediate(cams[i].gameObject);
        }
        var listeners = container.GetComponentsInChildren<AudioListener>(true);
        for (int i = 0; i < listeners.Length; i++)
        {
            if (listeners[i] != null)
                UnityEngine.Object.DestroyImmediate(listeners[i]);
        }

        // Ne PAS FixMaterials ici en batchmode (Shader.Find → matériaux roses).
        // Conversion runtime dans InteriorEnvironmentSetup.Apply().

        AttachInteriorGameplay(container, fromDemo: true);
        EnsureDirectory("Assets/Prefabs/World");
        if (AssetDatabase.LoadAssetAtPath<GameObject>(InteriorPrefabPath) != null)
            AssetDatabase.DeleteAsset(InteriorPrefabPath);

        var prefab = PrefabUtility.SaveAsPrefabAsset(container, InteriorPrefabPath);
        UnityEngine.Object.DestroyImmediate(container);
        EditorSceneManager.CloseScene(demo, true);

        if (!string.IsNullOrEmpty(previousPath))
        {
            string abs = previousPath.Replace("Assets/", Application.dataPath + "/").Replace("Assets\\", Application.dataPath + "\\");
            if (File.Exists(abs))
                EditorSceneManager.OpenScene(previousPath, OpenSceneMode.Single);
        }

        Debug.Log($"[Auto-Wire] Demo Asset Store copiée ({copied} racines) → {InteriorPrefabPath}");
        return prefab;
    }

    private static void AttachInteriorGameplay(GameObject container, bool fromDemo = false)
    {
        var interior = container.GetComponent<RestaurantInterior>();
        if (interior == null) interior = container.AddComponent<RestaurantInterior>();
        interior.interiorRoot = container;

        Transform Make(string name, Vector3 localPos)
        {
            var existing = container.transform.Find(name);
            if (existing != null) return existing;
            var go = new GameObject(name);
            go.transform.SetParent(container.transform, false);
            go.transform.localPosition = localPos;
            return go.transform;
        }

        Vector3 spawnPos = new Vector3(0f, 0f, -2.2f);
        Vector3 counterPos = new Vector3(0f, 0f, 1.8f);
        if (fromDemo)
        {
            // Place le joueur face au comptoir (Bar / Cash) d'après les noms Demo
            Transform bar = FindChildContains(container.transform, "bar")
                            ?? FindChildContains(container.transform, "cash")
                            ?? FindChildContains(container.transform, "doner");
            if (bar != null)
            {
                counterPos = container.transform.InverseTransformPoint(bar.position);
                counterPos.y = 0f;
                spawnPos = counterPos + new Vector3(0f, 0f, -3.2f);
            }
        }

        interior.playerSpawn = Make("PlayerSpawn", spawnPos);
        interior.counterPoint = Make("CounterPoint", counterPos);
        interior.customerSpawn = Make("CustomerSpawn", spawnPos + new Vector3(0f, 0f, -2f));
        interior.customerExit = Make("CustomerExit", spawnPos + new Vector3(4f, 0f, -1f));

        interior.employeeSlots = new Transform[3];
        for (int i = 0; i < 3; i++)
            interior.employeeSlots[i] = Make("EmpSlot_" + i, counterPos + new Vector3(-1.5f + i * 1.5f, 0f, 1.2f));

        interior.queueSlots = new Transform[5];
        for (int i = 0; i < 5; i++)
            interior.queueSlots[i] = Make("Queue_" + i, counterPos + new Vector3(0f, 0f, -0.8f - i * 0.9f));

        var spawner = container.GetComponent<NPCSpawner>();
        if (spawner == null) spawner = container.AddComponent<NPCSpawner>();
        interior.npcSpawner = spawner;

        if (container.GetComponent<HygieneVisualController>() == null)
            container.AddComponent<HygieneVisualController>();

        // Lumières Demo déjà présentes → n'ajoute Fill que si besoin
        if (!fromDemo)
        {
            var lights = container.transform.Find("InteriorLights");
            if (lights == null)
            {
                var lightHolder = new GameObject("InteriorLights");
                lightHolder.transform.SetParent(container.transform, false);
                AddPrefabPointLight(lightHolder.transform, "FillCenter", new Vector3(0f, 2.8f, 0f), 1.4f, 18f);
                AddPrefabPointLight(lightHolder.transform, "FillCounter", new Vector3(0f, 2.4f, 2.5f), 1.1f, 14f);
            }
        }
    }

    private static Transform FindChildContains(Transform root, string token)
    {
        if (root == null) return null;
        string t = token.ToLowerInvariant();
        var all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].name.ToLowerInvariant().Contains(t))
                return all[i];
        }
        return null;
    }

    private static void EnsureDirectory(string assetPath)
    {
        if (AssetDatabase.IsValidFolder(assetPath)) return;
        string[] parts = assetPath.Split('/');
        string cur = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = cur + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(cur, parts[i]);
            cur = next;
        }
    }

    private static void AddPrefabPointLight(Transform parent, string name, Vector3 localPos, float intensity, float range)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.intensity = intensity;
        light.range = range;
        light.color = new Color(1f, 0.95f, 0.85f);
        light.shadows = LightShadows.None;
    }

    private static RestaurantBuilding BuildRestaurantBuildingPrefab(VisualCatalog catalog)
    {
        var root = new GameObject("KebabBuilding_Assembled");
        var building = root.AddComponent<RestaurantBuilding>();

        if (catalog.kebabExteriorPrefab != null)
        {
            var ext = (GameObject)PrefabUtility.InstantiatePrefab(catalog.kebabExteriorPrefab);
            if (ext != null)
            {
                ext.name = "Exterior";
                ext.transform.SetParent(root.transform, false);
                ext.transform.localPosition = Vector3.zero;
            }
        }
        else
        {
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "FacadeFallback";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 2f, 0f);
            body.transform.localScale = new Vector3(8f, 4f, 6f);
            body.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Standard"))
            {
                color = new Color(0.55f, 0.25f, 0.1f)
            };
        }

        var entrance = new GameObject("Entrance").transform;
        entrance.SetParent(root.transform, false);
        entrance.localPosition = new Vector3(0f, 0f, -6f);
        building.entrancePoint = entrance;

        if (catalog.kebabInteriorPrefab != null)
        {
            var interiorGo = (GameObject)PrefabUtility.InstantiatePrefab(catalog.kebabInteriorPrefab);
            if (interiorGo != null)
            {
                interiorGo.name = "Interior";
                interiorGo.transform.SetParent(root.transform, false);
                interiorGo.transform.localPosition = new Vector3(0f, 0f, 10f);
                building.interior = interiorGo.GetComponent<RestaurantInterior>();
                if (building.interior != null)
                    building.interiorSpawnPoint = building.interior.playerSpawn;
            }
        }

        var box = root.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 2f, 2f);
        box.size = new Vector3(12f, 6f, 18f);

        PrefabUtility.SaveAsPrefabAsset(root, BuildingPrefabPath);
        UnityEngine.Object.DestroyImmediate(root);

        return AssetDatabase.LoadAssetAtPath<RestaurantBuilding>(BuildingPrefabPath);
    }
}
