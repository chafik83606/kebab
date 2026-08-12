using UnityEngine;

/// <summary>
/// Crée un personnage : prefab Asset Store si dispo, sinon humanoïde amélioré.
/// </summary>
public static class CharacterVisualFactory
{
    public static GameObject CreateCharacter(string name, float height, VisualCatalog catalog, bool isCustomer)
    {
        GameObject prefab = null;
        if (catalog != null)
            prefab = isCustomer ? catalog.GetRandomCustomer() : catalog.GetRandomEmployee();

        if (prefab != null)
        {
            string animKey = prefab.name;
            var go = Object.Instantiate(prefab);
            go.name = name;

            float scale = catalog != null ? catalog.characterScale : 1f;
            scale *= Random.Range(0.92f, 1.08f);
            go.transform.localScale = Vector3.one * scale;
            if (catalog != null)
                go.transform.position += catalog.characterPositionOffset;

            var cc = go.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            var cams = go.GetComponentsInChildren<Camera>();
            for (int i = 0; i < cams.Length; i++)
                cams[i].enabled = false;

            InteriorEnvironmentSetup.FixMaterials(go.transform);
            CharacterAnimatorSetup.Setup(go, animKey);
            return go;
        }

        return CreateImprovedHumanoid(name, height, isCustomer);
    }

    /// <summary>Humanoïde procédural un peu plus réaliste (tête, torse, bras, jambes).</summary>
    public static GameObject CreateImprovedHumanoid(string name, float height, bool isCustomer)
    {
        var root = new GameObject(name);

        Color skin = new Color(0.92f, 0.75f, 0.6f);
        Color shirt = isCustomer
            ? Color.HSVToRGB(Random.value, 0.45f, Random.Range(0.45f, 0.9f))
            : new Color(0.92f, 0.92f, 0.88f);
        Color pants = new Color(0.15f, 0.18f, 0.28f);

        // Torse
        var torso = CreatePart(root.transform, "Torso", PrimitiveType.Cube,
            new Vector3(0f, height * 0.55f, 0f), new Vector3(0.4f, height * 0.35f, 0.22f), shirt);

        // Tête
        CreatePart(root.transform, "Head", PrimitiveType.Sphere,
            new Vector3(0f, height * 0.82f, 0f), Vector3.one * (0.28f * height / 1.7f), skin);

        // Bras
        CreatePart(root.transform, "ArmL", PrimitiveType.Capsule,
            new Vector3(-0.32f, height * 0.55f, 0f), new Vector3(0.12f, 0.28f, 0.12f), shirt);
        CreatePart(root.transform, "ArmR", PrimitiveType.Capsule,
            new Vector3(0.32f, height * 0.55f, 0f), new Vector3(0.12f, 0.28f, 0.12f), shirt);

        // Jambes
        CreatePart(root.transform, "LegL", PrimitiveType.Capsule,
            new Vector3(-0.12f, height * 0.22f, 0f), new Vector3(0.14f, 0.32f, 0.14f), pants);
        CreatePart(root.transform, "LegR", PrimitiveType.Capsule,
            new Vector3(0.12f, height * 0.22f, 0f), new Vector3(0.14f, 0.32f, 0.14f), pants);

        // Collider simple pour le root (optionnel)
        var capsule = root.AddComponent<CapsuleCollider>();
        capsule.height = height;
        capsule.radius = 0.25f;
        capsule.center = new Vector3(0f, height * 0.5f, 0f);
        capsule.isTrigger = true;

        return root;
    }

    private static GameObject CreatePart(Transform parent, string name, PrimitiveType type,
        Vector3 localPos, Vector3 localScale, Color color)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.localPosition = localPos;
        go.transform.localScale = localScale;
        Object.Destroy(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().material.color = color;
        return go;
    }
}
