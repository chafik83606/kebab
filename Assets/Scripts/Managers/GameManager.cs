using UnityEngine;

/// <summary>
/// Orchestrateur du cycle de jeu (jour / mois).
/// Relie EmpireManager aux boutons UI et gère le démarrage.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Références")]
    public EmpireManager empireManager;
    public UIManager uiManager;

    [Header("Options")]
    [Tooltip("Si true, charge automatiquement la sauvegarde au démarrage")]
    public bool autoLoadOnStart = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (empireManager == null)
            empireManager = EmpireManager.Instance;

        // S'assurer que les références UI sont branchées
        if (uiManager != null)
            uiManager.Initialize();
    }

    /// <summary>Bouton UI : passer au jour suivant.</summary>
    public void OnNextDayClicked()
    {
        if (empireManager == null || empireManager.IsGameOver) return;
        empireManager.StartNewDay();
    }

    /// <summary>Bouton UI : payer les impôts.</summary>
    public void OnPayTaxesClicked()
    {
        if (empireManager == null) return;
        empireManager.PayTaxes();
    }

    /// <summary>Bouton UI : nouvelle partie.</summary>
    public void OnNewGameClicked()
    {
        if (empireManager == null) return;
        empireManager.NewGame();
    }

    /// <summary>Bouton UI : sauvegarder manuellement.</summary>
    public void OnSaveClicked()
    {
        if (empireManager == null) return;
        empireManager.AutoSave();
        empireManager.Notify("Partie sauvegardée.");
    }
}
