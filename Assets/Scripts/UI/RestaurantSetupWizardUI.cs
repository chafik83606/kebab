using UnityEngine;
using UnityEngine.UI;

/// <summary>Assistant : viande, ingrédients, employés, hygiène, mode auto/manuel.</summary>
public class RestaurantSetupWizardUI : MonoBehaviour
{
    public GameObject root;
    public Text stepTitleText;
    public Text stepInfoText;
    public Button nextButton;
    public Button backButton;

    private int step;
    private FranceMapData.MapCity city;
    private readonly RestaurantSetupConfig config = new RestaurantSetupConfig();
    private int undeclaredStaff;

    private void Awake()
    {
        Bind(nextButton, OnNext);
        Bind(backButton, OnBack);
    }

    private static void Bind(Button b, UnityEngine.Events.UnityAction a)
    {
        if (b == null) return;
        b.onClick.RemoveAllListeners();
        b.onClick.AddListener(a);
    }

    public void Begin(FranceMapData.MapCity c)
    {
        city = c;
        config.meat = MeatType.Poulet;
        config.salad = config.tomato = config.onion = IngredientFreshness.Frais;
        config.kitchenStaff = 0;
        config.declaredStaff = 0;
        undeclaredStaff = 0;
        config.hygieneStaff = 0;
        config.managementMode = ManagementMode.Automatic;
        step = 0;
        if (root != null) root.SetActive(true);
        RefreshStep();
    }

    public void Hide()
    {
        if (root != null) root.SetActive(false);
    }

    private void RefreshStep()
    {
        if (stepTitleText == null) return;
        switch (step)
        {
            case 0:
                stepTitleText.text = $"1/5 Viande · {city.displayName}";
                stepInfoText.text = config.meat.GetDisplayName();
                break;
            case 1:
                stepTitleText.text = "2/5 Ingrédients";
                stepInfoText.text = $"S/T/O : {config.salad.GetDisplayName()[0]}/{config.tomato.GetDisplayName()[0]}/{config.onion.GetDisplayName()[0]}";
                break;
            case 2:
                stepTitleText.text = "3/5 Employés";
                stepInfoText.text = $"Décl. {config.declaredStaff} · Black {undeclaredStaff}";
                break;
            case 3:
                stepTitleText.text = "4/5 Propreté";
                stepInfoText.text = config.hygieneStaff > 0 ? "Agent OUI" : "Sans agent";
                break;
            case 4:
                stepTitleText.text = "5/5 Mode";
                stepInfoText.text = config.managementMode == ManagementMode.Automatic ? "Auto" : "Manuel";
                break;
        }
        RebuildChoiceButtons();
        if (backButton != null) backButton.gameObject.SetActive(step > 0);
        if (nextButton != null)
        {
            var lbl = nextButton.GetComponentInChildren<Text>();
            if (lbl != null) lbl.text = step >= 4 ? "OUVRIR LE KEBAB" : "Suivant";
        }
    }

    private void RebuildChoiceButtons()
    {
        var choices = root != null ? root.transform.Find("Choices") : transform.Find("Choices");
        if (choices == null) return;
        for (int i = choices.childCount - 1; i >= 0; i--)
            Destroy(choices.GetChild(i).gameObject);

        switch (step)
        {
            case 0:
                AddChoice(choices, "Bœuf", () => config.meat = MeatType.Boeuf);
                AddChoice(choices, "Poulet", () => config.meat = MeatType.Poulet);
                AddChoice(choices, "Préfère pas savoir", () => config.meat = MeatType.PreferePasSavoir);
                break;
            case 1:
                AddChoice(choices, "Salade fraîche", () => config.salad = IngredientFreshness.Frais);
                AddChoice(choices, "Salade peu fraîche", () => config.salad = IngredientFreshness.PeuFrais);
                AddChoice(choices, "Tomate fraîche", () => config.tomato = IngredientFreshness.Frais);
                AddChoice(choices, "Tomate peu fraîche", () => config.tomato = IngredientFreshness.PeuFrais);
                AddChoice(choices, "Oignon frais", () => config.onion = IngredientFreshness.Frais);
                AddChoice(choices, "Oignon peu frais", () => config.onion = IngredientFreshness.PeuFrais);
                break;
            case 2:
                AddChoice(choices, "+ Déclaré", () => { config.declaredStaff++; config.kitchenStaff++; });
                AddChoice(choices, "+ Black", () => { undeclaredStaff++; config.kitchenStaff++; });
                AddChoice(choices, "− Employé", () =>
                {
                    if (undeclaredStaff > 0) { undeclaredStaff--; config.kitchenStaff--; }
                    else if (config.declaredStaff > 0) { config.declaredStaff--; config.kitchenStaff--; }
                });
                break;
            case 3:
                AddChoice(choices, "Agent propreté OUI", () => config.hygieneStaff = 1);
                AddChoice(choices, "Pas d'agent (risque)", () => config.hygieneStaff = 0);
                break;
            case 4:
                AddChoice(choices, "Auto (kebab seul)", () => config.managementMode = ManagementMode.Automatic);
                AddChoice(choices, "Manuel (caisse)", () => config.managementMode = ManagementMode.Manual);
                break;
        }
    }

    private void AddChoice(Transform parent, string label, UnityEngine.Events.UnityAction action)
    {
        int count = parent.childCount;
        var go = new GameObject("C" + count, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.05f + (count % 2) * 0.48f, 0.55f - (count / 2) * 0.22f);
        rt.anchorMax = rt.anchorMin + new Vector2(0.44f, 0.18f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = new Color(0.25f, 0.4f, 0.55f, 0.95f);
        var tGo = new GameObject("T", typeof(RectTransform));
        tGo.transform.SetParent(go.transform, false);
        Stretch(tGo.GetComponent<RectTransform>());
        var t = tGo.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text = label;
        t.fontSize = 17;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        go.GetComponent<Button>().onClick.AddListener(() => { action(); RefreshStep(); });
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private void OnNext()
    {
        if (step < 4) { step++; RefreshStep(); return; }
        config.kitchenStaff = config.declaredStaff + undeclaredStaff;
        config.undeclaredStaff = undeclaredStaff;
        GameFlowController.Instance?.CompleteSetup(config);
    }

    private void OnBack()
    {
        if (step > 0) { step--; RefreshStep(); }
    }
}
