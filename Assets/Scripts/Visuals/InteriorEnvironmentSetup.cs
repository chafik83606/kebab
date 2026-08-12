using UnityEngine;

/// <summary>
/// Éclairage intérieur + correction des matériaux magenta (shaders manquants sur mobile).
/// </summary>
public static class InteriorEnvironmentSetup
{
    private static Color savedAmbient;
    private static float savedAmbientIntensity;
    private static bool ambientSaved;

    private static Shader fallbackShader;

    public static void Apply(RestaurantInterior interior)
    {
        if (interior == null) return;

        Transform root = interior.interiorRoot != null ? interior.interiorRoot.transform : interior.transform;
        FixMaterials(root);
        EnsureLights(root);
        EnsureRoomShell(root);

        if (!ambientSaved)
        {
            savedAmbient = RenderSettings.ambientLight;
            savedAmbientIntensity = RenderSettings.ambientIntensity;
            ambientSaved = true;
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.62f, 0.55f, 0.45f);
        RenderSettings.ambientIntensity = 1.5f;
    }

    /// <summary>
    /// Sol + murs BoxCollider. Visibles seulement si l'asset Demo n'a pas assez de géométrie.
    /// </summary>
    public static void EnsureRoomShell(Transform root)
    {
        if (root == null) return;
        var old = root.Find("RoomShell");
        if (old != null)
            Object.DestroyImmediate(old.gameObject);

        // Toujours une coque visible : sur mobile beaucoup d'assets rendent blanc/magenta
        bool visibleFallback = true;

        var shell = new GameObject("RoomShell").transform;
        shell.SetParent(root, false);

        // Recalcule une taille d'après les bounds réels
        Bounds b = new Bounds(root.position, Vector3.one * 4f);
        bool has = false;
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            if (r == null) continue;
            if (!has) { b = r.bounds; has = true; }
            else b.Encapsulate(r.bounds);
        }

        Vector3 center = root.InverseTransformPoint(b.center);
        float w = Mathf.Max(10f, b.size.x + 1.5f);
        float d = Mathf.Max(8f, b.size.z + 1.5f);
        float h = Mathf.Max(3f, b.size.y);

        MakeShellPart(shell, "ShellFloor", new Vector3(center.x, -0.08f, center.z),
            new Vector3(w, 0.2f, d), new Color(0.55f, 0.48f, 0.38f), visibleFallback);

        MakeShellPart(shell, "ShellWallN", new Vector3(center.x, h * 0.5f, center.z + d * 0.5f),
            new Vector3(w, h, 0.25f), new Color(0.78f, 0.72f, 0.62f), visibleFallback);
        MakeShellPart(shell, "ShellWallS", new Vector3(center.x, h * 0.5f, center.z - d * 0.5f),
            new Vector3(w, h, 0.25f), new Color(0.74f, 0.68f, 0.58f), visibleFallback);
        MakeShellPart(shell, "ShellWallW", new Vector3(center.x - w * 0.5f, h * 0.5f, center.z),
            new Vector3(0.25f, h, d), new Color(0.76f, 0.70f, 0.60f), visibleFallback);
        MakeShellPart(shell, "ShellWallE", new Vector3(center.x + w * 0.5f, h * 0.5f, center.z),
            new Vector3(0.25f, h, d), new Color(0.76f, 0.70f, 0.60f), visibleFallback);

