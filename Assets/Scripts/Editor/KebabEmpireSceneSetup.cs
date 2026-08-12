using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Menu : Kebab Empire > Setup Main Scene
/// Génère managers + Canvas mobile avec toutes les actions visibles.
/// </summary>
public static class KebabEmpireSceneSetup
{
#if UNITY_EDITOR
    [MenuItem("Kebab Empire/Setup Main Scene")]
    public static void SetupMainScene()
    {
        // Nettoie une ancienne setup si présente
        DestroyIfExists("=== MANAGERS ===");
        DestroyIfExists("=== RESTAURANTS ===");
        DestroyIfExists("Canvas");
        DestroyIfExists("EventSystem");

        var managersGo = new GameObject("=== MANAGERS ===");
        var empireGo = new GameObject("EmpireManager");
        empireGo.transform.SetParent(managersGo.transform);
        var empire = empireGo.AddComponent<EmpireManager>();

        var competitorGo = new GameObject("CompetitorManager");
        competitorGo.transform.SetParent(managersGo.transform);
        var competitors = competitorGo.AddComponent<CompetitorManager>();
        empire.competitorManager = competitors;

        var gameGo = new GameObject("GameManager");
        gameGo.transform.SetParent(managersGo.transform);
        var game = gameGo.AddComponent<GameManager>();
        game.empireManager = empire;

        var counterGo = new GameObject("CounterService");
        counterGo.transform.SetParent(managersGo.transform);
        counterGo.AddComponent<CounterServiceController>();

        var restoRoot = new GameObject("=== RESTAURANTS ===");
        var restoGo = new GameObject("Restaurant_01");
        restoGo.transform.SetParent(restoRoot.transform);
        var restoMgr = restoGo.AddComponent<RestaurantManager>();
        restoGo.AddComponent<EmployeeManager>();
        restoGo.AddComponent<StockManager>();
        var hygiene = restoGo.AddComponent<HygieneVisualController>();
        CreateHygienePlaceholders(restoGo.transform, hygiene);
        empire.restaurantManagers.Add(restoMgr);

        var canvasGo = CreateCanvas();
        var ui = BuildUI(canvasGo.transform);
        game.uiManager = ui;
        ui.restaurantUI.uiManager = ui;

        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        if (Camera.main == null)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.10f, 0.08f);
            camGo.AddComponent<AudioListener>();
        }

        // Nouvelle partie (supprime ancienne save avec vieux soldes)
        SaveSystem.DeleteSave();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = canvasGo;

        EditorUtility.DisplayDialog(
            "Kebab Empire",
            "Scène prête !\n\n" +
            "1. File > Save As > Assets/Scenes/MainScene.unity\n" +
            "2. Play\n" +
            "3. GÉRER MON KEBAB → « Je fais le service » → « Passer derrière la caisse »\n" +
            "4. Demande : Salade ? Tomate ? Oignon ? Quelle sauce ? puis encaisse",
            "OK");
    }

    private static void DestroyIfExists(string name)
    {
        var go = GameObject.Find(name);
        if (go != null) Object.DestroyImmediate(go);
    }

    private static void CreateHygienePlaceholders(Transform parent, HygieneVisualController hygiene)
    {
        var stains = new GameObject[2];
        for (int i = 0; i < 2; i++)
        {
            var s = GameObject.CreatePrimitive(PrimitiveType.Quad);
            s.name = $"DirtStain_{i}";
            s.transform.SetParent(parent);
            s.transform.localPosition = new Vector3(-1f + i * 2f, 0.01f, 0f);
            s.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            s.transform.localScale = Vector3.one * 0.8f;
            s.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Standard"))
            {
                color = new Color(0.25f, 0.15f, 0.05f, 0.8f)
            };
            s.SetActive(false);
            stains[i] = s;
        }
        hygiene.dirtStains = stains;

        var trash = new GameObject[2];
        for (int i = 0; i < 2; i++)
        {
            var t = GameObject.CreatePrimitive(PrimitiveType.Cube);
            t.name = $"Trash_{i}";
            t.transform.SetParent(parent);
            t.transform.localPosition = new Vector3(-0.5f + i, 0.1f, 1f);
            t.transform.localScale = new Vector3(0.3f, 0.2f, 0.3f);
            t.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Standard"))
            {
                color = new Color(0.3f, 0.35f, 0.2f)
            };
            t.SetActive(false);
            trash[i] = t;
        }
        hygiene.trashItems = trash;

        var flies = new GameObject[3];
        for (int i = 0; i < 3; i++)
        {
            var f = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            f.name = $"Fly_{i}";
            f.transform.SetParent(parent);
            f.transform.localPosition = new Vector3(i * 0.3f - 0.3f, 1.5f, 0f);
            f.transform.localScale = Vector3.one * 0.08f;
            f.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Standard")) { color = Color.black };
            Object.DestroyImmediate(f.GetComponent<Collider>());
            f.AddComponent<FlyMover>();
            f.SetActive(false);
            flies[i] = f;
        }
        hygiene.flyPrefabs = flies;

        var swarmGo = new GameObject("FlySwarm");
        swarmGo.transform.SetParent(parent);
        swarmGo.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        var ps = swarmGo.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startColor = Color.black;
        main.startSize = 0.05f;
        main.maxParticles = 40;
        var emission = ps.emission;
        emission.rateOverTime = 15f;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        hygiene.flySwarm = ps;

        var mop = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        mop.name = "Mop";
        mop.transform.SetParent(parent);
        mop.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        mop.transform.localScale = new Vector3(0.1f, 0.5f, 0.1f);
        mop.SetActive(false);
        hygiene.mopObject = mop;
    }

    private static GameObject CreateCanvas()
    {
        var canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();
        return canvasGo;
    }

    private static UIManager BuildUI(Transform canvas)
    {
        Font font = GetFont();
        Color bg = new Color(0.10f, 0.08f, 0.06f, 0.96f);
        Color accent = new Color(0.90f, 0.55f, 0.15f);
        Color panel = new Color(0.18f, 0.13f, 0.09f, 0.98f);

        var uiGo = new GameObject("UIManager", typeof(RectTransform));
        uiGo.transform.SetParent(canvas, false);
        var ui = uiGo.AddComponent<UIManager>();

        // ========== DASHBOARD ==========
        var dash = CreatePanel("Dashboard", canvas, bg);
        StretchFull(dash);
        ui.dashboardPanel = dash.gameObject;

        // Zone scroll dashboard
        var dashScroll = CreateScrollArea(dash, "DashScroll", new Vector2(0.03f, 0.02f), new Vector2(0.97f, 0.98f));
        var dashContent = dashScroll.content;
        var dashLayout = dashContent.gameObject.AddComponent<VerticalLayoutGroup>();
        dashLayout.spacing = 14;
        dashLayout.padding = new RectOffset(20, 20, 20, 40);
        dashLayout.childControlHeight = true;
        dashLayout.childControlWidth = true;
        dashLayout.childForceExpandHeight = false;
        dashLayout.childForceExpandWidth = true;
        var dashFitter = dashContent.gameObject.AddComponent<ContentSizeFitter>();
        dashFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ui.moneyText = AddTextRow(dashContent, "Money", "12000 €", 52, accent, 70, font);
        ui.dayText = AddTextRow(dashContent, "Day", "Jour 1 — Mois 1", 30, Color.white, 40, font);
        ui.debtText = AddTextRow(dashContent, "Debt", "Dettes : 0 €", 26, new Color(1f, 0.45f, 0.35f), 36, font);
        ui.healthText = AddTextRow(dashContent, "Health", "Santé : Saine", 26, Color.white, 36, font);
        ui.reputationText = AddTextRow(dashContent, "Rep", "Réputation : 50", 26, Color.white, 36, font);
        ui.restaurantCountText = AddTextRow(dashContent, "Count", "Restaurants : 1", 26, Color.white, 36, font);

        ui.manageFirstRestaurantButton = AddButtonRow(dashContent, "BtnManage",
            "GÉRER MON KEBAB\n(ménage, viande, employés, service…)", 110, accent, font);

        ui.nextDayButton = AddButtonRow(dashContent, "BtnNextDay",
            "▶ Passer un jour", 90, new Color(0.75f, 0.4f, 0.1f), font);

        ui.payTaxesButton = AddButtonRow(dashContent, "BtnTaxes",
            "Payer impôts", 80, new Color(0.25f, 0.4f, 0.7f), font);

        ui.buyRestaurantButton = AddButtonRow(dashContent, "BtnBuyResto",
            $"Acheter un kebab ({GameConstants.BASE_RESTAURANT_PRICE:F0} €)", 80,
            new Color(0.3f, 0.55f, 0.3f), font);

        ui.competitorsText = AddTextRow(dashContent, "Competitors",
            "Concurrents : …", 24, new Color(1f, 0.85f, 0.5f), 70, font);

        ui.buyoutCompetitorButton = AddButtonRow(dashContent, "BtnBuyout",
            "Racheter concurrent (aucun en faillite)", 80, new Color(0.55f, 0.35f, 0.15f), font);

        AddTextRow(dashContent, "ListTitle", "Mes restaurants (toucher pour ouvrir)", 24, accent, 40, font);

        var listHost = new GameObject("RestaurantListHost", typeof(RectTransform), typeof(LayoutElement), typeof(VerticalLayoutGroup));
        listHost.transform.SetParent(dashContent, false);
        listHost.GetComponent<LayoutElement>().minHeight = 200;
        listHost.GetComponent<LayoutElement>().preferredHeight = 220;
        var listVlg = listHost.GetComponent<VerticalLayoutGroup>();
        listVlg.spacing = 10;
        listVlg.childControlHeight = true;
        listVlg.childControlWidth = true;
        listVlg.childForceExpandWidth = true;
        listVlg.childForceExpandHeight = false;
        ui.restaurantListParent = listHost.transform;

        ui.notificationText = AddTextRow(dashContent, "Notif", "", 24, new Color(1f, 0.9f, 0.5f), 80, font);

        // ========== DETAIL RESTAURANT (scroll) ==========
        var detail = CreatePanel("RestaurantDetail", canvas, bg);
        StretchFull(detail);
        detail.gameObject.SetActive(false);
        ui.restaurantDetailPanel = detail.gameObject;

        var restoUI = detail.gameObject.AddComponent<RestaurantUI>();
        ui.restaurantUI = restoUI;

        var detailScroll = CreateScrollArea(detail, "DetailScroll", new Vector2(0.03f, 0.02f), new Vector2(0.97f, 0.98f));
        var dContent = detailScroll.content;
        var dLayout = dContent.gameObject.AddComponent<VerticalLayoutGroup>();
        dLayout.spacing = 12;
        dLayout.padding = new RectOffset(16, 16, 16, 40);
        dLayout.childControlHeight = true;
        dLayout.childControlWidth = true;
        dLayout.childForceExpandHeight = false;
        dLayout.childForceExpandWidth = true;
        var dFitter = dContent.gameObject.AddComponent<ContentSizeFitter>();
        dFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        restoUI.nameText = AddTextRow(dContent, "RName", "Mon Premier Kebab", 40, accent, 55, font);
        restoUI.statusText = AddTextRow(dContent, "RStatus", "Ouvert", 26, Color.white, 36, font);
        restoUI.dirtText = AddTextRow(dContent, "RDirt", "Saleté : 0%", 26, Color.white, 36, font);
        restoUI.meatText = AddTextRow(dContent, "RMeat", "Viande : Poulet", 26, Color.white, 36, font);
        restoUI.stockText = AddTextRow(dContent, "RStock", "Stock : 25 kg", 26, Color.white, 36, font);
        restoUI.employeesText = AddTextRow(dContent, "REmp", "Employés : 1", 26, Color.white, 36, font);
        restoUI.equipmentText = AddTextRow(dContent, "REq", "Matériel", 24, Color.white, 36, font);
        restoUI.revenueText = AddTextRow(dContent, "RRev", "Revenu : 0 €", 26, Color.white, 36, font);
        restoUI.ownerServiceText = AddTextRow(dContent, "ROwner", "Service patron : NON", 26, new Color(0.9f, 0.8f, 0.4f), 40, font);

        AddTextRow(dContent, "SecHygiene", "— HYGIÈNE —", 22, accent, 34, font);
        restoUI.cleanButton = AddButtonRow(dContent, "BtnClean", "Faire le ménage (saleté → 0)", 80,
            new Color(0.2f, 0.55f, 0.65f), font);

        AddTextRow(dContent, "SecMeat", "— VIANDE —", 22, accent, 34, font);
        restoUI.meatBoeufButton = AddButtonRow(dContent, "BtnBoeuf", "Choisir Bœuf (premium, cher)", 70,
            new Color(0.7f, 0.25f, 0.2f), font);
        restoUI.meatPouletButton = AddButtonRow(dContent, "BtnPoulet", "Choisir Poulet (équilibré)", 70,
            new Color(0.8f, 0.6f, 0.2f), font);
        restoUI.meatMysteryButton = AddButtonRow(dContent, "BtnMystery", "Choisir « Je préfère pas savoir »", 70,
            new Color(0.35f, 0.5f, 0.2f), font);
        restoUI.buyMeatButton = AddButtonRow(dContent, "BtnBuyMeat", "Acheter 10 kg de viande", 75,
            new Color(0.55f, 0.3f, 0.25f), font);

        AddTextRow(dContent, "SecStaff", "— EMPLOYÉS —", 22, accent, 34, font);
        restoUI.hireDeclaredButton = AddButtonRow(dContent, "BtnHireDec", "Embaucher DÉCLARÉ (150 €/jour)", 75,
            new Color(0.3f, 0.5f, 0.35f), font);
        restoUI.hireUndeclaredButton = AddButtonRow(dContent, "BtnHireBlack", "Embaucher AU BLACK (70 €/jour)", 75,
            new Color(0.55f, 0.35f, 0.2f), font);
        restoUI.fireButton = AddButtonRow(dContent, "BtnFire", "Licencier le dernier employé", 70,
            new Color(0.55f, 0.2f, 0.2f), font);

        AddTextRow(dContent, "SecOwner", "— SERVICE PATRON / CAISSE —", 22, accent, 34, font);
        restoUI.ownerServiceButton = AddButtonRow(dContent, "BtnOwner", "Je fais le service moi-même", 85,
            new Color(0.65f, 0.45f, 0.15f), font);
        restoUI.openCounterButton = AddButtonRow(dContent, "BtnCounter",
            "Passer derrière la caisse\n(prendre les commandes)", 95,
            new Color(0.75f, 0.5f, 0.2f), font);

        AddTextRow(dContent, "SecEq", "— MATÉRIEL —", 22, accent, 34, font);
        restoUI.upgradeGrillButton = AddButtonRow(dContent, "BtnGrill", "Améliorer le Grill", 70,
            new Color(0.4f, 0.4f, 0.45f), font);
        restoUI.upgradeFridgeButton = AddButtonRow(dContent, "BtnFridge", "Améliorer le Frigo", 70,
            new Color(0.4f, 0.4f, 0.45f), font);
        restoUI.upgradeVitrineButton = AddButtonRow(dContent, "BtnVitrine", "Améliorer la Vitrine", 70,
            new Color(0.4f, 0.4f, 0.45f), font);

        restoUI.buyoutCompetitorButton = AddButtonRow(dContent, "BtnBuyoutDetail",
            "Racheter un concurrent en faillite", 75, new Color(0.55f, 0.4f, 0.15f), font);

        restoUI.backButton = AddButtonRow(dContent, "BtnBack", "← Retour à l'empire", 85,
            new Color(0.28f, 0.28f, 0.32f), font);

        // ========== CAISSE (prise de commande) ==========
        var counterUI = BuildCounterServiceUI(canvas, font, accent, bg);
        restoUI.customerServiceUI = counterUI;
        counterUI.restaurantUI = restoUI;
        counterUI.serviceController = Object.FindObjectOfType<CounterServiceController>();

        // ========== GAME OVER ==========
        var goPanel = CreatePanel("GameOver", canvas, new Color(0, 0, 0, 0.88f));
        StretchFull(goPanel);
        goPanel.gameObject.SetActive(false);
        ui.gameOverPanel = goPanel.gameObject;
        ui.gameOverText = CreateCenteredLabel(goPanel, "GOText", "GAME OVER", 40, Color.white, font);
        ui.newGameButton = CreateFixedButton(goPanel, "BtnNewGame", "Nouvelle partie",
            new Vector2(0, -200), new Vector2(500, 90), accent, font);

        // ========== HOSTILE ==========
        var ht = CreatePanel("HostileTakeover", canvas, new Color(0.4f, 0.05f, 0.05f, 0.92f));
        StretchFull(ht);
        ht.gameObject.SetActive(false);
        ui.hostileTakeoverPanel = ht.gameObject;
        ui.hostileTakeoverText = CreateCenteredLabel(ht, "HTText", "Rachat hostile !", 34, Color.white, font);
        ui.acceptTakeoverButton = CreateFixedButton(ht, "BtnAccept", "Accepter (Game Over)",
            new Vector2(-200, -180), new Vector2(360, 90), new Color(0.6f, 0.2f, 0.2f), font);
        ui.refuseTakeoverButton = CreateFixedButton(ht, "BtnRefuse", "Refuser (Faillite)",
            new Vector2(200, -180), new Vector2(360, 90), new Color(0.3f, 0.3f, 0.3f), font);

        return ui;
    }

    private static CustomerServiceUI BuildCounterServiceUI(Transform canvas, Font font, Color accent, Color bg)
    {
        var panel = CreatePanel("CounterServicePanel", canvas, new Color(0.08f, 0.07f, 0.05f, 0.97f));
        StretchFull(panel);
        panel.gameObject.SetActive(false);

        var uiGo = panel.gameObject.AddComponent<CustomerServiceUI>();
        uiGo.servicePanel = panel.gameObject;

        var scroll = CreateScrollArea(panel, "CounterScroll", new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.97f));
        var content = scroll.content;
        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 12;
        layout.padding = new RectOffset(16, 16, 16, 40);
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        AddTextRow(content, "Title", "CAISSE — Prise de commande", 36, accent, 50, font);

        uiGo.dialogueText = AddTextRow(content, "Dialogue", "…", 24, Color.white, 180, font);
        uiGo.ticketText = AddTextRow(content, "Ticket", "TICKET", 26, new Color(1f, 0.9f, 0.6f), 140, font);
        uiGo.hintText = AddTextRow(content, "Hint", "Active le service patron puis sers un client.", 24,
            new Color(0.7f, 0.85f, 1f), 60, font);

        uiGo.startCustomerButton = AddButtonRow(content, "BtnNextClient", "Client suivant", 80,
            new Color(0.3f, 0.55f, 0.35f), font);

        AddTextRow(content, "AskTitle", "— POSER LES QUESTIONS —", 22, accent, 34, font);
        uiGo.askSaladButton = AddButtonRow(content, "AskSalad", "Salade ?", 70, new Color(0.25f, 0.5f, 0.3f), font);
        uiGo.askTomatoButton = AddButtonRow(content, "AskTomato", "Tomate ?", 70, new Color(0.7f, 0.3f, 0.25f), font);
        uiGo.askOnionButton = AddButtonRow(content, "AskOnion", "Oignon ?", 70, new Color(0.55f, 0.45f, 0.2f), font);
        uiGo.askSauceButton = AddButtonRow(content, "AskSauce", "Quelle sauce ?", 75, new Color(0.6f, 0.35f, 0.15f), font);

        AddTextRow(content, "SauceTitle", "— PRÉSENTOIR SAUCES —", 22, accent, 34, font);

        var sauceRoot = new GameObject("SauceButtons", typeof(RectTransform), typeof(LayoutElement), typeof(VerticalLayoutGroup));
        sauceRoot.transform.SetParent(content, false);
        sauceRoot.GetComponent<LayoutElement>().preferredHeight = 420;
        sauceRoot.GetComponent<LayoutElement>().minHeight = 420;
        var sv = sauceRoot.GetComponent<VerticalLayoutGroup>();
        sv.spacing = 8;
        sv.childControlHeight = true;
        sv.childControlWidth = true;
        sv.childForceExpandWidth = true;
        sv.childForceExpandHeight = false;
        uiGo.sauceButtonsRoot = sauceRoot;

        uiGo.sauceBlancheButton = AddButtonRow(sauceRoot.transform, "SauceBlanche", "Blanche", 60, new Color(0.85f, 0.85f, 0.8f), font);
        SetButtonTextColor(uiGo.sauceBlancheButton, Color.black);
        uiGo.sauceAlgerienneButton = AddButtonRow(sauceRoot.transform, "SauceAlg", "Algérienne", 60, new Color(0.85f, 0.45f, 0.15f), font);
        uiGo.sauceSamouraiButton = AddButtonRow(sauceRoot.transform, "SauceSam", "Samouraï", 60, new Color(0.7f, 0.15f, 0.15f), font);
        uiGo.sauceHarissaButton = AddButtonRow(sauceRoot.transform, "SauceHar", "Harissa", 60, new Color(0.55f, 0.1f, 0.1f), font);
        uiGo.sauceKetchupMayoButton = AddButtonRow(sauceRoot.transform, "SauceKM", "Ketchup-Mayo", 60, new Color(0.8f, 0.35f, 0.3f), font);
        uiGo.sauceSansButton = AddButtonRow(sauceRoot.transform, "SauceSans", "Sans sauce", 60, new Color(0.35f, 0.35f, 0.38f), font);

        uiGo.checkoutButton = AddButtonRow(content, "BtnCheckout", "Encaisser le client", 90,
            new Color(0.2f, 0.6f, 0.35f), font);
        uiGo.cancelButton = AddButtonRow(content, "BtnCancelClient", "Client part (annuler)", 70,
            new Color(0.5f, 0.2f, 0.2f), font);
        uiGo.closeServiceButton = AddButtonRow(content, "BtnCloseCounter", "Quitter la caisse", 80,
            new Color(0.28f, 0.28f, 0.32f), font);

        return uiGo;
    }

    private static void SetButtonTextColor(Button btn, Color color)
    {
        if (btn == null) return;
        var t = btn.GetComponentInChildren<Text>();
        if (t != null) t.color = color;
    }

    // ---------- Helpers UI ----------

    private class ScrollRefs
    {
        public RectTransform content;
    }

    private static ScrollRefs CreateScrollArea(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        var scrollGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollGo.transform.SetParent(parent, false);
        var srt = scrollGo.GetComponent<RectTransform>();
        srt.anchorMin = anchorMin;
        srt.anchorMax = anchorMax;
        srt.offsetMin = Vector2.zero;
        srt.offsetMax = Vector2.zero;
        scrollGo.GetComponent<Image>().color = new Color(0, 0, 0, 0.01f);

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(scrollGo.transform, false);
        StretchFull(viewport.transform);
        viewport.GetComponent<Image>().color = Color.white;
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        var content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        var crt = content.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0f, 1f);
        crt.anchorMax = new Vector2(1f, 1f);
        crt.pivot = new Vector2(0.5f, 1f);
        crt.anchoredPosition = Vector2.zero;
        crt.sizeDelta = new Vector2(0f, 0f);

        var sr = scrollGo.GetComponent<ScrollRect>();
        sr.viewport = viewport.GetComponent<RectTransform>();
        sr.content = crt;
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 40f;

        return new ScrollRefs { content = crt };
    }

    private static Text AddTextRow(Transform parent, string name, string text, int size, Color color, float height, Font font)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        go.GetComponent<LayoutElement>().preferredHeight = height;
        go.GetComponent<LayoutElement>().minHeight = height;

        var t = go.AddComponent<Text>();
        t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAnchor.MiddleLeft;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    private static Button AddButtonRow(Transform parent, string name, string label, float height, Color color, Font font)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        go.GetComponent<LayoutElement>().preferredHeight = height;
        go.GetComponent<LayoutElement>().minHeight = height;
        go.GetComponent<Image>().color = color;

        var btn = go.GetComponent<Button>();
        var colors = btn.colors;
        colors.pressedColor = color * 0.75f;
        colors.highlightedColor = color * 1.1f;
        btn.colors = colors;

        var textGo = new GameObject("Label", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        StretchFull(textGo.transform);
        var t = textGo.AddComponent<Text>();
        t.font = font;
        t.text = label;
        t.fontSize = 28;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return btn;
    }

    private static Transform CreatePanel(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return go.transform;
    }

    private static void StretchFull(Transform t)
    {
        var rt = t as RectTransform ?? t.GetComponent<RectTransform>();
        if (rt == null) return;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static Text CreateCenteredLabel(Transform parent, string name, string text, int size, Color color, Font font)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(900, 300);
        var t = go.AddComponent<Text>();
        t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        return t;
    }

    private static Button CreateFixedButton(Transform parent, string name, string label,
        Vector2 anchoredPos, Vector2 size, Color color, Font font)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        go.GetComponent<Image>().color = color;
        var btn = go.GetComponent<Button>();

        var textGo = new GameObject("Label", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        StretchFull(textGo.transform);
        var t = textGo.AddComponent<Text>();
        t.font = font;
        t.text = label;
        t.fontSize = 26;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        return btn;
    }

    private static Font GetFont()
    {
        Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return f;
    }
#endif
}
