using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Sur la carte (vue dessus) : tap pour zoomer, 2e tap (ou double-tap) pour entrer.
/// </summary>
public class MapInputController : MonoBehaviour
{
    public GameCameraDirector cameraDirector;
    public float doubleTapTime = 0.7f;

    private float lastTapTime;
    private RestaurantBuilding lastTapped;

    private void Update()
    {
        if (cameraDirector == null)
            cameraDirector = GameCameraDirector.Instance;
        if (cameraDirector == null) return;
        if (cameraDirector.Mode != CameraGameMode.MapOverview) return;
        if (GameWorldManager.Instance != null && GameWorldManager.Instance.IsInsideRestaurant) return;

        if (IsPointerOverBlockingUI()) return;

        bool tap = false;
        Vector3 pos = Vector3.zero;

        if (Input.GetMouseButtonDown(0))
        {
            tap = true;
            pos = Input.mousePosition;
        }
        else if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            tap = true;
            pos = Input.GetTouch(0).position;
        }

        if (!tap) return;

        // Capturer le focus AVANT TryClickMap (qui zoome)
        RestaurantBuilding wasFocused = cameraDirector.FocusedBuilding;

        RestaurantBuilding building;
        if (!cameraDirector.TryClickMap(pos, out building) || building == null)
            return;

        float t = Time.unscaledTime;
        bool alreadyFocused = wasFocused == building;
        bool doubleTap = lastTapped == building && t - lastTapTime < doubleTapTime;

        // Déjà zoomé sur ce kebab → un tap suffit pour entrer
        if (alreadyFocused || doubleTap)
        {
            building.Enter();
            lastTapped = null;
            return;
        }

        lastTapped = building;
        lastTapTime = t;
        EmpireManager.Instance?.Notify($"{building.displayName} — retape ou ENTRER");
    }

    /// <summary>
    /// Ignore seulement l'UI qui bloque vraiment (boutons).
    /// Ne bloque pas si le doigt est sur une zone vide du canvas.
    /// </summary>
    private static bool IsPointerOverBlockingUI()
    {
        if (EventSystem.current == null) return false;

        var ped = new PointerEventData(EventSystem.current);
#if UNITY_ANDROID || UNITY_IOS
        if (Input.touchCount > 0)
            ped.position = Input.GetTouch(0).position;
        else
#endif
            ped.position = Input.mousePosition;

        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(ped, results);
        for (int i = 0; i < results.Count; i++)
        {
            var go = results[i].gameObject;
            if (go == null) continue;
            // Boutons / joysticks = bloquant. Texte prompt / panels vides = non.
            if (go.GetComponent<UnityEngine.UI.Button>() != null) return true;
            if (go.GetComponent<MobileJoystick>() != null) return true;
            if (go.GetComponent<MobileLookPad>() != null) return true;
            if (go.name != null && go.name.StartsWith("Btn")) return true;
        }
        return false;
    }
}
