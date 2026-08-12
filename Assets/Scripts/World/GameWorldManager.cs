using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Chef d'orchestre 3D : carte, rue, magasin, caisse, HUD.
/// </summary>
public class GameWorldManager : MonoBehaviour
{
    public static GameWorldManager Instance { get; private set; }

    [Header("Monde")]
    public CityWorldGenerator cityGenerator;
    public PlayerController player;
    public Transform worldSpawnPoint;
    public GameCameraDirector cameraDirector;
    public VisualCatalog visualCatalog;
    public CounterService3D counterService3D;

    [Header("HUD")]
    public GameObject gameplayHud;
    public Text moneyHudText;
    public Text dayHudText;
    public Text promptText;
    public Button enterRestaurantButton;
    public Button exitRestaurantButton;
    public Button nextDayButton;
    public Button openManageButton;
    public Button mapModeButton;
    public Button walkModeButton;
    public Button counterServiceButton;
    public GameObject manageOverlay;
    public GameObject mobileControlsRoot;

    public RestaurantBuilding CurrentBuilding { get; private set; }
    public bool IsInsideRestaurant { get; private set; }

    private Vector3 lastStreetPosition;
    private float lastStreetYaw;

    private void Awake()
    {
        Instance = this;
        if (visualCatalog == null)
            visualCatalog = Resources.Load<VisualCatalog>("VisualCatalog");
    }

    private void Start()
    {
        StartCoroutine(BootWorld());
    }

    private System.Collections.IEnumerator BootWorld()
    {
        // Attendre EmpireManager.Start (save chargée) — sinon carte trop tôt → double kebab
        for (int i = 0; i < 120; i++)
        {
            if (EmpireManager.Instance != null && EmpireManager.Instance.IsBooted)
                break;
            yield return null;
        }

        EnsureGameFlowUI();

        if (cityGenerator != null)
        {
            if (visualCatalog != null)
                cityGenerator.visualCatalog = visualCatalog;
            cityGenerator.Generate();
            ApplyCatalogToInteriors();
            int n = cityGenerator.spawnedKebabs.Count;
            Debug.Log($"[Kebab] Ville générée — kebabs: {n}");
        }

        if (cameraDirector == null)
            cameraDirector = FindObjectOfType<GameCameraDirector>();

        if (cameraDirector != null)
            cameraDirector.SetMapMode(worldSpawnPoint != null ? worldSpawnPoint.position : Vector3.zero);

        if (player != null)
        {
            player.OnNearRestaurantChanged += OnNearRestaurant;
            player.interactRange = 8f;
            KebabWorldBootstrap.EnsurePlayerVisualPublic(player);
        }

        EmpireManager.Instance?.EnsureRestaurantManagers();
        EnsureManageOverlay();
        WireHud();

        if (EmpireManager.Instance != null)
            EmpireManager.Instance.OnEmpireUpdated += RefreshHud;

        RefreshHud();
        ApplyFlowPhase();
    }

    private void EnsureGameFlowUI()
    {
        var flow = FindObjectOfType<GameFlowController>();
        if (flow == null)
        {
            var go = new GameObject("GameFlowController");
            flow = go.AddComponent<GameFlowController>();
        }

        if (flow.mapUI == null && gameplayHud != null)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            KebabWorldBootstrap.BuildGameFlowUI(gameplayHud.transform, font);
        }

