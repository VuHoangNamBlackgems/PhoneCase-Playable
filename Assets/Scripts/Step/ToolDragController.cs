using UnityEngine;

public class ToolDragController : MonoBehaviour
{
   [Header("Refs")]
    [SerializeField] Camera worldCam;
    [SerializeField] MeshCollider caseCollider;     // optional (để snap khi ở trên case)
    [SerializeField] Collider toolCollider;         // optional (chỉ bắt đầu kéo khi bấm trúng tool)

    [Header("Options")]
    [SerializeField] bool requireStartOnTool = true;
    [SerializeField] float surfaceOffset = 0.002f;
    [SerializeField] float smoothTime = 0.03f;
    [SerializeField] float rayMaxDistance = 1000f;

    [Header("Drag Mode")]
    [Tooltip("Nếu bật: khi con trỏ ở trên case thì tool vẫn bám mặt (đẹp mắt) nhưng Y luôn bị khóa.")]
    [SerializeField] bool preferSnapOnCase = false;

    [Header("Y Lock")]
    [SerializeField] bool lockY = true;            // KHÓA Y
    [SerializeField] bool lockYFromStart = true;   // lấy Y tại thời điểm bắt đầu kéo
    [SerializeField] float fixedY = 0f;            // nếu không lấy từ lúc bắt đầu thì dùng giá trị này

    [Header("Bounds (optional)")]
    [SerializeField] BoxCollider movementBounds;   // nếu muốn giới hạn X/Z

    bool _dragging;
    float _lockedY;
    float _camDistAtStart;
    Vector3 _vel;

    void Update()
    {
        if (!worldCam) return;

        Vector3 pointer = (Input.touchCount > 0) ? (Vector3)Input.GetTouch(0).position : Input.mousePosition;
        bool down = Input.GetMouseButtonDown(0) ||
                    (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
        bool up   = Input.GetMouseButtonUp(0) ||
                    (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended);

        Ray r = worldCam.ScreenPointToRay(pointer);

        // Bắt đầu kéo
        if (down)
        {
            bool ok = !requireStartOnTool || (toolCollider && toolCollider.Raycast(r, out _, rayMaxDistance));
            if (ok)
            {
                _dragging = true;
                _lockedY = lockYFromStart ? transform.position.y : fixedY;
                // khoảng cách dọc hướng nhìn tới tool (fallback cho ortho / ray song song plane)
                _camDistAtStart = Vector3.Dot(transform.position - worldCam.transform.position, worldCam.transform.forward);
            }
        }

        if (up) _dragging = false;
        if (!_dragging) return;

        // Tính target
        Vector3 target = transform.position;
        bool snapped = false;

        if (preferSnapOnCase && caseCollider && caseCollider.Raycast(r, out var hit, rayMaxDistance))
        {
            target = hit.point + hit.normal * surfaceOffset;
            snapped = true;
        }

        if (!snapped)
        {
            // Kéo theo mặt phẳng ngang tại Y đã khóa
            var plane = new Plane(Vector3.up, new Vector3(0f, _lockedY, 0f));
            if (plane.Raycast(r, out float d))
            {
                target = r.GetPoint(d);
            }
            else
            {
                // fallback cho camera ortho
                var p = worldCam.ScreenToWorldPoint(new Vector3(pointer.x, pointer.y, _camDistAtStart));
                target = p;
            }
        }

        // Giới hạn X/Z nếu có bounds
        if (movementBounds)
        {
            var b = movementBounds.bounds;
            target.x = Mathf.Clamp(target.x, b.min.x, b.max.x);
            target.z = Mathf.Clamp(target.z, b.min.z, b.max.z);
        }

        if (lockY) target.y = _lockedY;       // KHÓA Y
        transform.position = Vector3.SmoothDamp(transform.position, target, ref _vel, smoothTime);
    }
}
