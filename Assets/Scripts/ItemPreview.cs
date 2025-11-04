using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Events;

/// Ô sticker trong ScrollRect:
/// - Kéo trong list -> ScrollRect cuộn như bình thường.
/// - Item LOCK: Tap -> gọi xem reward để unlock, KHÔNG spawn ghost.
/// - Item UNLOCK: Giữ 1 nhịp hoặc kéo ra khỏi vùng ScrollRect -> tạo ghost để thả lên case.
public class ItemPreview : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler,
    IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler,
    IScrollHandler
{
    [Header("Tham chiếu")] public Sprite sprite;
    public Image imgIcon;
    public ScrollRect scrollRect;
    public Canvas canvas;
    [SerializeField] bool isUnlock = false;
    [SerializeField] Image imgReward; // overlay lock/reward badge
    public StickerManager3D manager;

    [Header("Pickup Rules")] [Tooltip("Giữ bao lâu để cho phép nhặt khỏi list (giây)")] [SerializeField]
    float longPressSeconds = 0.12f;

    [Tooltip("Cho phép nhặt khi kéo ra ngoài Rect của ScrollRect")] [SerializeField]
    bool pickupWhenExitScrollRect = true;

    [Tooltip("Giới hạn scale ghost")] [SerializeField]
    Vector2 ghostScaleRange = new Vector2(0.3f, 6f);

    private ItemDataSO item;
    public ItemDataSO Item => item;

    RectTransform _selfRt;
    RectTransform _scrollRt;
    RectTransform _canvasRt;

    RectTransform ghost;
    Image ghostImg;
    float ghostScale = 1f;

    bool _pressed;
    bool _scrolling;
    bool _draggingGhost;
    Vector2 _downPos;
    float _downTime;
    float _dragThresholdPx;

    // Ngưỡng để coi là "tap ngắn" (không kéo)
    const float SHORT_TAP_TIME = 0.25f;

    public bool IsUnlock
    {
        get => isUnlock;
        set => isUnlock = value;
    }

    void Awake()
    {
        _selfRt = (RectTransform)transform;
        if (!imgIcon) imgIcon = GetComponent<Image>();
        if (!canvas) canvas = GetComponentInParent<Canvas>();
        _canvasRt = canvas ? (RectTransform)canvas.transform : null;
        _scrollRt = scrollRect ? (RectTransform)scrollRect.transform : null;

        float scale = canvas ? canvas.scaleFactor : 1f;
        _dragThresholdPx = (EventSystem.current ? EventSystem.current.pixelDragThreshold : 10f) * scale;

        // Cho phép bấm thẳng vào icon reward để mở khóa
        if (imgReward) imgReward.raycastTarget = true;
    }

    void OnEnable()
    {
        RefreshLockUI();
    }

    public void SetUp(ItemDataSO item, Canvas canvas, StickerManager3D manager)
    {
        scrollRect = manager.scrollRect;
        this.item = item;
        this.sprite = item._iconPreview;
        if (imgIcon) imgIcon.sprite = this.sprite;
        this.manager = manager;
        if (canvas)
        {
            this.canvas = canvas;
            _canvasRt = (RectTransform)canvas.transform;
        }

        RefreshLockUI();
    }

    public void SetUpUnlock(bool isUnlock)
    {
        IsUnlock = isUnlock;
        RefreshLockUI();
    }

    void RefreshLockUI()
    {
        if (imgReward) imgReward.gameObject.SetActive(!IsUnlock);
    }

    public void OnPointerDown(PointerEventData e)
    {
        _pressed = true;
        _downPos = e.position;
        _downTime = Time.unscaledTime;
        _scrolling = false;
        _draggingGhost = false;
    }

    public void OnPointerUp(PointerEventData e)
    {
        // Nếu đang LOCK và đây là một "tap ngắn" (không kéo) -> mở khóa
        if (!IsUnlock && !_scrolling && !_draggingGhost)
        {
            float held = Time.unscaledTime - _downTime;
            bool moved = Vector2.Distance(e.position, _downPos) >= _dragThresholdPx;
            if (held <= SHORT_TAP_TIME && !moved)
                PromptUnlock();
        }

        _pressed = false;
    }

    public void OnInitializePotentialDrag(PointerEventData e)
    {
        if (scrollRect) scrollRect.OnInitializePotentialDrag(e);
    }

    public void OnBeginDrag(PointerEventData e)
    {
        // Bắt đầu bằng cuộn list
        _scrolling = true;
        if (scrollRect) scrollRect.OnBeginDrag(e);
    }

    public void OnDrag(PointerEventData e)
    {
        if (_draggingGhost)
        {
            if (ghost) ghost.position = e.position;
            return;
        }

        // Đang cuộn list
        if (_scrolling)
        {
            if (scrollRect) scrollRect.OnDrag(e);

            // Item đang LOCK -> chỉ cho cuộn, KHÔNG nhặt ghost.
            if (!IsUnlock) return;

            bool longPressed = (Time.unscaledTime - _downTime) >= longPressSeconds;
            bool movedEnough = Vector2.Distance(e.position, _downPos) >= _dragThresholdPx;
            bool outOfScroll = pickupWhenExitScrollRect && _scrollRt &&
                               !RectTransformUtility.RectangleContainsScreenPoint(_scrollRt, e.position,
                                   canvas ? canvas.worldCamera : null);

            // Điều kiện để chuyển từ cuộn -> nhặt ghost
            if ((longPressed && movedEnough) || outOfScroll)
            {
                StartGhost(e);
                if (scrollRect) scrollRect.OnEndDrag(e);
                _scrolling = false;
                _draggingGhost = true;
            }
        }
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (_scrolling)
        {
            if (scrollRect) scrollRect.OnEndDrag(e);
            _scrolling = false;
            return;
        }

        if (_draggingGhost)
        {
            TryDrop(e);
            DestroyGhost();
            _draggingGhost = false;
        }
    }

    public void OnScroll(PointerEventData e)
    {
        if (_draggingGhost)
        {
            ghostScale = Mathf.Clamp(ghostScale + e.scrollDelta.y * 0.1f, ghostScaleRange.x, ghostScaleRange.y);
            if (ghost) ghost.localScale = Vector3.one * ghostScale;
        }
        else
        {
            if (scrollRect) scrollRect.OnScroll(e);
        }
    }

    void StartGhost(PointerEventData e)
    {
        if (sprite == null || manager == null || canvas == null) return;

        var go = new GameObject("Ghost_" + sprite.name,
            typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        ghost = (RectTransform)go.transform;
        ghost.SetParent(canvas.transform, false);

        ghostImg = go.GetComponent<Image>();
        ghostImg.sprite = sprite;
        ghostImg.preserveAspect = true;
        ghostImg.raycastTarget = false;
        go.GetComponent<CanvasGroup>().blocksRaycasts = false;

        ghost.sizeDelta = _selfRt.rect.size;
        ghost.position = Input.mousePosition;
        ghost.localScale = Vector3.one;
        ghostScale = 1f;
    }

    void TryDrop(PointerEventData e)
    {
        if (!ghost) return;
        if (!IsUnlock) return; // an toàn

        // Tính bề rộng ghost theo pixel -> ra bề rộng UV theo chiều rộng RT sticker
        float pixelWidth = RectTransformUtility.PixelAdjustRect(ghost, canvas).width;
        float uvWidth = Mathf.Clamp01(pixelWidth / Mathf.Max(1, manager.stickerRT.width));

        if (manager.RaycastUV(e.position, out var uv, out var hit))
        {
            // Add sticker -> Manager sẽ tự redraw + spawn gizmo
            manager.AddSticker(item.id, sprite.texture, uv, hit.point, uvWidth, pixelWidth, 0f);
        }
    }

    void DestroyGhost()
    {
        if (ghost) Destroy(ghost.gameObject);
        ghost = null;
        ghostImg = null;
    }

    // ====== UNLOCK ======
    void PromptUnlock()
    {
        OnUnlockResult();
    }

    void OnUnlockResult()
    {
        if (!isUnlock)
        {
           /* CallAdsManager.ShowRewardVideo("reward", () =>
            {
                UserTracking.ItemPick("Sticker2D", item.id);
                isUnlock = true;
                SetUpUnlock(true);
                UserGameData.UnlockItemSticker2D(item.id);
            });*/
            return;
        }

        SetUpUnlock(true);
        UserGameData.UnlockItemSticker2D(item.id);
    }
}