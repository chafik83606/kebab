using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Joystick virtuel tactile (moitié gauche de l'écran typiquement).
/// </summary>
public class MobileJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("UI")]
    public RectTransform background;
    public RectTransform handle;
    public float handleRange = 60f;

    [Header("Sortie")]
    [Range(0f, 1f)] public float deadZone = 0.08f;

    public Vector2 Value { get; private set; }

    private Canvas canvas;
    private Camera uiCam;
    private bool dragging;

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        if (background == null) background = transform as RectTransform;
        if (handle == null && transform.childCount > 0)
            handle = transform.GetChild(0) as RectTransform;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        dragging = true;
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging || background == null) return;

        uiCam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background, eventData.position, uiCam, out local);

        Vector2 clamped = Vector2.ClampMagnitude(local, handleRange);
        if (handle != null)
            handle.anchoredPosition = clamped;

        Vector2 raw = clamped / handleRange;
        Value = raw.magnitude < deadZone ? Vector2.zero : raw;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        dragging = false;
        Value = Vector2.zero;
        if (handle != null)
            handle.anchoredPosition = Vector2.zero;
    }
}
