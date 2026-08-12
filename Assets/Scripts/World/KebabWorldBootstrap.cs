using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Si la scène active est encore l'ancienne UI 2D (test1), construit le monde 3D au Play.
/// </summary>
public static class KebabWorldBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoFixOnPlay()
    {
        if (!Application.isPlaying) return;
        if (Object.FindObjectOfType<GameWorldManager>() != null) return;

        Debug.LogWarning("[Kebab] Scène UI 2D détectée — bascule automatique vers le monde 3D.");
        BuildPlayableWorld(runtime: true);
    }

    /// <summary>
    /// Construit (ou complète) le monde 3D jouable.
    /// </summary>
    public static GameWorldManager BuildPlayableWorld(bool runtime)
    {
        DisableLegacyUi();

        var catalog = Resources.Load<VisualCatalog>("VisualCatalog");
        var managers = EnsureRoot("=== MANAGERS ===");

        var empire = EnsureComponent<EmpireManager>(managers, "EmpireManager");
        var competitors = EnsureComponent<CompetitorManager>(managers, "CompetitorManager");
        empire.competitorManager = competitors;
        EnsureComponent<CounterServiceController>(managers, "CounterService");
        EnsureComponent<GameManager>(managers, "GameManager");

        var counter3d = EnsureComponent<CounterService3D>(managers, "CounterService3D");
        counter3d.visualCatalog = catalog;
        counter3d.serviceLogic = Object.FindObjectOfType<CounterServiceController>();

        EnsureCameraAndLight();

        var world = EnsureRoot("=== WORLD ===");
        var city = EnsureComponent<CityWorldGenerator>(world, "CityWorld");
        city.visualCatalog = catalog;

        Transform spawn = world.transform.Find("WorldSpawn");
        if (spawn == null)
        {
            var spawnGo = new GameObject("WorldSpawn");
            spawnGo.transform.SetParent(world.transform);
            spawn = spawnGo.transform;
            spawn.position = new Vector3(0f, 0.1f, -8f);
        }

        var player = EnsurePlayer(spawn.position);
        var camDir = EnsureComponent<GameCameraDirector>(managers, "CameraDirector");
        camDir.mainCamera = Camera.main;
        camDir.player = player;
        camDir.mapCenter = spawn;

        var mapInput = EnsureComponent<MapInputController>(managers, "MapInput");
        mapInput.cameraDirector = camDir;

        var gwm = EnsureComponent<GameWorldManager>(managers, "GameWorldManager");
        gwm.cityGenerator = city;
        gwm.player = player;
        gwm.worldSpawnPoint = spawn;
        gwm.cameraDirector = camDir;
        gwm.visualCatalog = catalog;
        gwm.counterService3D = counter3d;

        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        if (gwm.gameplayHud == null)
            BuildGameplayHud(gwm, counter3d);

        EnsureComponent<GameFlowController>(managers, "GameFlowController");
        // BuildGameFlowUI est appelé depuis GameWorldManager.EnsureGameFlowUI() quand le HUD existe.

        // Caméra claire ciel (pas fond noir)
        if (Camera.main != null)
        {
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = new Color(0.45f, 0.65f, 0.9f);
            Camera.main.orthographic = false;
            Camera.main.nearClipPlane = 0.1f;
            Camera.main.farClipPlane = 250f;
            Camera.main.transform.position = new Vector3(0f, 45f, -8f);
            Camera.main.transform.rotation = Quaternion.Euler(70f, 0f, 0f);
        }

        Debug.Log("[Kebab] Monde 3D prêt. Catalogue = " + (catalog != null ? "OK" : "MANQUANT"));
        return gwm;
    }

    private static void DisableLegacyUi()
    {
        DestroyOrDisable("Canvas");
        DestroyOrDisable("=== RESTAURANTS ===");

        var ui = Object.FindObjectOfType<UIManager>();
        if (ui != null)
        {
            // Désactive tout le canvas parent éventuel
            var canvas = ui.GetComponentInParent<Canvas>();
            if (canvas != null) canvas.gameObject.SetActive(false);
            else ui.gameObject.SetActive(false);
        }

        // Ancien panel caisse éventuel hors GameplayHUD
        var panels = Object.FindObjectsOfType<CustomerServiceUI>(true);
        foreach (var p in panels)
        {
            if (p != null && (p.transform.root.name == "Canvas" || p.name == "CounterServicePanel"))
            {
                if (p.transform.root.name == "Canvas")
                    p.transform.root.gameObject.SetActive(false);
            }
        }
    }

    private static void DestroyOrDisable(string name)
    {
        var go = GameObject.Find(name);
        if (go == null) return;
        go.SetActive(false);
        if (Application.isPlaying) Object.Destroy(go);
        else Object.DestroyImmediate(go);
    }

    private static GameObject EnsureRoot(string name)
    {
        var go = GameObject.Find(name);
        if (go == null) go = new GameObject(name);
        return go;
    }

    private static T EnsureComponent<T>(GameObject parent, string childName) where T : Component
    {
        var t = parent.transform.Find(childName);
        GameObject go;
        if (t != null) go = t.gameObject;
        else
        {
            go = new GameObject(childName);
            go.transform.SetParent(parent.transform, false);
        }

        var c = go.GetComponent<T>();
        if (c == null) c = go.AddComponent<T>();
        return c;
    }

    private static void EnsureCameraAndLight()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
        }

        if (Object.FindObjectOfType<Light>() == null)
        {
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            light.intensity = 1.15f;
            light.shadows = LightShadows.Soft;
        }
    }

    private static PlayerController EnsurePlayer(Vector3 spawnPos)
    {
        var existing = Object.FindObjectOfType<PlayerController>();
        if (existing != null)
        {
            EnsurePlayerVisual(existing);
            existing.interactRange = 8f;
            return existing;
        }

        var playerRoot = EnsureRoot("=== PLAYER ===");
        var playerGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        playerGo.name = "Player";
        playerGo.transform.SetParent(playerRoot.transform);
        playerGo.transform.position = spawnPos;

        var col = playerGo.GetComponent<CapsuleCollider>();
        if (col != null)
        {
            if (Application.isPlaying) Object.Destroy(col);
            else Object.DestroyImmediate(col);
        }

        var cc = playerGo.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.35f;
        cc.center = new Vector3(0f, 0.9f, 0f);

        var rend = playerGo.GetComponent<Renderer>();
        if (rend != null) rend.enabled = false; // remplacé par le mesh personnage

        var pivot = new GameObject("CameraPivot").transform;
        pivot.SetParent(playerGo.transform);
        pivot.localPosition = new Vector3(0f, 1.6f, 0f);

        var player = playerGo.AddComponent<PlayerController>();
        player.cameraPivot = pivot;
        player.interactRange = 8f;
        EnsurePlayerVisual(player);
        return player;
    }

    private static void EnsurePlayerVisual(PlayerController player)
    {
        if (player == null) return;
        if (player.transform.Find("PlayerVisual") != null) return;

        var catalog = Resources.Load<VisualCatalog>("VisualCatalog");
        GameObject visual;
        if (catalog != null && catalog.playerPrefab != null)
        {
            visual = Object.Instantiate(catalog.playerPrefab);
            visual.name = "PlayerVisual";
            float scale = catalog.characterScale > 0.01f ? catalog.characterScale : 1f;
            visual.transform.localScale = Vector3.one * scale;
            InteriorEnvironmentSetup.FixMaterials(visual.transform);
            CharacterAnimatorSetup.Setup(visual, catalog.playerPrefab.name);
        }
        else
            visual = CharacterVisualFactory.CreateImprovedHumanoid("PlayerVisual", 1.8f, false);

        visual.name = "PlayerVisual";
        visual.transform.SetParent(player.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;

        var rend = player.GetComponent<Renderer>();
        if (rend != null) rend.enabled = false;
    }

    /// <summary>Appelé au boot si la scène a déjà un Player sans mesh.</summary>
    public static void EnsurePlayerVisualPublic(PlayerController player) => EnsurePlayerVisual(player);

    // ---- HUD (identique au setup éditeur) ----

    public static void BuildGameplayHud(GameWorldManager gwm, CounterService3D counter3d)
    {
        if (gwm.gameplayHud != null) return;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                     ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

        var canvasGo = new GameObject("GameplayHUD");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        canvasGo.AddComponent<GraphicRaycaster>();
        gwm.gameplayHud = canvasGo;

        var top = Panel(canvasGo.transform, "TopBar", new Color(0, 0, 0, 0.55f),
            new Vector2(0, 0.9f), Vector2.one);
        gwm.moneyHudText = Label(top.transform, "Money", "12000 €", 40, TextAnchor.MiddleLeft,
            new Vector2(24, 0), new Vector2(400, 60), new Color(1f, 0.8f, 0.2f), font);
        gwm.dayHudText = Label(top.transform, "Day", "Jour 1", 32, TextAnchor.MiddleRight,
            new Vector2(-24, 0), new Vector2(280, 50), Color.white, font);

        gwm.promptText = Label(canvasGo.transform, "Prompt", "", 26, TextAnchor.LowerCenter,
            new Vector2(0, 260), new Vector2(950, 90), Color.white, font);
        gwm.promptText.alignment = TextAnchor.MiddleCenter;

        gwm.mapModeButton = Btn(canvasGo.transform, "BtnMap", "Carte", new Vector2(-280, 28), new Vector2(120, 48),
            new Color(0.3f, 0.35f, 0.5f), font);
        gwm.walkModeButton = Btn(canvasGo.transform, "BtnWalk", "Marcher", new Vector2(-140, 28), new Vector2(120, 48),
            new Color(0.25f, 0.5f, 0.35f), font);
        gwm.nextDayButton = Btn(canvasGo.transform, "BtnDay", "▶ Jour", new Vector2(0, 28), new Vector2(120, 48),
            new Color(0.25f, 0.4f, 0.7f), font);
        gwm.counterServiceButton = Btn(canvasGo.transform, "BtnCash", "Caisse", new Vector2(140, 28), new Vector2(120, 48),
            new Color(0.75f, 0.45f, 0.15f), font);
        gwm.openManageButton = Btn(canvasGo.transform, "BtnManage", "Gérer", new Vector2(280, 28), new Vector2(120, 48),
            new Color(0.35f, 0.55f, 0.3f), font);

        gwm.enterRestaurantButton = Btn(canvasGo.transform, "BtnEnter", "ENTRER",
            new Vector2(0, 95), new Vector2(420, 64), new Color(0.85f, 0.45f, 0.1f), font);
        gwm.enterRestaurantButton.gameObject.SetActive(false);

        gwm.exitRestaurantButton = Btn(canvasGo.transform, "BtnExit", "SORTIR",
            new Vector2(0, 95), new Vector2(280, 56), new Color(0.35f, 0.35f, 0.4f), font);
        gwm.exitRestaurantButton.gameObject.SetActive(false);

        gwm.manageOverlay = BuildManageOverlay(canvasGo.transform, font);

        BuildGameFlowUI(canvasGo.transform, font);

        var mobileRoot = new GameObject("MobileControls", typeof(RectTransform));
        mobileRoot.transform.SetParent(canvasGo.transform, false);
        Stretch(mobileRoot.GetComponent<RectTransform>());
        gwm.mobileControlsRoot = mobileRoot;

        var moveJoy = BuildJoystick(mobileRoot.transform, "MoveJoystick",
            new Vector2(0.03f, 0.12f), new Vector2(0.28f, 0.28f));
        // Look pad compact, coin bas-droit
        var lookPad = BuildLookPad(mobileRoot.transform, "LookPad",
            new Vector2(0.70f, 0.12f), new Vector2(0.98f, 0.30f));

        var bridge = mobileRoot.AddComponent<MobileInputBridge>();
        bridge.moveJoystick = moveJoy;
        bridge.lookPad = lookPad;
        bridge.player = gwm.player;

        // Barre d'actions AU-DESSUS des joysticks (sinon le carré blanc mange Carte)
        RaiseHudButtonsAboveControls(gwm);

        var serviceUI = BuildCounterPanel(canvasGo.transform, font);
        counter3d.serviceUI = serviceUI;
        serviceUI.serviceController = Object.FindObjectOfType<CounterServiceController>();
    }

    private static MobileJoystick BuildJoystick(Transform parent, string name, Vector2 aMin, Vector2 aMax)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(Image));
        root.transform.SetParent(parent, false);
        var rt = root.GetComponent<RectTransform>();
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        root.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);

        var bg = new GameObject("Bg", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(root.transform, false);
        var bgrt = bg.GetComponent<RectTransform>();
        bgrt.anchorMin = bgrt.anchorMax = new Vector2(0.5f, 0.5f);
        bgrt.sizeDelta = new Vector2(120, 120);
        bg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.22f);

        var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(bg.transform, false);
        var hrt = handle.GetComponent<RectTransform>();
        hrt.sizeDelta = new Vector2(52, 52);
        handle.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.4f);

        var joy = root.AddComponent<MobileJoystick>();
        joy.background = bgrt;
        joy.handle = hrt;
        joy.handleRange = 42f;
        return joy;
    }

    public static void RaiseHudButtonsAboveControls(GameWorldManager gwm)
    {
        if (gwm == null) return;
        void Top(Component c)
        {
            if (c != null) c.transform.SetAsLastSibling();
        }
        Top(gwm.mapModeButton);
        Top(gwm.walkModeButton);
        Top(gwm.nextDayButton);
        Top(gwm.counterServiceButton);
        Top(gwm.openManageButton);
        Top(gwm.enterRestaurantButton);
        Top(gwm.exitRestaurantButton);
        if (gwm.manageOverlay != null)
            gwm.manageOverlay.transform.SetAsLastSibling();
    }

    private static MobileLookPad BuildLookPad(Transform parent, string name, Vector2 aMin, Vector2 aMax)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(Image));
        root.transform.SetParent(parent, false);
        var rt = root.GetComponent<RectTransform>();
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        // Joystick / look pad : ne pas bloquer toute la largeur avec un Image opaque
        root.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
        root.GetComponent<Image>().raycastTarget = true;
        var pad = root.AddComponent<MobileLookPad>();
        pad.sensitivity = 0.2f;
        return pad;
    }

    private static CustomerServiceUI BuildCounterPanel(Transform canvas, Font font)
    {
        var panel = Panel(canvas, "CounterServicePanel", new Color(0.05f, 0.05f, 0.05f, 0.82f),
            Vector2.zero, Vector2.one);
        panel.SetActive(false);

        var ui = panel.AddComponent<CustomerServiceUI>();
        ui.servicePanel = panel;

        var box = Panel(panel.transform, "OrderBox", new Color(0.1f, 0.08f, 0.06f, 0.92f),
            new Vector2(0.05f, 0f), new Vector2(0.95f, 0.48f));

        ui.dialogueText = Label(box.transform, "Dialogue", "…", 24, TextAnchor.UpperLeft,
            new Vector2(20, -10), new Vector2(900, 100), Color.white, font);
        ui.ticketText = Label(box.transform, "Ticket", "TICKET", 22, TextAnchor.UpperLeft,
            new Vector2(20, -110), new Vector2(900, 100), new Color(1f, 0.9f, 0.5f), font);
        ui.hintText = Label(box.transform, "Hint", "", 22, TextAnchor.UpperLeft,
            new Vector2(20, -210), new Vector2(900, 40), new Color(0.7f, 0.85f, 1f), font);

        ui.startCustomerButton = Btn(box.transform, "Next", "Client suivant", new Vector2(-300, 36), new Vector2(260, 72),
            new Color(0.3f, 0.55f, 0.3f), font);
        ui.askSaladButton = Btn(box.transform, "Salad", "Salade ?", new Vector2(-90, 36), new Vector2(160, 72),
            new Color(0.25f, 0.5f, 0.3f), font);
        ui.askTomatoButton = Btn(box.transform, "Tomato", "Tomate ?", new Vector2(80, 36), new Vector2(160, 72),
            new Color(0.7f, 0.3f, 0.25f), font);
        ui.askOnionButton = Btn(box.transform, "Onion", "Oignon ?", new Vector2(250, 36), new Vector2(160, 72),
            new Color(0.55f, 0.45f, 0.2f), font);
        ui.askSauceButton = Btn(box.transform, "SauceQ", "Sauce ?", new Vector2(400, 120), new Vector2(160, 72),
            new Color(0.6f, 0.35f, 0.15f), font);

        var sauces = new GameObject("Sauces", typeof(RectTransform));
        sauces.transform.SetParent(box.transform, false);
        var srt = sauces.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0.05f, 0.35f);
        srt.anchorMax = new Vector2(0.95f, 0.55f);
        ui.sauceButtonsRoot = sauces;

        ui.sauceBlancheButton = MiniBtn(sauces.transform, "Blanche", new Vector2(-350, 0), font);
        ui.sauceAlgerienneButton = MiniBtn(sauces.transform, "Algérienne", new Vector2(-210, 0), font);
        ui.sauceSamouraiButton = MiniBtn(sauces.transform, "Samouraï", new Vector2(-70, 0), font);
        ui.sauceHarissaButton = MiniBtn(sauces.transform, "Harissa", new Vector2(70, 0), font);
        ui.sauceKetchupMayoButton = MiniBtn(sauces.transform, "Ketch-Mayo", new Vector2(210, 0), font);
        ui.sauceSansButton = MiniBtn(sauces.transform, "Sans", new Vector2(350, 0), font);

        ui.checkoutButton = Btn(box.transform, "Pay", "ENCAISSER", new Vector2(-140, 120), new Vector2(280, 80),
            new Color(0.2f, 0.6f, 0.35f), font);
        ui.cancelButton = Btn(box.transform, "Cancel", "Annuler", new Vector2(120, 120), new Vector2(180, 80),
            new Color(0.5f, 0.2f, 0.2f), font);
        ui.closeServiceButton = Btn(box.transform, "Close", "Quitter caisse", new Vector2(320, 120), new Vector2(220, 80),
            new Color(0.3f, 0.3f, 0.35f), font);

        return ui;
    }

    private static Button MiniBtn(Transform parent, string label, Vector2 pos, Font font)
    {
        return Btn(parent, "S_" + label, label, pos, new Vector2(130, 50),
            new Color(0.45f, 0.3f, 0.15f), font);
    }

    private static GameObject BuildManageOverlay(Transform canvas, Font font)
    {
        // Voile léger — le magasin / la carte 3D reste visible
        var panel = Panel(canvas, "ManageOverlay", new Color(0f, 0f, 0f, 0.01f),
            Vector2.zero, Vector2.one);
        panel.SetActive(false);
        Panel(panel.transform, "OverlayDim", new Color(0f, 0f, 0f, 0.38f),
            Vector2.zero, Vector2.one);

        var ui = panel.AddComponent<ManageOverlayUI>();

        var box = Panel(panel.transform, "OverlayCard", new Color(0.1f, 0.11f, 0.14f, 0.92f),
            new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.54f));

        ui.infoText = Label(box.transform, "Info", "Gestion…", 18, TextAnchor.UpperLeft,
            new Vector2(16, -8), new Vector2(960, 64), Color.white, font);
        ui.infoText.alignment = TextAnchor.UpperLeft;
        ui.infoText.horizontalOverflow = HorizontalWrapMode.Wrap;
        ui.infoText.verticalOverflow = VerticalWrapMode.Truncate;

        // Viande
        ui.meatBoeufButton = Btn(box.transform, "MeatB", "Bœuf", new Vector2(-280, 320), new Vector2(170, 52),
            new Color(0.55f, 0.2f, 0.15f), font);
        ui.meatPouletButton = Btn(box.transform, "MeatP", "Poulet", new Vector2(-90, 320), new Vector2(170, 52),
            new Color(0.7f, 0.55f, 0.15f), font);
        ui.meatMysteryButton = Btn(box.transform, "MeatM", "Préfère pas savoir", new Vector2(130, 320), new Vector2(220, 52),
            new Color(0.35f, 0.45f, 0.2f), font);

        // Accompagnements
        ui.sideFreshButton = Btn(box.transform, "SideF", "Tout frais", new Vector2(-280, 255), new Vector2(170, 52),
            new Color(0.25f, 0.55f, 0.35f), font);
        ui.modeToggleButton = Btn(box.transform, "Mode", "Auto / Manuel", new Vector2(-90, 255), new Vector2(170, 52),
            new Color(0.45f, 0.35f, 0.55f), font);
        ui.sideLowButton = Btn(box.transform, "SideL", "Peu frais", new Vector2(130, 255), new Vector2(170, 52),
            new Color(0.45f, 0.35f, 0.25f), font);

        // Staff & ménage
        ui.hireDeclaredButton = Btn(box.transform, "HireD", "Embaucher déclaré", new Vector2(-220, 190), new Vector2(210, 52),
            new Color(0.3f, 0.45f, 0.65f), font);
        ui.hireBlackButton = Btn(box.transform, "HireB", "Embaucher black", new Vector2(30, 190), new Vector2(210, 52),
            new Color(0.45f, 0.3f, 0.45f), font);
        ui.fireButton = Btn(box.transform, "Fire", "Licencier", new Vector2(280, 190), new Vector2(150, 52),
            new Color(0.5f, 0.25f, 0.25f), font);

        ui.cleanButton = Btn(box.transform, "Clean", "Ménage", new Vector2(-280, 125), new Vector2(170, 52),
            new Color(0.3f, 0.55f, 0.45f), font);
        ui.buyMeatButton = Btn(box.transform, "Meat", "+10 kg viande", new Vector2(-90, 125), new Vector2(170, 52),
            new Color(0.65f, 0.35f, 0.2f), font);
        ui.payTaxesButton = Btn(box.transform, "Taxes", "Payer impôts", new Vector2(130, 125), new Vector2(200, 52),
            new Color(0.25f, 0.4f, 0.7f), font);

        ui.buyRestaurantButton = Btn(box.transform, "BuyResto", "Acheter un kebab", new Vector2(-80, 60), new Vector2(280, 52),
            new Color(0.75f, 0.5f, 0.15f), font);
        ui.closeButton = Btn(box.transform, "Close", "Fermer", new Vector2(220, 60), new Vector2(180, 52),
            new Color(0.35f, 0.35f, 0.4f), font);

        return panel;
    }

    /// <summary>Accessible depuis GameWorldManager si HUD scène sans overlay.</summary>
    public static GameObject BuildManageOverlayPublic(Transform canvas, Font font)
    {
        return BuildManageOverlay(canvas, font);
    }

    public static void BuildGameFlowUI(Transform canvas, Font font = null)
    {
        // Reconstruire si pas overlay jeu (anciennes builds plein écran noir)
        var existing = canvas.Find("FranceMapUI");
        if (existing != null)
        {
            bool hasZoom = existing.GetComponentInChildren<MapZoomPan>(true) != null;
            bool hasFrame = false;
            bool hasOverlay = existing.Find("OverlayDim") != null;
            foreach (var t in existing.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "MapFrame") { hasFrame = true; break; }
            }
            if (hasZoom && hasFrame && hasOverlay)
                return;
            Object.Destroy(existing.gameObject);
            var wizOld = canvas.Find("SetupWizard");
            if (wizOld != null) Object.Destroy(wizOld.gameObject);
            var manageOld = canvas.Find("ManageOverlay");
            if (manageOld != null) Object.Destroy(manageOld.gameObject);
        }

        if (font == null)
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                   ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

        var flow = Object.FindObjectOfType<GameFlowController>();
        if (flow == null)
        {
            var go = new GameObject("GameFlowController");
            flow = go.AddComponent<GameFlowController>();
        }

        // --- Carte France / Monde (overlay sur le jeu 3D) ---
        var mapRoot = Panel(canvas, "FranceMapUI", new Color(0f, 0f, 0f, 0.01f),
            Vector2.zero, Vector2.one);
        mapRoot.SetActive(false);
        Panel(mapRoot.transform, "OverlayDim", new Color(0f, 0f, 0f, 0.32f),
            Vector2.zero, Vector2.one);
        var mapCard = Panel(mapRoot.transform, "OverlayCard", new Color(0.1f, 0.12f, 0.16f, 0.88f),
            new Vector2(0.04f, 0.10f), new Vector2(0.96f, 0.88f));

        var mapUi = mapRoot.AddComponent<FranceMapUI>();
        mapUi.root = mapRoot;
        mapUi.titleText = Label(mapCard.transform, "Title", "France", 28,
            TextAnchor.UpperCenter, new Vector2(0, -16), new Vector2(900, 44), Color.white, font);
        mapUi.hintText = Label(mapCard.transform, "Hint", "Tape une ville", 17,
            TextAnchor.UpperCenter, new Vector2(0, -52), new Vector2(900, 32), new Color(0.8f, 0.85f, 1f), font);
        mapUi.franceTabButton = Btn(mapCard.transform, "TabFR", "France", new Vector2(-120, 56), new Vector2(180, 52),
            new Color(0.2f, 0.35f, 0.55f), font);
        mapUi.worldTabButton = Btn(mapCard.transform, "TabWorld", "Monde", new Vector2(120, 56), new Vector2(180, 52),
            new Color(0.35f, 0.3f, 0.5f), font);

        // Viewport avec masque
        var mapBg = Panel(mapCard.transform, "MapBg", new Color(0.05f, 0.08f, 0.12f, 0.95f),
            new Vector2(0.03f, 0.12f), new Vector2(0.97f, 0.84f));
        mapBg.AddComponent<RectMask2D>();

        // Contenu zoomable
        var contentGo = new GameObject("MapContent", typeof(RectTransform));
        contentGo.transform.SetParent(mapBg.transform, false);
        Stretch(contentGo.GetComponent<RectTransform>());

        // Cadre au ratio de la texture (évite d'étirer la carte → pins décalés)
        var frameGo = new GameObject("MapFrame", typeof(RectTransform));
        frameGo.transform.SetParent(contentGo.transform, false);
        var frameRt = frameGo.GetComponent<RectTransform>();
        frameRt.anchorMin = frameRt.anchorMax = new Vector2(0.5f, 0.5f);
        frameRt.pivot = new Vector2(0.5f, 0.5f);
        frameRt.sizeDelta = new Vector2(100f, 100f);
        var aspect = frameGo.AddComponent<AspectRatioFitter>();
        aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        aspect.aspectRatio = 1024f / 1536f; // map_france.png

        var mapImgGo = new GameObject("MapImage", typeof(RectTransform), typeof(RawImage));
        mapImgGo.transform.SetParent(frameGo.transform, false);
        Stretch(mapImgGo.GetComponent<RectTransform>());
        mapUi.mapImage = mapImgGo.GetComponent<RawImage>();
        mapUi.mapImage.texture = ProceduralMapGenerator.GetFranceMap();
        mapUi.mapImage.color = Color.white;
        mapUi.mapImage.raycastTarget = false;

        var citiesRootGo = new GameObject("CitiesRoot", typeof(RectTransform));
        citiesRootGo.transform.SetParent(frameGo.transform, false);
        Stretch(citiesRootGo.GetComponent<RectTransform>());
        mapUi.citiesRoot = citiesRootGo.transform;

        // Zone de drag / pinch sur le viewport
        var dragCatch = mapBg.GetComponent<Image>();
        if (dragCatch != null) dragCatch.raycastTarget = true;
        var zoom = mapBg.AddComponent<MapZoomPan>();
        zoom.content = contentGo.GetComponent<RectTransform>();
        mapUi.zoomPan = zoom;

        // Boutons zoom
        mapUi.zoomInButton = Btn(mapCard.transform, "ZoomIn", "+", new Vector2(420, 180), new Vector2(64, 64),
            new Color(0.2f, 0.45f, 0.35f), font);
        mapUi.zoomOutButton = Btn(mapCard.transform, "ZoomOut", "−", new Vector2(420, 110), new Vector2(64, 64),
            new Color(0.35f, 0.3f, 0.3f), font);
        var zinLabel = mapUi.zoomInButton.GetComponentInChildren<Text>();
        if (zinLabel != null) zinLabel.fontSize = 36;
        var zoutLabel = mapUi.zoomOutButton.GetComponentInChildren<Text>();
        if (zoutLabel != null) zoutLabel.fontSize = 36;

        var confirm = Panel(mapRoot.transform, "Confirm", new Color(0, 0, 0, 0.45f),
            Vector2.zero, Vector2.one);
        confirm.SetActive(false);
        mapUi.confirmPanel = confirm;
        var confirmBox = Panel(confirm.transform, "Box", new Color(0.12f, 0.11f, 0.1f, 0.96f),
            new Vector2(0.12f, 0.38f), new Vector2(0.88f, 0.58f));
        mapUi.confirmText = Label(confirmBox.transform, "Txt", "Confirmer ?", 22,
            TextAnchor.UpperCenter, new Vector2(0, -16), new Vector2(760, 80), Color.white, font);
        mapUi.confirmYesButton = Btn(confirmBox.transform, "Yes", "CONFIRMER", new Vector2(-100, 56), new Vector2(220, 56),
            new Color(0.2f, 0.6f, 0.35f), font);
        mapUi.confirmNoButton = Btn(confirmBox.transform, "No", "Annuler", new Vector2(120, 56), new Vector2(180, 56),
            new Color(0.5f, 0.25f, 0.25f), font);

        // --- Assistant configuration (bottom sheet) ---
        var wizRoot = Panel(canvas, "SetupWizard", new Color(0f, 0f, 0f, 0.01f),
            Vector2.zero, Vector2.one);
        wizRoot.SetActive(false);
        Panel(wizRoot.transform, "OverlayDim", new Color(0f, 0f, 0f, 0.35f),
            Vector2.zero, Vector2.one);
        var wizCard = Panel(wizRoot.transform, "OverlayCard", new Color(0.1f, 0.11f, 0.14f, 0.93f),
            new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.46f));

        var wiz = wizRoot.AddComponent<RestaurantSetupWizardUI>();
        wiz.root = wizRoot;
        wiz.stepTitleText = Label(wizCard.transform, "StepTitle", "Configuration", 24,
            TextAnchor.UpperCenter, new Vector2(0, -12), new Vector2(900, 40), new Color(1f, 0.85f, 0.3f), font);
        wiz.stepInfoText = Label(wizCard.transform, "StepInfo", "", 18,
            TextAnchor.UpperCenter, new Vector2(0, -48), new Vector2(900, 28), new Color(0.85f, 0.9f, 1f), font);
        wiz.stepInfoText.alignment = TextAnchor.MiddleCenter;

        var choicesGo = new GameObject("Choices", typeof(RectTransform));
        choicesGo.transform.SetParent(wizCard.transform, false);
        var crt = choicesGo.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.05f, 0.28f);
        crt.anchorMax = new Vector2(0.95f, 0.72f);
        crt.offsetMin = crt.offsetMax = Vector2.zero;

        wiz.backButton = Btn(wizCard.transform, "WBack", "Retour", new Vector2(-120, 56), new Vector2(160, 50),
            new Color(0.35f, 0.35f, 0.4f), font);
        wiz.nextButton = Btn(wizCard.transform, "WNext", "Suivant", new Vector2(120, 56), new Vector2(200, 50),
            new Color(0.25f, 0.55f, 0.35f), font);

        flow.mapUI = mapUi;
        flow.wizardUI = wiz;
        mapRoot.transform.SetAsLastSibling();
        wizRoot.transform.SetAsLastSibling();
    }

    public static void BuildGameFlowUI(Transform canvas)
    {
        BuildGameFlowUI(canvas, null);
    }

    private static GameObject Panel(Transform parent, string name, Color color, Vector2 aMin, Vector2 aMax)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = color;
        return go;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static Text Label(Transform parent, string name, string msg, int size, TextAnchor anchor,
        Vector2 pos, Vector2 sizeDelta, Color color, Font font)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        if (anchor == TextAnchor.MiddleLeft) { rt.anchorMin = rt.anchorMax = new Vector2(0, 0.5f); rt.pivot = new Vector2(0, 0.5f); }
        else if (anchor == TextAnchor.MiddleRight) { rt.anchorMin = rt.anchorMax = new Vector2(1, 0.5f); rt.pivot = new Vector2(1, 0.5f); }
        else if (anchor == TextAnchor.LowerCenter) { rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f); rt.pivot = new Vector2(0.5f, 0f); }
        else if (anchor == TextAnchor.UpperLeft) { rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1); }
        else { rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); }
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeDelta;
        var t = go.AddComponent<Text>();
        t.font = font; t.text = msg; t.fontSize = size; t.color = color;
        t.alignment = TextAnchor.MiddleLeft;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    private static Button Btn(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color color, Font font)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        go.GetComponent<Image>().color = color;
        var textGo = new GameObject("L", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        Stretch(textGo.GetComponent<RectTransform>());
        var t = textGo.AddComponent<Text>();
        t.font = font; t.text = label; t.fontSize = 20; t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        return go.GetComponent<Button>();
    }
}
