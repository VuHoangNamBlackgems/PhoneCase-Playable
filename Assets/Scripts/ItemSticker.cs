using UnityEngine;

public class ItemSticker : MonoBehaviour
{
    public Sprite Sprite { get; private set; }
    public float SizeUV { get; private set; } = 0.25f; // mặc định
    public float RotationRad { get; private set; } = 0f;
    public bool IsOnHandle { get; private set; }

    RectTransform _rect; System.Action<ItemSticker> _onRemove;

    public void Init(Sprite sp, System.Action<ItemSticker> onRemove){
        Sprite = sp; _rect = (RectTransform)transform; _onRemove = onRemove;
        // TODO: add Image, Button X, Handle… và gán callback thay đổi SizeUV/RotationRad
    }
    public void MoveToScreen(Vector2 screenPos, Camera uiCam){
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)_rect.parent, screenPos, uiCam, out var local);
        _rect.anchoredPosition = local;
    }
    public void ShowAsBakedOutline(){ /* hiệu ứng */ }
}