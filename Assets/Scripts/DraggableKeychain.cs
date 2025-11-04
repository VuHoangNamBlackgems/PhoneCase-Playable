using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableKeychain : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    Camera cam;
    Collider surface;
    Transform caseRoot;
    float offset;
    bool alignToNormal;
    Vector3 rotOffset;
    bool dragging;

    public void Init(Camera cam, Collider surface, Transform caseRoot, float surfaceOffset, bool alignToNormal, Vector3 rotOffset)
    {
        this.cam = cam;
        this.surface = surface;
        this.caseRoot = caseRoot;
        this.offset = surfaceOffset;
        this.alignToNormal = alignToNormal;
        this.rotOffset = rotOffset;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        dragging = true;
        MoveTo(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragging) MoveTo(eventData.position);
    }

    public void OnPointerUp(PointerEventData eventData) => dragging = false;

    void MoveTo(Vector2 screenPos)
    {
        if (!cam || !surface) return;

        Ray ray = cam.ScreenPointToRay(screenPos);
        if (surface.Raycast(ray, out var hit, 100f))
        {
            // đặt sát mặt ốp + nhô ra một chút
            transform.position = hit.point + hit.normal * offset;

            if (alignToNormal)
            {
                // trục "up" của charm hướng ra khỏi mặt ốp (theo normal)
                var rot = Quaternion.LookRotation(caseRoot.forward, hit.normal) * Quaternion.Euler(rotOffset);
                transform.rotation = rot;
            }
        }
    }
}