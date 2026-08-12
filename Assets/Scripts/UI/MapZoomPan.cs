using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Zoom (+/−, pincement) et déplacement (glisser) sur la carte France/Monde.
/// </summary>
public class MapZoomPan : MonoBehaviour, IDragHandler, IScrollHandler, IBeginDragHandler
{
    public RectTransform content;
    public float minZoom = 1f;
    public float maxZoom = 3.2f;
    public float zoomStep = 0.35f;
    public float wheelSensitivity = 0.15f;

    private float zoom = 1f;
    private Vector2 pan;
    private bool dragging;
    private float lastPinchDist = -1f;

    public float Zoom => zoom;

    public void ZoomIn() => SetZoom(zoom + zoomStep, null);
    public void ZoomOut() => SetZoom(zoom - zoomStep, null);
    public void ResetView()
    {
        zoom = 1f;
        pan = Vector2.zero;
        Apply();
    }

    public void SetZoom(float value, Vector2? pivotLocal)
    {
        float prev = zoom;
        zoom = Mathf.Clamp(value, minZoom, maxZoom);
        if (Mathf.Approximately(prev, zoom)) return;

        // Zoom vers le centre du viewport si pas de pivot
        if (pivotLocal.HasValue && content != null)
        {
            Vector2 pivot = pivotLocal.Value;
            pan = pivot - (pivot - pan) * (zoom / prev);
        }
        ClampPan();
        Apply();
    }

    private void Update()
    {
        // Pincement tactile
        if (Input.touchCount == 2 && content != null)
        {
            var t0 = Input.GetTouch(0);
            var t1 = Input.GetTouch(1);
            float dist = Vector2.Distance(t0.position, t1.position);
            if (lastPinchDist > 0f)
            {
                float delta = (dist - lastPinchDist) / 300f;
                SetZoom(zoom + delta, null);
            }
            lastPinchDist = dist;
            dragging = false;
        }
        else
        {
            lastPinchDist = -1f;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (Input.touchCount > 1) return;
        dragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging || content == null) return;
        if (Input.touchCount > 1) return;
        if (zoom <= 1.01f) return;

        pan += eventData.delta;
        ClampPan();
        Apply();
    }

    public void OnScroll(PointerEventData eventData)
    {
        float delta = eventData.scrollDelta.y * wheelSensitivity;
        if (Mathf.Abs(delta) < 0.01f) return;
        SetZoom(zoom + delta, null);
    }

    private void ClampPan()
    {
        if (content == null) return;
        var parent = content.parent as RectTransform;
        if (parent == null) return;

        float maxX = parent.rect.width * 0.5f * (zoom - 1f);
        float maxY = parent.rect.height * 0.5f * (zoom - 1f);
        pan.x = Mathf.Clamp(pan.x, -maxX, maxX);
        pan.y = Mathf.Clamp(pan.y, -maxY, maxY);
    }

    private void Apply()
    {
        if (content == null) return;
        content.localScale = Vector3.one * zoom;
        content.anchoredPosition = pan;
    }
}
