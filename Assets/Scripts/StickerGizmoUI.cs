using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StickerGizmoUI : MonoBehaviour,
    IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI")]
    public RectTransform rect;          // tự lấy nếu để trống
    public Button btnClose;
    public RectTransform handleSR;

    [Header("Top-most")]
    [SerializeField] int sortOrder = 50000;  // rất cao

    // runtime
    StickerManager3D mgr;
    bool draggingMove, draggingHandle;
    int activePointer = int.MinValue;
    Vector2 centerScreen, startDir;
    float startDist, baseSize, baseRot;

    void Awake()
    {
        if (!rect) rect = (RectTransform)transform;
        EnsureRaycastables();
        gameObject.SetActive(false); // để sẵn, mặc định tắt
    }

    void EnsureRaycastables()
    {
        var img = rect.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>();
        img.raycastTarget = true;
        if (handleSR)
        {
            var h = handleSR.GetComponent<Image>() ?? handleSR.gameObject.AddComponent<Image>();
            h.raycastTarget = true;
        }
    }

    public void Open(StickerManager3D manager)
    {
        mgr = manager;

        // Button ❌
        btnClose.onClick.RemoveAllListeners();
        btnClose.onClick.AddListener(() =>
        {
            Debug.Log("click close");
            mgr.DeleteSelected(); Close();
        });

        // Handle events
        var trig = handleSR.GetComponent<EventTrigger>() ?? handleSR.gameObject.AddComponent<EventTrigger>();
       /* Master.AddEventTriggerListener(trig, EventTriggerType.PointerDown, OnHandleDown);
        Master.AddEventTriggerListener(trig, EventTriggerType.Drag,       OnHandleDrag);
        Master.AddEventTriggerListener(trig, EventTriggerType.PointerUp,  _ => draggingHandle = false);*/

        // reset state
        draggingMove = draggingHandle = false;
        activePointer = int.MinValue;

        rect.SetAsLastSibling();
        gameObject.SetActive(true);
    }

    public void Close()
    {
        draggingMove = draggingHandle = false;
        activePointer = int.MinValue;
        gameObject.SetActive(false);
    }

    // ===== MOVE =====
    public void OnPointerDown(PointerEventData e)
    {
        if (activePointer == int.MinValue) activePointer = e.pointerId;
    }
    public void OnBeginDrag(PointerEventData e)
    {
        if (e.pointerId != activePointer) return;
        draggingMove = true;
    }
    public void OnDrag(PointerEventData e)
    {
        if (e.pointerId != activePointer) return;
        if (draggingHandle) return;
        if (draggingMove) mgr.DragMoveSelected(e.position);
    }
    public void OnEndDrag(PointerEventData e)
    {
        if (e.pointerId != activePointer) return;
        draggingMove = false;
        activePointer = int.MinValue;
    }

    // ===== SCALE + ROTATE (Overlay => camera = null) =====
    void OnHandleDown(BaseEventData bed)
    {
        draggingHandle = true;
        var e = (PointerEventData)bed;

        Camera uiCam = null; // QUAN TRỌNG: Overlay thì dùng null
        centerScreen = RectTransformUtility.WorldToScreenPoint(uiCam, rect.position);

        startDir  = (Vector2)e.position - centerScreen;
        startDist = Mathf.Max(1e-4f, startDir.magnitude);

        baseSize = mgr.GetSelectedSize();
        baseRot  = mgr.GetSelectedRotation();

        rect.SetAsLastSibling();
    }

    void OnHandleDrag(BaseEventData bed)
    {
        if (!draggingHandle) return;
        var e = (PointerEventData)bed;

        Vector2 curDir  = (Vector2)e.position - centerScreen;
        float   curDist = Mathf.Max(1e-4f, curDir.magnitude);

        float ratio = curDist / startDist;          
        mgr.SetSelectedSize(baseSize * ratio);
        float delta = Vector2.SignedAngle(startDir, curDir); 
        mgr.SetSelectedRotation(baseRot - delta);
        Debug.Log("Delta: " + delta + ratio);
    }
}