        if (visibleFallback)
        {
            MakeShellPart(shell, "ShellCeiling", new Vector3(center.x, h + 0.1f, center.z),
                new Vector3(w, 0.15f, d), new Color(0.92f, 0.9f, 0.86f), true);
        }
    }

    private static void MakeShellPart(Transform parent, string name, Vector3 localPos, Vector3 scale, Color color, bool visible)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;

        var rend = go.GetComponent<Renderer>();
        if (rend != null)
        {
            if (!visible)
                rend.enabled = false;
            else
            {
                var sh = Shader.Find("Mobile/Diffuse")
                         ?? Shader.Find("Legacy Shaders/Diffuse")
                         ?? Shader.Find("Standard");
                var mat = new Material(sh);
                mat.color = color;
                rend.sharedMaterial = mat;
            }
        }
    }

    public static void Restore()
    {
        if (!ambientSaved) return;
        RenderSettings.ambientLight = savedAmbient;
        RenderSettings.ambientIntensity = savedAmbientIntensity;
        ambientSaved = false;
    }

    private static void EnsureLights(Transform root)
    {
        var holder = root.Find("InteriorLights");
        if (holder != null) return;

        holder = new GameObject("InteriorLights").transform;
        holder.SetParent(root, false);

        AddPointLight(holder, "FillCenter", new Vector3(0f, 2.8f, 0f), 1.4f, 18f);
        AddPointLight(holder, "FillCounter", new Vector3(0f, 2.4f, 2.5f), 1.1f, 14f);
        AddPointLight(holder, "FillBack", new Vector3(0f, 2.6f, -3f), 0.85f, 12f);

        var dirGo = new GameObject("InteriorDirectional");
        dirGo.transform.SetParent(holder, false);
        dirGo.transform.localRotation = Quaternion.Euler(55f, -25f, 0f);
        var dir = dirGo.AddComponent<Light>();
        dir.type = LightType.Directional;
        dir.intensity = 0.55f;
        dir.color = new Color(1f, 0.97f, 0.9f);
        dir.shadows = LightShadows.None;
    }

    private static void AddPointLight(Transform parent, string name, Vector3 localPos, float intensity, float range)
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

    public static void FixMaterials(Transform root)
    {
        if (root == null) return;
        if (fallbackShader == null)
        {
            fallbackShader = Shader.Find("Mobile/Diffuse")
                             ?? Shader.Find("Legacy Shaders/Diffuse")
                             ?? Shader.Find("Unlit/Texture")
                             ?? Shader.Find("Sprites/Default")
                             ?? Shader.Find("Standard");
        }

        if (fallbackShader == null)
        {
            Debug.LogWarning("[Kebab] Aucun shader fallback trouvé — matériaux non convertis.");
            return;
        }

        var renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r == null) continue;

            // Instance materials (évite de casser les assets partagés rose)
            var mats = r.materials;
            bool changed = false;
            for (int m = 0; m < mats.Length; m++)
            {
                if (!NeedsFix(mats[m])) continue;
                mats[m] = CreateFallbackMaterial(mats[m], r.gameObject.name);
                changed = true;
            }
            if (changed)
                r.materials = mats;
        }
    }

    private static bool NeedsFix(Material mat)
    {
        if (mat == null) return true;
        if (mat.shader == null) return true;
        string name = mat.shader.name;
        if (!mat.shader.isSupported) return true;
        if (name.Contains("InternalError")) return true;
        if (name.StartsWith("Hidden/")) return true;
        if (name.Contains("Universal Render Pipeline") || name.Contains("HDRP"))
            return true;
        // Standard / Glass souvent roses ou invisibles sur Android Built-in
        if (name == "Standard" || name.StartsWith("Standard ") || name.Contains("Glass") || name.Contains("Transparent"))
            return true;
        return false;
    }

    private static Material CreateFallbackMaterial(Material original, string objectName)
    {
        var mat = new Material(fallbackShader);
        Color col = GuessColor(objectName);
        Texture tex = ExtractTexture(original);
        if (original != null)
        {
            if (original.HasProperty("_Color"))
                col = original.color;
            else if (original.HasProperty("_BaseColor"))
                col = original.GetColor("_BaseColor");
        }

        if (mat.HasProperty("_Color")) mat.color = col;
        if (tex != null)
        {
            if (mat.HasProperty("_MainTex")) mat.mainTexture = tex;
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
        }
        return mat;
    }

    private static Texture ExtractTexture(Material original)
    {
        if (original == null) return null;
        if (original.HasProperty("_MainTex"))
        {
            var t = original.GetTexture("_MainTex");
            if (t != null) return t;
        }
        if (original.HasProperty("_BaseMap"))
        {
            var t = original.GetTexture("_BaseMap");
            if (t != null) return t;
        }
        return original.mainTexture;
    }

    private static Color GuessColor(string objectName)
    {
        string n = objectName.ToLowerInvariant();
        if (n.Contains("floor") || n.Contains("tile")) return new Color(0.72f, 0.58f, 0.38f);
        if (n.Contains("wall") || n.Contains("ceiling")) return new Color(0.85f, 0.82f, 0.75f);
        if (n.Contains("bar") || n.Contains("counter") || n.Contains("table")) return new Color(0.55f, 0.35f, 0.18f);
        if (n.Contains("metal")) return new Color(0.65f, 0.65f, 0.68f);
        if (n.Contains("glass")) return new Color(0.75f, 0.85f, 0.9f, 0.6f);
        if (n.Contains("meat") || n.Contains("doner")) return new Color(0.55f, 0.28f, 0.15f);
        return new Color(0.7f, 0.65f, 0.58f);
    }
}
