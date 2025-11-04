using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragObject : MonoBehaviour
{
    [Header("Refs")]
    public Camera cam;                 // Để trống sẽ lấy Camera.main
    public Collider pickCollider;      // Collider để “bắt” kéo

    [Header("Options")]
    public bool requireStartOnThis = true;

    bool _dragging;
    float _screenZ;        // giữ độ sâu theo camera
    Vector3 _offsetWorld;  // offset để kéo mượt

    // EXPOSE cho script khác
    public bool IsDragging => _dragging;
    public Vector3 PointerScreenPos { get; private set; }
    public Ray CurrentRay { get; private set; }
    public bool down;
    public bool up;
    void Awake()
    {
        if (!cam) cam = Camera.main;
    }

    void Update()
    {
        PointerScreenPos = (Input.touchCount > 0)
            ? (Vector3)Input.GetTouch(0).position
            : (Vector3)Input.mousePosition;

        down = Input.GetMouseButtonDown(0) ||
               (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
        up   = Input.GetMouseButtonUp(0)   ||
                    (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended);

        CurrentRay = cam.ScreenPointToRay(PointerScreenPos);

        // BẮT ĐẦU KÉO
        if (down)
        {
            bool canStart = !requireStartOnThis || (pickCollider && pickCollider.Raycast(CurrentRay, out _, 1000f));
            if (canStart)
            {
                _dragging = true;
                _screenZ = cam.WorldToScreenPoint(transform.position).z; // khóa độ sâu theo camera
                Vector3 p = cam.ScreenToWorldPoint(new Vector3(PointerScreenPos.x, PointerScreenPos.y, _screenZ));
                _offsetWorld = transform.position - p;
            }
        }

        if (up) _dragging = false;

        // KÉO THEO CAMERA XY
        if (_dragging)
        {
            Vector3 p = cam.ScreenToWorldPoint(new Vector3(PointerScreenPos.x, PointerScreenPos.y, _screenZ)) + _offsetWorld;
            transform.position = p; // giữ khoảng cách tới camera, “đi” theo trục X,Y của màn hình
        }
    }
}
