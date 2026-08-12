using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Génère une petite ville procédurale + les bâtiments kebab du joueur.
/// </summary>
public class CityWorldGenerator : MonoBehaviour
{
    [Header("Ville")]
    public int blockCount = 5;
    public float blockSize = 20f;
    public Material groundMaterial;
    public Material roadMaterial;
    public Material buildingMaterial;
    public Material kebabMaterial;

    [Header("Prefabs optionnels")]
    public RestaurantBuilding kebabPrefab;
    public VisualCatalog visualCatalog;

    public readonly List<RestaurantBuilding> spawnedKebabs = new List<RestaurantBuilding>();

    private Transform cityRoot;
    private Transform kebabsRoot;

    public void Generate()
    {
        if (visualCatalog == null)
            visualCatalog = Resources.Load<VisualCatalog>("VisualCatalog");

        if (kebabPrefab == null && visualCatalog != null)
            kebabPrefab = visualCatalog.restaurantBuildingPrefab;

        Clear();

        cityRoot = new GameObject("City").transform;
        cityRoot.SetParent(transform);
        kebabsRoot = new GameObject("PlayerKebabs").transform;
        kebabsRoot.SetParent(transform);

        CreateGround();
        CreateCityBlocks();
        SpawnPlayerRestaurants();
    }

    public void RefreshRestaurants()
    {
        // Recrée uniquement les kebabs du joueur (DestroyImmediate pour éviter les fantômes 1 frame)
        if (kebabsRoot != null)
        {
            for (int i = kebabsRoot.childCount - 1; i >= 0; i--)
            {
                var child = kebabsRoot.GetChild(i).gameObject;
                if (Application.isPlaying) Object.DestroyImmediate(child);
                else Object.DestroyImmediate(child);
            }
        }
        spawnedKebabs.Clear();
        SpawnPlayerRestaurants();
    }

