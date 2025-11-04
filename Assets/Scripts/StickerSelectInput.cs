// StickerSelectInput.cs
using UnityEngine;
using UnityEngine.EventSystems;

/// Bắt click/tap trên case để chọn lại sticker đã dán.
/// - Click lên sticker: chọn sticker nằm trên cùng và hiện gizmo ngay.
/// - Click vùng trống: bỏ chọn (ẩn gizmo).
/// - Bỏ qua khi click đang đè UI (nút, scroll, gizmo...).
public class StickerSelectInput : MonoBehaviour
{
    public StickerManager3D manager;

    void Update()
    {
        if (!manager) return;

        // Desktop: chuột trái
        if (Input.GetMouseButtonDown(0))
        {
            // Nếu đang click UI thì bỏ qua
            if (EventSystem.current && EventSystem.current.IsPointerOverGameObject()) return;

            manager.TrySelectByClick(Input.mousePosition); // <-- hiện gizmo ngay nếu trúng
        }

        // Mobile: 1 ngón
        if (Input.touchCount == 1)
        {
            var t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began)
            {
                if (EventSystem.current && EventSystem.current.IsPointerOverGameObject(t.fingerId)) return;
                manager.TrySelectByClick(t.position);
            }
        }
    }
}