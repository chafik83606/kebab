using UnityEngine;
using UnityEngine.UI;

/// <summary>Carte France / Monde — zoom, pan, villes calibrées.</summary>
public class FranceMapUI : MonoBehaviour
{
    public GameObject root;
    public Text titleText;
    public Text hintText;
    public Text confirmText;
    public GameObject confirmPanel;
    public Button confirmYesButton;
    public Button confirmNoButton;
    public Button franceTabButton;
    public Button worldTabButton;
    public Button zoomInButton;
    public Button zoomOutButton;
    public Transform citiesRoot;
    public RawImage mapImage;
    public MapZoomPan zoomPan;

    private bool worldTab;
    private FranceMapData.MapCity? selectedCity;

    private void Awake()
    {
        Bind(confirmYesButton, OnConfirmYes);
        Bind(confirmNoButton, OnConfirmNo);
        Bind(franceTabButton, () => SwitchTab(false));
        Bind(worldTabButton, () => SwitchTab(true));
        Bind(zoomInButton, () => zoomPan?.ZoomIn());
        Bind(zoomOutButton, () => zoomPan?.ZoomOut());
    }

    private static void Bind(Button b, UnityEngine.Events.UnityAction a)
    {
        if (b == null) return;
        b.onClick.RemoveAllListeners();
        b.onClick.AddListener(a);
    }

    public void Show(bool worldMap)
    {
        ProceduralMapGenerator.InvalidateCache();
        if (root != null) root.SetActive(true);
        if (confirmPanel != null) confirmPanel.SetActive(false);
        zoomPan?.ResetView();
        SwitchTab(worldMap);
        if (hintText != null)
            hintText.text = "Tape une ville · +/- zoom";
    }

    public void Hide()
    {
        if (root != null) root.SetActive(false);
        HideConfirm();
    }

    public void SwitchTab(bool world)
    {
        worldTab = world;
        if (GameFlowController.Instance != null)
            GameFlowController.Instance.ShowWorldMap = world;

        if (titleText != null)
            titleText.text = world ? "Monde" : "France";

        zoomPan?.ResetView();
        UpdateMapTexture();
        UpdateTabHighlight();
        RebuildCityPins();
    }

    private void UpdateMapTexture()
    {
        if (mapImage == null) return;
        mapImage.texture = worldTab
            ? ProceduralMapGenerator.GetWorldMap()
            : ProceduralMapGenerator.GetFranceMap();
        mapImage.color = Color.white;

        // Ratio du cadre = ratio de la texture (France portrait, Monde paysage)
        var frame = mapImage.transform.parent;
        if (frame != null)
        {
            var aspect = frame.GetComponent<AspectRatioFitter>();
            if (aspect != null)
            {
                var tex = mapImage.texture;
                if (tex != null && tex.height > 0)
                    aspect.aspectRatio = (float)tex.width / tex.height;
                else
                    aspect.aspectRatio = worldTab ? (1536f / 1024f) : (1024f / 1536f);
            }
        }
    }

    private void UpdateTabHighlight()
    {
        SetTabColor(franceTabButton, !worldTab);
        SetTabColor(worldTabButton, worldTab);
    }

    private static void SetTabColor(Button btn, bool active)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img == null) return;
        img.color = active
            ? new Color(0.25f, 0.55f, 0.85f, 1f)
            : new Color(0.28f, 0.28f, 0.35f, 0.85f);
    }

    private void RebuildCityPins()
    {
        if (citiesRoot == null) return;
        for (int i = citiesRoot.childCount - 1; i >= 0; i--)
            Destroy(citiesRoot.GetChild(i).gameObject);

        var cities = FranceMapData.Cities;
        for (int i = 0; i < cities.Length; i++)
        {
            if (cities[i].isFrance == worldTab) continue;
            CreateCityPin(cities[i]);
        }
    }

    private void CreateCityPin(FranceMapData.MapCity city)
    {
        Vector2 pos = FranceMapData.GetUiPos(city, worldTab);

        var rootGo = new GameObject("Pin_" + city.id, typeof(RectTransform));
        rootGo.transform.SetParent(citiesRoot, false);
        var rt = rootGo.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = pos;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(90f, 44f);
        rt.anchoredPosition = Vector2.zero;

        var dot = new GameObject("Dot", typeof(RectTransform), typeof(Image));
        dot.transform.SetParent(rootGo.transform, false);
        var dotRt = dot.GetComponent<RectTransform>();
        dotRt.anchorMin = dotRt.anchorMax = new Vector2(0.5f, 0.62f);
        dotRt.pivot = new Vector2(0.5f, 0.5f);
        dotRt.sizeDelta = new Vector2(18f, 18f);
        dot.GetComponent<Image>().color = new Color(0.95f, 0.12f, 0.08f, 1f);
        dot.GetComponent<Image>().raycastTarget = false;

        var labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(rootGo.transform, false);
        var lrt = labelGo.GetComponent<RectTransform>();
        lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 0.08f);
        lrt.pivot = new Vector2(0.5f, 1f);
        lrt.sizeDelta = new Vector2(100f, 24f);
        var t = labelGo.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text = city.displayName;
        t.fontSize = worldTab ? 11 : 13;
        t.fontStyle = FontStyle.Bold;
        t.color = Color.white;
        t.alignment = TextAnchor.UpperCenter;
        t.raycastTarget = false;
        var outline = labelGo.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(1f, -1f);

        var hit = rootGo.AddComponent<Image>();
        hit.color = new Color(0, 0, 0, 0.01f);
        var btn = rootGo.AddComponent<Button>();
        var captured = city;
        btn.onClick.AddListener(() => OnCityTapped(captured));
    }

    private void OnCityTapped(FranceMapData.MapCity city)
    {
        selectedCity = city;
        GameFlowController.Instance?.SelectCity(city);
    }

    public void ShowConfirm(FranceMapData.MapCity city, float price)
    {
        selectedCity = city;
        if (confirmPanel != null) confirmPanel.SetActive(true);
        if (confirmText != null)
            confirmText.text =
                $"Installer un kebab à {city.displayName} ?\n" +
                $"Passage ×{city.locationMultiplier:F2} · Coût : {price:F0} €";
    }

    public void HideConfirm()
    {
        if (confirmPanel != null) confirmPanel.SetActive(false);
        selectedCity = null;
    }

    private void OnConfirmYes() => GameFlowController.Instance?.ConfirmPlacement();
    private void OnConfirmNo() => GameFlowController.Instance?.CancelPlacement();
}
