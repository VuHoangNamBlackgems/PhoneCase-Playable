using System.Collections.Generic;
using EZhex1991.EZSoftBone;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class DraggableOnSurface : MonoBehaviour
{
    [Header("Refs")]
    public Camera cam;
    public MeshCollider backSurface;

    [Header("Drag")]
    public float offset = 0.003f;
    [Range(0.01f, 0.5f)] public float smoothTime = 0.08f;
    public float maxSpeed = 100f;
    public float rayMaxDistance = 100f;

    bool dragging;
    Vector3 targetPos, vel;
    public bool Dragging => dragging;

    void Awake()
    {
        if (!cam) cam = Camera.main;
        Input.simulateMouseWithTouches = true;
        targetPos = transform.position;
    }

    void Start()
    {
        MoveIntoCaseAtBackSurfaceCenter();
    }

    void OnDisable() => dragging = false;

    void OnMouseDown()
    {
        if (IsPointerOverUI()) return;
        dragging = true;
        TryUpdate(Input.mousePosition, snap: true);
    }

    void OnMouseUp() => dragging = false;

    void Update()
    {
        if (dragging)
        {
            if (IsPointerOverUI()) return;
            TryUpdate(Input.mousePosition, snap: false);
            transform.position = Vector3.SmoothDamp(
                transform.position, targetPos, ref vel, smoothTime, maxSpeed, Time.deltaTime);
        }
    }

    bool TryUpdate(Vector2 screenPos, bool snap)
    {
        if (!cam || !backSurface) return false;

        Ray ray = cam.ScreenPointToRay(screenPos);
        if (backSurface.Raycast(ray, out var hit, rayMaxDistance))
        {
            targetPos = hit.point + hit.normal * offset;
            if (snap)
            {
                transform.position = targetPos;
                vel = Vector3.zero;
            }
            return true;
        }
        return false;
    }
    
    public bool MoveIntoCaseAtBackSurfaceCenter(bool snapNow = true)
    {
        if (!backSurface) return false;

        Vector3 worldCenter = backSurface.bounds.center;

        Ray r = cam ? new Ray(cam.transform.position, (worldCenter - cam.transform.position).normalized)
            : new Ray(transform.position + (worldCenter - transform.position).normalized * -0.5f,
                (worldCenter - transform.position).normalized);

        if (backSurface.Raycast(r, out var hit, rayMaxDistance))
        {
            targetPos = hit.point + hit.normal * offset;
            if (snapNow) { transform.position = targetPos; vel = Vector3.zero; }
            return true;
        }

        Vector3 cp = backSurface.ClosestPoint(worldCenter);
        Vector3 approxNormal = (cp - worldCenter).sqrMagnitude > 1e-6f ? (cp - worldCenter).normalized
            : Vector3.up;
        targetPos = cp + approxNormal * offset;
        if (snapNow) { transform.position = targetPos; vel = Vector3.zero; }
        return true;
    }


    bool IsPointerOverUI()
    {
        if (!EventSystem.current) return false;
        if (Input.touchCount > 0)
            return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
        return EventSystem.current.IsPointerOverGameObject();
    }

    public void Disable()
    {
        var col = GetComponent<Collider>();
        if (col) col.enabled = false;
        dragging = false;
    }
}
