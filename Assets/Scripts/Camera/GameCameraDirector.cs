using UnityEngine;

/// <summary>
/// Modes caméra : carte vue dessus, suivi joueur, service caisse 3D.
/// </summary>
public enum CameraGameMode
{
    MapOverview,
    PlayerFollow,
    CounterService
}

/// <summary>
/// Directeur de caméra : carte top-down, zoom resto, 3e personne, caisse.
/// </summary>
public class GameCameraDirector : MonoBehaviour
{
    public static GameCameraDirector Instance { get; private set; }

    [Header("Références")]
    public Camera mainCamera;
    public PlayerController player;
    public Transform mapCenter;
    public float mapHeight = 55f;
    public float mapOrthoOrDistance = 45f;
    public float zoomHeight = 22f;
    public float moveSpeed = 14f;
    public float zoomSpeed = 10f;
    [Tooltip("Plus petit = vue plus plongeante (meilleur en portrait mobile)")]
    public float mapLookBackFactor = 0.2f;

    [Header("Caisse 3D")]
    public Transform counterCameraPoint;
    public float counterBlendSpeed = 5f;

    public CameraGameMode Mode { get; private set; } = CameraGameMode.MapOverview;

    private Transform zoomTarget;
    private Vector3 mapFocus;

    /// <summary>Restaurant actuellement zoomé sur la carte (null = vue générale).</summary>
    public RestaurantBuilding FocusedBuilding =>
        zoomTarget != null ? zoomTarget.GetComponent<RestaurantBuilding>() : null;

    private void Awake()
    {
        Instance = this;
        if (mainCamera == null) mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (mainCamera == null) return;

        switch (Mode)
        {
            case CameraGameMode.MapOverview:
                UpdateMapCamera();
                break;
            case CameraGameMode.PlayerFollow:
                // PlayerController gère déjà la caméra en LateUpdate
                break;
            case CameraGameMode.CounterService:
                UpdateCounterCamera();
                break;
        }
    }

    public void SetMapMode(Vector3? focus = null)
    {
        Mode = CameraGameMode.MapOverview;
        mapFocus = focus ?? (mapCenter != null ? mapCenter.position : Vector3.zero);
        zoomTarget = null;
        if (player != null)
            player.SetInputEnabled(false);

        if (mainCamera != null)
        {
            mainCamera.orthographic = false;
            mainCamera.fieldOfView = 55f;
        }
        SnapMapCamera();
    }

    public void ZoomToRestaurant(RestaurantBuilding building)
    {
        if (building == null) return;
        Mode = CameraGameMode.MapOverview;
        zoomTarget = building.transform;
        mapFocus = building.transform.position;
        if (player != null)
            player.SetInputEnabled(false);
        SnapMapCamera();
    }

    public void SetPlayerMode()
    {
        Mode = CameraGameMode.PlayerFollow;
        zoomTarget = null;
        if (player != null)
        {
            player.SetInputEnabled(true);
            player.SnapCameraBehind();
        }
        if (mainCamera != null)
            mainCamera.fieldOfView = 60f;
    }

    public void SetCounterMode(Transform camPoint)
    {
        Mode = CameraGameMode.CounterService;
        counterCameraPoint = camPoint;
        if (player != null)
            player.SetInputEnabled(false);
    }

    private void UpdateMapCamera()
    {
        Vector3 desiredPos;
        Quaternion desiredRot;
        GetMapCameraPose(out desiredPos, out desiredRot);
        mainCamera.transform.position = Vector3.Lerp(
            mainCamera.transform.position, desiredPos, Time.deltaTime * moveSpeed);
        mainCamera.transform.rotation = Quaternion.Slerp(
            mainCamera.transform.rotation, desiredRot, Time.deltaTime * zoomSpeed);
    }

    private void SnapMapCamera()
    {
        if (mainCamera == null) return;
        Vector3 pos;
        Quaternion rot;
        GetMapCameraPose(out pos, out rot);
        mainCamera.transform.position = pos;
        mainCamera.transform.rotation = rot;
    }

    private void GetMapCameraPose(out Vector3 pos, out Quaternion rot)
    {
        float height = zoomTarget != null ? zoomHeight : mapHeight;
        Vector3 focus = zoomTarget != null ? zoomTarget.position : mapFocus;
        // Vue plongeante : remplit l'écran en portrait (évite le grand ciel bleu)
        pos = focus + new Vector3(0f, height, -height * mapLookBackFactor);
        rot = Quaternion.LookRotation((focus + Vector3.up * 0.5f) - pos, Vector3.up);
    }

    private void UpdateCounterCamera()
    {
        if (counterCameraPoint == null) return;
        mainCamera.transform.position = Vector3.Lerp(
            mainCamera.transform.position,
            counterCameraPoint.position,
            Time.deltaTime * counterBlendSpeed);
        mainCamera.transform.rotation = Quaternion.Slerp(
            mainCamera.transform.rotation,
            counterCameraPoint.rotation,
            Time.deltaTime * counterBlendSpeed);
    }

    /// <summary>Clic sur la carte (raycast sol / bâtiment).</summary>
    public bool TryClickMap(Vector3 screenPos, out RestaurantBuilding building)
    {
        building = null;
        if (Mode != CameraGameMode.MapOverview || mainCamera == null) return false;

        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        RaycastHit hit;
        bool didHit = Physics.Raycast(ray, out hit, 400f);

        if (didHit)
            building = hit.collider.GetComponentInParent<RestaurantBuilding>();

        // Fallback : rayon proche d'un kebab (collider manquant / trop petit)
        if (building == null)
            building = FindRestaurantNearRay(ray, 10f);

        if (building != null)
        {
            ZoomToRestaurant(building);
            return true;
        }

        if (didHit)
        {
            mapFocus = hit.point;
            zoomTarget = null;
        }
        return false;
    }

    private static RestaurantBuilding FindRestaurantNearRay(Ray ray, float maxDist)
    {
        var buildings = Object.FindObjectsOfType<RestaurantBuilding>();
        if (buildings == null || buildings.Length == 0) return null;

        RestaurantBuilding best = null;
        float bestDist = maxDist;
        for (int i = 0; i < buildings.Length; i++)
        {
            Vector3 p = buildings[i].transform.position + Vector3.up * 2f;
            float d = Vector3.Cross(ray.direction, p - ray.origin).magnitude;
            // Devant la caméra
            if (Vector3.Dot(ray.direction, p - ray.origin) < 0f) continue;
            if (d < bestDist)
            {
                bestDist = d;
                best = buildings[i];
            }
        }
        return best;
    }
}
