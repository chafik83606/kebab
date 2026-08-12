using UnityEngine;

/// <summary>
/// Contrôleur visuel de l'hygiène d'un restaurant.
/// Active/désactive taches, déchets, mouches et particules selon les paliers de saleté.
///
/// Paliers :
///   0-20%  Propre      → tout désactivé
///  21-50%  Négligé     → taches sombres / graisse
///  51-75%  Crado       → taches + déchets + 2-3 mouches
///  76-100% Infestation → nuage de mouches (particules) + sol graisseux
/// </summary>
public class HygieneVisualController : MonoBehaviour
{
    [Header("Paliers — Négligé (21-50%)")]
    [Tooltip("Taches de graisse / saleté au sol et sur le grill")]
    public GameObject[] dirtStains;

    [Header("Paliers — Crado (51-75%)")]
    [Tooltip("Déchets au sol")]
    public GameObject[] trashItems;
    [Tooltip("Mouches individuelles (2-3)")]
    public GameObject[] flyPrefabs;

    [Header("Paliers — Infestation (76-100%)")]
    [Tooltip("Nuage de mouches (ParticleSystem)")]
    public ParticleSystem flySwarm;

    [Header("Animation ménage")]
    [Tooltip("Objet serpillière / balai joué lors du nettoyage")]
    public GameObject mopObject;
    public float mopAnimationDuration = 1.2f;

    [Header("Debug")]
    [SerializeField] private float lastDirtLevel = -1f;

    private void Awake()
    {
        // État initial : propre
        SetAllInactive();
        if (mopObject != null)
            mopObject.SetActive(false);
    }

    /// <summary>
    /// Met à jour tous les éléments visuels selon le niveau de saleté (0-100).
    /// </summary>
    public void UpdateVisuals(float dirtLevel)
    {
        dirtLevel = Mathf.Clamp(dirtLevel, 0f, 100f);
        lastDirtLevel = dirtLevel;

        DirtLevel level = GetLevel(dirtLevel);

        // --- Taches (dès Négligé) ---
        bool showStains = level >= DirtLevel.Neglected;
        SetActiveArray(dirtStains, showStains);

        // --- Déchets (dès Crado) ---
        bool showTrash = level >= DirtLevel.Dirty;
        SetActiveArray(trashItems, showTrash);

        // --- Mouches individuelles (Crado uniquement, pas infestation) ---
        bool showFlies = level == DirtLevel.Dirty;
        SetActiveArray(flyPrefabs, showFlies);

        // --- Nuage de particules (Infestation) ---
        if (flySwarm != null)
        {
            if (level == DirtLevel.Infestation)
            {
                if (!flySwarm.isPlaying)
                    flySwarm.Play();
            }
            else
            {
                if (flySwarm.isPlaying)
                    flySwarm.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }

    /// <summary>Joue une petite animation de serpillière puis remet le visuel à propre.</summary>
    public void PlayCleanAnimation()
    {
        StopAllCoroutines();
        StartCoroutine(CleanRoutine());
    }

    private System.Collections.IEnumerator CleanRoutine()
    {
        if (mopObject != null)
        {
            mopObject.SetActive(true);

            // Petite animation de va-et-vient
            Vector3 start = mopObject.transform.localPosition;
            float elapsed = 0f;
            while (elapsed < mopAnimationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / mopAnimationDuration;
                float sway = Mathf.Sin(t * Mathf.PI * 4f) * 0.15f;
                mopObject.transform.localPosition = start + new Vector3(sway, 0f, 0f);
                yield return null;
            }

            mopObject.transform.localPosition = start;
            mopObject.SetActive(false);
        }

        UpdateVisuals(0f);
    }

    // ======================== HELPERS ========================

    private static DirtLevel GetLevel(float dirt)
    {
        if (dirt <= GameConstants.DIRT_THRESHOLD_NEGLECTED) return DirtLevel.Clean;
        if (dirt <= GameConstants.DIRT_THRESHOLD_DIRTY) return DirtLevel.Neglected;
        if (dirt <= GameConstants.DIRT_THRESHOLD_INFESTATION) return DirtLevel.Dirty;
        return DirtLevel.Infestation;
    }

    private void SetAllInactive()
    {
        SetActiveArray(dirtStains, false);
        SetActiveArray(trashItems, false);
        SetActiveArray(flyPrefabs, false);
        if (flySwarm != null && flySwarm.isPlaying)
            flySwarm.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private static void SetActiveArray(GameObject[] objects, bool active)
    {
        if (objects == null) return;
        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null)
                objects[i].SetActive(active);
        }
    }

#if UNITY_EDITOR
    /// <summary>Test rapide depuis l'inspecteur (clic droit → Test Visuals).</summary>
    [ContextMenu("Test: Propre (0%)")]
    private void TestClean() => UpdateVisuals(0f);

    [ContextMenu("Test: Négligé (35%)")]
    private void TestNeglected() => UpdateVisuals(35f);

    [ContextMenu("Test: Crado (60%)")]
    private void TestDirty() => UpdateVisuals(60f);

    [ContextMenu("Test: Infestation (90%)")]
    private void TestInfestation() => UpdateVisuals(90f);
#endif
}