        if (EmpireManager.Instance != null)
            flow.InitFromSave((int)EmpireManager.Instance.CurrentPhase, EmpireManager.Instance.AwaitingSetup);
    }

    public void ApplyFlowPhase()
    {
        var flow = GameFlowController.Instance;
        var empire = EmpireManager.Instance;

        if (flow == null) return;

        if (empire != null && empire.RestaurantCount == 0)
        {
            SetGameplayHudVisible(true, minimalOverlay: true);
            flow.BeginMapSelection(false);
            if (cameraDirector != null)
                cameraDirector.SetMapMode(worldSpawnPoint != null ? worldSpawnPoint.position : Vector3.zero);
            if (promptText != null)
            {
                promptText.gameObject.SetActive(true);
                promptText.text = "Tape une ville sur la carte";
            }
            return;
        }

        if (flow.IsBlockingGameplay)
        {
            SetGameplayHudVisible(true, minimalOverlay: true);
            if (flow.Phase == GamePhase.MapSelection)
                flow.ShowMap();
            if (flow.Phase == GamePhase.SetupWizard && promptText != null)
            {
                promptText.gameObject.SetActive(true);
                promptText.text = "Configure ton kebab — le magasin reste visible";
            }
            if (cameraDirector != null && !IsInsideRestaurant)
                cameraDirector.SetMapMode(worldSpawnPoint != null ? worldSpawnPoint.position : Vector3.zero);
            return;
        }

        SetGameplayHudVisible(true);
        ShowMapHud();
        RefreshCounterVisibility();
        if (promptText != null && empire != null)
        {
            int n = empire.RestaurantCount;
            promptText.text = n > 0
                ? $"CARTE — {n} kebab · Gérer / ▶ Jour"
                : "Carte — Achetez un emplacement via Gérer";
        }
    }

    public void OnFlowPhaseChanged()
    {
        if (cityGenerator != null)
            cityGenerator.RefreshRestaurants();
        ApplyCatalogToInteriors();
        ApplyFlowPhase();
        RefreshHud();
    }

    /// <summary>Après l'assistant : entre directement dans le magasin fraîchement ouvert.</summary>
    public void EnterNewlyFoundedRestaurant()
    {
        if (cityGenerator == null || cityGenerator.spawnedKebabs.Count == 0)
            return;
        var building = cityGenerator.spawnedKebabs[cityGenerator.spawnedKebabs.Count - 1];
        if (building == null) return;
        EnterRestaurant(building);
    }

    private void SetGameplayHudVisible(bool visible, bool minimalOverlay = false)
    {
        if (minimalOverlay)
        {
            // Pendant carte / assistant : garder l'argent et le jour visibles, pas la barre du bas
            if (mapModeButton != null) mapModeButton.gameObject.SetActive(false);
            if (walkModeButton != null) walkModeButton.gameObject.SetActive(false);
            if (nextDayButton != null) nextDayButton.gameObject.SetActive(false);
            if (openManageButton != null) openManageButton.gameObject.SetActive(false);
            if (enterRestaurantButton != null) enterRestaurantButton.gameObject.SetActive(false);
            if (mobileControlsRoot != null) mobileControlsRoot.SetActive(false);
            return;
        }

        if (mapModeButton != null) mapModeButton.gameObject.SetActive(visible);
        if (walkModeButton != null) walkModeButton.gameObject.SetActive(visible);
        if (nextDayButton != null) nextDayButton.gameObject.SetActive(visible);
        if (openManageButton != null) openManageButton.gameObject.SetActive(visible);
        if (enterRestaurantButton != null) enterRestaurantButton.gameObject.SetActive(visible);
        if (mobileControlsRoot != null) mobileControlsRoot.SetActive(visible);
    }

    private void EnsureManageOverlay()
    {
        if (manageOverlay != null)
        {
            var ui = manageOverlay.GetComponent<ManageOverlayUI>();
            if (ui != null && ui.meatBoeufButton != null && manageOverlay.transform.Find("OverlayDim") != null)
                return;
            Destroy(manageOverlay);
            manageOverlay = null;
        }
        if (manageOverlay != null || gameplayHud == null) return;
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                     ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        manageOverlay = KebabWorldBootstrap.BuildManageOverlayPublic(gameplayHud.transform, font);
    }

    private void ApplyCatalogToInteriors()
    {
        if (cityGenerator == null) return;
        foreach (var b in cityGenerator.spawnedKebabs)
        {
            if (b.interior != null && b.interior.npcSpawner != null)
                b.interior.npcSpawner.visualCatalog = visualCatalog;
        }
        if (counterService3D != null)
            counterService3D.visualCatalog = visualCatalog;
    }

    private void OnDestroy()
    {
        if (player != null)
            player.OnNearRestaurantChanged -= OnNearRestaurant;
        if (EmpireManager.Instance != null)
            EmpireManager.Instance.OnEmpireUpdated -= RefreshHud;
    }

    private void WireHud()
    {
        EnlargeEnterButton();
        CompactHudButtons();
        Bind(enterRestaurantButton, TryEnterFromHud);
        Bind(exitRestaurantButton, ExitRestaurant);
        Bind(nextDayButton, () =>
        {
            if (GameFlowController.Instance != null && GameFlowController.Instance.IsBlockingGameplay) return;
            float before = EmpireManager.Instance != null ? EmpireManager.Instance.Money : 0f;
            EmpireManager.Instance?.StartNewDay();
            if (promptText != null && EmpireManager.Instance != null)
            {
                float delta = EmpireManager.Instance.Money - before;
                string place = "";
                if (CurrentBuilding != null)
                {
                    var d = EmpireManager.Instance.GetRestaurant(CurrentBuilding.restaurantIndex);
                    if (d != null) place = d.locationName + " · ";
                }
                promptText.gameObject.SetActive(true);
                promptText.text = delta >= 0f
                    ? $"{place}Jour {EmpireManager.Instance.CurrentDay} · +{delta:F0} €"
                    : $"{place}Jour {EmpireManager.Instance.CurrentDay} · {delta:F0} €";
            }
        });
        Bind(openManageButton, ToggleManageOverlay);
        Bind(mapModeButton, GoToMapMode);
        Bind(walkModeButton, GoToWalkMode);
        Bind(counterServiceButton, StartCounterService);
    }

    /// <summary>Bouton ENTRER (carte ou proximité).</summary>
    public void TryEnterFromHud()
    {
        RestaurantBuilding target = null;
        if (player != null && player.NearestRestaurant != null)
            target = player.NearestRestaurant;
        else if (cameraDirector != null && cameraDirector.FocusedBuilding != null)
            target = cameraDirector.FocusedBuilding;
        else
            target = FindBestRestaurantForEnter();

        if (target != null)
            EnterRestaurant(target);
        else
            EmpireManager.Instance?.Notify("Aucun kebab trouvé — avance un jour ou relance.");
    }

    private void HideCounterServiceButton()
    {
        RefreshCounterVisibility();
    }

    public void RefreshCounterVisibility()
    {
        if (counterServiceButton == null) return;
        bool show = false;
        if (IsInsideRestaurant && CurrentBuilding != null && EmpireManager.Instance != null)
        {
            var data = EmpireManager.Instance.GetRestaurant(CurrentBuilding.restaurantIndex);
            show = data != null && data.managementMode == ManagementMode.Manual;
        }
        counterServiceButton.gameObject.SetActive(show);
        if (show)
            ResizeHudBtn(counterServiceButton, new Vector2(230f, 16f), new Vector2(100f, 40f), "Caisse");
    }

    /// <summary>Réduit la barre du bas pour laisser voir le magasin — assez haut pour le geste Android.</summary>
    private void CompactHudButtons()
    {
        ResizeHudBtn(mapModeButton, new Vector2(-250f, 72f), new Vector2(110f, 48f), "Carte");
        ResizeHudBtn(walkModeButton, new Vector2(-125f, 72f), new Vector2(110f, 48f), "Marcher");
        ResizeHudBtn(nextDayButton, new Vector2(0f, 72f), new Vector2(120f, 48f), "▶ Jour");
        RefreshCounterVisibility();
        ResizeHudBtn(openManageButton, new Vector2(250f, 72f), new Vector2(110f, 48f), "Gérer");
        ResizeHudBtn(counterServiceButton, new Vector2(125f, 72f), new Vector2(110f, 48f), "Caisse");
        ResizeHudBtn(exitRestaurantButton, new Vector2(280f, 140f), new Vector2(160f, 48f), "SORTIR");
        if (promptText != null)
        {
            var prt = promptText.GetComponent<RectTransform>();
            if (prt != null)
            {
                prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 1f);
                prt.pivot = new Vector2(0.5f, 1f);
                prt.anchoredPosition = new Vector2(0f, -100f);
                prt.sizeDelta = new Vector2(700f, 56f);
            }
            promptText.fontSize = 20;
            promptText.alignment = TextAnchor.MiddleCenter;
        }
    }

    private static void ResizeHudBtn(Button btn, Vector2 pos, Vector2 size, string label)
    {
        if (btn == null) return;
        var rt = btn.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }
        var t = btn.GetComponentInChildren<Text>();
        if (t != null)
        {
            t.text = label;
            t.fontSize = 18;
            t.raycastTarget = false;
        }
    }

    private void EnlargeEnterButton()
    {
        if (enterRestaurantButton == null) return;
        var rt = enterRestaurantButton.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 70f);
            rt.sizeDelta = new Vector2(360f, 52f);
        }
        var label = enterRestaurantButton.GetComponentInChildren<Text>();
        if (label != null)
        {
            label.text = "ENTRER";
            label.fontSize = 20;
            label.raycastTarget = false;
        }
        enterRestaurantButton.transform.SetAsLastSibling();
    }

    private static void Bind(Button btn, UnityEngine.Events.UnityAction action)
    {
        if (btn == null) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(action);
    }

    private RestaurantBuilding FindBestRestaurantForEnter()
    {
        if (cityGenerator != null && cityGenerator.spawnedKebabs != null && cityGenerator.spawnedKebabs.Count > 0)
        {
            if (cameraDirector != null && cameraDirector.FocusedBuilding != null)
                return cameraDirector.FocusedBuilding;
            return cityGenerator.spawnedKebabs[0];
        }

        var buildings = FindObjectsOfType<RestaurantBuilding>();
        if (buildings == null || buildings.Length == 0) return null;
        if (cameraDirector != null && cameraDirector.FocusedBuilding != null)
            return cameraDirector.FocusedBuilding;

        Vector3 focus = worldSpawnPoint != null ? worldSpawnPoint.position : Vector3.zero;
        if (cameraDirector != null && Camera.main != null)
        {
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.55f, 0f));
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 400f))
                focus = hit.point;
        }

        RestaurantBuilding best = buildings[0];
        float bestDist = Vector3.Distance(focus, best.transform.position);
        for (int i = 1; i < buildings.Length; i++)
        {
            float d = Vector3.Distance(focus, buildings[i].transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = buildings[i];
            }
        }
        return best;
    }

    private void OnNearRestaurant(RestaurantBuilding building)
    {
        if (IsInsideRestaurant) return;
        if (cameraDirector != null && cameraDirector.Mode != CameraGameMode.PlayerFollow) return;

        if (promptText != null)
        {
            promptText.gameObject.SetActive(building != null);
            if (building != null)
                promptText.text = $"{building.displayName}\nProche — tape ENTRER";
        }
        if (enterRestaurantButton != null)
        {
            enterRestaurantButton.gameObject.SetActive(building != null);
            var label = enterRestaurantButton.GetComponentInChildren<Text>();
            if (label != null && building != null)
                label.text = "ENTRER — " + building.displayName;
        }
    }

    public void GoToMapMode()
    {
        if (IsInsideRestaurant)
            ExitRestaurant();

        if (counterService3D != null)
            counterService3D.EndSession();

        if (cameraDirector != null)
            cameraDirector.SetMapMode(worldSpawnPoint != null ? worldSpawnPoint.position : Vector3.zero);

        ShowMapHud();
        EmpireManager.Instance?.Notify("Vue carte — tape un kebab pour zoomer.");
    }

    public void GoToWalkMode()
    {
        if (counterService3D != null)
            counterService3D.EndSession();

        if (cameraDirector != null)
            cameraDirector.SetPlayerMode();

        // Place le joueur devant le kebab le plus proche / le premier
        if (player != null && !IsInsideRestaurant)
        {
            RestaurantBuilding target = null;
            if (cityGenerator != null && cityGenerator.spawnedKebabs.Count > 0)
                target = cityGenerator.spawnedKebabs[0];
            if (target != null)
            {
                Vector3 door = target.EntrancePoint.position;
                Vector3 look = target.transform.position - door;
                look.y = 0f;
                if (look.sqrMagnitude < 0.01f) look = Vector3.forward;
                player.Teleport(door - look.normalized * 2.5f + Vector3.up * 0.1f,
                    Quaternion.LookRotation(look).eulerAngles.y);
                player.cameraDistance = 6f;
                player.SnapCameraBehind();
            }
            else if (worldSpawnPoint != null &&
                     Vector3.Distance(player.transform.position, worldSpawnPoint.position) > 80f)
            {
                player.Teleport(worldSpawnPoint.position, worldSpawnPoint.eulerAngles.y);
            }
        }

        ShowStreetHud();
        EmpireManager.Instance?.Notify("Mode rue — approche le kebab et tape ENTRER.");
    }

    public void EnterRestaurant(RestaurantBuilding building)
    {
        if (building == null) return;

        // Si intérieur manquant (prefab cassé), en créer un sur place
        if (building.interior == null)
        {
            Debug.LogWarning("[Kebab] Intérieur manquant — création fallback.");
            EmpireManager.Instance?.Notify("Intérieur généré (fallback).");
            var go = new GameObject("InteriorFallback");
            go.transform.SetParent(building.transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, 8f);
            var interior = go.AddComponent<RestaurantInterior>();
            interior.interiorRoot = go;
            var spawn = new GameObject("PlayerSpawn").transform;
            spawn.SetParent(go.transform, false);
            spawn.localPosition = new Vector3(2f, 1f, -2f);
            interior.playerSpawn = spawn;
            var counter = new GameObject("CounterPoint").transform;
            counter.SetParent(go.transform, false);
            counter.localPosition = new Vector3(0f, 1f, 1f);
            interior.counterPoint = counter;
            building.interior = interior;
            building.interiorSpawnPoint = spawn;
        }

        // Activer la hiérarchie (prefab parfois désactivé)
        building.gameObject.SetActive(true);
        if (building.interior != null)
        {
            // Échelle humaine — jamais écrasée par le scale carte
            var interiorT = building.interior.transform;
            interiorT.localScale = Vector3.one;
            interiorT.localPosition = new Vector3(0f, 0f, 0f);
            if (building.interior.interiorRoot != null)
            {
                building.interior.interiorRoot.SetActive(true);
                building.interior.interiorRoot.transform.localScale = Vector3.one;
            }
            building.interior.SetActiveInterior(true);

            int rendered = 0;
            foreach (var r in building.interior.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null) continue;
                r.enabled = true;
                r.gameObject.SetActive(true);
                rendered++;
            }
            Debug.Log($"[Kebab] Intérieur renderers actifs : {rendered}");
            InteriorEnvironmentSetup.Apply(building.interior);
        }
        SetExteriorVisible(building, false);
        SetMapColliderEnabled(building, false);

        // Cache aussi la ville autour pour voir clairement le magasin
        SetCityVisible(false);

        CurrentBuilding = building;
        IsInsideRestaurant = true;

        if (cameraDirector != null)
            cameraDirector.SetPlayerMode();

        if (player != null)
        {
            lastStreetPosition = player.transform.position;
            lastStreetYaw = player.transform.eulerAngles.y;
            Transform spawn = building.interiorSpawnPoint != null
                ? building.interiorSpawnPoint
                : building.interior.playerSpawn;

            Vector3 spawnPos;
            float spawnYaw = 0f; // face comptoir (+Z)
            if (spawn != null && !float.IsNaN(spawn.position.x))
            {
                spawnPos = spawn.position;
                spawnPos.y = building.interior.transform.position.y + 0.1f;
            }
            else
            {
                spawnPos = building.interior.transform.TransformPoint(new Vector3(0f, 0.1f, -2.5f));
            }

            // Oriente vers le comptoir
            if (building.interior.counterPoint != null)
            {
                Vector3 look = building.interior.counterPoint.position - spawnPos;
                look.y = 0f;
                if (look.sqrMagnitude > 0.01f)
                    spawnYaw = Quaternion.LookRotation(look).eulerAngles.y;
            }

            player.Teleport(spawnPos, spawnYaw);
            player.cameraDistance = 4.2f;
            player.SnapCameraBehind();
        }

        try
        {
            var data = EmpireManager.Instance != null
                ? EmpireManager.Instance.GetRestaurant(building.restaurantIndex)
                : null;
            if (data != null && building.interior != null)
            {
                if (building.interior.npcSpawner != null)
                    building.interior.npcSpawner.visualCatalog = visualCatalog;
                building.interior.StartSimulation(data);
                var hygiene = building.interior.GetComponentInChildren<HygieneVisualController>();
                if (hygiene != null)
                    hygiene.UpdateVisuals(data.currentDirt);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[Kebab] StartSimulation: " + ex.Message);
        }

        ShowInteriorHud();
        EmpireManager.Instance?.Notify($"Dans {building.displayName}");
        Debug.Log($"[Kebab] EnterRestaurant OK → {building.displayName}");
    }

    /// <summary>HUD mode caisse 3D (panneau commande ouvert).</summary>
    public void ShowCounterHud()
    {
        SetActive(exitRestaurantButton, false);
        SetActive(enterRestaurantButton, false);
        SetActive(counterServiceButton, false);
        SetActive(mobileControlsRoot, false);
        if (promptText != null)
        {
            promptText.gameObject.SetActive(true);
            promptText.text = "CAISSE — Client suivant · Salade/Tomate/Oignon/Sauce · Encaisser";
        }
        if (player != null) player.SetInputEnabled(false);
    }

    private static void SetExteriorVisible(RestaurantBuilding building, bool visible)
    {
        if (building == null) return;
        Transform interiorT = building.interior != null ? building.interior.transform : null;
        for (int i = 0; i < building.transform.childCount; i++)
        {
            var child = building.transform.GetChild(i);
            if (interiorT != null && (child == interiorT || child.IsChildOf(interiorT)))
                continue;
            if (child.GetComponent<RestaurantInterior>() != null)
                continue;
            if (child.name == "Entrance")
                continue;
            if (child.name == "MapBeacon")
            {
                child.gameObject.SetActive(visible);
                continue;
            }
            child.gameObject.SetActive(visible);
        }
    }

    private static void SetMapColliderEnabled(RestaurantBuilding building, bool enabled)
    {
        if (building == null) return;
        var box = building.GetComponent<BoxCollider>();
        if (box != null) box.enabled = enabled;
    }

    private void SetCityVisible(bool visible)
    {
        if (cityGenerator == null) return;
        var city = cityGenerator.transform.Find("City");
        if (city != null) city.gameObject.SetActive(visible);
    }

    public void ExitRestaurant()
    {
        if (!IsInsideRestaurant) return;

        if (counterService3D != null)
            counterService3D.EndSession();

        if (CurrentBuilding != null && CurrentBuilding.interior != null)
            CurrentBuilding.interior.StopSimulation();

        SetExteriorVisible(CurrentBuilding, true);
        SetMapColliderEnabled(CurrentBuilding, true);
        SetCityVisible(true);
        InteriorEnvironmentSetup.Restore();

        if (counterService3D != null)
            counterService3D.EndSession();

        if (player != null)
        {
            Vector3 back = CurrentBuilding != null && CurrentBuilding.entrancePoint != null
                ? CurrentBuilding.entrancePoint.position
                : lastStreetPosition;
            player.cameraDistance = 6f;
            player.Teleport(back, lastStreetYaw);
        }

        IsInsideRestaurant = false;
        CurrentBuilding = null;

        if (cameraDirector != null)
            cameraDirector.SetPlayerMode();

        ShowStreetHud();
        EmpireManager.Instance?.Notify("Retour dans la rue.");
    }

    private void StartCounterService()
    {
        if (!IsInsideRestaurant || CurrentBuilding == null)
        {
            EmpireManager.Instance?.Notify("Entre d'abord dans un kebab.");
            return;
        }

        var data = EmpireManager.Instance?.GetRestaurant(CurrentBuilding.restaurantIndex);
        if (data == null) return;

        if (data.managementMode != ManagementMode.Manual)
        {
            EmpireManager.Instance?.Notify("Mode manuel requis — active-le dans Gérer.");
            return;
        }

        data.ownerIsWorking = true;

        if (counterService3D == null)
            counterService3D = FindObjectOfType<CounterService3D>();

        if (counterService3D != null)
        {
            counterService3D.visualCatalog = visualCatalog;
            EnlargeCounterButtons(counterService3D.serviceUI);
            counterService3D.BeginSession(CurrentBuilding.interior, data);
        }
        else
        {
            EmpireManager.Instance?.Notify("CounterService3D manquant — relance Setup 3D.");
        }
    }

    private static void EnlargeCounterButtons(CustomerServiceUI ui)
    {
        if (ui == null) return;
        ResizeBtn(ui.checkoutButton, new Vector2(280, 80));
        ResizeBtn(ui.startCustomerButton, new Vector2(260, 72));
        ResizeBtn(ui.askSaladButton, new Vector2(160, 72));
        ResizeBtn(ui.askTomatoButton, new Vector2(160, 72));
        ResizeBtn(ui.askOnionButton, new Vector2(160, 72));
        ResizeBtn(ui.askSauceButton, new Vector2(160, 72));
    }

    private static void ResizeBtn(Button btn, Vector2 size)
    {
        if (btn == null) return;
        var rt = btn.GetComponent<RectTransform>();
        if (rt != null) rt.sizeDelta = size;
        var label = btn.GetComponentInChildren<Text>();
        if (label != null && btn.name.Contains("Pay"))
        {
            label.text = "ENCAISSER";
            label.fontSize = 28;
        }
    }

    private void ShowMapHud()
    {
        SetActive(exitRestaurantButton, false);
        SetActive(enterRestaurantButton, true);
        SetActive(counterServiceButton, false);
        SetActive(openManageButton, true);
        SetActive(mobileControlsRoot, false);
        if (manageOverlay != null) manageOverlay.SetActive(false);
        if (promptText != null)
        {
            promptText.gameObject.SetActive(true);
            int n = cityGenerator != null ? cityGenerator.spawnedKebabs.Count : 0;
            promptText.text = n > 0
                ? $"CARTE — {n} kebab · Gérer / ▶ Jour · Tape = zoom"
                : "CARTE — Tape un kebab (zoom) · Retape = entrer";
        }
        if (player != null) player.SetInputEnabled(false);
    }

    private void ShowStreetHud()
    {
        SetActive(exitRestaurantButton, false);
        SetActive(counterServiceButton, false);
        SetActive(openManageButton, true);
        SetActive(mobileControlsRoot, true);
        SetActive(enterRestaurantButton, player != null && player.NearestRestaurant != null);
        if (enterRestaurantButton != null)
            enterRestaurantButton.transform.SetAsLastSibling();
        ShrinkLookPadIfNeeded();
        FixJoystickLayout();
        KebabWorldBootstrap.RaiseHudButtonsAboveControls(this);
        if (promptText != null && (player == null || player.NearestRestaurant == null))
            promptText.gameObject.SetActive(false);
        if (manageOverlay != null) manageOverlay.SetActive(false);
        if (player != null) player.SetInputEnabled(true);
    }

    /// <summary>Look pad trop haut = bloque le bouton ENTRER.</summary>
    private void ShrinkLookPadIfNeeded()
    {
        if (mobileControlsRoot == null) return;
        var pad = mobileControlsRoot.GetComponentInChildren<MobileLookPad>(true);
        if (pad == null) return;
        var rt = pad.GetComponent<RectTransform>();
        if (rt == null) return;
        rt.anchorMin = new Vector2(0.75f, 0.18f);
        rt.anchorMax = new Vector2(0.98f, 0.34f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    /// <summary>Joystick compact, hors de la vue principale.</summary>
    private void FixJoystickLayout()
    {
        if (mobileControlsRoot == null) return;
        var joy = mobileControlsRoot.GetComponentInChildren<MobileJoystick>(true);
        if (joy == null) return;
        var rt = joy.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0.02f, 0.18f);
            rt.anchorMax = new Vector2(0.22f, 0.32f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        if (joy.background != null)
        {
            joy.background.sizeDelta = new Vector2(96f, 96f);
            joy.handleRange = 34f;
            var img = joy.background.GetComponent<Image>();
            if (img != null) img.color = new Color(0f, 0f, 0f, 0.18f);
        }
        if (joy.handle != null)
        {
            joy.handle.sizeDelta = new Vector2(40f, 40f);
            var himg = joy.handle.GetComponent<Image>();
            if (himg != null) himg.color = new Color(1f, 1f, 1f, 0.35f);
        }
        var rootImg = joy.GetComponent<Image>();
        if (rootImg != null)
        {
            rootImg.color = new Color(1f, 1f, 1f, 0.01f);
            rootImg.raycastTarget = true;
        }
    }

    public void ShowInteriorHud()
    {
        SetActive(exitRestaurantButton, true);
        SetActive(enterRestaurantButton, false);
        RefreshCounterVisibility();
        SetActive(openManageButton, true);
        SetActive(nextDayButton, true);
        SetActive(mapModeButton, true);
        SetActive(walkModeButton, true);
        SetActive(mobileControlsRoot, true);
        FixJoystickLayout();
        ShrinkLookPadIfNeeded();
        CompactHudButtons();
        KebabWorldBootstrap.RaiseHudButtonsAboveControls(this);
        if (promptText != null)
        {
            promptText.gameObject.SetActive(true);
            var data = CurrentBuilding != null && EmpireManager.Instance != null
                ? EmpireManager.Instance.GetRestaurant(CurrentBuilding.restaurantIndex) : null;
            string place = data != null ? data.locationName : (CurrentBuilding != null ? CurrentBuilding.displayName : "Magasin");
            promptText.text = data != null && data.managementMode == ManagementMode.Manual
                ? $"{place} — Caisse pour servir · ▶ Jour ou Gérer"
                : $"{place} — Regarde ton kebab · Gérer / ▶ Jour pour gagner";
        }
        if (player != null) player.SetInputEnabled(true);
    }

    private void ToggleManageOverlay()
    {
        EnsureManageOverlay();

        if (manageOverlay == null)
        {
            EmpireManager.Instance?.Notify("Gestion indisponible.");
            return;
        }

        bool show = !manageOverlay.activeSelf;
        manageOverlay.SetActive(show);
        if (show)
            manageOverlay.GetComponent<ManageOverlayUI>()?.Refresh();
        if (player != null) player.SetInputEnabled(!show);
    }

    private void RefreshHud()
    {
        var e = EmpireManager.Instance;
        if (e == null) return;
        if (moneyHudText != null) moneyHudText.text = $"{e.Money:F0} €";
        if (dayHudText != null)
        {
            string extra = "";
            if (IsInsideRestaurant && CurrentBuilding != null)
            {
                var data = e.GetRestaurant(CurrentBuilding.restaurantIndex);
                if (data != null)
                    extra = $" · Viande {data.meatStockKg:F0}kg · Rép {data.reputation:F0}";
            }
            dayHudText.text = $"Jour {e.CurrentDay}{extra}";
        }

        if (cityGenerator != null && !IsInsideRestaurant)
        {
            if (cityGenerator.spawnedKebabs.Count != e.RestaurantCount)
            {
                cityGenerator.RefreshRestaurants();
                ApplyCatalogToInteriors();
            }
        }

        if (IsInsideRestaurant && CurrentBuilding != null)
        {
            CurrentBuilding.UpdateSign();
            var data = e.GetRestaurant(CurrentBuilding.restaurantIndex);
            if (data != null)
                CurrentBuilding.interior?.GetComponentInChildren<HygieneVisualController>()
                    ?.UpdateVisuals(data.currentDirt);
        }

        if (manageOverlay != null && manageOverlay.activeSelf)
            manageOverlay.GetComponent<ManageOverlayUI>()?.Refresh();
    }

    private static void SetActive(Button btn, bool active)
    {
        if (btn != null) btn.gameObject.SetActive(active);
    }

    private static void SetActive(GameObject go, bool active)
    {
        if (go != null) go.SetActive(active);
    }
}