    public void Clear()
    {
        spawnedKebabs.Clear();
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }
    }

    private void CreateGround()
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(cityRoot);
        ground.transform.localScale = new Vector3(blockCount * 2.5f, 1f, blockCount * 2.5f);
        ground.transform.position = Vector3.zero;
        ApplyMat(ground, groundMaterial ?? MakeMat(new Color(0.22f, 0.28f, 0.18f)));
    }

    private void CreateCityBlocks()
    {
        float offset = (blockCount - 1) * blockSize * 0.5f;
        for (int x = 0; x < blockCount; x++)
        {
            for (int z = 0; z < blockCount; z++)
            {
                // Routes
                Vector3 pos = new Vector3(x * blockSize - offset, 0f, z * blockSize - offset);
                CreateRoadCross(pos);

                // Immeubles décoratifs (pas au centre réservé aux kebabs)
                if (Random.value > 0.35f)
                    CreateDecorBuilding(pos + new Vector3(Random.Range(-6f, 6f), 0f, Random.Range(-6f, 6f)));
            }
        }
    }

    private void CreateRoadCross(Vector3 center)
    {
        var roadH = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roadH.name = "RoadH";
        roadH.transform.SetParent(cityRoot);
        roadH.transform.position = center + Vector3.up * 0.02f;
        roadH.transform.localScale = new Vector3(blockSize, 0.05f, 3.5f);
        ApplyMat(roadH, roadMaterial ?? MakeMat(new Color(0.18f, 0.18f, 0.2f)));

        var roadV = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roadV.name = "RoadV";
        roadV.transform.SetParent(cityRoot);
        roadV.transform.position = center + Vector3.up * 0.025f;
        roadV.transform.localScale = new Vector3(3.5f, 0.05f, blockSize);
        ApplyMat(roadV, roadMaterial ?? MakeMat(new Color(0.18f, 0.18f, 0.2f)));
    }

    private void CreateDecorBuilding(Vector3 pos)
    {
        float h = Random.Range(3f, 10f);
        var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
        b.name = "Building";
        b.transform.SetParent(cityRoot);
        b.transform.position = pos + Vector3.up * (h * 0.5f);
        b.transform.localScale = new Vector3(Random.Range(3f, 6f), h, Random.Range(3f, 6f));
        ApplyMat(b, buildingMaterial ?? MakeMat(new Color(
            Random.Range(0.25f, 0.45f),
            Random.Range(0.25f, 0.4f),
            Random.Range(0.3f, 0.5f))));
    }

    private void SpawnPlayerRestaurants()
    {
        float offset = (blockCount - 1) * blockSize * 0.5f;

        // Pas de kebab fantôme : uniquement les restos réellement possédés
        if (EmpireManager.Instance == null || EmpireManager.Instance.RestaurantCount == 0)
        {
            Debug.Log("[Kebab] SpawnPlayerRestaurants → 0 (en attente d'emplacement)");
            return;
        }

        IReadOnlyList<RestaurantData> restos = EmpireManager.Instance.Restaurants;
        int count = restos.Count;

        for (int i = 0; i < count; i++)
        {
            Vector3 pos;
            if (restos[i].mapWorldX != 0f || restos[i].mapWorldZ != 0f)
            {
                pos = new Vector3(restos[i].mapWorldX, 0f, restos[i].mapWorldZ);
            }
            else if (i == 0)
            {
                pos = new Vector3(6f, 0f, 4f);
            }
            else
            {
                int bx = i % blockCount;
                int bz = (i / blockCount) % blockCount;
                pos = new Vector3(bx * blockSize - offset + 5f, 0f, bz * blockSize - offset + 5f);
            }
            string name = !string.IsNullOrEmpty(restos[i].restaurantName)
                ? restos[i].restaurantName
                : ("Kebab " + restos[i].locationName);
            var building = CreateKebabBuilding(pos, i, name);
            spawnedKebabs.Add(building);
        }

        Debug.Log($"[Kebab] SpawnPlayerRestaurants → {spawnedKebabs.Count} kebab(s)");
    }

    private RestaurantBuilding CreateKebabBuilding(Vector3 pos, int index, string restoName)
    {
        GameObject root;
        RestaurantBuilding building;

        if (kebabPrefab != null)
        {
            building = Instantiate(kebabPrefab, pos, Quaternion.identity, kebabsRoot);
            root = building.gameObject;

            if (building.interior != null && building.interior.npcSpawner != null && visualCatalog != null)
                building.interior.npcSpawner.visualCatalog = visualCatalog;

            // Ne PAS scaler l'intérieur avec l'extérieur (sinon tables/doner = grains de riz)
            FitExteriorOnly(root);
            EnsureMapCollider(root);
            AddMapBeacon(root.transform);
            InteriorEnvironmentSetup.FixMaterials(root.transform);
            building.Bind(index, restoName);
            return building;
        }

        if (visualCatalog != null && (visualCatalog.kebabExteriorPrefab != null || visualCatalog.kebabInteriorPrefab != null))
        {
            root = new GameObject("KebabBuilding");
            root.transform.SetParent(kebabsRoot);
            root.transform.position = pos;
            building = root.AddComponent<RestaurantBuilding>();

            if (visualCatalog.kebabExteriorPrefab != null)
            {
                var ext = Instantiate(visualCatalog.kebabExteriorPrefab, root.transform);
                ext.name = "Exterior";
                ext.transform.localPosition = Vector3.zero;
                var rends = ext.GetComponentsInChildren<Renderer>();
                if (rends.Length > 0)
                {
                    Bounds b = rends[0].bounds;
                    for (int r = 1; r < rends.Length; r++) b.Encapsulate(rends[r].bounds);
                    float size = Mathf.Max(b.size.x, b.size.z);
                    if (size > 0.01f && size < 3f)
                        ext.transform.localScale = Vector3.one * (8f / size);
                    else if (size > 25f)
                        ext.transform.localScale = Vector3.one * (12f / size);
                }
                InteriorEnvironmentSetup.FixMaterials(ext.transform);
            }

            var entrance = new GameObject("Entrance").transform;
            entrance.SetParent(root.transform);
            entrance.localPosition = new Vector3(0f, 0f, -6f);
            building.entrancePoint = entrance;

            if (visualCatalog.kebabInteriorPrefab != null)
            {
                var interiorGo = Instantiate(visualCatalog.kebabInteriorPrefab, root.transform);
                interiorGo.name = "Interior";
                interiorGo.transform.localPosition = new Vector3(0f, 0f, 10f);
                building.interior = interiorGo.GetComponent<RestaurantInterior>();
                if (building.interior == null)
                {
                    building.interior = interiorGo.AddComponent<RestaurantInterior>();
                    building.interior.interiorRoot = interiorGo;
                }
                if (building.interior.npcSpawner != null)
                    building.interior.npcSpawner.visualCatalog = visualCatalog;
                building.interiorSpawnPoint = building.interior.playerSpawn;
            }
            else
            {
                var interiorGo = BuildInterior(root.transform);
                building.interior = interiorGo.GetComponent<RestaurantInterior>();
                building.interiorSpawnPoint = building.interior.playerSpawn;
            }

            var box = root.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 2f, 2f);
            box.size = new Vector3(12f, 6f, 18f);

            AddMapBeacon(root.transform);
            building.Bind(index, restoName);
            return building;
        }

        // Fallback procédural
        root = new GameObject("KebabBuilding");
        root.transform.SetParent(kebabsRoot);
        root.transform.position = pos;
        building = root.AddComponent<RestaurantBuilding>();

        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Facade";
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0f, 2f, 0f);
        body.transform.localScale = new Vector3(8f, 4f, 6f);
        ApplyMat(body, kebabMaterial ?? MakeMat(new Color(0.55f, 0.25f, 0.1f)));

        var sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sign.name = "Sign";
        sign.transform.SetParent(root.transform);
        sign.transform.localPosition = new Vector3(0f, 4.3f, -3.1f);
        sign.transform.localScale = new Vector3(5f, 1f, 0.2f);
        ApplyMat(sign, MakeMat(new Color(0.9f, 0.55f, 0.1f)));
        building.signRenderer = sign.GetComponent<Renderer>();

        AddMapBeacon(root.transform);

        var door = GameObject.CreatePrimitive(PrimitiveType.Cube);
        door.name = "Door";
        door.transform.SetParent(root.transform);
        door.transform.localPosition = new Vector3(0f, 1.2f, -3.05f);
        door.transform.localScale = new Vector3(1.6f, 2.4f, 0.15f);
        ApplyMat(door, MakeMat(new Color(0.15f, 0.1f, 0.08f)));

        var entranceFb = new GameObject("Entrance").transform;
        entranceFb.SetParent(root.transform);
        entranceFb.localPosition = new Vector3(0f, 0f, -5f);
        building.entrancePoint = entranceFb;

        var interiorFb = BuildInterior(root.transform);
        building.interior = interiorFb.GetComponent<RestaurantInterior>();
        building.interiorSpawnPoint = building.interior.playerSpawn;

        var boxFb = root.AddComponent<BoxCollider>();
        boxFb.center = new Vector3(0f, 2f, 0f);
        boxFb.size = new Vector3(12f, 6f, 14f);

        building.Bind(index, restoName);
        return building;
    }

    private static void AddMapBeacon(Transform parent)
    {
        if (parent.Find("MapBeacon") != null) return;
        var beacon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        beacon.name = "MapBeacon";
        beacon.transform.SetParent(parent);
        beacon.transform.localPosition = new Vector3(0f, 8f, 0f);
        beacon.transform.localScale = new Vector3(2f, 4f, 2f);
        ApplyMat(beacon, MakeMat(new Color(1f, 0.35f, 0.05f)));
        Object.Destroy(beacon.GetComponent<Collider>());
    }

    /// <summary>Remet le bâtiment à une taille lisible sur la carte mobile.</summary>
    private static void NormalizeBuildingScale(GameObject root)
    {
        var rends = root.GetComponentsInChildren<Renderer>();
        if (rends == null || rends.Length == 0) return;
        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        float size = Mathf.Max(b.size.x, b.size.z);
        if (size < 0.5f)
            root.transform.localScale *= (10f / Mathf.Max(size, 0.01f));
        else if (size > 20f)
            root.transform.localScale *= (12f / size);
    }

    /// <summary>
    /// Scale uniquement la façade carte. L'intérieur reste à l'échelle joueur (1 unité = 1 m).
    /// </summary>
    private static void FitExteriorOnly(GameObject root)
    {
        if (root == null) return;
        root.transform.localScale = Vector3.one;

        Transform exterior = root.transform.Find("Exterior");
        Transform interior = null;
        for (int i = 0; i < root.transform.childCount; i++)
        {
            var c = root.transform.GetChild(i);
            if (c.name.Contains("Interior") || c.GetComponent<RestaurantInterior>() != null)
                interior = c;
        }

        if (exterior != null)
        {
            var rends = exterior.GetComponentsInChildren<Renderer>();
            if (rends != null && rends.Length > 0)
            {
                Bounds b = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                float size = Mathf.Max(b.size.x, b.size.z);
                if (size > 0.01f && size < 3f)
                    exterior.localScale = Vector3.one * (9f / size);
                else if (size > 18f)
                    exterior.localScale = Vector3.one * (12f / size);
            }
        }

        if (interior != null)
        {
            interior.localScale = Vector3.one;
            // Intérieur centré sous le bâtiment (plus derrière la façade)
            interior.localPosition = new Vector3(0f, 0f, 0f);
            for (int i = 0; i < interior.childCount; i++)
                interior.GetChild(i).gameObject.SetActive(true);
            foreach (var r in interior.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null) continue;
                r.enabled = true;
                r.gameObject.SetActive(true);
            }
        }
    }

    /// <summary>Collider large pour tap carte / double-tap (prefabs Asset Store souvent sans collider utile).</summary>
    private static void EnsureMapCollider(GameObject root)
    {
        if (root == null) return;
        var existing = root.GetComponent<BoxCollider>();
        if (existing == null)
            existing = root.AddComponent<BoxCollider>();
        existing.isTrigger = false;
        existing.center = new Vector3(0f, 2.5f, 2f);
        existing.size = new Vector3(14f, 8f, 18f);
    }

    private GameObject BuildInterior(Transform parent)
    {
        var interiorRoot = new GameObject("Interior");
        interiorRoot.transform.SetParent(parent);
        interiorRoot.transform.localPosition = new Vector3(0f, 0f, 8f);

        var interior = interiorRoot.AddComponent<RestaurantInterior>();
        interior.interiorRoot = interiorRoot;

        // Sol intérieur
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.SetParent(interiorRoot.transform);
        floor.transform.localPosition = new Vector3(0f, 0.05f, 0f);
        floor.transform.localScale = new Vector3(12f, 0.1f, 10f);
        ApplyMat(floor, MakeMat(new Color(0.35f, 0.28f, 0.2f)));

        // Murs
        CreateWall(interiorRoot.transform, new Vector3(0f, 1.5f, 5f), new Vector3(12f, 3f, 0.2f));
        CreateWall(interiorRoot.transform, new Vector3(-6f, 1.5f, 0f), new Vector3(0.2f, 3f, 10f));
        CreateWall(interiorRoot.transform, new Vector3(6f, 1.5f, 0f), new Vector3(0.2f, 3f, 10f));

        // Comptoir
        var counter = GameObject.CreatePrimitive(PrimitiveType.Cube);
        counter.name = "Counter";
        counter.transform.SetParent(interiorRoot.transform);
        counter.transform.localPosition = new Vector3(0f, 0.7f, 2.5f);
        counter.transform.localScale = new Vector3(6f, 1.2f, 1f);
        ApplyMat(counter, MakeMat(new Color(0.25f, 0.18f, 0.12f)));

        var counterPoint = new GameObject("CounterPoint").transform;
        counterPoint.SetParent(interiorRoot.transform);
        counterPoint.localPosition = new Vector3(0f, 0f, 1.2f);
        interior.counterPoint = counterPoint;

        // Slots employés derrière le comptoir
        interior.employeeSlots = new Transform[3];
        for (int i = 0; i < 3; i++)
        {
            var slot = new GameObject("EmpSlot_" + i).transform;
            slot.SetParent(interiorRoot.transform);
            slot.localPosition = new Vector3(-2f + i * 2f, 0f, 3.5f);
            interior.employeeSlots[i] = slot;
        }

        // File d'attente clients
        interior.queueSlots = new Transform[5];
        for (int i = 0; i < 5; i++)
        {
            var slot = new GameObject("Queue_" + i).transform;
            slot.SetParent(interiorRoot.transform);
            slot.localPosition = new Vector3(0f, 0f, 0.5f - i * 1.2f);
            interior.queueSlots[i] = slot;
        }

        var spawn = new GameObject("CustomerSpawn").transform;
        spawn.SetParent(interiorRoot.transform);
        spawn.localPosition = new Vector3(0f, 0f, -4f);
        interior.customerSpawn = spawn;

        var exit = new GameObject("CustomerExit").transform;
        exit.SetParent(interiorRoot.transform);
        exit.localPosition = new Vector3(4f, 0f, -4f);
        interior.customerExit = exit;

        var playerSpawn = new GameObject("PlayerSpawn").transform;
        playerSpawn.SetParent(interiorRoot.transform);
        playerSpawn.localPosition = new Vector3(3f, 0f, -2f);
        interior.playerSpawn = playerSpawn;

        var spawner = interiorRoot.AddComponent<NPCSpawner>();
        interior.npcSpawner = spawner;

        // Visuels hygiène optionnels
        var hygiene = interiorRoot.AddComponent<HygieneVisualController>();
        // Placeholders saleté au sol
        var stain = GameObject.CreatePrimitive(PrimitiveType.Quad);
        stain.name = "DirtStain";
        stain.transform.SetParent(interiorRoot.transform);
        stain.transform.localPosition = new Vector3(1f, 0.12f, 0f);
        stain.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        stain.transform.localScale = Vector3.one * 1.5f;
        ApplyMat(stain, MakeMat(new Color(0.2f, 0.12f, 0.05f)));
        stain.SetActive(false);
        hygiene.dirtStains = new[] { stain };

        return interiorRoot;
    }

    private void CreateWall(Transform parent, Vector3 localPos, Vector3 scale)
    {
        var w = GameObject.CreatePrimitive(PrimitiveType.Cube);
        w.name = "Wall";
        w.transform.SetParent(parent);
        w.transform.localPosition = localPos;
        w.transform.localScale = scale;
        ApplyMat(w, MakeMat(new Color(0.75f, 0.7f, 0.6f)));
    }

    private static void ApplyMat(GameObject go, Material mat)
    {
        var r = go.GetComponent<Renderer>();
        if (r != null) r.sharedMaterial = mat;
    }

    private static Material MakeMat(Color c)
    {
        var mat = new Material(Shader.Find("Standard"));
        mat.color = c;
        return mat;
    }
}
