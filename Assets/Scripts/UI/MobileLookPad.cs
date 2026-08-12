using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Zone tactile droite pour tourner la caméra (look).
/// </summary>
public class MobileLookPad : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public float sensitivity = 0.15f;
    public Vector2 LookDelta { get; private set; }

    private bool pressing;
    private Vector2 lastPos;

    public void OnPointerDown(PointerEventData eventData)
    {
        pressing = true;
        lastPos = eventData.position;
        LookDelta = Vector2.zero;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!pressing) return;
        Vector2 delta = (eventData.position - lastPos) * sensitivity;
        lastPos = eventData.position;
        LookDelta = new Vector2(delta.x, delta.y);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pressing = false;
        LookDelta = Vector2.zero;
    }

    private void LateUpdate()
    {
        // Consommé chaque frame par le PlayerController
        // On ne reset pas ici si OnDrag n'a pas été appelé : soft decay
        if (!pressing)
            LookDelta = Vector2.zero;
    }
}
