using UnityEngine;

/// <summary>
/// Service caisse en 3D : caméra derrière le comptoir, client face au joueur,
/// questions salade/tomate/oignon/sauce.
/// </summary>
public class CounterService3D : MonoBehaviour
{
    public static CounterService3D Instance { get; private set; }

    [Header("Références")]
    public CounterServiceController serviceLogic;
    public CustomerServiceUI serviceUI;
    public Transform counterCameraPoint;
    public Transform customerStandPoint;
    public VisualCatalog visualCatalog;

    [Header("État")]
    public GameObject activeCustomerVisual;

    private RestaurantInterior currentInterior;
    private RestaurantData currentData;
    private bool sessionActive;

    private void Awake()
    {
        Instance = this;
        if (serviceLogic == null)
            serviceLogic = FindObjectOfType<CounterServiceController>();
    }

    private void OnEnable()
    {
        if (serviceLogic != null)
        {
            serviceLogic.OnServiceStateChanged += OnServiceChanged;
            serviceLogic.OnServiceFinished += OnServiceFinished;
        }
    }

    private void OnDisable()
    {
        if (serviceLogic != null)
        {
            serviceLogic.OnServiceStateChanged -= OnServiceChanged;
            serviceLogic.OnServiceFinished -= OnServiceFinished;
        }
    }

    /// <summary>Démarre le mode caisse 3D dans un intérieur.</summary>
    public void BeginSession(RestaurantInterior interior, RestaurantData data)
    {
        currentInterior = interior;
        currentData = data;
        sessionActive = true;

        if (GameWorldManager.Instance != null)
            GameWorldManager.Instance.ShowCounterHud();

        if (interior != null)
            InteriorEnvironmentSetup.Apply(interior);

        EnsureCameraPoints(interior);

        if (GameCameraDirector.Instance != null && counterCameraPoint != null)
            GameCameraDirector.Instance.SetCounterMode(counterCameraPoint);

        if (GameWorldManager.Instance != null && GameWorldManager.Instance.player != null)
        {
            GameWorldManager.Instance.player.gameObject.SetActive(false);
            playerHiddenForCounter = true;
        }

        if (serviceUI != null)
            serviceUI.OpenForRestaurant(data);

        EmpireManager.Instance?.Notify("Caisse ouverte — Client suivant !");

        // Premier client automatique
        if (serviceLogic != null && data != null)
            serviceLogic.StartNextCustomer(data);
    }

    private bool playerHiddenForCounter;

    public void EndSession()
    {
        if (!sessionActive && (serviceUI == null || !serviceUI.servicePanel.activeSelf))
            return;

        sessionActive = false;
        ClearCustomerVisual();

        if (serviceLogic != null && serviceLogic.HasActiveCustomer)
            serviceLogic.CancelCustomer();

        if (serviceUI != null)
            serviceUI.ClosePanelImmediate();

        if (GameWorldManager.Instance != null && GameWorldManager.Instance.player != null)
            GameWorldManager.Instance.player.gameObject.SetActive(true);

        playerHiddenForCounter = false;

        if (GameCameraDirector.Instance != null)
            GameCameraDirector.Instance.SetPlayerMode();

        if (GameWorldManager.Instance != null)
            GameWorldManager.Instance.ShowInteriorHud();

        EmpireManager.Instance?.Notify("Fin du service caisse.");
    }

    private void OnServiceChanged()
    {
        if (!sessionActive || serviceLogic == null) return;

        if (serviceLogic.HasActiveCustomer && activeCustomerVisual == null)
            SpawnCustomerFacingCounter();
    }

    private void OnServiceFinished()
    {
        ClearCustomerVisual();
    }

    private void SpawnCustomerFacingCounter()
    {
        ClearCustomerVisual();

        Vector3 pos = customerStandPoint != null
            ? customerStandPoint.position
            : (currentInterior != null && currentInterior.counterPoint != null
                ? currentInterior.counterPoint.position + currentInterior.transform.forward * -1.8f
                : transform.position + Vector3.forward * 1.5f);

        activeCustomerVisual = CharacterVisualFactory.CreateCharacter(
            "ClientCaisse", 1.75f, visualCatalog, true);
        activeCustomerVisual.transform.position = pos;

        if (counterCameraPoint != null)
        {
            Vector3 look = counterCameraPoint.position - pos;
            look.y = 0f;
            if (look.sqrMagnitude > 0.01f)
                activeCustomerVisual.transform.rotation = Quaternion.LookRotation(look);
        }
        else
        {
            activeCustomerVisual.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        }

        var bob = activeCustomerVisual.AddComponent<SimpleIdleBob>();
        bob.amplitude = 0.03f;
    }

    private void ClearCustomerVisual()
    {
        if (activeCustomerVisual != null)
        {
            Destroy(activeCustomerVisual);
            activeCustomerVisual = null;
        }
    }

    private void EnsureCameraPoints(RestaurantInterior interior)
    {
        if (interior == null) return;

        Vector3 counter = interior.counterPoint != null
            ? interior.counterPoint.position
            : interior.transform.position + Vector3.up * 1f;

        Vector3 forward = interior.counterPoint != null
            ? interior.counterPoint.forward
            : interior.transform.forward;

        if (counterCameraPoint == null)
        {
            var go = new GameObject("CounterCameraPoint");
            go.transform.SetParent(interior.transform);
            counterCameraPoint = go.transform;
        }

        // Derrière le comptoir (côté employé)
        counterCameraPoint.position = counter + forward * 2.2f + Vector3.up * 1.55f;
        Vector3 lookTarget = counter + forward * -2f + Vector3.up * 1.35f;
        counterCameraPoint.rotation = Quaternion.LookRotation(lookTarget - counterCameraPoint.position);

        if (customerStandPoint == null)
        {
            var stand = new GameObject("CustomerStand").transform;
            stand.SetParent(interior.transform);
            customerStandPoint = stand;
        }

        // Devant le comptoir (côté client)
        customerStandPoint.position = counter + forward * -1.8f;
        customerStandPoint.rotation = Quaternion.LookRotation(forward);
    }
}

/// <summary>Léger balancement idle pour PNJ à la caisse.</summary>
public class SimpleIdleBob : MonoBehaviour
{
    public float amplitude = 0.04f;
    public float speed = 2.5f;
    private Vector3 origin;

    private void Start() => origin = transform.position;

    private void Update()
    {
        transform.position = origin + Vector3.up * (Mathf.Sin(Time.time * speed) * amplitude);
    }
}
